using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EEUniverse.Library;

namespace EEUData
{
    public partial class WorldData
    {
        public string Title { get; protected set; }
        public string OwnerUsername { get; protected set; }
        /// <summary>
        /// -1: none
        /// </summary>
        public int BackgroundColor { get; protected set; }

        public int Width { get; protected set; }
        public int Height { get; protected set; }

        protected virtual void ParseInit(Message m, bool deserializeStuff = true)
        {
            if (m.Type != MessageType.Init) throw new ArgumentException("message is not of init type", nameof(m));
            Title = m.GetString(6);
            OwnerUsername = m.GetString(7);
            BackgroundColor = m.GetInt(8);
            Width = m.GetInt(9);
            Height = m.GetInt(10);

            if (deserializeStuff)
            {
                var index = 11;
                var mlist = m.Data.ToList();
                this.Blocks = DeserializeBlockData(mlist, this.Width, this.Height, ref index);
                index++;//timeOffset
                index++;//isOwner
                index++;//role
                //usernameColor (custom)
                this.Zones = DeserializeZoneData(mlist, this.Width, this.Height, ref index);
            }
        }
        public virtual void Parse(Message m)
        {
            switch (m.Type)
            {
                case MessageType.Init:
                    ParseInit(m);
                    break;
                case MessageType.Clear:
                    System.Diagnostics.Trace.Assert(m.Count == 0);
                    HandleClear();
                    Zones.Clear();
                    break;
                case MessageType.BgColor://[0] = int
                    System.Diagnostics.Trace.Assert(m.Count == 1);
                    BackgroundColor = m.GetInt(0);
                    break;
                case MessageType.ZoneCreate://1, 0 | 2, 1
                    {
                        System.Diagnostics.Trace.Assert(m.Count == 2);
                        var id = m.GetInt(0);
                        var type = m.GetInt(1);
                        Zones.Add(id, new Zone(id, type) { Map = new bool[Width, Height] });
                        break;
                    }
                case MessageType.ZoneEdit:
                    {
                        System.Diagnostics.Trace.Assert(m.Count == 6);
                        var id = m.GetInt(0);
                        var mode = m.GetBool(1);
                        int x = m.GetInt(2), y = m.GetInt(3), w = m.GetInt(4), h = m.GetInt(5);
                        for (int xx = x; xx < w + x; xx++)
                            for (int yy = y; yy < h + y; yy++)
                            {
                                Zones[id].Map[xx, yy] = mode;
                            }
                    }
                    break;
                case MessageType.ZoneDelete:
                    System.Diagnostics.Trace.Assert(m.Count == 1);
                    Zones.Remove(m.GetInt(0));
                    break;
                case MessageType.PlaceBlock:
                    {
                        int pid = m.GetInt(0), l = m.GetInt(1), x = m.GetInt(2), y = m.GetInt(3), id = m.GetInt(4);
                        switch ((BlockId)id)
                        {
                            case BlockId.SignWood:
                            case BlockId.SignRed:
                            case BlockId.SignGreen:
                            case BlockId.SignBlue:
                                Blocks[l, x, y] = new Sign(id, m.GetString(5), m.GetInt(6), pid);
                                break;

                            case BlockId.Portal:
                                Blocks[l, x, y] = new Portal(id, m.GetInt(5), m.GetInt(6), m.GetInt(7), m.GetBool(8), pid);
                                break;

                            case BlockId.EffectClear:
                            case BlockId.EffectMultiJump:
                            case BlockId.EffectHighJump:
                                Blocks[l, x, y] = new Effect(id, (id != (int)BlockId.EffectClear) ? m.GetInt(5) : 0, pid);
                                break;

                            default:
                                Blocks[l, x, y] = new Block(id, pid);
                                break;
                        }
                        break;
                    }
            }
        }
    }
}
