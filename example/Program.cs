using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using EEUniverse.Library;
using EEUData;

namespace example
{
    class Program
    {
        static string GetClipboardText()
        {
            string ret = null;
            var t = new Thread(new ThreadStart(() => ret = System.Windows.Forms.Clipboard.GetText()));
            System.Diagnostics.Contracts.Contract.Assert(t.TrySetApartmentState(ApartmentState.STA));
            t.Start();
            t.Join();
            return ret;
        }

        static EEUClient cli;
        static EEUConnection con;
        static RoomData data;
        static void Main(string[] args)
        {
            cli = new EEUClient(GetClipboardText());//get eeu token from clipboard
            cli.Connect();//don't forget to connect

            Console.WriteLine(cli.SelfInfo.ToString());//print info about self

            var lobby = cli.CreateLobbyConnection();//get lobby
            Console.WriteLine(lobby.LoadStats().ToString());//print lobby stats
            foreach (var room in lobby.LoadRooms())
            {//print every room in lobby
                Console.WriteLine(room.ToString());
            }

            data = new RoomData();
            con = cli.CreateWorldConnection("worldid");//connect to world
            con.OnMessage += OnMessage;
            con.ChatPrefix = "[ExampleBot] ";//change prefix for commands like Chat
            con.Init();

            List<BlockId> blockids = (Enum.GetValues(typeof(BlockId)) as BlockId[]).ToList();//get all BlockId values and store them in a list
            int id = 0;

            while (cli.Connected)
            {//fill world with incrementing ids
                Console.ReadLine();//wait for enter key
                for (int i = 0; i < 3; i++)
                {
                    for (int y = 0; y < data.Height; y++)
                        for (int x = 0; x < data.Width; x++)
                        {//loop over world
                            var bid = blockids[id++];//get next block in list
                            if (id >= blockids.Count) id = 0;//loop over
                            var layer = bid.IsBackground() ? 0 : 1;//if block is bg, place it in bg layer
                            if (data.Blocks[layer, x, y].Id != (ushort)bid) con.PlaceBlock(layer, x, y, bid);//place block
                        }//for x,y
                    Thread.Sleep(1);//small delay
                }//for

                FillWorld(0, 1);//clear foreground layer
            }//while
            Console.WriteLine("not connected");
            Console.ReadLine();
        }

        private static void FillWorld(int id, int layer)
        {
            for (int y = 0; y < data.Height; y++)
                for (int x = 0; x < data.Width; x++)
                {
                    if (data.Blocks[layer, x, y].Id == id) continue;//skip placing if the block is already there
                    con.PlaceBlock(layer, x, y, id);
                }
        }

        private static bool TryGetBlockIdFromString(string str, out int result)
        {
            if (int.TryParse(str, out result)) return true;//try to parse id,
            else if (Enum.TryParse(str, true, out BlockId bid))//then try to parse block name,
            {
                result = (int)bid;//cast the enum to int
                return true;
            }
            else return false;//otherwise return false
        }

        private static void OnMessage(object ws, Message m)
        {
            //handle PlayerExit before data.Parse; we want to get the player object before it gets removed
            switch (m.Type)
            {
                case MessageType.PlayerExit:
                    {
                        con.ChatRespond(data.Players[m.GetInt(0)].Username, "goodbye!");
                        break;
                    }
            }
            data.Parse(m);//parse messages
            //we can now do whatever
            switch (m.Type)
            {
                case MessageType.Init:
                    con.Chat("connected!");
                    pingTimer.Change(0, 5000);//start measuring ping
                    break;
                case MessageType.PlayerJoin:
                    {
                        string username = m.GetString(1);
                        con.ChatRespond(username, "hello!");
                        foreach (var item in globalEffects)
                        {//give player effects, if any
                            con.GiveEffect(username, item.Key, item.Value);
                        }
                        break;
                    }
                case MessageType.Chat:
                    {
                        const string PREFIXES = ".,!";//chat prefixes, we can access the string like an array later
                        var id = m.GetInt(0);
                        var player = data.Players[id];
                        var username = player.Username;
                        string message = m.GetString(1);
                        string[] args = message.Split(' ');//message split into array of words
                        char prefix;//this will keep the prefix used
                        if (PREFIXES.Contains(args[0][0]))//if first character of first word of message is a prefix
                        {
                            prefix = args[0][0];//note prefix used
                            args[0] = args[0].Substring(1);//remove first character
                        }
                        else break;//not a command so break

                        switch (args[0])
                        {//handle commands
                            case "help":
                                con.ChatRespond(username, $"available commands: gibedit, count <id/blockname>, edges [id/blockname]. prefixes are {PREFIXES}");
                                break;
                            case "gibedit":
                                con.GiveEdit(username);
                                break;
                            case "count":
                                {//count blocks in world
                                    if (args.Length < 2) break;//break if argument is not specified

                                    if (!TryGetBlockIdFromString(args[1], out int result))
                                    {
                                        con.ChatRespond(username, "invalid block id!");
                                        break;
                                    }
                                    int count = 0;
                                    foreach (var block in data.Blocks)//for every block in blocks,
                                        if (block.Id == result) count++;//if id is what we are looking for, increment count
                                    con.ChatRespond(username, $"There are {count} blocks of id {result}.");
                                    //also note if you count empty blocks it will count double as it counts both foreground and background layer
                                }
                                break;
                            case "fill":
                                {//fill entire level with a block
                                    if (args.Length < 2) break;
                                    if (!TryGetBlockIdFromString(args[1], out int result))
                                    {
                                        con.ChatRespond(username, "invalid block id!");
                                        break;
                                    }
                                    var layer = ((BlockId)result).IsBackground() ? 0 : 1;//place bgs on bg layer, fgs on fg layer
                                    FillWorld(result, layer);
                                    break;
                                }
                            case "edges":
                                {//fill world borders with id
                                    int result;
                                    if (args.Length < 2)
                                    {//if argument not specified, use air
                                        result = 0;
                                    }
                                    else
                                    {//try to parse id from string
                                        if (!TryGetBlockIdFromString(args[1], out result))
                                        {
                                            con.ChatRespond(username, "invalid block id!");
                                            break;
                                        }
                                    }

                                    //fill edges with id
                                    var layer = ((BlockId)result).IsBackground() ? 0 : 1;
                                    var width = data.Width;
                                    var height = data.Height;
                                    for (int i = 0; i < width; i++)
                                    {//top and bottom
                                        if (data.Blocks[layer, i, 0].Id != result) con.PlaceBlock(layer, i, 0, result);
                                        if (data.Blocks[layer, i, height - 1].Id != result) con.PlaceBlock(layer, i, height - 1, result);
                                    }
                                    for (int i = 1; i < height - 1; i++)
                                    {//left and right
                                        if (data.Blocks[layer, 0, i].Id != result) con.PlaceBlock(layer, 0, i, result);
                                        if (data.Blocks[layer, width - 1, i].Id != result) con.PlaceBlock(layer, width - 1, i, result);
                                    }
                                    break;
                                }
                            default:
                                con.ChatRespond(username, "unknown command. type .help for help.");
                                break;
                        }

                        break;
                    }
                case MessageType.PlaceBlock:
                    {//snakebot
                        int l = m.GetInt(1), x = m.GetInt(2), y = m.GetInt(3), id = m.GetInt(4);

                        int msdelay = 0;//we can change the delay depending on the block used

                        //animate blocks
                        int tid = -1;
                        const int metaldelay = 600;
                        switch ((BlockId)id)
                        {//metal snake
                            case BlockId.MetalGold: tid = (int)BlockId.MetalBronze; msdelay = metaldelay; break;
                            case BlockId.MetalBronze: tid = (int)BlockId.MetalCopper; msdelay = metaldelay; break;
                            case BlockId.MetalCopper: tid = (int)BlockId.Empty; msdelay = metaldelay; break;
                        }
                        const int tilesdelay = 50;
                        if (tid == -1)
                        {//tiles snake
                            if (id >= (int)BlockId.TilesWhite && id <= (int)BlockId.TilesBlue) { msdelay = tilesdelay; tid = id + 1; }//check for range to avoid too much code repeat
                            else if (id == (int)BlockId.TilesPurple) { msdelay = tilesdelay; tid = 0; }
                        }
                        if (tid == -1) break;//the block is not what we're looking for; break

                        msdelay -= ping;//subtract our ping
                        if (msdelay < 0) msdelay = 0;//if time ends up being in negatives then set to 0
                        Task.Delay(msdelay).ContinueWith((_) =>//delay a bit and continue asynchronously to not block the thread
                        {
                            //compare id with latest id at that location, if they are different then break, it will be queued by the new block
                            //that way if you place a block again at that location the snake won't break unless it happens to be after it tries to place it
                            if (id != data.Blocks[l, x, y].Id) return;//gotta use return inside lambda now

                            con.PlaceBlock(l, x, y, tid);//place new block after time has passed
                        });
                        break;
                    }
                case MessageType.Effect:
                    {//global effects
                        var pid = m.GetInt(0);
                        var eid = m.GetInt(1);
                        var ecfg = m.GetInt(2) + 1;//eeu is buggy

                        if (eid == (int)EffectType.MultiJump && ecfg == 0) ecfg = 999 + 1;//you can't give infinite jumps from a command so this is the closest we can get

                        //since we receive the message again after we give effect we need to search for it to not enter a loop when someone spams effects
                        if (pid == data.BotId) break;//break if id is our bot
                        if (globalEffectsIgnore.TryGetValue(pid, out var list))
                        {
                            bool breakoff = false;//need this to break from nested scope
                            foreach (var tup in list)
                            {
                                if (eid == tup.eid && ecfg == tup.ecfg)
                                {
                                    list.Remove(tup);//remove the effect from list
                                    breakoff = true;//set flag to break later
                                    break;//break from foreach
                                }
                            }
                            if (breakoff) break;//break from switch
                        }

                        if (eid == 0) globalEffects.Clear();//if clearing effects, clear the dictionary
                        else globalEffects[eid] = ecfg;//else store effect in dict

                        Console.WriteLine($"{pid} {eid} {ecfg}");

                        foreach (var player in data.Players.Values)
                        {//give everyone else the effect
                            if (player.Id == pid) continue;//skip player that originally got the effect

                            if (!globalEffectsIgnore.ContainsKey(pid)) globalEffectsIgnore.Add(pid, new List<(int eid, int ecfg)>());//init list if it's not there
                            globalEffectsIgnore[pid].Add((eid, ecfg));//add effect to be ignored later

                            if (eid == 0) con.ClearEffects(player.Username);//effect id 0 means clear all
                            else con.GiveEffect(player.Username, eid, ecfg);//give effect
                        }
                        break;
                    }
                case MessageType.PlayerMove:
                    {//stalkbot
                        var id = m.GetInt(0);
                        if (id == data.BotId) break;//break if id is bot
                        if (data.Players[id].Username != data.OwnerUsername) break;//break if not world owner, in other worlds, only stalk world owner (you)

                        //skip playerid and take 30 elements that form playermove, turn to array
                        var msg = m.Data.Skip(1).Take(30).ToArray();
                        con.SendL(MessageType.PlayerMove, msg);//send it
                        //this is not taking god mode into account which can lead to interesting effects (flying without visible godmode and falling upon hitting a block)
                        break;
                    }
            }//switch
        }//onmessage
        static int ping = 0;//in milliseconds
        static Timer pingTimer = new Timer(new TimerCallback((_) => Console.WriteLine((ping = con.GetPing()) + "ms ping")), null, -1, -1);//measures ping and prints to console
        static Dictionary<int, int> globalEffects = new Dictionary<int, int>();//keep track of effects
        static Dictionary<int, List<(int eid, int ecfg)>> globalEffectsIgnore = new Dictionary<int, List<(int eid, int ecfg)>>();//we use a valuetuple to store more than 1 value when necessary
    }
}
