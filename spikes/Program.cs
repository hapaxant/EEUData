using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EEUniverse.Library;
using EEUData;
using EEWData;
using System.IO;
using System.Diagnostics;
using System.Threading;

namespace spikes
{
    class Program
    {
        static EEWClient cli;
        static Connection con;
        static object _lock = new object();
        static void Restart() { lock (_lock) { /*Process.Start(System.Reflection.Assembly.GetExecutingAssembly().Location);*/ Environment.Exit(-1); } }
        static string GetToken(bool forcenew)
        {
            const string TOKENPATH = "token.txt";
            bool exists = File.Exists(TOKENPATH);
            if (forcenew || !exists)
            {
                if (!exists) File.WriteAllText(TOKENPATH, "");
                Console.WriteLine("Please update your token.txt.");
                //Environment.Exit(-7);
                Process.Start(Environment.OSVersion.Platform == PlatformID.Win32NT ? "notepad.exe" : "nano", TOKENPATH).WaitForExit();
            }
            return File.ReadAllText(TOKENPATH);
        }
        static void Main(string[] args)
        {
            void tryConnect(bool shid)
            {
                cli = new EEWClient(GetToken(shid));
                cli.Connect();
                cli.Socket.OnClose += (o, e) => { Console.WriteLine(e); Restart(); };
                cli.Socket.OnError += delegate { Task.Delay(500).Wait(); Restart(); };
            }
            try
            {
                tryConnect(false);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                cli?.Dispose();
                tryConnect(true);
            }
            world = new EEWData.WorldData();
            players = new EEWData.PlayerData();
            con = (Connection)cli.CreateWorldConnection("599542530172426");
            con.OnMessage += OnMessage;
            con.Send(MessageType.Init, 0);
            new Timer((_) => con.Send(MessageType.Ping), null, 5000, 5000);
            Thread.Sleep(-1);
        }

        static EEWData.WorldData world;
        static EEWData.PlayerData players;
        private static void OnMessage(object sender, Message m)
        {
            world.Parse(m);
            players.Parse(m);

            switch ((int)m.Type)
            {
                case (int)MessageType.Init:
                    Console.WriteLine("init");
                    break;
                case (int)MessageType.PlayerAdd:
                case (int)MessageType.PlayerJoin:
                    {
                        int id = m.GetInt(0);
                        myplayers.Add(id, new MyPlayer() { player = players[id] });
                        break;
                    }
                case (int)MessageType.PlayerExit:
                    {
                        int id = m.GetInt(0);
                        myplayers.Remove(id);
                        break;
                    }
                case (int)CustomMessageType.CoinCollected:
                    {
                        int pid = m.GetInt(0), n = m.GetInt(1), x = m.GetInt(2), y = m.GetInt(3);
                        switch (world[0, x, y].Id)
                        {
                            case (int)CustomBlockId.HazardsSawBlade:
                                var username = players[pid].Username;
                                var myp = myplayers[pid];
                                var p = myp.checkpoint;
                                myp.deathCount++;
                                con.Send(MessageType.Chat, "/reset " + username);
                                if (p != null) con.Send(MessageType.Chat, $"/tp {username} {p.X} {p.Y}");
                                break;
                            case (int)BlockId.EffectClear:
                                myplayers[pid].checkpoint = new Point(x, y);
                                break;
                        }
                        break;
                    }
                case (int)MessageType.Chat:
                    {
                        int pid = m.GetInt(0);
                        var username = players[pid].Username;
                        if (m.GetString(1) == ".deaths")
                            con.Send(MessageType.Chat, $"@{username}: {myplayers[pid].deathCount}");
                    }
                    break;
                case (int)MessageType.PlaceBlock:
                    {
                        int pid = m.GetInt(0), l = m.GetInt(1), x = m.GetInt(2), y = m.GetInt(3), id = m.GetInt(4);
                        if (id == (int)BlockId.EffectClear && l == 0)
                            foreach (var item in myplayers.Values)
                            {
                                var c = item.checkpoint;
                                if (c != null)
                                    if (c.X == x && c.Y == y) item.checkpoint = null;
                            }
                        if (id == 0 && l == 1)
                        {
                            if (world[0, x, y].Id == (int)CustomBlockId.HazardsSawBlade || world[0, x, y].Id == (int)BlockId.EffectClear)
                                con.Send(MessageType.PlaceBlock, 0, x, y, 0);
                        }
                        else if (l == 0)
                        {
                            if (id == (int)CustomBlockId.HazardsSawBlade || id == (int)BlockId.EffectClear)
                                con.Send(MessageType.PlaceBlock, 1, x, y, (int)BlockId.CoinGold);
                        }
                        else if (id == (int)CustomBlockId.HazardsSawBlade && l == 1)
                        {
                            con.Send(MessageType.PlaceBlock, 0, x, y, (int)CustomBlockId.HazardsSawBlade);
                            con.Send(MessageType.PlaceBlock, 1, x, y, (int)BlockId.CoinGold);
                        }
                    }
                    break;
                case (int)MessageType.Won:
                    con.Send(MessageType.Chat, "/giveedit " + players[m.GetInt(0)].Username);
                    break;
            }
        }
        static Dictionary<int, MyPlayer> myplayers = new Dictionary<int, MyPlayer>();
        class Point
        {
            public Point(int x, int y)
            {
                this.X = x;
                this.Y = y;
            }
            public int X { get; set; }
            public int Y { get; set; }
        }
        class MyPlayer
        {
            public Player player;
            public Point checkpoint;
            public int deathCount;
        }
    }
}
