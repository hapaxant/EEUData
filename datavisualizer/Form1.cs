using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using EEUniverse.Library;
using EEUData;
using EEWData;
using WorldData = EEWData.WorldData;
using PlayerData = EEWData.PlayerData;
//using BlockId = EEWData.BlockId;
using System.IO;
using System.Diagnostics;
using Message = EEUniverse.Library.Message;
using System.Drawing.Drawing2D;

namespace datavisualizer
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        EEWClient cli;
        Connection con;
        const string TOKENPATH = "token.txt";
        string GetToken(bool forcenew)
        {
            if (!File.Exists(TOKENPATH) || forcenew)
            {
                File.WriteAllText(TOKENPATH, "");
                Process.Start("notepad.exe", TOKENPATH).WaitForExit();
            }
            return File.ReadAllText(TOKENPATH);
        }
        private void Form1_Load(object sender, EventArgs e)
        {
            if (File.Exists(WIDPATH)) textBox1.Text = File.ReadAllText(WIDPATH);
        }

        const string WIDPATH = "wid.txt";
        private void button1_Click(object sender, EventArgs e)
        {
            if (button1.Text == "disconnect")
            {
                button1.Enabled = false;
                playersd.Clear();
                zones.Clear();
                listBox1.Items.Clear();
                listBox2.Items.Clear();
                cli?.Dispose();
                b?.Dispose();
                b2?.Dispose();
                g?.Dispose();
                g2?.Dispose();
                pictureBox1.Image = null;
                pictureBoxWithInterpolationMode1.Image = null;
                button1.Text = "connect";
                button1.Enabled = true;
                return;
            }
            button1.Enabled = false;
            var wid = textBox1.Text;
            File.WriteAllText(WIDPATH, wid);
            void tryConnect(bool shid)
            {
                cli = new EEWClient(GetToken(shid));
                cli.Connect();
                //var t = cli.ConnectAsync();
                //while (!t.IsCompleted) Task.Delay(1).Wait();
                //var exx = t.Exception;
                //if (exx != null) throw exx;
            }
            try
            {
                tryConnect(false);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
                tryConnect(true);
            }
            world = new WorldData();
            players = new PlayerData();
            cli.OnDisconnect += delegate (object o, CloseEventArgs ee) { MessageBox.Show(ee.ToString(), "OnDisconnect", MessageBoxButtons.OK, MessageBoxIcon.Error); };
            con = (Connection)cli.CreateWorldConnection(wid);
            con.OnMessage += OnMessage;
            con.Send(MessageType.Init, 0);
            button1.Text = "disconnect";
            button1.Enabled = true;
        }

        WorldData world;
        PlayerData players;
        Graphics g;
        Bitmap b;

        Color GetColor(int fg, int bg) => GetColor((ushort)fg, (ushort)bg);
        Color GetColor(ushort fg, ushort bg)
        {
            var ct = WorldData.BlockColors;
            Color c = Color.Black;
            int n = ct[fg];
            if (n == -1) n = ct[bg];
            if (n == -1 && bg != 0) n = -2;
            if (n == -2) c = Color.Transparent;
            var wbg = world.BackgroundColor;
            if (n == -1) c = wbg != -1 ? Color.FromArgb(unchecked((int)0xff000000 | world.BackgroundColor)) : Color.Black;
            if (n >= 0) c = Color.FromArgb(WorldData.FromBlockColorToArgb(n));
            return c;
        }
        void ReloadPicturebox()
        {
            int w = world.Width, h = world.Height;
            pictureBox1.Invoke((Action)delegate ()
            {
                b?.Dispose();
                g?.Dispose();
                pictureBox1.Image = b = new Bitmap(w, h);
                //g = pictureBox1.CreateGraphics();
                g = Graphics.FromImage(b);
                g.Clear(Color.Black);
                for (int y = 0; y < world.Height; y++)
                {
                    for (int x = 0; x < world.Width; x++)
                    {
                        var n = (int)world[1, x, y].Id;
                        var n2 = (int)world[0, x, y].Id;
                        b.SetPixel(x, y, GetColor(n, n2));
                    }
                }
                pictureBox1.Invalidate();
            });
        }
        Dictionary<int, player> playersd = new Dictionary<int, player>();
        Dictionary<int, zone> zones = new Dictionary<int, zone>();
#pragma warning disable IDE1006 // Naming Styles // idgaf
        class player
        {
            public player(int id, string name) { this.id = id; this.name = name; }
            public int id;
            public string name;
            public override string ToString()
            {
                return $"{id} {name}";
            }
        }
        class zone
        {
            public zone(int id, int type)
            {
                this.id = id;
                this.name = $"{id} {((EEUData.ZoneType)type).ToString()}";
            }
            public int id;
            public string name;
            public override string ToString()
            {
                return $"{name}";
            }
        }
#pragma warning restore IDE1006 // Naming Styles
        private void OnMessage(object sender, Message m)
        {
            Console.WriteLine(m.Type);
            world.Parse(m);
            players.Parse(m);
            switch (m.Type)
            {
                case MessageType.Init:
                    ReloadPicturebox();
                    listBox1.Invoke((Action)delegate ()
                    {
                        listBox1.Items.Clear();
                        zones.Clear();
                        pictureBoxWithInterpolationMode1.Image = b2 = new Bitmap(world.Width, world.Height);
                        g2 = Graphics.FromImage(b2);
                        //g2 = pictureBoxWithInterpolationMode1.CreateGraphics();
                        foreach (var item in world.Zones.Values)
                        {
                            var z = new zone(item.Id, (int)item.Type);
                            zones.Add(item.Id, z);
                            listBox1.Items.Add(z);
                        }
                    });
                    DrawZone(-1);
                    break;
                case MessageType.ZoneEdit:
                    //DrawZone(m.GetInt(0));
                    DrawZone(-1);
                    break;
                case MessageType.ZoneCreate:
                    {
                        //listBox1.Items.Add($"{m.GetInt(1)==0? WorldData.ZoneType}");
                        int id = -1;
                        listBox1.Invoke((Action)delegate ()
                        {
                            id = m.GetInt(0);
                            var z = new zone(id, m.GetInt(1));
                            zones.Add(id, z);
                            listBox1.Items.Add(z);
                        });
                        //DrawZone(id);
                        DrawZone(-1);
                        break;
                    }
                case MessageType.ZoneDelete:
                    {
                        listBox1.Invoke((Action)delegate ()
                        {
                            var id = m.GetInt(0);
                            var z = zones[id];
                            zones.Remove(id);
                            listBox1.Items.Remove(z);
                        });
                        DrawZone(-1);
                        break;
                    }
                case MessageType.PlaceBlock:
                    /*[0] = 2 (Int32)//playerid
                      [1] = 1 (Int32)//layer
                      [2] = 47 (Int32)//x
                      [3] = 168 (Int32)//y
                      [4] = 5 (Int32)*///id
                    this.Invoke((Action)delegate ()
                    {
                        int l = m.GetInt(1), x = m.GetInt(2), y = m.GetInt(3), id = m.GetInt(4);
                        if (l == 0) b.SetPixel(x, y, GetColor((int)world[1, x, y].Id, id));
                        else b.SetPixel(x, y, GetColor(id, (int)world[0, x, y].Id));
                        //pictureBox1.Invalidate(new Rectangle(x, y, 1, 1));
                        pictureBox1.Invalidate();
                    });
                    break;
                case MessageType.BgColor:
                    ReloadPicturebox();
                    break;
                case (MessageType)CustomMessageType.Loadlevel:
                    ReloadPicturebox();
                    listBox1.Invoke((Action)delegate ()
                    {
                        listBox1.Items.Clear();
                        zones.Clear();
                        foreach (var item in world.Zones.Values)
                        {
                            var z = new zone(item.Id, (int)item.Type);
                            zones.Add(item.Id, z);
                            listBox1.Items.Add(z);
                        }
                    });
                    DrawZone(-1);
                    break;
                case MessageType.Clear:
                    ReloadPicturebox();
                    listBox1.Invoke((Action)delegate () { listBox1.Items.Clear(); });
                    zones.Clear();
                    DrawZone(-1);
                    break;
                case MessageType.PlayerMove:
                    {
                        var id = m.GetInt(0);
                        if (pid == id)
                        {
                            var p = players[id];
                            var f = p.Keys;
                            bool space = f.HasFlag(MovementKeys.Spacebar),
                                 up = f.HasFlag(MovementKeys.Up),
                                 down = f.HasFlag(MovementKeys.Down),
                                 left = f.HasFlag(MovementKeys.Left),
                                 right = f.HasFlag(MovementKeys.Right);
                            var x = p.X;
                            var y = p.Y;
                            this.Invoke((Action)delegate
                            {
                                var b = Color.Black;
                                var g = Color.LimeGreen;
                                pictureBoxSpace.BackColor = space ? g : b;
                                pictureBoxUp.BackColor = up ? g : b;
                                pictureBoxDown.BackColor = down ? g : b;
                                pictureBoxLeft.BackColor = left ? g : b;
                                pictureBoxRight.BackColor = right ? g : b;
                                label4.Text = $"X: {x}\nY: {y}";
                            });
                        }
                    }
                    break;
                case MessageType.PlayerAdd:
                case MessageType.PlayerJoin:
                    this.Invoke((Action)delegate
                    {
                        var id = m.GetInt(0);
                        var p = new player(id, m.GetString(1));
                        playersd.Add(id, p);
                        listBox2.Items.Add(p);
                    });
                    break;
                case MessageType.PlayerExit:
                    this.Invoke((Action)delegate
                    {
                        var id = m.GetInt(0);
                        var p = playersd[id];
                        playersd.Remove(id);
                        listBox2.Items.Remove(p);
                    });
                    break;
            }
        }

        Bitmap b2;
        Graphics g2;
        void DrawZone(int id = -1, int highlight = -1)
        {
            pictureBoxWithInterpolationMode1.Invoke((Action)delegate
            {
                if (id == -1)
                {
                    g2.Clear(Color.Fuchsia);
                    //g2?.Dispose();
                    //b2?.Dispose();
                    //pictureBoxWithInterpolationMode1.Image = b2 = new Bitmap(world.Width, world.Height);
                    //g2 = Graphics.FromImage(b2);
                    foreach (var item in zones.Keys)
                        if (item != highlight) DrawZone(item, highlight);
                    if (highlight != -1) DrawZone(highlight, highlight);
                }
                else
                {
                    var z = world.Zones[id];
                    var m = z.Map;
                    int w = m.GetLength(0);
                    int h = m.GetLength(1);
                    var t = (int)z.Type;
                    var c = t == 0 ? Color.Gold : Color.Gray;
                    if (id == highlight)
                    {
                        byte max(byte l, byte r) => Math.Max(l, r);
                        const byte b = 48;
                        byte br(byte a) { var d = max(a, (byte)(a + b)); if (a > d) return 255; else return d; }
                        c = Color.FromArgb(br(c.R), br(c.G), br(c.B));
                    }
                    //c = Color.FromArgb((byte)(255 * .75), c);
                    for (int y = 0; y < h; y++)
                        for (int x = 0; x < w; x++)
                        {
                            if (m[x, y]) b2.SetPixel(x, y, c);
                            //if (m[x, y]) g2.FillRectangle(new Pen(c).Brush, x, y, 1, 1);
                        }
                }
                pictureBoxWithInterpolationMode1.Invalidate();
            });
        }
        private void listBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (listBox1.SelectedItem == null) DrawZone(-1);
            else DrawZone(-1, ((zone)listBox1.SelectedItem).id);
        }

        int pid = -1;
        private void listBox2_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (listBox2.SelectedItem == null) pid = -1;
            else pid = ((player)listBox2.SelectedItem).id;
        }
    }
    static class ConnectionExtensions
    {
        public static void PlaceBlock(this IConnection con, int layer, int x, int y, BlockId id, params object[] args) => con.Send(MessageType.PlaceBlock, new object[] { layer, x, y, (int)id }.Concat(args).ToArray());
        public static void PlaceBlock(this IConnection con, int layer, int x, int y, int id, params object[] args) => con.Send(MessageType.PlaceBlock, new object[] { layer, x, y, id }.Concat(args).ToArray());
        public static void Chat(this IConnection con, string message) => con.Send(MessageType.Chat, message);
    }

    public class PictureBoxWithInterpolationMode : PictureBox
    {
        public InterpolationMode InterpolationMode { get; set; } = InterpolationMode.NearestNeighbor;

        protected override void OnPaint(PaintEventArgs paintEventArgs)
        {
            paintEventArgs.Graphics.InterpolationMode = InterpolationMode;
            base.OnPaint(paintEventArgs);
        }
    }
}
