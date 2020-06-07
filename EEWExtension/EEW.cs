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
using System.Threading;

namespace EEWData
{
    public class EEWClient : Client, IClient
    {
        private void SetHost() => base.MultiplayerHost = "wss://everybodyedits-universe.com/api/ws";
        public EEWClient(string token) : base(token) { SetHost(); /*_connectUrl = "/?access_token=";*/ }

        /// <summary>
        /// set to false when building headless apps
        /// </summary>
        public static bool StartEditor { get; set; } = Environment.OSVersion.Platform == PlatformID.Win32NT;
        private static readonly object _tokenLock = new object();
        //public static string TokenPath { get; set; } = "token.txt";
        public static string _tokenPath = "token.txt";
        public static string _cachedToken = null;
        public static string TokenPath
        {
            get { lock (_tokenLock) return _tokenPath; }
            set
            {
                lock (_tokenLock)
                {
                    if (_tokenPath != value) _cachedToken = null;
                    _tokenPath = value;
                }
            }
        }
        //public static void StoreToken(string token) => File.WriteAllText(TokenPath, token);
        public static void StoreToken(string token) { lock (_tokenLock) { _cachedToken = token; File.WriteAllText(_tokenPath, token); } }
        public static string GetToken(bool forcenew = false) => GetToken(forcenew, StartEditor);
        public static string GetToken(bool forcenew = false, bool startEditor = true)
        {
            if (_cachedToken != null) return _cachedToken;
            lock (_tokenLock)
            {
                var exists = File.Exists(TokenPath);
                if (forcenew || !exists)
                {
                    if (!exists) File.WriteAllText(TokenPath, "");
                    if (startEditor) Process.Start(Environment.OSVersion.Platform == PlatformID.Win32NT ? "notepad.exe" : "nano", TokenPath).WaitForExit();
                    else throw new ArgumentException($"Please update {TokenPath} file.");
                }
                var token = File.ReadAllText(TokenPath);
                _cachedToken = token;
                return token;
            }
        }

        public EEWClient() : base(null) { SetHost(); }
        public EEWClient(string username, string password) : this(username, password, out _) { }
        public EEWClient(string username, string password, out string token)
        {//who needs flurl and json anyway?
            SetHost();
            using (WebClient client = new WebClient() { Proxy = null })
            {
                client.Headers[HttpRequestHeader.ContentType] = "application/json";
                var result = client.UploadString("https://everybodyedits-universe.com/api/auth/login", $@"{{""username"":""{username}"",""password"":""{password}""}}");
                result = result.Remove(result.Length - 2, 2);
                token = _token = result.Substring(result.LastIndexOf('"') + 1);
            }
        }
        public override void Connect()
        {
            try
            {
                if (_token == null) _token = GetToken(false, StartEditor);
                base.Dispose(false);
                base.Connect();
            }
            catch//todo: add suitable exceptions
            {
                _token = GetToken(true, StartEditor);
                base.Dispose(false);
                base.Connect();
            }
        }
        /// <summary>
        /// do not use this method directly lol
        /// </summary>
        public override Task ConnectAsync() => Task.Run(() => base.ConnectAsync());
        public new EEWConnection CreateWorldConnection(string worldId)
        {
            if (!this.Connected) this.Connect();
            return new EEWConnection(this, ConnectionScope.World, worldId);
        }
    }
    public class EEWConnection : Connection, IConnection
    {
        public EEWConnection(IClient client, ConnectionScope scope, string worldId) : base(client, scope, worldId) { }
        public bool Connected { get => _client.Connected; }

        public string ChatPrefixText { get; set; } = "[Bot] ";
        //public void PlaceBlock(int layer, int x, int y, CustomBlockId id, params object[] args) => SendL(MessageType.PlaceBlock, new object[] { layer, x, y, (int)id }.Concat(args).ToArray());
        //public void PlaceBlock(int layer, int x, int y, BlockId id, params object[] args) => SendL(MessageType.PlaceBlock, new object[] { layer, x, y, (int)id }.Concat(args).ToArray());
        //public void PlaceBlock(int layer, int x, int y, int id, params object[] args) => SendL(MessageType.PlaceBlock, new object[] { layer, x, y, id }.Concat(args).ToArray());
        public void PlaceBlock(int l, int x, int y, Block block)
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
            ((Connection)this).PlaceBlock(l, x, y, (BlockId)block.Id, args);
        }
        public void ChatRespond(string username, string message, string prefix = null) => ChatPrefix($"@{username}: {message}", prefix);
        public void ChatPrefix(string message, string prefix = null) => Chat((prefix ?? ChatPrefixText) + message);
        public void Chat(string message) => SendL(MessageType.Chat, message);
        public void ChatDMPrefix(string username, string message, string prefix = null) => ChatDM(username, (prefix ?? ChatPrefixText) + message);
        public void ChatDM(string username, string message) => Chat($"/pm {username} {message}");
        public void SetGod(string username, bool hasgod) => SendL(MessageType.Chat, $"/god {username} {hasgod}");
        public void SetEdit(string username, bool hasedit) => SendL(MessageType.Chat, $"/edit {username} {hasedit}");
        public void Save() => SendL(MessageType.Chat, "/save");
        public void Load() => SendL(MessageType.Chat, "/load");
        private const int INITTIMEOUT = 5000;
        private const int INITRESENDDELAY = 100;
        public bool Init() => Init(INITTIMEOUT, INITRESENDDELAY, CancellationToken.None);
        public bool Init(int timeout) => Init(timeout, INITRESENDDELAY, CancellationToken.None);
        public bool Init(int timeout, int resendDelay) => Init(timeout, resendDelay, CancellationToken.None);
        public bool Init(int timeout, int resendDelay, CancellationToken cancellationToken)
        {//when sending init normally it sometimes drops it for some reason, so we resend it continiously until we get confirmation from server here
            if (timeout < -1) throw new ArgumentOutOfRangeException(nameof(timeout));
            if (resendDelay < -1) throw new ArgumentOutOfRangeException(nameof(resendDelay));
            if (cancellationToken.IsCancellationRequested) throw new OperationCanceledException(cancellationToken);
            using (var mre = new ManualResetEventSlim(false, 0))
            {
                void onmsg(object o, Message e) { if (e.Type == MessageType.Init) { this.OnMessage -= onmsg; mre?.Set(); } }
                this.OnMessage += onmsg;
                var stopwatch = Stopwatch.StartNew();
                while (Connected && !mre.IsSet)
                {
                    SendL(MessageType.Init, 0);
                    if (resendDelay < 0) return mre.Wait(timeout, cancellationToken);
                    if (timeout < 0)
                    {
                        if (mre.Wait(resendDelay, cancellationToken)) return true;
                        else continue;
                    }

                    int time = Math.Min(resendDelay, timeout - (int)stopwatch.ElapsedMilliseconds);
                    if (time < 0) return mre.IsSet;
                    else if (mre.Wait(time, cancellationToken)) return true;
                }
                return mre.IsSet;
            }
        }

        private readonly object _sendLock = new object();
        public bool UseLocking { get; set; } = false;
        public bool UseAsync { get; set; } = true;
        public void SendL(MessageType type, params object[] args)
        {
            if (UseAsync)
            {
                if (UseLocking) lock (_sendLock) SendAsync(type, args);
                else SendAsync(type, args);
            }
            else
            {
                if (UseLocking) lock (_sendLock) Send(type, args);
                else Send(type, args);
            }
        }
    }

    public enum CustomBlockId : ushort
    {
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

    public class WorldData : EEUData.WorldData
    {
        static WorldData()
        {
            var dict = new Dictionary<ushort, int>();
            foreach (var item in EEWData.WorldData.EEWBlockColors)
            {
                dict.Add(item.Key, item.Value);
            }
            foreach (var item in EEUData.WorldData.BlockColors)
            {
                if (dict.ContainsKey(item.Key)) continue;//skip conflicting blocks
                dict.Add(item.Key, item.Value);
            }
            WorldData.BlockColors = dict;
            //foreach (var item in dict)
            //{
            //    var c = item.Value;
            //    Console.WriteLine($"{(!int.TryParse(((BlockId)item.Key).ToString(), out int _) ? ((BlockId)item.Key).ToString() : ((CustomBlockId)item.Key).ToString())} #{c.ToString("X6")}");
            //}
        }

        public static new int GetARGBColor(ushort fg = (ushort)BlockId.Black, ushort bg = (ushort)BlockId.Black, int backgroundColor = -1) => WorldData.FromBlockColorToARGB(GetBlockColor(fg, bg, backgroundColor));
        public static new int GetBlockColor(ushort fg = (ushort)BlockId.Black, ushort bg = (ushort)BlockId.Black, int backgroundColor = -1)
        {
            unchecked
            {
                var ct = WorldData.BlockColors;
                const int BLACK = 0;
                const int TRANSPARENT = -2;
                int c = BLACK;
                int n = ct[fg];
                if (n == -1) n = ct[bg];
                if (n == -1 && bg != 0) n = -2;
                if (n == -2) c = TRANSPARENT;
                if (n == -1) c = backgroundColor != -1 ? backgroundColor : BLACK;
                if (n >= 0) c = n;
                return c;
                //var ct = WorldData.BlockColors;
                //const int BLACK = (int)0xff000000;
                //const int TRANSPARENT = -2;
                //int c = BLACK;
                //int n = ct[fg];
                //if (n == -1) n = ct[bg];
                //if (n == -1) n = backgroundColor;
                //if (n == -1) c = BLACK;
                //if (n == -2) c = TRANSPARENT;
                //if (n >= 0) c = n;
                //return c;
            }
        }
        public override int GetARGBColor(int x, int y, int layer = -1, int backgroundColor = -2) => GetBlockColor(x, y, layer, backgroundColor);
        public override int GetBlockColor(int x, int y, int layer = -1, int backgroundColor = -2)
        {
            if (backgroundColor == -2) backgroundColor = this.BackgroundColor;//i think -2 can be an actual color in the world? but i really doubt that's going to be a problem
            if (layer < -1 && layer > 1) throw new ArgumentOutOfRangeException(nameof(layer));
            if (layer == -1)
                return WorldData.GetBlockColor((ushort)this.Blocks[1, x, y].Id, (ushort)this.Blocks[0, x, y].Id, backgroundColor);
            else if (layer == 0)
                return WorldData.GetBlockColor((ushort)BlockId.Empty, (ushort)this.Blocks[0, x, y].Id, backgroundColor);
            else //if (layer == 1)
                return WorldData.GetBlockColor((ushort)this.Blocks[1, x, y].Id, (ushort)BlockId.Empty, backgroundColor);
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
                this.Zones = new ConcurrentDictionary<int, Zone>(DeserializeZoneData(mlist, this.Width, this.Height, ref index));
            }
        }

        public override void Parse(Message m)
        {
            switch (m.Type)
            {
                case (MessageType)CustomMessageType.Loadlevel:
                    LoadLevel(m);
                    break;
                case MessageType.PlaceBlock:
                    var id = (int)m[4];
                    //fix block conflicts
                    if (Enum.IsDefined(typeof(BlockId), id)) base.Parse(m);
                    else switch ((CustomBlockId)id)
                        {//custom block declarations
                            default:
                                Blocks[(int)m[1], (int)m[2], (int)m[3]] = new Block(id, (int)m[0]);
                                break;
                        }
                    //switch ((BlockId)id)
                    //{
                    //    case BlockId.SwitchesLocalSwitch://those don't exist in eew, and cause conflicts
                    //    case BlockId.SwitchesLocalReset:
                    //    case BlockId.SwitchesLocalDoor:
                    //        int pid = m.GetInt(0), l = m.GetInt(1), x = m.GetInt(2), y = m.GetInt(3);
                    //        Blocks[l, x, y] = new Block(id, pid);
                    //        break;
                    //    default:
                    //        base.Parse(m);
                    //        break;
                    //}
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
            this.Zones = new ConcurrentDictionary<int, Zone>(DeserializeZoneData(mlist, this.Width, this.Height, ref index));
        }

        public static new Block[,,] DeserializeBlockData(List<object> m, int width, int height, ref int index)
        {
            var blocks = new Block[2, width, height];

            for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++)
                {
                    int value = 0;
                    if (m[index++] is int iValue)
                        value = iValue;

                    var backgroundId = value >> 16;
                    var foregroundId = 65535 & value;

                    blocks[0, x, y] = new Block(backgroundId);
                    blocks[1, x, y] = HandleBlock(m, foregroundId, ref index);
                }
            return blocks;
        }
        internal protected static new Block HandleBlock(List<object> m, int foregroundId, ref int index, bool returnBlocks = true)
        {
            switch (foregroundId)
            {
                case (int)BlockId.SignWood:
                case (int)BlockId.SignRed:
                case (int)BlockId.SignGreen:
                case (int)BlockId.SignBlue:
                    {
                        string text = (string)m[index++];
                        int morph = (int)m[index++];
                        if (!returnBlocks) return null;
                        return new Sign(foregroundId, text, morph);
                    }

                case (int)BlockId.Portal:
                    {
                        int rotation = (int)m[index++];
                        int p_id = (int)m[index++];
                        int t_id = (int)m[index++];
                        bool flip = (bool)m[index++];
                        if (!returnBlocks) return null;
                        return new Portal(foregroundId, rotation, p_id, t_id, flip);
                    }

                case (int)BlockId.EffectClear:
                case (int)BlockId.EffectMultiJump:
                case (int)BlockId.EffectHighJump:
                    {
                        int r = (foregroundId == (int)BlockId.EffectClear) ? 0 : (int)m[index++];
                        if (!returnBlocks) return null;
                        return new Effect(foregroundId, r);
                    }

                default:
                    {
                        if (!returnBlocks) return null;
                        return new Block(foregroundId);
                    }
            }
        }

        public static readonly new Dictionary<ushort, int> BlockColors;
        public static readonly Dictionary<ushort, int> EEWBlockColors = new Dictionary<ushort, int>()
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

    public class PlayerData : EEUData.PlayerData
    {
        public ConcurrentDictionary<int, string> UsernameColors = new ConcurrentDictionary<int, string>();
        protected override void ParseInit(Message m, out int index)
        {
            index = 11;
            int width = m.GetInt(9), height = m.GetInt(10);
            for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++)
                {
                    int value = 0;
                    if (m[index++] is int iValue)
                        value = iValue;

                    var foregroundId = 65535 & value;
                    WorldData.HandleBlock(m.Data, foregroundId, ref index, false);
                }
            this.TimeOffset = m.GetInt(index++);
            var canEdit = m.GetBool(index++);
            var role = m.GetInt(index++);

            var id = BotId = m.GetInt(0);
            Players.Add(id, new Player(id, m.GetString(1)) { Smiley = m.GetInt(2), X = m.GetDouble(4), Y = m.GetDouble(5), IsBot = true, HasEdit = canEdit, Rank = (Rank)role });
        }
        public override void Parse(Message m)
        {
            switch (m.Type)
            {
                case MessageType.Init:
                    this.ParseInit(m, out int index);
                    UsernameColors.Add(m.GetInt(0), m.GetString(index));
                    break;
                case MessageType.PlayerAdd://existing players
                    //todo? last time i checked this is never received
                    break;
                case MessageType.PlayerJoin://new joining players
                    //Players.Add(m.GetInt(0), new Player(m.GetInt(0), m.GetString(1)) {  });
                    base.Parse(m);
                    var pid = (int)m[0];
                    Players[pid].LocalSwitches = null;
                    UsernameColors.Add(pid, m.GetString(11));
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
}
