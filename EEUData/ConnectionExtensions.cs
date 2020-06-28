using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EEUniverse.Library;
using EEUData;

namespace EEUData
{
    public static class ConnectionExtensions
    {
        //public static string ChatPrefixText { get; set; } = "[Bot] ";
        private static readonly Dictionary<int, string> connectionPrefixes = new Dictionary<int, string>();
        private const string DEFAULTPREFIX = "[Bot] ";
        /// <summary>
        /// if you used it, pass null when you're done with the connection
        /// </summary>
        public static void SetChatPrefix(this IConnection con, string prefix)
        {
            var hash = con.GetHashCode();
            if (prefix == null) { if (connectionPrefixes.ContainsKey(hash)) connectionPrefixes.Remove(hash); }
            else connectionPrefixes[hash] = prefix;
        }
        public static string GetChatPrefix(this IConnection con)
        {
            var hash = con.GetHashCode();
            if (connectionPrefixes.ContainsKey(hash)) return connectionPrefixes[hash];
            else return DEFAULTPREFIX;
        }

        //yes, you have to ToString() with the invariant cultureinfo if you dont want it to break on random computers
        //basically, at least on my machine, if i just do $"{x} {y}" it'll convert to string and use ',' as separators (2,5 5,7), instead of '.' (2.5 5.7)
        //why tho
        private readonly static System.Globalization.CultureInfo c = System.Globalization.CultureInfo.InvariantCulture;
        public static void Init(this IConnection con) => Init(con, 0);
        public static void Init(this IConnection con, int timeOffset) => SendL(con, MessageType.Init, timeOffset);
        public static void PlaceBlock(this IConnection con, int layer, int x, int y, BlockId id, params object[] args) => SendL(con, MessageType.PlaceBlock, new object[] { layer, x, y, (int)id }.Concat(args).ToArray());
        public static void PlaceBlock(this IConnection con, int layer, int x, int y, int id, params object[] args) => SendL(con, MessageType.PlaceBlock, new object[] { layer, x, y, id }.Concat(args).ToArray());
        public static void PlaceBlock(this IConnection con, int l, int x, int y, Block block)
        {
            object[] args;
            switch (block)
            {
                case Platform b:
                    args = new object[] { b.Rotation };
                    break;
                case Switch b:
                    if (!b.Inverted.HasValue) args = new object[] { b.Value };
                    else args = new object[] { b.Value, b.Inverted.Value };
                    break;
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
            PlaceBlock(con, l, x, y, block.Id, args);
        }
        public static void ChatRespond(this IConnection con, string username, string message, string prefix = null) => ChatPrefix(con, $"@{username}: {message}", prefix);
        public static void ChatPrefix(this IConnection con, string message, string prefix = null) => Chat(con, (prefix ?? GetChatPrefix(con)) + message);
        public static void Chat(this IConnection con, string message) => SendL(con, MessageType.Chat, message);
        public static void ChatDMPrefix(this IConnection con, string username, string message, string prefix = null) => ChatDM(con, username, (prefix ?? GetChatPrefix(con)) + message);
        public static void ChatDM(this IConnection con, string username, string message) => Chat(con, $"/pm {username} {message}");
        public static void GiveEdit(this IConnection con, string username) => Chat(con, $"/giveedit {username}");
        public static void TakeEdit(this IConnection con, string username) => Chat(con, $"/takeedit {username}");
        public static void SetEdit(this IConnection con, string username, bool hasEdit) => Chat(con, $"/{(hasEdit ? "give" : "take")}edit {username}");
        public static void GiveGod(this IConnection con, string username) => Chat(con, $"/givegod {username}");
        public static void TakeGod(this IConnection con, string username) => Chat(con, $"/takegod {username}");
        public static void SetGod(this IConnection con, string username, bool hasGod) => Chat(con, $"/{(hasGod ? "give" : "take")}god {username}");
        public static void ClearBackgroundColor(this IConnection con) => SetBackgroundColor(con, "none");
        public static int GetBackgroundColor(this IConnection con)//you can do .ToString("X6") to get hex code
        {
            var msg = ReceiveMessage(con, new Message(ConnectionScope.World, MessageType.Chat, "/bg"), (m) => m.Type == MessageType.ChatInfo && m.GetString(0).StartsWith("Background color is "));
            if (msg == null) throw new TimeoutException("did not receive response from server in time");
            var parts = msg.GetString(0).Split(' ');
            if (parts[3] == "none") return -1;
            var r = byte.Parse(parts[4].Substring(1));
            var g = byte.Parse(parts[5]);
            var b = byte.Parse(parts[6].Remove(parts[6].Length - 1, 1));
            return (r << 16 | g << 8 | b);
        }
        public static void SetBackgroundColor(this IConnection con, int rgb) => SetBackgroundColor(con, rgb.ToString("X6"));
        public static void SetBackgroundColor(this IConnection con, string hexcode) => con.Send(MessageType.Chat, "/bg " + hexcode);
        public static void Teleport(this IConnection con, string username, double x, double y) => SendL(con, MessageType.Chat, $"/tp {username} {x.ToString(c)} {y.ToString(c)}");
        public static void Teleport(this IConnection con, string username, int x, int y) => Teleport(con, username, (double)x, (double)y);
        public static void SetTitle(this IConnection con, string title) => SendL(con, MessageType.Chat, "/title " + title);
        public static void Save(this IConnection con) => Chat(con, "/save");
        public static void Clear(this IConnection con) => Chat(con, "/clear");
        public static void ResetAll(this IConnection con) => Chat(con, "/resetall");
        public static void ResetSelf(this IConnection con) => Chat(con, "/reset");
        public static void Reset(this IConnection con, string username) => Chat(con, "/reset " + username);
        public static void ClearEffects(this IConnection con, string username) => Chat(con, $"/effect {username} clear");
        public static void GiveEffect(this IConnection con, string username, EffectType effect, int config=1) => GiveEffect(con, username, (int)effect, config);
        public static void GiveEffect(this IConnection con, string username, int effect, int config = 1) => Chat(con, $"/effect {username} {effect} {config}");

        public enum WorldVisibility
        {//todo: should move this somewhere into WorldData?
            Public = 0,
            Unlisted = 1,
            Friends = 2,
            Private = 3,
        }
        public static WorldVisibility GetWorldVisibility(this IConnection con)
        {
            var msg = ReceiveMessage(con, new Message(ConnectionScope.World, MessageType.Chat, "/visibility"), (m) => m.Type == MessageType.ChatInfo && m.GetString(0).StartsWith("World is "));
            if (msg == null) throw new TimeoutException("did not receive response from server in time");
            switch (msg.GetString(0).Split(' ')[2])
            {
                case "public": return WorldVisibility.Public;
                case "friends": return WorldVisibility.Friends;
                case "unlisted": return WorldVisibility.Unlisted;
                case "private": return WorldVisibility.Private;
                default: throw new InvalidOperationException("server returned unknown visibility type");
            }
        }
        public static void SetWorldVisibility(this IConnection con, WorldVisibility visibility) => Chat(con, "/visibility " + (int)visibility);

        //is this messy? idk
        private static Message ReceiveMessage(IConnection con, Message send, Func<Message, bool> recv, int timeout = 5000)
        {
            using (var mre = new System.Threading.ManualResetEventSlim(false, 0))
            {
                Message ret = null;
                void onmsg(object o, Message m)
                {
                    if (recv(m))
                    {
                        if (ret == null)
                        {
                            ret = m;
                            con.OnMessage -= onmsg;
                            mre?.Set();
                        }
                    }
                }
                con.OnMessage += onmsg;
                con.Send(send);
                if (!mre.Wait(timeout))
                {
                    con.OnMessage -= onmsg;
                    return null;
                }
                else return ret;
            }
        }

        //todo: yes, this is shared across all potential instances of IConnection. maybe i should move all of this into a class you would instantiate for each IConnection?
        //or just use the hash thing from ChatPrefix? i have no idea
        private static readonly object _sendLock = new object();
        public static bool UseLocking { get; set; } = false;
        public static bool UseAsync { get; set; } = true;
        public static void SendL(this IConnection con, MessageType type, params object[] args)
        {
            if (UseAsync)
            {
                if (UseLocking) lock (_sendLock) con.SendAsync(type, args);
                else con.SendAsync(type, args);
            }
            else
            {
                if (UseLocking) lock (_sendLock) con.Send(type, args);
                else con.Send(type, args);
            }
        }
    }
}
