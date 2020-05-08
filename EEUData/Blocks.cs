using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EEUData
{
    public enum BlockId : ushort
    {
        #region fg
        //gravity
        Empty = 0,
        GravityLeft = 13,
        GravityUp = 14,
        GravityRight = 15,
        GravityNone = 16,
        GravitySlow = 71,
        //basic
        BasicWhite = 1,
        BasicGrey = 2,
        BasicBlack = 3,
        BasicRed = 4,
        BasicOrange = 5,
        BasicYellow = 6,
        BasicGreen = 7,
        BasicCyan = 8,
        BasicBlue = 9,
        BasicPurple = 10,
        //stone
        StoneWhite = 18,
        StoneGrey = 19,
        StoneBlack = 20,
        StoneRed = 21,
        StoneOrange = 22,
        StoneYellow = 23,
        StoneGreen = 24,
        StoneCyan = 25,
        StoneBlue = 26,
        StonePurple = 27,
        //beveled
        BeveledWhite = 28,
        BeveledGrey = 29,
        BeveledBlack = 30,
        BeveledRed = 31,
        BeveledOrange = 32,
        BeveledYellow = 33,
        BeveledGreen = 34,
        BeveledCyan = 35,
        BeveledBlue = 36,
        BeveledPurple = 37,
        //metal
        MetalSilver = 38,
        MetalSteel = 39,
        MetalIron = 40,
        MetalGold = 41,
        MetalBronze = 42,
        MetalCopper = 43,
        //glass
        GlassWhite = 45,
        GlassBlack = 46,
        GlassRed = 47,
        GlassOrange = 48,
        GlassYellow = 49,
        GlassGreen = 50,
        GlassCyan = 51,
        GlassBlue = 52,
        GlassPurple = 53,
        GlassPink = 54,
        //tiles
        TilesWhite = 72,
        TilesGrey = 73,
        TilesBlack = 74,
        TilesRed = 75,
        TilesOrange = 76,
        TilesYellow = 77,
        TilesGreen = 78,
        TilesCyan = 79,
        TilesBlue = 80,
        TilesPurple = 81,
        //special
        Black = 12,
        Secret = 95,
        Clear = 96,
        //signs
        SignWood = 55,
        SignRed = 56,
        SignGreen = 57,
        SignBlue = 58,
        //coins
        CoinGold = 11,
        CoinBlue = 97,
        //control
        Spawn = 44,
        Godmode = 17,
        Crown = 70,
        Portal = 59,
        //actions(effects)
        EffectClear = 92,
        EffectMultiJump = 93,
        EffectHighJump = 94,
        #endregion
        #region bg
        //basic bg
        BgBasicWhite = 60,
        BgBasicGrey = 61,
        BgBasicBlack = 62,
        BgBasicRed = 63,
        BgBasicOrange = 64,
        BgBasicYellow = 65,
        BgBasicGreen = 66,
        BgBasicCyan = 67,
        BgBasicBlue = 68,
        BgBasicPurple = 69,
        //tiles bg
        BgTilesWhite = 82,
        BgTilesGrey = 83,
        BgTilesBlack = 84,
        BgTilesRed = 85,
        BgTilesOrange = 86,
        BgTilesYellow = 87,
        BgTilesGreen = 88,
        BgTilesCyan = 89,
        BgTilesBlue = 90,
        BgTilesPurple = 91,
        #endregion
    }

    public partial class WorldData
    {
        public Block this[int l, int x, int y]
        {
            get { return Blocks[l, x, y]; }
            set { Blocks[l, x, y] = value; }
        }

        public Block[,,] Blocks { get; protected set; }

        protected internal virtual void HandleClear()
        {
            int w = Width, h = Height;
            var b = new Block[2, w, h];
            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    b[0, x, y] = new Block(BlockId.Empty);
                    b[1, x, y] = new Block(x == 1 && y == 1 ? BlockId.Spawn :
                                           (y == 0 || y == (h - 1)) || (x == 0 || x == (w - 1)) ? BlockId.BasicGrey : BlockId.Empty);
                }
            }
            Blocks = b;
            //for (int x = 0; x < Width; x++)
            //{
            //    Blocks[0, x, 0] = new Block(BlockId.Empty);
            //    Blocks[0, x, Height - 1] = new Block(BlockId.Empty);
            //    Blocks[1, x, 0] = new Block(BlockId.BasicGrey);
            //    Blocks[1, x, Height - 1] = new Block(BlockId.BasicGrey);
            //}
            //for (int y = 1; y < Height - 1; y++)
            //{
            //    Blocks[0, 0, y] = new Block(BlockId.Empty);
            //    Blocks[0, Width - 1, y] = new Block(BlockId.Empty);
            //    Blocks[1, 0, y] = new Block(BlockId.BasicGrey);
            //    Blocks[1, Width - 1, y] = new Block(BlockId.BasicGrey);
            //}
            //Blocks[1, 1, 1] = new Block(BlockId.Spawn);
        }


        /// <summary>
        /// -2 = invisible like black block
        /// -1 = transparent like coin
        /// </summary>
        public static readonly Dictionary<ushort, int> BlockColors = new Dictionary<ushort, int>()
        {
            //gravity
            { (ushort)BlockId.Empty, -1 },
            { (ushort)BlockId.GravityLeft, -1 },
            { (ushort)BlockId.GravityUp, -1 },
            { (ushort)BlockId.GravityRight, -1 },
            { (ushort)BlockId.GravityNone, -1 },
            { (ushort)BlockId.GravitySlow, -1 },
            //basic
            { (ushort)BlockId.BasicWhite, 11842740 },
            { (ushort)BlockId.BasicGrey, 7368816 },
            { (ushort)BlockId.BasicBlack, 3421236 },
            { (ushort)BlockId.BasicRed, 11678012 },
            { (ushort)BlockId.BasicOrange, 12216104 },
            { (ushort)BlockId.BasicYellow, 11641905 },
            { (ushort)BlockId.BasicGreen, 3975215 },
            { (ushort)BlockId.BasicCyan, 3775669 },
            { (ushort)BlockId.BasicBlue, 3363761 },
            { (ushort)BlockId.BasicPurple, 10171570 },
            //stone
            { (ushort)BlockId.StoneWhite, 10131601 },
            { (ushort)BlockId.StoneGrey, 5789779 },
            { (ushort)BlockId.StoneBlack, 3157805 },
            { (ushort)BlockId.StoneRed, 7678252 },
            { (ushort)BlockId.StoneOrange, 8538912 },
            { (ushort)BlockId.StoneYellow, 7498280 },
            { (ushort)BlockId.StoneGreen, 4224037 },
            { (ushort)BlockId.StoneCyan, 2914915 },
            { (ushort)BlockId.StoneBlue, 3096436 },
            { (ushort)BlockId.StonePurple, 5515637 },
            //beveled
            { (ushort)BlockId.BeveledWhite, 10921638 },
            { (ushort)BlockId.BeveledGrey, 6908265 },
            { (ushort)BlockId.BeveledBlack, 3815994 },
            { (ushort)BlockId.BeveledRed, 11941933 },
            { (ushort)BlockId.BeveledOrange, 11954222 },
            { (ushort)BlockId.BeveledYellow, 11902507 },
            { (ushort)BlockId.BeveledGreen, 3257164 },
            { (ushort)BlockId.BeveledCyan, 3252917 },
            { (ushort)BlockId.BeveledBlue, 3494586 },
            { (ushort)BlockId.BeveledPurple, 11481269 },
            //metal
            { (ushort)BlockId.MetalSilver, 12303292 },
            { (ushort)BlockId.MetalSteel, 9211279 },
            { (ushort)BlockId.MetalIron, 5658714 },
            { (ushort)BlockId.MetalGold, 14529619 },
            { (ushort)BlockId.MetalBronze, 14454605 },
            { (ushort)BlockId.MetalCopper, 13134671 },
            //glass
            { (ushort)BlockId.GlassWhite, 14211288 },
            { (ushort)BlockId.GlassBlack, 5329233 },
            { (ushort)BlockId.GlassRed, 15305358 },
            { (ushort)BlockId.GlassOrange, 15250569 },
            { (ushort)BlockId.GlassYellow, 15258761 },
            { (ushort)BlockId.GlassGreen, 9037973 },
            { (ushort)BlockId.GlassCyan, 9096681 },
            { (ushort)BlockId.GlassBlue, 8687847 },
            { (ushort)BlockId.GlassPurple, 11897320 },
            { (ushort)BlockId.GlassPink, 15043305 },
            //tiles
            { (ushort)BlockId.TilesWhite, 12170655 },
            { (ushort)BlockId.TilesGrey, 9605517 },
            { (ushort)BlockId.TilesBlack, 6908004 },
            { (ushort)BlockId.TilesRed, 11431789 },
            { (ushort)BlockId.TilesOrange, 11437421 },
            { (ushort)BlockId.TilesYellow, 11443054 },
            { (ushort)BlockId.TilesGreen, 8365941 },
            { (ushort)BlockId.TilesCyan, 7906971 },
            { (ushort)BlockId.TilesBlue, 7834277 },
            { (ushort)BlockId.TilesPurple, 9206184 },
            //bg basic
            { (ushort)BlockId.BgBasicWhite, 7566195},
            { (ushort)BlockId.BgBasicGrey, 4210752 },
            { (ushort)BlockId.BgBasicBlack, 657930 },
            { (ushort)BlockId.BgBasicRed, 6691364 },
            { (ushort)BlockId.BgBasicOrange, 6696474 },
            { (ushort)BlockId.BgBasicYellow, 6706202 },
            { (ushort)BlockId.BgBasicGreen, 2909722 },
            { (ushort)BlockId.BgBasicCyan, 1728102 },
            { (ushort)BlockId.BgBasicBlue, 1718374 },
            { (ushort)BlockId.BgBasicPurple, 5184102 },
            //bg tiles
            { (ushort)BlockId.BgTilesWhite, 6578517 },
            { (ushort)BlockId.BgTilesGrey, 4605510 },
            { (ushort)BlockId.BgTilesBlack, 2763306 },
            { (ushort)BlockId.BgTilesRed, 5123885 },
            { (ushort)BlockId.BgTilesOrange, 5127469 },
            { (ushort)BlockId.BgTilesYellow, 5130285 },
            { (ushort)BlockId.BgTilesGreen, 3361586 },
            { (ushort)BlockId.BgTilesCyan, 2969165 },
            { (ushort)BlockId.BgTilesBlue, 2965071 },
            { (ushort)BlockId.BgTilesPurple, 4339544 },
            //specials
            { (ushort)BlockId.Black, -2 },
            { (ushort)BlockId.Secret, -2 },
            { (ushort)BlockId.Clear, -1 },
            //signs
            { (ushort)BlockId.SignWood, -1 },
            { (ushort)BlockId.SignRed, -1 },
            { (ushort)BlockId.SignGreen, -1 },
            { (ushort)BlockId.SignBlue, -1 },
            //coins
            { (ushort)BlockId.CoinGold, -1 },
            { (ushort)BlockId.CoinBlue, -1 },
            //actions
            { (ushort)BlockId.Spawn, -1 },
            { (ushort)BlockId.Godmode, -1 },
            { (ushort)BlockId.Crown, -1 },
            { (ushort)BlockId.Portal, -1 },
            //effects
            { (ushort)BlockId.EffectClear, -1 },
            { (ushort)BlockId.EffectMultiJump, -1 },
            { (ushort)BlockId.EffectHighJump, -1 },
        };
        //public static int FromBlockColorToArgb(int blockcolor) => unchecked((int)0xff000000) | (blockcolor - int.MinValue);
        public static int FromBlockColorToArgb(int blockcolor)
        {
            unchecked
            {
                return (blockcolor >= 0) ? ((int)0xff000000 | blockcolor) : ((blockcolor == -1) ? 0 : 1);
            }
            //if (blockcolor >= 0)
            //    return unchecked((int)0xff000000 | blockcolor);
            //else return blockcolor == -1 ? 0 : 1;
        }
        public static int FromArgbToBlockColor(int argb)
        {
            unchecked
            {
                if ((argb & (int)0xff000000) != (int)0xff000000) throw new ArgumentOutOfRangeException("bits 0xff000000 need to be set.");
                return (argb < 0 || argb > 1) ? (int)0x00ffffff & argb : argb == 0 ? -1 : -2;
            }
        }
        /// <summary>
        /// Block[layer,x,y]
        /// </summary>
        public static Block[,,] DeserializeBlockData(List<object> m, int width, int height, ref int index)
        {
            var blocks = new Block[2, width, height];

            for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++)
                {
                    int value = 0;
                    if (m[index++] is int iValue)
                        value = iValue;

                    var backgroundId = value >> 16;
                    var foregroundId = 65535 & value;

                    blocks[0, x, y] = new Block(backgroundId);
                    switch (foregroundId)
                    {
                        case (int)BlockId.SignWood:
                        case (int)BlockId.SignRed:
                        case (int)BlockId.SignGreen:
                        case (int)BlockId.SignBlue:
                            {
                                string text = (string)m[index++];
                                int morph = (int)m[index++];
                                blocks[1, x, y] = new Sign(foregroundId, text, morph);
                                break;
                            }

                        case (int)BlockId.Portal:
                            {
                                int rotation = (int)m[index++];
                                int p_id = (int)m[index++];
                                int t_id = (int)m[index++];
                                bool flip = (bool)m[index++];
                                blocks[1, x, y] = new Portal(foregroundId, rotation, p_id, t_id, flip);
                                break;
                            }

                        case (int)BlockId.EffectClear:
                        case (int)BlockId.EffectMultiJump:
                        case (int)BlockId.EffectHighJump:
                            {
                                int r = (foregroundId == (int)BlockId.EffectClear) ? 0 : (int)m[index++];
                                blocks[1, x, y] = new Effect(foregroundId, r);
                                break;
                            }

                        default: blocks[1, x, y] = new Block(foregroundId); break;
                    }
                }
            return blocks;
        }
    }

    public class Block
    {
        public Block(BlockId id, int playerId = 0) : this((int)id, playerId) { }
        public Block(int id, int playerId = 0)
        {
            this.Id = id;
            this.PlayerID = playerId;
        }

        /// <summary>
        /// Id of block
        /// </summary>
        public int Id { get; set; }
        /// <summary>
        /// Player ID of whoever placed block. 0 if unknown
        /// </summary>
        public int PlayerID { get; set; }
    }
    public class Sign : Block
    {
        public Sign(BlockId blockId = BlockId.SignWood, string text = "", int morph = 0, int playerId = 0) : this((int)blockId, text, morph, playerId) { }
        public Sign(int blockId = (int)BlockId.SignWood, string text = "", int morph = 0, int playerId = 0) : base(blockId, playerId) { this.Text = text; this.Morph = morph; }

        public string Text { get; set; }
        public int Morph { get; set; }
    }
    public class Portal : Block
    {
        public Portal(BlockId blockId = BlockId.Portal, int rotation = 0, int thisId = 0, int targetId = 0, bool flipped = false, int playerId = 0) : this((int)blockId, rotation, thisId, targetId, flipped, playerId) { }
        public Portal(int blockId = (int)BlockId.Portal, int rotation = 0, int thisId = 0, int targetId = 0, bool flipped = false, int playerId = 0) : base(BlockId.Portal, playerId)
        {
            this.Rotation = rotation;
            this.ThisId = thisId;
            this.TargetId = targetId;
            this.Flipped = flipped;
        }

        public int Rotation { get; set; }
        public int ThisId { get; set; }
        public int TargetId { get; set; }
        public bool Flipped { get; set; }
    }
    public class Effect : Block
    {
        public Effect(BlockId blockId = BlockId.EffectClear, int amount = 0, int playerId = 0) : base(blockId, playerId) => this.Amount = amount;
        public Effect(int blockId = (int)BlockId.EffectClear, int amount = 0, int playerId = 0) : base(blockId, playerId) => this.Amount = amount;

        public int Amount { get; set; }
    }
}
