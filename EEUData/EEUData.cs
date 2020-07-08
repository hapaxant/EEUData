using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EEUniverse.Library;

namespace EEUData
{
    public partial class RoomData
    {
        /// <summary>
        /// if one bool is set to false, then related properties may not get updated.
        /// </summary>
        public RoomData(bool parseWorldData = true, bool parsePlayerData = true)
        {
            if (!parseWorldData && !parsePlayerData) throw new ArgumentException("cannot set both bools to false");
            this.ParseWorldData = parseWorldData;
            this.ParsePlayerData = parsePlayerData;
        }
        public bool ParseWorldData { get; protected set; }
        public bool ParsePlayerData { get; protected set; }

        protected virtual void ParseInit(Message m, out int index, bool deserializeStuff = true)
        {
            if (m.Type != MessageType.Init) throw new ArgumentException("message is not of init type", nameof(m));
            Title = m.GetString(6);
            OwnerUsername = m.GetString(7);
            BackgroundColor = m.GetInt(8);
            Width = m.GetInt(9);
            Height = m.GetInt(10);

            index = 11;
            if (deserializeStuff)
            {
                var mlist = m.Data;
                this.Blocks = DeserializeBlockData(mlist, this.Width, this.Height, ref index);

                this.TimeOffset = m.GetDouble(index++);
                var canEdit = m.GetBool(index++);
                var rank = m.GetInt(index++);

                var id = BotId = m.GetInt(0);
                _players.Add(id, new Player(id, m.GetString(1))
                {
                    Smiley = m.GetInt(2),
                    X = m.GetDouble(4),
                    Y = m.GetDouble(5),
                    IsBot = true,
                    HasEdit = canEdit,
                    Rank = (Rank)rank
                });

                this._zones = new Dictionary<int, Zone>(DeserializeZoneData(mlist, this.Width, this.Height, ref index));
            }
        }
        public virtual void Parse(Message m)
        {
            if (m.Type == MessageType.Init)
            {
                this.GlobalSwitches = new bool[MAXSWITCHESCOUNT];
                ParseInit(m, out var _, ParseWorldData || ParsePlayerData);
            }
            if (ParseWorldData) switch (m.Type)
                {
                    case MessageType.PlaceBlock:
                        {
                            int l = m.GetInt(1), x = m.GetInt(2), y = m.GetInt(3), id = m.GetInt(4);
                            Blocks[l, x, y] = HandleBlock(m);
                            break;
                        }
                    case MessageType.Meta:
                        Title = m.GetString(0);
                        break;
                    case MessageType.ZoneCreate:
                        {
                            System.Diagnostics.Trace.Assert(m.Count == 2);
                            var id = m.GetInt(0);
                            var type = m.GetInt(1);
                            _zones.Add(id, new Zone(id, type) { Map = new bool[Width, Height] });
                            break;
                        }
                    case MessageType.ZoneDelete:
                        _zones.Remove(m.GetInt(0));
                        break;
                    case MessageType.ZoneEdit:
                        {
                            System.Diagnostics.Trace.Assert(m.Count == 6);
                            var id = m.GetInt(0);
                            var mode = m.GetBool(1);
                            int x = m.GetInt(2), y = m.GetInt(3), w = m.GetInt(4), h = m.GetInt(5);
                            for (int xx = x; xx < w + x; xx++)
                                for (int yy = y; yy < h + y; yy++)
                                {
                                    _zones[id].Map[xx, yy] = mode;
                                }
                            break;
                        }
                    case MessageType.Clear:
                        HandleClear();
                        _zones.Clear();
                        break;
                    case MessageType.BgColor:
                        BackgroundColor = m.GetInt(0);
                        break;
                }
            if (ParsePlayerData) switch (m.Type)
                {
                    case MessageType.PlayerJoin:
                        {
                            var id = m.GetInt(0);
                            _players.Add(id, new Player(id, m.GetString(1))
                            {
                                LocalSwitches = new bool[MAXSWITCHESCOUNT],
                                Smiley = m.GetInt(2),
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
                        _players.Remove(m.GetInt(0));
                        break;
                    case MessageType.PlayerMove:
                        {
                            if (!_players.ContainsKey(m.GetInt(0))) break;//believe it or not, this check is necessary
                            var p = _players[m.GetInt(0)];

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
                            _players.Add(id, new Player(id, m.GetString(1))
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
                            p._effects.Clear();
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
                            if (eid == (int)EffectType.None) p._effects.Clear();
                            else if (num == 0) if (p.Effects.ContainsKey((EffectType)eid)) p._effects.Remove((EffectType)eid); else { }
                            else p._effects[(EffectType)eid] = num;
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
    }
}
