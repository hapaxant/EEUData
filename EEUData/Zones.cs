using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EEUniverse.Library;

namespace EEUData
{
    public enum ZoneType
    {
        Edit = 0,
        Vision = 1,
    }
    public class Zone
    {
        //public Zone() { }
        public Zone(int id, int type) : this(id, (ZoneType)type) { }
        public Zone(int id, ZoneType type) { this.Id = id; this.Type = type; }

        public int Id { get; set; }
        public ZoneType Type { get; set; }

        public bool[,] Map { get; set; }
    }
    public partial class WorldData
    {
        public ConcurrentDictionary<int, Zone> Zones { get; protected set; }
        public static ConcurrentDictionary<int, Zone> DeserializeZoneData(List<object> initData, int width, int height, ref int index)
        {
            var zones = new Dictionary<int, Zone>();

            var m = new Message(ConnectionScope.World, MessageType.Init, initData);
            var totalZoneCount = m.GetInt(index++);
            for (var i = 0; i < totalZoneCount; i++)
            {
                var zoneId = m.GetInt(index++);
                var zoneType = m.GetInt(index++);
                var startX = m.GetInt(index++);
                var startY = m.GetInt(index++);
                var endX = m.GetInt(index++);
                var endY = m.GetInt(index++);
                var zoneByteArray = m.GetBytes(index++);
                var zoneMap = new bool[width, height];

                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        if (startX <= x && x < endX && startY <= y && y < endY)
                        {
                            var te = (y - startY) * (endX - startX) + (x - startX);
                            var ee = (int)Math.Floor(te / 8d);
                            var ie = te % 8;

                            zoneMap[x, y] = Convert.ToBoolean(zoneByteArray[ee] & 1 << ie);
                        }
                        else zoneMap[x, y] = false;
                    }
                }

                zones.Add(zoneId, new Zone(zoneId, zoneType)
                {
                    //Id = zoneId,
                    //Type = (ZoneType)zoneType,
                    Map = zoneMap
                });
            }

            return new ConcurrentDictionary<int, Zone>(zones);
        }
    }
}
