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
    static class ConnectionExtensions
    {
        public const string CHATPREFIX = "[Bot] ";
        public static void PlaceBlock(this IConnection con, int layer, int x, int y, BlockId id, params object[] args) => SendL(con, MessageType.PlaceBlock, new object[] { layer, x, y, (int)id }.Concat(args).ToArray());
        public static void PlaceBlock(this IConnection con, int layer, int x, int y, int id, params object[] args) => SendL(con, MessageType.PlaceBlock, new object[] { layer, x, y, id }.Concat(args).ToArray());
        public static void PlaceBlock(this IConnection con, int l, int x, int y, Block block)
        {
            object[] args;
            switch (block)
            {
                case Effect b:
                    args = new object[] { b.Amount };
                    break;
                case Sign b:
                    args = new object[] { b.Text, b.Morph };
                    break;
                case Portal b:
                    args = new object[] { b.Rotation, b.ThisId, b.TargetId, b.Flipped };
                    break;
                case Block b:
                    args = new object[0];
                    break;
                default:
                    throw new InvalidOperationException();
            }
            PlaceBlock(con, l, x, y, (BlockId)block.Id, args);
        }
        public static void Chat(this IConnection con, string message) => SendL(con, MessageType.Chat, message);
        public static void ChatRespond(this IConnection con, string username, string message, string prefix = CHATPREFIX) => ChatPrefix(con, $"@{username}: {message}");
        public static void ChatPrefix(this IConnection con, string message, string prefix = CHATPREFIX) => Chat(con, prefix + message);
        public static void ChatDM(this IConnection con, string username, string message) => Chat(con, $"/pm {username} {message}");
        public static void ChatDMPrefix(this IConnection con, string username, string message, string prefix = CHATPREFIX) => ChatDM(con, username, prefix + message);
        public static void SetGod(this IConnection con, string username, bool hasgod) => SendL(con, MessageType.Chat, $"/god {username} {hasgod}");
        public static void SetEdit(this IConnection con, string username, bool hasedit) => SendL(con, MessageType.Chat, $"/edit {username} {hasedit}");
        public static void Save(this IConnection con) => SendL(con, MessageType.Chat, "/save");

        private static readonly object _sendLock = new object();
        public static void SendL(this IConnection con, MessageType type, params object[] args) { lock (_sendLock) con.Send(type, args); }
    }
    //}
    /*
    int index = 0;
    for (var y = 0; y < world.Height; y++)
    {
        for (var x = 0; x < world.Width; x++)
        {
            int value = 0;
            if (m[index++] is int iValue)
                value = iValue;

            var backgroundId = value >> 16;
            var foregroundId = 65535 & value;

            world.Blocks[0, x, y] = new Block(backgroundId);
            switch (foregroundId)
            {
                case (int)BlockId.SignWood:
                case (int)BlockId.SignRed:
                case (int)BlockId.SignGreen:
                case (int)BlockId.SignBlue:
                    {
                        string text = m.GetString(index++);
                        int morph = m.GetInt(index++);
                        world.Blocks[1, x, y] = new Sign(foregroundId, text, morph);
                        break;
                    }

                case (int)BlockId.Portal:
                    {
                        int rotation = m.GetInt(index++);
                        int p_id = m.GetInt(index++);
                        int t_id = m.GetInt(index++);
                        bool flip = m.GetBool(index++);
                        world.Blocks[1, x, y] = new Portal(foregroundId, p_id, t_id, rotation, flip);
                        break;
                    }

                case (int)BlockId.EffectClear:
                case (int)BlockId.EffectMultiJump:
                case (int)BlockId.EffectHighJump:
                    {
                        int r = (foregroundId == (int)BlockId.EffectClear) ? 0 : m.GetInt(index++);
                        world.Blocks[1, x, y] = new Effect(foregroundId, r);
                        break;
                    }

                default: world.Blocks[1, x, y] = new Block(foregroundId); break;
            }
        }
    }
    */
    //}
    class Program
    {
        //static EEWClient client;
        static Client cli;
        static Connection con;
        static WorldData world;
        static PlayerData players;
        static void Main(string[] args)
        {
            string GetToken(bool forcenew)
            {
                const string TOKENPATH = "token.txt";
                bool exists = File.Exists(TOKENPATH);
                if (forcenew || !exists)
                {
                    if (!exists) File.WriteAllText(TOKENPATH, "");
                    Console.WriteLine("Please update your token.txt.");
                    //Environment.Exit(-7);
                    System.Diagnostics.Process.Start(Environment.OSVersion.Platform == PlatformID.Win32NT ? "notepad.exe" : "nano", TOKENPATH).WaitForExit();
                }
                return File.ReadAllText(TOKENPATH);
            }
            void tryConnect(bool shid)
            {
                cli = new EEWClient(GetToken(shid));
                cli.Connect();
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
            con = (Connection)cli.CreateWorldConnection("id");
            con.OnMessage += OnMessage;
            con.Send(MessageType.Init, 0);
            new System.Threading.Timer((_) => con.Send(MessageType.Ping), null, 5000, 5000);

            mre.Wait();

            int id = 0;
            int[] ids;
            List<int> idsl = new List<int>();
            idsl.AddRange((Enum.GetValues(typeof(BlockId)) as BlockId[]).Select(x => (int)x));
            idsl.AddRange((Enum.GetValues(typeof(CustomBlockId)) as CustomBlockId[]).Select(x => (int)x));
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
