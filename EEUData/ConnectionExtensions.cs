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
            if (prefix == null && connectionPrefixes.ContainsKey(hash)) connectionPrefixes.Remove(hash);
            else connectionPrefixes[con.GetHashCode()] = prefix;
        }
        public static string GetChatPrefix(this IConnection con)
        {
            var hash = con.GetHashCode();
            if (connectionPrefixes.ContainsKey(hash)) return connectionPrefixes[hash];
            else return DEFAULTPREFIX;
        }

        public static void PlaceBlock(this IConnection con, int layer, int x, int y, BlockId id, params object[] args) => SendL(con, MessageType.PlaceBlock, new object[] { layer, x, y, (int)id }.Concat(args).ToArray());
        public static void PlaceBlock(this IConnection con, int layer, int x, int y, int id, params object[] args) => SendL(con, MessageType.PlaceBlock, new object[] { layer, x, y, id }.Concat(args).ToArray());
        public static void PlaceBlock(this IConnection con, int l, int x, int y, Block block)
        {
            object[] args;
            switch (block)
            {
                case Switch b:
                    if (b.Inverted == null) args = new object[] { b.Value };
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
        public static void Save(this IConnection con) => Chat(con, "/save");
        public static void Init(this IConnection con) => SendL(con, MessageType.Init, 0);
        public static void Init(this IConnection con, int timeOffset) => SendL(con, MessageType.Init, timeOffset);


        private static readonly object _sendLock = new object();
        private static bool UseLocking { get; set; } = false;
        private static bool UseAsync { get; set; } = true;
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
