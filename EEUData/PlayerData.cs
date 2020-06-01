using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EEUniverse.Library;
using System.Diagnostics;

namespace EEUData
{
    [Flags]
    public enum MovementKeys : int
    {
        None = 0,
        Spacebar = 1,
        Up = 2,
        Down = 4,
        Left = 8,
        Right = 16,
    }
    public class Player
    {
        public Player(int id, string username)
        {
            this.Id = id;
            this.Username = username;

            this.Effects = new ConcurrentDictionary<EffectType, int>();
            this.Switches = new ConcurrentDictionary<int, bool>();
        }

        public int Id { get; set; }
        public string Username { get; set; }
        public int Smiley { get; set; }
        public double X { get; set; }
        public double Y { get; set; }
        //i think?
        //public bool IsJumping { get; set; }
        public MovementKeys Keys { get; set; }

        public bool IsBot { get; set; }

        public bool HasEdit { get; set; }
        public bool HasGod { get; set; }
        /// <summary>
        /// this doesnt work properly
        /// </summary>
        public bool IsOwner { get; set; }
        public int Role { get; set; }
        public double Time { get; set; }
        public bool Won { get; set; }
        public bool God { get; set; }
        public ConcurrentDictionary<EffectType, int> Effects { get; set; }
        public ConcurrentDictionary<int, bool> Switches { get; set; }
    }
    public enum EffectType : int
    {
        None = 0,
        MultiJump = 1,
        HighJump = 2,
    }
    public enum SmileyType : int
    {
        Happy = 0,
        Grinning = 1,
        Meh = 2,
        Sad = 3,
        Curious = 4,
        Angry = 5,
        Joyful = 6,
        Cheeky = 7,
    }
    public class PlayerData
    {
        public Player this[int index]
        {
            get { return Players[index]; }
            //internal protected set { Players[index] = value; }
        }
        public ConcurrentDictionary<int, Player> Players { get; protected set; } = new ConcurrentDictionary<int, Player>();
        public int BotId { get; protected set; }

        public int TimeOffset { get; protected set; }
        public int TimeSinceCreation { get; protected set; }

        protected virtual void ParseInit(Message m, out int index)
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

                    switch (foregroundId)
                    {
                        case (int)BlockId.SignWood:
                        case (int)BlockId.SignRed:
                        case (int)BlockId.SignGreen:
                        case (int)BlockId.SignBlue:
                            index += 2;
                            break;
                        case (int)BlockId.Portal:
                            index += 4;
                            break;
                        case (int)BlockId.EffectMultiJump:
                        case (int)BlockId.EffectHighJump:
                            index += 1;
                            break;
                        case (int)BlockId.SwitchesLocalSwitch:
                        case (int)BlockId.SwitchesLocalReset:
                            index += 1;
                            break;
                        case (int)BlockId.SwitchesLocalDoor:
                            index += 2;
                            break;
                    }
                }
            TimeOffset = m.GetInt(index++);
            var isOwner = m.GetBool(index++);
            var role = m.GetInt(index++);

            var id = BotId = m.GetInt(0);
            Players.Add(id, new Player(id, m.GetString(1)) { Smiley = /*(SmileyType)*/m.GetInt(2), X = m.GetDouble(4), Y = m.GetDouble(5), IsBot = true, IsOwner = isOwner, Role = role });
        }
        public virtual void Parse(Message m)
        {
            switch (m.Type)
            {
                case MessageType.Init:
                    ParseInit(m, out _);
                    break;
                case MessageType.PlayerAdd://existing players
                    {//todo (like anything else)
                        var id = m.GetInt(0);
                        Players.Add(id, new Player(id, m.GetString(1)) { X = m.GetDouble(7), Y = m.GetDouble(8), });
                    }
                    break;
                case MessageType.PlayerJoin://new joining players
                    {
                        var id = m.GetInt(0);
                        Players.Add(id, new Player(id, m.GetString(1))
                        {
                            Smiley = /*(SmileyType)*/m.GetInt(2),
                            Time = m.GetDouble(3),
                            X = m.GetDouble(4),
                            Y = m.GetDouble(5),
                            Keys = m.GetBool(6) ? MovementKeys.Spacebar : MovementKeys.None,
                            HasEdit = m.GetBool(7),
                            Role = m.GetInt(8),
                            HasGod = m.GetBool(9),
                            Won = m.GetBool(10),
                        });
                        break;
                    }
                case MessageType.PlayerExit:
                    Players.Remove(m.GetInt(0));
                    break;
                case MessageType.CanEdit:
                    Players[m.GetInt(0)].HasEdit = m.GetBool(1);
                    break;
                case MessageType.CanGod:
                    Players[m.GetInt(0)].HasGod = m.GetBool(1);
                    break;
                case MessageType.Effect:
                    {
                        var pid = m.GetInt(0);
                        var eid = m.GetInt(1);
                        var num = m.GetInt(2);
                        var p = Players[pid];
                        if (eid == (int)EffectType.None) p.Effects.Clear();
                        else if (num == 0) if (p.Effects.ContainsKey((EffectType)eid)) p.Effects.Remove((EffectType)eid); else { }
                        else p.Effects.AddOrUpdate((EffectType)eid, num, (_, __) => num);
                        break;
                    }
                case MessageType.Reset:
                    {
                        var p = Players[m.GetInt(0)];
                        p.Effects.Clear();
                        p.Time = m.GetDouble(1);
                        p.X = m.GetDouble(2);
                        p.Y = m.GetDouble(3);
                        p.Won = false;
                        break;
                    }
                case MessageType.Teleport:
                    {
                        var p = Players[m.GetInt(0)];
                        p.Time = m.GetDouble(1);
                        p.X = m.GetDouble(2);
                        p.Y = m.GetDouble(3);
                    }
                    break;
                case MessageType.PlayerMove:
                    {
                        // No
                        var p = Players[m.GetInt(0)];
                        p.Time = m.GetDouble(1) + m.GetDouble(2);
                        p.X = m.GetDouble(5);
                        p.Y = m.GetDouble(6);
                        p.God = m.GetBool(31);
                        // I Said No
                        var space = m.GetBool(29);
                        var xk = m.GetInt(3);
                        var yk = m.GetInt(4);
                        p.Keys = ((space ? MovementKeys.Spacebar : 0) |
                            (xk < 0 ? MovementKeys.Left : 0) | (xk > 0 ? MovementKeys.Right : 0) |
                            (yk < 0 ? MovementKeys.Up : 0) | (yk > 0 ? MovementKeys.Down : 0));
                        // Stop
                        break;
                    }
                case MessageType.PlayerSmiley:
                    Players[m.GetInt(0)].Smiley = m.GetInt(1);
                    break;
                case MessageType.Won:
                    Players[m.GetInt(0)].Won = true;
                    break;
                case MessageType.SwitchInfo:
                    {
                        var pid = m.GetInt(0);
                        var state = m.GetBool(1);
                        var index = 1;
                        while (++index < m.Count)
                        {
                            Players[pid].Switches[(int)m[index]] = state;
                        }
                        break;
                    }
            }
        }
    }
    public static class ConcurrentDictionaryExtension
    {
        public static void Add<TKey, TValue>(this ConcurrentDictionary<TKey, TValue> dict, TKey key, TValue value) => Trace.Assert(dict.TryAdd(key, value));
        public static void Remove<TKey, TValue>(this ConcurrentDictionary<TKey, TValue> dict, TKey key) => Trace.Assert(dict.TryRemove(key, out _));
    }
}
