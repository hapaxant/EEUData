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

            this._effects = new Dictionary<EffectType, int>();
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

        public Rank Rank { get; set; }
        public bool Won { get; set; }
        protected internal Dictionary<EffectType, int> _effects;
        public IReadOnlyDictionary<EffectType, int> Effects { get => _effects; }
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

        public object Tag { get; set; }
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
    public partial class RoomData
    {
        private const int MAXSWITCHESCOUNT = 1000;
        public Player this[int index]
        {
            get { return Players[index]; }
        }
        public bool[] GlobalSwitches { get; protected set; }
        protected internal Dictionary<int, Player> _players = new Dictionary<int, Player>();
        public IReadOnlyDictionary<int, Player> Players { get => _players; }
        public int BotId { get; protected set; }

        public double TimeOffset { get; protected set; }
        public int TimeSinceCreation { get; protected set; }

        protected static MovementKeys GetKeys(int xk, int yk, bool space)
        {
            return ((space ? MovementKeys.Spacebar : 0) |
                (xk < 0 ? MovementKeys.Left : 0) | (xk > 0 ? MovementKeys.Right : 0) |
                (yk < 0 ? MovementKeys.Up : 0) | (yk > 0 ? MovementKeys.Down : 0));
        }
    }
}
