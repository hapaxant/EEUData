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
    public partial class RoomData
    {

        protected internal Dictionary<int, Zone> _zones;
        public IReadOnlyDictionary<int, Zone> Zones { get => _zones; }
        public static Dictionary<int, Zone> DeserializeZoneData(List<object> m, int width, int height, ref int index)
        {
            var zones = new Dictionary<int, Zone>();

            //var m = new Message(ConnectionScope.World, MessageType.Init, initData);
            var totalZoneCount = (int)m[index++];
            for (var i = 0; i < totalZoneCount; i++)
            {
                var zoneId = (int)m[index++];
                var zoneType = (int)m[index++];
                var startX = (int)m[index++];
                var startY = (int)m[index++];
                var endX = (int)m[index++];
                var endY = (int)m[index++];
                var zoneByteArray = (byte[])m[index++];
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

                zones.Add(zoneId, new Zone(zoneId, zoneType) { Map = zoneMap });
            }

            return zones;
        }
    }
}
