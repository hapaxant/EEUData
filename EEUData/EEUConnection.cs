using System;
using System.Linq;
using EEUniverse.Library;
using System.Threading.Tasks;

namespace EEUData
{
    public class EEUConnection : Connection
    {
        public EEUConnection(IClient client, ConnectionScope scope, string worldId = "") : base(client, scope, worldId) { }

        public void Disconnect() => ((IConnection)this).Send(new Message(ConnectionScope.Lobby, MessageType.RoomDisconnect));
        public Task DisconnectAsync() => ((IConnection)this).SendAsync(new Message(ConnectionScope.Lobby, MessageType.RoomDisconnect));

        private string _chatPrefix = "[Bot] ";
        public string ChatPrefix { get => _chatPrefix; set => _chatPrefix = value ?? ""; }

        private readonly static System.Globalization.CultureInfo c = System.Globalization.CultureInfo.InvariantCulture;
        public void Init() => Init(0);
        public void Init(int timeOffset) => SendL(MessageType.Init, timeOffset);
        public void Ping() => SendL(MessageType.Ping);
        public void ChangeSmiley(SmileyType id) => ChangeSmiley((int)id);
        public void ChangeSmiley(int id) => SendL(MessageType.PlayerSmiley, id);
        public void ToggleGod() => SendL(MessageType.PlayerGod);
        public void CreateZone(ZoneType type) => CreateZone((int)type);
        public void CreateZone(int type) => SendL(MessageType.ZoneCreate, type);
        public void DeleteZone(int id) => SendL(MessageType.ZoneDelete, id);
        public void EditZone(int id, bool set, int x, int y, int width, int height) => SendL(MessageType.ZoneEdit, id, set, x, y, width, height);
        public void EnterZone(int id) => SendL(MessageType.ZoneEnter, id);
        public void ExitZone(int id) => SendL(MessageType.ZoneExit, id);
        public void Effect(int id, int config = 0) => SendL(MessageType.Effect, id, config);
        public void LocalSwitch(bool state, params int[] ids) => SendL(MessageType.SwitchLocal, new object[] { state }.Concat(ids.Cast<object>()).ToArray());
        public void GlobalSwitch(bool state, params int[] ids) => SendL(MessageType.SwitchGlobal, new object[] { state }.Concat(ids.Cast<object>()).ToArray());
        public void GoldCoin(int amount) => SendL(MessageType.CoinGold, amount);
        public void BlueCoin(int amount) => SendL(MessageType.CoinBlue, amount);
        public void PlaceBlock(int layer, int x, int y, BlockId id, params object[] args) => SendL(MessageType.PlaceBlock, new object[] { layer, x, y, (int)id }.Concat(args).ToArray());
        public void PlaceBlock(int layer, int x, int y, int id, params object[] args) => SendL(MessageType.PlaceBlock, new object[] { layer, x, y, (int)id }.Concat(args).ToArray());
        public void PlaceBlock(int layer, int x, int y, ushort id, params object[] args) => SendL(MessageType.PlaceBlock, new object[] { layer, x, y, (int)id }.Concat(args).ToArray());
        public void PlaceBlock(int layer, int x, int y, Block block) => PlaceBlock(layer, x, y, block.Id, block.Data?.Serialize()?.ToArray() ?? new object[0]);
        public void ChatRespond(string username, string message, string prefix = null) => Chat($"@{username}: {message}", prefix);
        public void Chat(string message, string prefix = null) => ChatRaw((prefix ?? ChatPrefix) + message);
        public void ChatRaw(string message) => SendL(MessageType.Chat, message);
        public void ChatDM(string username, string message, string prefix = null) => ChatDMRaw(username, (prefix ?? ChatPrefix) + message);
        public void ChatDMRaw(string username, string message) => ChatRaw($"/pm {username} {message}");
        public void GiveEdit(string username) => ChatRaw($"/giveedit {username}");
        public void TakeEdit(string username) => ChatRaw($"/takeedit {username}");
        public void SetEdit(string username, bool hasEdit) => ChatRaw($"/{(hasEdit ? "give" : "take")}edit {username}");
        public void GiveGod(string username) => ChatRaw($"/givegod {username}");
        public void TakeGod(string username) => ChatRaw($"/takegod {username}");
        public void SetGod(string username, bool hasGod) => ChatRaw($"/{(hasGod ? "give" : "take")}god {username}");
        public void SetBackgroundColor(int rgb) => SetBackgroundColor((rgb & 0xFFFFFF).ToString("X6"));
        public void SetBackgroundColor(string hexcode) => ChatRaw("/bg " + hexcode);
        public void Teleport(string username, int x, int y) => Teleport(username, (double)x, (double)y);
        public void Teleport(string username, double x, double y) => ChatRaw($"/tp {username} {x.ToString(c)} {y.ToString(c)}");
        public void SetTitle(string title) => ChatRaw("/title " + title);
        public void Save() => ChatRaw("/save");
        public void Clear() => ChatRaw("/clear");
        public void ResetAll() => ChatRaw("/resetall");
        public void ResetSelf() => ChatRaw("/reset");
        public void Reset(string username) => ChatRaw("/reset " + username);
        public void ClearEffects(string username) => ChatRaw($"/effect {username} clear");
        public void ClearEffect(string username, EffectType effect) => GiveEffect(username, (int)effect, 0);
        public void ClearEffect(string username, int effect) => GiveEffect(username, (int)effect, 0);
        public void GiveEffect(string username, EffectType effect, int config = 0) => GiveEffect(username, (int)effect, config);
        public void GiveEffect(string username, int effect, int config = 0) => ChatRaw($"/effect {username} {effect} {config}");
        public void SetWorldVisibility(WorldVisibility visibility) => ChatRaw("/visibility " + (int)visibility);
        public WorldVisibility GetWorldVisibility()
        {
            var msg = _client.ReceiveMessage(new Message(ConnectionScope.World, MessageType.Chat, "/visibility"), (m) => m.Type == MessageType.ChatInfo && m.GetString(0).StartsWith("World is "));
            switch (msg.GetString(0).Split(' ')[2])
            {
                case "public": return WorldVisibility.Public;
                case "friends": return WorldVisibility.Friends;
                case "unlisted": return WorldVisibility.Unlisted;
                case "private": return WorldVisibility.Private;
                default: throw new InvalidOperationException("server returned unknown visibility type");
            }
        }
        public void ClearBackgroundColor() => SetBackgroundColor("none");
        public int GetBackgroundColor()//you can do .ToString("X6") to get hex code
        {
            var msg = _client.ReceiveMessage(new Message(ConnectionScope.World, MessageType.Chat, "/bg"), (m) => m.Type == MessageType.ChatInfo && m.GetString(0).StartsWith("Background color is "));
            var parts = msg.GetString(0).Split(' ');
            if (parts[3] == "none") return -1;
            var r = byte.Parse(parts[4].Substring(1));
            var g = byte.Parse(parts[5]);
            var b = byte.Parse(parts[6].Remove(parts[6].Length - 1, 1));
            return (r << 16 | g << 8 | b);
        }
        //i regret this decision
        public void Move(double lastTime = 0, double currentTime = 0, int horizonal = 0, int vertical = 0, double x = 0, double y = 0,
                        double xvelocity = 0, double yvelocity = 0, double xacceleration = 0, double yacceleration = 0, double drag = 0,
                        double edge0 = 0, double edge1 = 0, double edge2 = 0, double edge3 = 0, int edge4 = 0, int edge5 = 0,
                        double sliding0 = 0, double sliding1 = 0, double sliding2 = 0, double sliding3 = 0, int sliding4 = 0, int sliding5 = 0,
                        double horizonalforce = 0, double verticalforce = 0, int xgrid = 0, int ygrid = 0, int edge = 0, bool space = false, double spacetime = 0)
                => SendL(MessageType.PlayerMove, lastTime, currentTime, horizonal, vertical, x, y,
                         xvelocity, yvelocity, xacceleration, yacceleration, drag,
                         edge0, edge1, edge2, edge3, edge4, edge5,
                         sliding0, sliding1, sliding2, sliding3, sliding4, sliding5,
                         horizonalforce, verticalforce, xgrid, ygrid, edge, space, spacetime);

        private readonly object _sendLock = new object();
        public bool UseLocking { get; set; } = false;
        public bool UseAsync { get; set; } = true;
        public void SendL(MessageType type, params object[] args)
        {
            if (UseAsync)
            {
                if (UseLocking) lock (_sendLock) _client.SendAsync(_scope, type, args);
                else _client.SendAsync(_scope, type, args);
            }
            else
            {
                if (UseLocking) lock (_sendLock) _client.Send(_scope, type, args);
                else _client.Send(_scope, type, args);
            }
        }
    }
}
