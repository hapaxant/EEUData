using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace EEUniverse.Library
{
    /// <summary>
    /// Represents the type of a message.<para/>
    /// See https://luciferx.net/eeu/protocol for more info.
    /// </summary>
    public enum MessageType
    {
        /// <summary>
        /// https://luciferx.net/eeu/protocol#SelfInfo
        /// </summary>
        [Scope(ConnectionScope.None)] SelfInfo = 23,

        /// <summary>
        /// https://luciferx.net/eeu/protocol#Init
        /// </summary>
        [Scope(ConnectionScope.World)] Init = 0,
        /// <summary>
        /// https://luciferx.net/eeu/protocol#Ping
        /// </summary>
        [Scope(ConnectionScope.World)] Ping = 1,
        /// <summary>
        /// https://luciferx.net/eeu/protocol#Pong
        /// </summary>
        [Scope(ConnectionScope.World)] Pong = 2,
        /// <summary>
        /// https://luciferx.net/eeu/protocol#Chat
        /// </summary>
        [Scope(ConnectionScope.World)] Chat = 3,
        /// <summary>
        /// https://luciferx.net/eeu/protocol#ChatOld
        /// </summary>
        [Scope(ConnectionScope.World)] ChatOld = 4,
        /// <summary>
        /// https://luciferx.net/eeu/protocol#PlaceBlock
        /// </summary>
        [Scope(ConnectionScope.World)] PlaceBlock = 5,
        /// <summary>
        /// https://luciferx.net/eeu/protocol#PlayerJoin
        /// </summary>
        [Scope(ConnectionScope.World)] PlayerJoin = 6,
        /// <summary>
        /// https://luciferx.net/eeu/protocol#PlayerExit
        /// </summary>
        [Scope(ConnectionScope.World)] PlayerExit = 7,
        /// <summary>
        /// https://luciferx.net/eeu/protocol#PlayerMove
        /// </summary>
        [Scope(ConnectionScope.World)] PlayerMove = 8,
        /// <summary>
        /// https://luciferx.net/eeu/protocol#PlayerSmiley
        /// </summary>
        [Scope(ConnectionScope.World)] PlayerSmiley = 9,
        /// <summary>
        /// https://luciferx.net/eeu/protocol#PlayerGod
        /// </summary>
        [Scope(ConnectionScope.World)] PlayerGod = 10,
        /// <summary>
        /// https://luciferx.net/eeu/protocol#CanEdit
        /// </summary>
        [Scope(ConnectionScope.World)] CanEdit = 11,
        /// <summary>
        /// https://luciferx.net/eeu/protocol#Meta
        /// </summary>
        [Scope(ConnectionScope.World)] Meta = 12,
        /// <summary>
        /// https://luciferx.net/eeu/protocol#ChatInfo
        /// </summary>
        [Scope(ConnectionScope.World)] ChatInfo = 13,
        /// <summary>
        /// https://luciferx.net/eeu/protocol#PlayerAdd
        /// </summary>
        [Scope(ConnectionScope.World)] PlayerAdd = 14,
        /// <summary>
        /// https://luciferx.net/eeu/protocol#ZoneCreate
        /// </summary>
        [Scope(ConnectionScope.World)] ZoneCreate = 15,
        /// <summary>
        /// https://luciferx.net/eeu/protocol#ZoneDelete
        /// </summary>
        [Scope(ConnectionScope.World)] ZoneDelete = 16,
        /// <summary>
        /// https://luciferx.net/eeu/protocol#ZoneEdit
        /// </summary>
        [Scope(ConnectionScope.World)] ZoneEdit = 17,
        /// <summary>
        /// https://luciferx.net/eeu/protocol#ZoneEnter
        /// </summary>
        [Scope(ConnectionScope.World)] ZoneEnter = 18,
        /// <summary>
        /// https://luciferx.net/eeu/protocol#ZoneExit
        /// </summary>
        [Scope(ConnectionScope.World)] ZoneExit = 19,
        /// <summary>
        /// https://luciferx.net/eeu/protocol#LimitedEdit
        /// </summary>
        [Scope(ConnectionScope.World)] LimitedEdit = 20,
        /// <summary>
        /// https://luciferx.net/eeu/protocol#ChatPMTo
        /// </summary>
        [Scope(ConnectionScope.World)] ChatPMTo = 21,
        /// <summary>
        /// https://luciferx.net/eeu/protocol#ChatPMFrom
        /// </summary>
        [Scope(ConnectionScope.World)] ChatPMFrom = 22,
        /// <summary>
        /// https://luciferx.net/eeu/protocol#Clear
        /// </summary>
        [Scope(ConnectionScope.World)] Clear = 24,
        /// <summary>
        /// https://luciferx.net/eeu/protocol#CanGod
        /// </summary>
        [Scope(ConnectionScope.World)] CanGod = 25,
        /// <summary>
        /// https://luciferx.net/eeu/protocol#BgColor
        /// </summary>
        [Scope(ConnectionScope.World)] BgColor = 26,
        /// <summary>
        /// https://luciferx.net/eeu/protocol#Won
        /// </summary>
        [Scope(ConnectionScope.World)] Won = 27,
        /// <summary>
        /// https://luciferx.net/eeu/protocol#Reset
        /// </summary>
        [Scope(ConnectionScope.World)] Reset = 28,
        /// <summary>
        /// https://luciferx.net/eeu/protocol#Notify
        /// </summary>
        [Scope(ConnectionScope.World)] Notify = 29,
        /// <summary>
        /// https://luciferx.net/eeu/protocol#Teleport
        /// </summary>
        [Scope(ConnectionScope.World)] Teleport = 30,
        /// <summary>
        /// https://luciferx.net/eeu/protocol#Effect
        /// </summary>
        [Scope(ConnectionScope.World)] Effect = 31,
        /// <summary>
        /// https://luciferx.net/eeu/protocol#SwitchLocal
        /// </summary>
        [Scope(ConnectionScope.World)] SwitchLocal = 32,
        /// <summary>
        /// https://luciferx.net/eeu/protocol#SwitchGlobal
        /// </summary>
        [Scope(ConnectionScope.World)] SwitchGlobal = 33,
        /// <summary>
        /// https://luciferx.net/eeu/protocol#CoinGold
        /// </summary>
        [Scope(ConnectionScope.World)] CoinGold = 34,
        /// <summary>
        /// https://luciferx.net/eeu/protocol#CoinBlue
        /// </summary>
        [Scope(ConnectionScope.World)] CoinBlue = 35,

        //TODO: Should probably find a better way to implement these.
        //      Also don't know how accurate the names are.
        /// <summary>
        /// https://luciferx.net/eeu/protocol#RoomConnect
        /// </summary>
        [Scope(ConnectionScope.Lobby)] RoomConnect = 0,
        /// <summary>
        /// https://luciferx.net/eeu/protocol#RoomDisconnect
        /// </summary>
        [Scope(ConnectionScope.Lobby)] RoomDisconnect = 1,
        /// <summary>
        /// https://luciferx.net/eeu/protocol#LoadRooms
        /// </summary>
        [Scope(ConnectionScope.Lobby)] LoadRooms = 2,
        /// <summary>
        /// https://luciferx.net/eeu/protocol#LoadStats
        /// </summary>
        [Scope(ConnectionScope.Lobby)] LoadStats = 3,
    }

    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    internal sealed class ScopeAttribute : Attribute
    {
        public ConnectionScope Scope { get; }

        public ScopeAttribute(ConnectionScope scope) => Scope = scope;
    }

    public static class MessageTypeExtensions
    {
        private static ConnectionScope GetScope(FieldInfo field) => field.GetCustomAttribute<ScopeAttribute>().Scope;

        private static readonly Dictionary<(ConnectionScope scope, MessageType type), string> _names = typeof(MessageType)
            .GetFields()
            .Where(field => field.IsStatic)
            .ToDictionary(
                field => (GetScope(field), (MessageType)field.GetValue(null)),
                field => field.Name
            );

        /// <summary>
        /// Returns a string that represents the current message.
        /// </summary>
        /// <param name="messageType">The type of the message.</param>
        /// <param name="connectionScope">The scope of the message.</param>
        public static string ToString(this MessageType messageType, ConnectionScope connectionScope)
        {
            var key = (connectionScope, messageType);
            if (_names.ContainsKey(key))
                return _names[key];

            return ((int)messageType).ToString();
        }
    }
}
