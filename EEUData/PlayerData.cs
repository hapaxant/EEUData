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
    public enum Rank : int
    {
        Member = -1,
        Owner = 0,
        Developer = 1,
        Moderator = 2,
        GraphicsDesigner = 3,
        Composer = 4,
        WorldCurator = 5,
    }
    public class Player
    {
        public Player(int id, string username)
        {
            this.Id = id;
            this.Username = username;

            this.Effects = new ConcurrentDictionary<EffectType, int>();
            //this.Effects = new int[EFFECTSCOUNT];
        }

        public int Id { get; set; }
        public string Username { get; set; }
        public int Smiley { get; set; }
        public double X { get; set; }
        public double Y { get; set; }
        public MovementKeys Keys { get; set; }
        public bool God { get; set; }

        public bool IsBot { get; set; }

        public bool HasEdit { get; set; }
        public bool HasGod { get; set; }

        //public bool IsOwner { get => Rank == Rank.Owner; }
        public Rank Rank { get; set; }
        //public double Time { get; set; }
        public bool Won { get; set; }
        public ConcurrentDictionary<EffectType, int> Effects { get; set; }
        //public int[] Effects { get; set; }
        public bool[] LocalSwitches { get; set; }

        #region physics crud
        public double PathLastTime { get; set; }
        public double PathCurrentTime { get; set; }
        public double VelocityX { get; set; }
        public double VelocityY { get; set; }
        public double AccelerationX { get; set; }
        public double AccelerationY { get; set; }
        public double Drag { get; set; }
        public object[] Edges { get; set; }
        public object[] Sliding { get; set; }
        public double ForceX { get; set; }
        public double ForceY { get; set; }
        public int GridX { get; set; }
        public int GridY { get; set; }
        public int Edge { get; set; }
        public double SpacePressedTime { get; set; }
        #endregion

        public int GoldCoins { get; set; }
        public int BlueCoins { get; set; }

        //private const int EFFECTSCOUNT = 2;
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
        private const int MAXSWITCHESCOUNT = 1000;
        public Player this[int index]
        {
            get { return Players[index]; }
            //internal protected set { Players[index] = value; }
        }
        public bool[] GlobalSwitches { get; protected set; }
        public ConcurrentDictionary<int, Player> Players { get; protected set; } = new ConcurrentDictionary<int, Player>();
        public int BotId { get; protected set; }

        public double TimeOffset { get; protected set; }
        public int TimeSinceCreation { get; protected set; }

        protected virtual void ParseInit(Message m, out int index)
        {
            if (m.Type != MessageType.Init) throw new ArgumentException("message type is not init");
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
            this.TimeOffset = m.GetDouble(index++);
            var canEdit = m.GetBool(index++);
            var rank = m.GetInt(index++);

            var id = BotId = m.GetInt(0);
            Players.Add(id, new Player(id, m.GetString(1))
            {
                Smiley = m.GetInt(2),
                X = m.GetDouble(4),
                Y = m.GetDouble(5),
                IsBot = true,
                HasEdit = canEdit,
                Rank = (Rank)rank
            });
        }
        public virtual void Parse(Message m)
        {
            switch (m.Type)
            {
                case MessageType.Init:
                    this.GlobalSwitches = new bool[MAXSWITCHESCOUNT];
                    ParseInit(m, out _);
                    break;
                case MessageType.PlayerJoin:
                    {
                        var id = m.GetInt(0);
                        Players.Add(id, new Player(id, m.GetString(1))
                        {
                            LocalSwitches = new bool[MAXSWITCHESCOUNT],
                            Smiley = m.GetInt(2),
                            //Time = m.GetDouble(3),
                            PathLastTime = m.GetDouble(3),//docs say creation timestamp, but it's not mentioned anywhere else? (besides init) //todo fix
                            X = m.GetDouble(4),
                            Y = m.GetDouble(5),
                            God = m.GetBool(6),
                            HasEdit = m.GetBool(7),
                            Rank = (Rank)m.GetInt(8),
                            HasGod = m.GetBool(9),
                            Won = m.GetBool(10),//how do you join a world already won anyway? is the win status gonna persist at some point?
                        });
                        break;
                    }
                case MessageType.PlayerExit:
                    Players.Remove(m.GetInt(0));
                    break;
                case MessageType.PlayerMove:
                    {
                        var p = Players[m.GetInt(0)];

                        p.PathLastTime = m.GetDouble(1);
                        p.PathCurrentTime = m.GetDouble(2);
                        p.Keys = GetKeys(m.GetInt(3), m.GetInt(4), m.GetBool(29));
                        p.X = m.GetDouble(5);
                        p.Y = m.GetDouble(6);
                        p.VelocityX = m.GetDouble(7);
                        p.VelocityY = m.GetDouble(8);
                        p.AccelerationX = m.GetDouble(9);
                        p.AccelerationY = m.GetDouble(10);
                        p.Drag = m.GetDouble(11);
                        p.Edges = new object[] { m.GetDouble(12), m.GetDouble(13), m.GetDouble(14), m.GetDouble(15), m.GetInt(16), m.GetInt(17) };
                        p.Sliding = new object[] { m.GetDouble(18), m.GetDouble(19), m.GetDouble(20), m.GetDouble(21), m.GetInt(22), m.GetInt(23) };
                        p.ForceX = m.GetDouble(24);
                        p.ForceY = m.GetDouble(25);
                        p.GridX = m.GetInt(26);
                        p.GridY = m.GetInt(27);
                        p.Edge = m.GetInt(28);
                        p.SpacePressedTime = m.GetDouble(30);
                        p.God = m.GetBool(31);

                        break;
                    }
                case MessageType.PlayerSmiley:
                    Players[m.GetInt(0)].Smiley = m.GetInt(1);
                    break;
                case MessageType.PlayerGod:
                    Players[m.GetInt(0)].God = m.GetBool(1);
                    break;
                case MessageType.CanEdit:
                    Players[m.GetInt(0)].HasEdit = m.GetBool(1);
                    break;
                case MessageType.PlayerAdd:
                    {
                        var id = m.GetInt(0);
                        Players.Add(id, new Player(id, m.GetString(1))
                        {//big big chungus big chungus big chungus
                            LocalSwitches = new bool[MAXSWITCHESCOUNT],
                            Smiley = m.GetInt(2),
                            PathLastTime = m.GetDouble(3),
                            PathCurrentTime = m.GetDouble(4),
                            Keys = GetKeys(m.GetInt(5), m.GetInt(6), m.GetBool(31)),
                            X = m.GetDouble(7),
                            Y = m.GetDouble(8),
                            VelocityX = m.GetDouble(9),
                            VelocityY = m.GetDouble(10),
                            AccelerationX = m.GetDouble(11),
                            AccelerationY = m.GetDouble(12),
                            Drag = m.GetDouble(13),
                            Edges = new object[] { m.GetDouble(14), m.GetDouble(15), m.GetDouble(16), m.GetDouble(17), m.GetInt(18), m.GetInt(19) },
                            Sliding = new object[] { m.GetDouble(20), m.GetDouble(21), m.GetDouble(22), m.GetDouble(23), m.GetInt(24), m.GetInt(25) },
                            ForceX = m.GetDouble(26),
                            ForceY = m.GetDouble(27),
                            GridX = m.GetInt(28),
                            GridY = m.GetInt(29),
                            Edge = m.GetInt(30),
                            //(31) parsed above
                            SpacePressedTime = m.GetDouble(32),
                            God = m.GetBool(33),
                            HasEdit = m.GetBool(34),
                            Rank = (Rank)m.GetInt(35),
                            HasGod = m.GetBool(36),
                            Won = m.GetBool(37),
                        });
                    }
                    break;
                case MessageType.CanGod:
                    Players[m.GetInt(0)].HasGod = m.GetBool(1);
                    break;
                case MessageType.Won:
                    Players[m.GetInt(0)].Won = true;
                    break;
                case MessageType.Reset:
                    {
                        var p = Players[m.GetInt(0)];
                        p.Effects.Clear();
                        p.PathLastTime = m.GetDouble(1);//todo: ?
                        p.X = m.GetInt(2);
                        p.Y = m.GetInt(3);
                        p.Won = false;
                        p.God = false;
                        if (p.LocalSwitches != null) p.LocalSwitches = new bool[MAXSWITCHESCOUNT];
                        break;
                    }
                case MessageType.Teleport:
                    {
                        var p = Players[m.GetInt(0)];
                        p.PathLastTime = m.GetDouble(1);//todo: ?
                        p.X = m.GetDouble(2);
                        p.Y = m.GetDouble(3);
                        break;
                    }
                case MessageType.Effect:
                    {
                        var pid = m.GetInt(0);
                        var eid = m.GetInt(1);
                        var num = m.GetInt(2);
                        var p = Players[pid];
                        if (eid == (int)EffectType.None) p.Effects.Clear();
                        else if (num == 0) if (p.Effects.ContainsKey((EffectType)eid)) p.Effects.Remove((EffectType)eid); else { }
                        else p.Effects[(EffectType)eid] = num;
                        //else p.Effects.AddOrUpdate((EffectType)eid, num, (_, __) => num);
                        break;
                    }
                case MessageType.SwitchLocal:
                    {
                        var pid = (int)m[0];
                        var p = Players[pid];
                        var state = (bool)m[1];
                        if ((int)m[2] == -1)
                        {
                            for (int i = 0; i < MAXSWITCHESCOUNT; i++)
                            {
                                p.LocalSwitches[i] = state;
                            }
                        }
                        else
                        {
                            var index = 1;
                            while (++index < m.Count)
                            {
                                var c = (int)m[index];
                                p.LocalSwitches[c] = state;
                            }
                        }
                        break;
                    }
                case MessageType.SwitchGlobal:
                    {
                        var state = (bool)m[0];
                        if ((int)m[1] == -1)
                        {
                            for (int i = 0; i < MAXSWITCHESCOUNT; i++)
                            {
                                this.GlobalSwitches[i] = state;
                            }
                        }
                        else
                        {
                            var index = 0;
                            while (++index < m.Count)
                            {
                                var c = (int)m[index];
                                this.GlobalSwitches[c] = state;
                            }
                        }
                        break;
                    }
                case MessageType.CoinGold:
                    {
                        var p = Players[m.GetInt(0)];
                        p.GoldCoins = m.GetInt(1);
                        break;
                    }
                case MessageType.CoinBlue:
                    {
                        var p = Players[m.GetInt(0)];
                        p.BlueCoins = m.GetInt(1);
                        break;
                    }
            }
        }

        private static MovementKeys GetKeys(int xk, int yk, bool space)
        {
            return ((space ? MovementKeys.Spacebar : 0) |
                (xk < 0 ? MovementKeys.Left : 0) | (xk > 0 ? MovementKeys.Right : 0) |
                (yk < 0 ? MovementKeys.Up : 0) | (yk > 0 ? MovementKeys.Down : 0));
        }
    }
    public static class ConcurrentDictionaryExtension
    {
        public static void Add<TKey, TValue>(this ConcurrentDictionary<TKey, TValue> dict, TKey key, TValue value) => Trace.Assert(dict.TryAdd(key, value));
        public static void Remove<TKey, TValue>(this ConcurrentDictionary<TKey, TValue> dict, TKey key) => Trace.Assert(dict.TryRemove(key, out _));
    }
}
