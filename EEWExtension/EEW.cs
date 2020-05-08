using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EEUniverse.Library;
using EEUData;
using System.IO;
using System.Net;
using System.Diagnostics;

namespace EEWData
{
    public class EEWClient : Client, IClient
    {
        private void SetHost() => this.MultiplayerHost = "wss://everybodyedits-universe.com/api/ws";
        public EEWClient(string token) : base(token) { SetHost(); /*_connectUrl = "/?access_token=";*/ }

        /// <summary>
        /// set to false when building headless apps
        /// </summary>
        public static bool StartEditor { get; set; } = Environment.OSVersion.Platform == PlatformID.Win32NT;
        public static string TokenPath { get; set; } = "token.txt";
        public static void StoreToken(string token) => File.WriteAllText(TokenPath, token);
        public static string GetToken(bool forcenew = false) => GetToken(forcenew, StartEditor);
        public static string GetToken(bool forcenew = false, bool startEditor = true)
        {
            var exists = File.Exists(TokenPath);
            if (forcenew || !exists)
            {
                if (!exists) File.WriteAllText(TokenPath, "");
                if (startEditor) Process.Start(Environment.OSVersion.Platform == PlatformID.Win32NT ? "notepad.exe" : "nano", TokenPath).WaitForExit();
                else throw new ArgumentException($"Please update {TokenPath} file.");
            }
            return File.ReadAllText(TokenPath);
        }

        public EEWClient() : base(null) { SetHost(); }
        public EEWClient(string username, string password) : this(username, password, out _) { }
        public EEWClient(string username, string password, out string token) //: base(null)
        {//who needs flurl and json anyway?
            using (WebClient client = new WebClient() { Proxy = null })
            {
                client.Headers[HttpRequestHeader.ContentType] = "application/json";
                var result = client.UploadString("https://everybodyedits-universe.com/api/auth/login", $@"{{""username"":""{username}"",""password"":""{password}""}}");
                result = result.Remove(result.Length - 2, 2);
                token = _token = result.Substring(result.LastIndexOf('"') + 1);
            }
            SetHost();
        }
        new public void Connect()
        {
            //SetHost();
            if (_token == null)
                _token = GetToken(false, StartEditor);
            try
            {
                base.Dispose();
                base.Connect();//ConnectAsync().ConfigureAwait(false).GetAwaiter().GetResult();
            }
            catch//todo: add suitable exceptions
            {
                _token = GetToken(true, StartEditor);
                base.Dispose();
                base.Connect();
            }
        }
    }

    //*/*//*////////can't inherit from other enum sooo*//*/*//*/*/
    public enum CustomBlockId : ushort
    {
        #region old
        /*
        #region fg
        //gravity
        Empty = 0,
        GravityLeft = 13,
        GravityUp = 14,
        GravityRight = 15,
        GravityNone = 16,
        GravitySlow = 71,
        //basic
        BasicWhite = 1,
        BasicGrey = 2,
        BasicBlack = 3,
        BasicRed = 4,
        BasicOrange = 5,
        BasicYellow = 6,
        BasicGreen = 7,
        BasicCyan = 8,
        BasicBlue = 9,
        BasicPurple = 10,
        //stone
        StoneWhite = 18,
        StoneGrey = 19,
        StoneBlack = 20,
        StoneRed = 21,
        StoneOrange = 22,
        StoneYellow = 23,
        StoneGreen = 24,
        StoneCyan = 25,
        StoneBlue = 26,
        StonePurple = 27,
        //beveled
        BeveledWhite = 28,
        BeveledGrey = 29,
        BeveledBlack = 30,
        BeveledRed = 31,
        BeveledOrange = 32,
        BeveledYellow = 33,
        BeveledGreen = 34,
        BeveledCyan = 35,
        BeveledBlue = 36,
        BeveledPurple = 37,
        //metal
        MetalSilver = 38,
        MetalSteel = 39,
        MetalIron = 40,
        MetalGold = 41,
        MetalBronze = 42,
        MetalCopper = 43,
        //glass
        GlassWhite = 45,
        GlassBlack = 46,
        GlassRed = 47,
        GlassOrange = 48,
        GlassYellow = 49,
        GlassGreen = 50,
        GlassCyan = 51,
        GlassBlue = 52,
        GlassPurple = 53,
        GlassPink = 54,
        //tiles
        TilesWhite = 72,
        TilesGrey = 73,
        TilesBlack = 74,
        TilesRed = 75,
        TilesOrange = 76,
        TilesYellow = 77,
        TilesGreen = 78,
        TilesCyan = 79,
        TilesBlue = 80,
        TilesPurple = 81,
        //special
        Black = 12,
        Secret = 95,
        Clear = 96,
        //signs
        SignWood = 55,
        SignRed = 56,
        SignGreen = 57,
        SignBlue = 58,
        //coins
        GoldCoin = 11,
        //control
        Spawn = 44,
        Godmode = 17,
        Crown = 70,
        Portal = 59,
        //actions(effects)
        EffectClear = 92,
        EffectMultiJump = 93,
        EffectHighJump = 94,
        #endregion
        #region bg
        //basic bg
        BgBasicWhite = 60,
        BgBasicGrey = 61,
        BgBasicBlack = 62,
        BgBasicRed = 63,
        BgBasicOrange = 64,
        BgBasicYellow = 65,
        BgBasicGreen = 66,
        BgBasicCyan = 67,
        BgBasicBlue = 68,
        BgBasicPurple = 69,
        //tiles bg
        BgTilesWhite = 82,
        BgTilesGrey = 83,
        BgTilesBlack = 84,
        BgTilesRed = 85,
        BgTilesOrange = 86,
        BgTilesYellow = 87,
        BgTilesGreen = 88,
        BgTilesCyan = 89,
        BgTilesBlue = 90,
        BgTilesPurple = 91,
        #endregion
        */
        #endregion

        #region new
        //NEW
        //brick
        BrickWhite = 99,
        BrickBlack = 100,
        BrickRed = 101,
        BrickOrange = 102,
        BrickYellow = 103,
        BrickGreen = 104,
        BrickCyan = 105,
        BrickBlue = 106,
        BrickPurple = 107,
        //special
        FaceHappy = 97,
        FaceSad = 98,
        //arena wall
        ArenaWallWhite = 108,
        ArenaWallBlack = 109,
        ArenaWallRed = 110,
        ArenaWallOrange = 111,
        ArenaWallYellow = 112,
        ArenaWallGreen = 113,
        ArenaWallCyan = 114,
        ArenaWallBlue = 115,
        ArenaWallPurple = 116,
        //bloody pack
        #region blood pack
        BloodIntestine = 117,
        BloodFreshBlock = 118,
        BloodDriedBlock = 119,
        BloodDriedFloor = 120,
        BloodDriedCornerRight = 121,
        BloodDriedCornerLeft = 122,
        BloodSplattersA = 123,
        BloodSplattersB = 124,
        BloodSplattersC = 125,
        BloodSplattersD = 126,
        BloodSplattersE = 127,
        BloodSplattersF = 128,
        BloodSplattersG = 129,
        BloodSplattersH = 130,
        BloodSplattersI = 131,
        BloodSplattersJ = 132,
        BloodSplattersK = 133,
        BloodBubble = 134,
        BloodBubbleSmall = 135,
        BloodTop = 136,
        BloodRoof = 137,
        BloodLiquid = 138,
        BloodDropletAir = 139,
        BloodCascade = 140,
        #endregion
        //hazards
        HazardsSawBlade = 141,
        #endregion
    }

    public enum CustomMessageType
    {
        Loadlevel = 127,
        RegisterSoundEffect = 123,
        ConfirmRegisterSoundEffect = 122,
        SetSoundEffectState = 121,
        ConfirmSoundEffects = 120,
        CoinCollected = 119,
        SetZoom = 118,
        CanZoom = 117,
    }

    public enum CustomSmileyType
    {
        Flushed = 8,
        Delirium = 9,
        HalfSkull = 10,
        Ghoul = 11,
        Samurai = 12,
        Sick = 13,
        Eye = 14,
        Unit = 15,
    }

    //public class Player : EEUData.Player
    //{
    //    public Player(int id, string username) : base(id, username) { }

    //    public string UsernameColor { get; set; }
    //}

    public class PlayerData : EEUData.PlayerData
    {
        //protected override void ParseInit(Message m, out int index)
        //{
        //    //var id = BotId = m.GetInt(0);
        //    //Players.Add(id, new Player(id, m.GetString(1)) { Smiley = (SmileyType)m.GetInt(2), X = m.GetDouble(4), Y = m.GetDouble(5), IsBot = true });

        //}
        public ConcurrentDictionary<int, string> UsernameColors = new ConcurrentDictionary<int, string>();
        public override void Parse(Message m)
        {
            switch (m.Type)
            {
                case MessageType.Init:
                    base.ParseInit(m, out int index);
                    UsernameColors.Add(m.GetInt(0), m.GetString(index));
                    break;
                //Players.Add(m.GetInt(0), new Player(m.GetInt(0), m.GetString(1)) { UsernameColor = m.GetString(11) });
                //break;
                case MessageType.PlayerAdd://existing players
                    break;
                case MessageType.PlayerJoin://new joining players
                    //Players.Add(m.GetInt(0), new Player(m.GetInt(0), m.GetString(1)) {  });
                    base.Parse(m);
                    UsernameColors.Add(m.GetInt(0), m.GetString(11));
                    break;
                case MessageType.PlayerExit:
                    base.Parse(m);
                    UsernameColors.Remove(m.GetInt(0));
                    break;
                default:
                    base.Parse(m);
                    break;
            }
        }
    }

    public class WorldData : EEUData.WorldData
    {
        static WorldData()
        {
            //Stopwatch stopwatch = Stopwatch.StartNew();
            var dict = new Dictionary<ushort, int>();
            foreach (var item in EEUData.WorldData.BlockColors)
            {
                if (item.Key == (ushort)BlockId.CoinBlue) continue;//this conflicts with CustomBlockId.FaceHappy, so we skip it
                dict.Add(item.Key, item.Value);
            }
            foreach (var item in EEWData.WorldData.EEWBlockColors)
            {
                dict.Add(item.Key, item.Value);
            }
            BlockColors = dict;
            //stopwatch.Stop();
            //foreach (var item in dict)
            //{
            //    var c = item.Value;
            //    Console.WriteLine($"{(!int.TryParse(((BlockId)item.Key).ToString(), out int _) ? ((BlockId)item.Key).ToString() : ((CustomBlockId)item.Key).ToString())} #{c.ToString("X6")}");
            //}
            //Console.WriteLine($"Initializing WorldData (BlockColors): {stopwatch.Elapsed}");
        }

        protected override void ParseInit(Message m, bool deserializeStuff = true)
        {
            base.ParseInit(m, false);
            if (deserializeStuff)
            {
                var index = 11;
                var mlist = m.Data.ToList();
                this.Blocks = DeserializeBlockData(mlist, this.Width, this.Height, ref index);
                index++;//timeOffset
                index++;//isOwner
                index++;//role
                index++;//usernameColor (custom)
                this.Zones = DeserializeZoneData(mlist, this.Width, this.Height, ref index);
            }
        }

        public override void Parse(Message m)
        {
            switch (m.Type)
            {
                case (MessageType)CustomMessageType.Loadlevel:
                    LoadLevel(m);
                    break;
                default:
                    base.Parse(m);
                    break;
            }
        }

        protected void LoadLevel(Message m)
        {
            if ((int)m.Type != (int)CustomMessageType.Loadlevel) throw new ArgumentException("message is not of type loadlevel", nameof(m));

            var index = 0;
            var mlist = m.Data.ToList();
            this.Blocks = DeserializeBlockData(mlist, this.Width, this.Height, ref index);
            this.Zones = DeserializeZoneData(mlist, this.Width, this.Height, ref index);
        }

        public static new Dictionary<ushort, int> BlockColors { get; protected set; }

        //protected static Lazy<Dictionary<int, int>> BlockColorsFromEEUAndEEW;
        protected static readonly Dictionary<ushort, int> EEWBlockColors = new Dictionary<ushort, int>()
        {
            //brick
            { (ushort)CustomBlockId.BrickWhite, 10131601 },
            { (ushort)CustomBlockId.BrickBlack, 3157805 },
            { (ushort)CustomBlockId.BrickRed, 7678252 },
            { (ushort)CustomBlockId.BrickOrange, 8538912 },
            { (ushort)CustomBlockId.BrickYellow, 7498280 },
            { (ushort)CustomBlockId.BrickGreen, 4224037 },
            { (ushort)CustomBlockId.BrickCyan, 2914915 },
            { (ushort)CustomBlockId.BrickBlue, 3096436 },
            { (ushort)CustomBlockId.BrickPurple, 5515637 },
            //special
            { (ushort)CustomBlockId.FaceHappy, 5130285 },
            { (ushort)CustomBlockId.FaceSad, 5130285 },
            //arena wall
            { (ushort)CustomBlockId.ArenaWallWhite, 0x828282 },
            { (ushort)CustomBlockId.ArenaWallBlack, 0x191919 },
            { (ushort)CustomBlockId.ArenaWallRed, 0x8C0000 },
            { (ushort)CustomBlockId.ArenaWallOrange, 0x632600 },
            { (ushort)CustomBlockId.ArenaWallYellow, 0x635200 },
            { (ushort)CustomBlockId.ArenaWallGreen, 0x005109 },
            { (ushort)CustomBlockId.ArenaWallCyan, 0x004C4C },
            { (ushort)CustomBlockId.ArenaWallBlue, 0x000B4C },
            { (ushort)CustomBlockId.ArenaWallPurple, 0x34004C },
            //blood
            { (ushort)CustomBlockId.BloodIntestine, 13918281 },
            { (ushort)CustomBlockId.BloodFreshBlock, 11273220 },
            { (ushort)CustomBlockId.BloodDriedBlock, 7799041 },
            { (ushort)CustomBlockId.BloodDriedFloor, -1 },
            { (ushort)CustomBlockId.BloodDriedCornerRight, -1 },
            { (ushort)CustomBlockId.BloodDriedCornerLeft, -1 },
            { (ushort)CustomBlockId.BloodSplattersA, -1 },
            { (ushort)CustomBlockId.BloodSplattersB, -1 },
            { (ushort)CustomBlockId.BloodSplattersC, -1 },
            { (ushort)CustomBlockId.BloodSplattersD, -1 },
            { (ushort)CustomBlockId.BloodSplattersE, -1 },
            { (ushort)CustomBlockId.BloodSplattersF, -1 },
            { (ushort)CustomBlockId.BloodSplattersG, -1 },
            { (ushort)CustomBlockId.BloodSplattersH, -1 },
            { (ushort)CustomBlockId.BloodSplattersI, -1 },
            { (ushort)CustomBlockId.BloodSplattersJ, -1 },
            { (ushort)CustomBlockId.BloodSplattersK, -1 },
            { (ushort)CustomBlockId.BloodBubble, -1 },
            { (ushort)CustomBlockId.BloodBubbleSmall, -1 },
            { (ushort)CustomBlockId.BloodTop, -1 },
            { (ushort)CustomBlockId.BloodRoof, -1 },
            { (ushort)CustomBlockId.BloodLiquid, -1 },
            { (ushort)CustomBlockId.BloodDropletAir, -1 },
            { (ushort)CustomBlockId.BloodCascade, -1 },   
            //hazards
            { (ushort)CustomBlockId.HazardsSawBlade, -1 },
        };

    }
    public static class ConnectionExtensions
    {
        public static string ChatPrefixText { get; set; } = "[Bot] ";
        public static void PlaceBlock(this IConnection con, int layer, int x, int y, CustomBlockId id, params object[] args) => SendL(con, MessageType.PlaceBlock, new object[] { layer, x, y, (int)id }.Concat(args).ToArray());
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
        public static void ChatRespond(this IConnection con, string username, string message, string prefix = null) => ChatPrefix(con, $"@{username}: {message}", prefix);
        public static void ChatPrefix(this IConnection con, string message, string prefix = null) => Chat(con, (prefix ?? ChatPrefixText) + message);
        public static void Chat(this IConnection con, string message) => SendL(con, MessageType.Chat, message);
        public static void ChatDMPrefix(this IConnection con, string username, string message, string prefix = null) => ChatDM(con, username, (prefix ?? ChatPrefixText) + message);
        public static void ChatDM(this IConnection con, string username, string message) => Chat(con, $"/pm {username} {message}");
        public static void SetGod(this IConnection con, string username, bool hasgod) => SendL(con, MessageType.Chat, $"/god {username} {hasgod}");
        public static void SetEdit(this IConnection con, string username, bool hasedit) => SendL(con, MessageType.Chat, $"/edit {username} {hasedit}");
        public static void Save(this IConnection con) => SendL(con, MessageType.Chat, "/save");
        public static void Load(this IConnection con) => SendL(con, MessageType.Chat, "/load");

        private static readonly object _sendLock = new object();
        public static bool UseLocking { get; set; } = true;
        public static bool UseAsync { get; set; } = false;
        public static void SendL(this IConnection con, MessageType type, params object[] args)
        {
            if (UseLocking)
            {
                lock (_sendLock)
                {
                    if (UseAsync)
                        con.SendAsync(type, args);
                    else
                        con.Send(type, args);
                }
            }
            else
            {
                if (UseAsync)
                    con.SendAsync(type, args);
                else
                    con.Send(type, args);
            }
        }
    }
}
