using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EEUniverse.Library;

namespace EEUData
{
    public partial class RoomData
    {
        public string Title { get; protected set; }
        public string OwnerUsername { get; protected set; }
        /// <summary>
        /// -1: none
        /// </summary>
        public int BackgroundColor { get; protected set; }

        public int Width { get; protected set; }
        public int Height { get; protected set; }
    }
}
