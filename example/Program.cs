using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EEUniverse.Library;
//using EEUniverse.LoginExtensions;
using System.IO;
using System.Threading;
using System.Timers;
using Timer = System.Timers.Timer;
using EEUData;
using EEWData;
using WorldData = EEWData.WorldData;
using PlayerData = EEWData.PlayerData;
//using BlockId = EEWData.BlockId;

namespace example
{
    class Program
    {
        static EEWClient cli;
        static EEWConnection con;
        static WorldData world;
        static PlayerData players;
        static void Main(string[] args)
        {
            cli = new EEWClient();
            world = new EEWData.WorldData();
            players = new EEWData.PlayerData();
            con = cli.CreateWorldConnection("620055035978451");
            con.OnMessage += OnMessage;
            con.Send(MessageType.Init, 0);
            new System.Threading.Timer((_) => con.Send(MessageType.Ping), null, 5000, 5000);

            mre.Wait();

            int id = 0;
            int[] ids;
            List<int> idsl = new List<int>();
            idsl.AddRange((Enum.GetValues(typeof(CustomBlockId)) as CustomBlockId[]).Select(x => (int)x));
            idsl.AddRange((Enum.GetValues(typeof(BlockId)) as BlockId[]).Select(x => (int)x));
            idsl = idsl.Distinct().ToList();
            //ids = new int[idsl.Count];
            ids = idsl.ToArray();
            //idsl.Select(x => ids[id++] = x);
            Console.WriteLine(ids.Length);
            ;

            Console.ReadLine();
            bool a = false;
            while (cli.Socket.ReadyState == WebSocketSharp.WebSocketState.Open)
            {
                for (int i = 0; i < 10; i++)
                {
                    for (int y = 0; y < world.Height; y++)
                        for (int x = 0; x < world.Width; x++)
                        {
                            id++;
                            if (id == (int)BlockId.CoinGold) id++;
                            if (id >= ids.Length) { if (ids.Length % 2 == 0) { a = !a; } id = !a ? 0 : 1; }
                            con.SendAsync(MessageType.PlaceBlock, 0, x, y, ids[id]);
                            id++;
                            if (id == (int)BlockId.CoinGold) id++;
                            if (id >= ids.Length) { if (ids.Length % 2 == 0) { a = !a; } id = !a ? 0 : 1; }
                            con.SendAsync(MessageType.PlaceBlock, 1, x, y, ids[id]);
                        }
                }
                Thread.Sleep(1);
            }
            //Thread.Sleep(-1);
        }
        static ManualResetEventSlim mre = new ManualResetEventSlim();
        static int botid = -1;
        //static bool ass = false;
        //static int test = 100;
        static int replacemode = 0;
        static int fillmode = 0;
        static int x1;
        static int y1;
        static int x2;
        static int y2;
        static int fromb;
        private static void OnMessage(object ws, Message m)
        {
            switch (m.Type)
            {
                case MessageType.Init:
                    Console.WriteLine(nameof(MessageType.Init));
                    mre.Set();
                    break;
                case MessageType.Ping:
                    Console.WriteLine(nameof(MessageType.Ping));
                    break;
                case MessageType.Pong:
                    Console.WriteLine(nameof(MessageType.Pong));
                    break;
                case MessageType.Chat:
                    Console.WriteLine(nameof(MessageType.Chat));
                    break;
                default:
                    //Console.WriteLine(m.Type);
                    break;
            }
            players.Parse(m);
            world.Parse(m);
            //Console.WriteLine(m.ToString());
            void giveEditAndGod(string username)
            {
                con.Chat("/giveedit " + username);
                con.Chat("/givegod " + username);
            }
            switch (m.Type)
            {
                case MessageType.Init:
                    botid = m.GetInt(0);
                    //con.Send(MessageType.PlaceBlock, 1, 5, 5, (int)BlockId.Empty);

                    //players.Add(m.GetInt(0), new Player(m.GetInt(0), m.GetString(1), true));
                    //con.SendAsync(MessageType.Chat, "/killroom");
                    //connection.SendAsync(MessageType.Chat, $"[BOT] Connected!");
                    break;
                case MessageType.PlayerAdd://existing players
                    giveEditAndGod(m.GetString(1));
                    break;
                case MessageType.PlayerJoin://new joining players
                    giveEditAndGod(m.GetString(1));
                    break;
                case MessageType.PlayerExit:
                    //players.Remove(m.GetInt(0));
                    break;
                case MessageType.Chat:
                    {
                        const string PREFIXES = ".,!";
                        var id = m.GetInt(0);
                        var player = players.Players[id];
                        var username = player.Username;

                        //if (m.GetString(1).StartsWith("!ping"))
                        //{
                        //    //connection.SendAsync(MessageType.Chat, $"[BOT @{player.Username}] Pong!");
                        //}

                        string message = m.GetString(1);
                        string[] args = message.Split(' ');
                        char prefix;
                        if (args.Length > 0 && args[0].Length > 0 && PREFIXES.Contains(args[0][0])) { prefix = args[0][0]; args[0] = args[0].Substring(1); }
                        else break;

                        switch (args[0])
                        {
                            case "g":
                                con.SetEdit(username, true);
                                con.SetGod(username, true);
                                break;
                            case "r":
                            case "replace":
                                if (prefix == ',') con.Chat($"/reset {username}");
                                else
                                {
                                    if (player.Username != "kubapolish") break;
                                    replacemode = 1;
                                }
                                break;
                            case "f":
                            case "fill":
                                if (player.Username != "kubapolish") break;
                                fillmode = 1;
                                break;
                            case "help":
                                con.Chat("available command: count [id/blockname]");
                                break;
                            case "count":
                                {
                                    if (args.Length < 2) break;
                                    var str = args[1];
                                    BlockId bid = 0;
                                    if (!int.TryParse(str, out int result))
                                        if (!Enum.TryParse(str, true, out bid)) break;
                                    if (bid != 0) result = (int)bid;
                                    int count = 0;
                                    foreach (var item in world.Blocks)
                                        if ((int)item.Id == result) count++;
                                    con.Send(MessageType.Chat, $"There are {count} blocks of id {result}.");
                                }
                                break;
                            //case "edges":
                            //    {
                            //        for (int i = 0; i < world.Width; i++)
                            //        {
                            //            con.PlaceBlock(1, i, 0, BlockId.Empty);
                            //            con.PlaceBlock(1, i, world.Height - 1, BlockId.Empty);
                            //        }
                            //        for (int i = 1; i < world.Height - 1; i++)
                            //        {
                            //            con.PlaceBlock(1, 0, i, BlockId.Empty);
                            //            con.PlaceBlock(1, world.Width - 1, i, BlockId.Empty);
                            //        }
                            //        break;
                            //    }
                            default:
                                break;
                        }

                        break;
                    }
                case MessageType.PlaceBlock:
                    //Console.WriteLine(m);
                    //case MessageType.PlayerMove:
                    //Console.WriteLine(m.ToString());
                    /*Scope = World, Id = PlaceBlock,
                    [0] = 2 (Int32)//playerid
                    [1] = 1 (Int32)//layer
                    [2] = 47 (Int32)//x
                    [3] = 168 (Int32)//y
                    [4] = 5 (Int32)*///id
                    if (m.GetInt(0) == botid) break;
                    if (players[m.GetInt(0)].Username != "kubapolish") break;

                    int l = m.GetInt(1);
                    int x = m.GetInt(2);
                    int y = m.GetInt(3);
                    int b = m.GetInt(4);

                    if (replacemode == 2)
                    {
                        replacemode = 0;
                        x2 = x;
                        y2 = y;

                        int temp;
                        if (x1 > x2)
                        {
                            temp = x1;
                            x1 = x2;
                            x2 = temp;
                        }
                        if (y1 > y2)
                        {
                            temp = y1;
                            y1 = y2;
                            y2 = temp;
                        }
                        for (int yy = 0; yy <= y2 - y1; yy++)
                            for (int xx = 0; xx <= x2 - x1; xx++)
                            {
                                int tx = xx + x1, ty = yy + y1;
                                if (((int)world[l, tx, ty].Id) == fromb) con.PlaceBlock(l, tx, ty, world[l, x, y]);
                            }
                    }
                    if (replacemode == 1)
                    {
                        replacemode++;
                        x1 = x;
                        y1 = y;
                        fromb = b;
                    }

                    if (fillmode == 2)
                    {
                        fillmode = 0;
                        x2 = x;
                        y2 = y;

                        int temp;
                        if (x1 > x2)
                        {
                            temp = x1;
                            x1 = x2;
                            x2 = temp;
                        }
                        if (y1 > y2)
                        {
                            temp = y1;
                            y1 = y2;
                            y2 = temp;
                        }
                        for (int yy = 0; yy <= y2 - y1; yy++)
                            for (int xx = 0; xx <= x2 - x1; xx++)
                            {
                                con.PlaceBlock(l, xx + x1, yy + y1, world[l, x, y]);
                            }
                    }
                    if (fillmode == 1)
                    {
                        fillmode++;
                        x1 = x;
                        y1 = y;
                    }
                    //con.Send(MessageType.PlaceBlock, m.GetInt(1), m.GetInt(2), m.GetInt(3), (int)BlockId.ArenaWallPurple);
                    //con.Send(MessageType.Chat, "/takeedit kubapolish");
                    break;
            }
        }
    }
}
