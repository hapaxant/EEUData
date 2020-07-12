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
using System.IO;
using Message = EEUniverse.Library.Message;//Message type conflicts with System.Windows.Forms.Message
using System.Drawing.Drawing2D;

namespace datavisualizer
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        EEUClient cli;
        EEUConnection con;
        RoomData data;
        Graphics g;
        Bitmap b;

        private void Form1_Load(object sender, EventArgs e)
        {
            if (File.Exists(WIDPATH)) textBox1.Text = File.ReadAllText(WIDPATH);
            if (string.IsNullOrWhiteSpace(textBox1.Text)) textBox1.Text = "WorldID";
        }

        const string WIDPATH = "wid.txt";
        private void button1_Click(object sender, EventArgs e)
        {
            if (button1.Text == "disconnect")
            {
                button1.Enabled = false;
                players.Clear();
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
            try
            {
                cli = new EEUClient(Clipboard.GetText());
                cli.Connect();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
                button1.Enabled = true;
                return;
            }
            data = new RoomData();
            cli.OnDisconnect += delegate (object o, CloseEventArgs ee) { MessageBox.Show(ee.ToString(), "OnDisconnect", MessageBoxButtons.OK, MessageBoxIcon.Error); };
            con = cli.CreateWorldConnection(wid);
            con.OnMessage += OnMessage;
            con.Init();
            button1.Text = "disconnect";
            button1.Enabled = true;
        }

        Color GetColor(int fg, int bg, int bgcolor) => Color.FromArgb(RoomData.GetARGBColor((ushort)fg, (ushort)bg, bgcolor));
        int bgcolor;
        void ReloadPicturebox()
        {
            int w = data.Width, h = data.Height;
            pictureBox1.Invoke((Action)delegate ()
            {
                b?.Dispose();
                g?.Dispose();
                pictureBox1.Image = b = new Bitmap(w, h);
                g = Graphics.FromImage(b);
                g.Clear(Color.Black);
                for (int y = 0; y < h; y++)
                {
                    for (int x = 0; x < w; x++)
                    {
                        var n = (int)data.Blocks[1, x, y].Id;
                        var n2 = (int)data.Blocks[0, x, y].Id;
                        b.SetPixel(x, y, GetColor(n, n2, bgcolor));
                    }
                }
                pictureBox1.Invalidate();
            });
        }
        Dictionary<int, MyPlayer> players = new Dictionary<int, MyPlayer>();
        Dictionary<int, MyZone> zones = new Dictionary<int, MyZone>();
        class MyPlayer
        {
            public MyPlayer(int id, string name) { this.id = id; this.name = name; }
            public int id;
            public string name;
            public override string ToString()
            {
                return $"{id} {name}";
            }
        }
        class MyZone
        {
            public MyZone(int id, int type)
            {
                this.id = id;
                this.name = $"{id} {((ZoneType)type).ToString()}";
            }
            public int id;
            public string name;
            public override string ToString()
            {
                return $"{name}";
            }
        }
        private void OnMessage(object sender, Message m)
        {
            Console.WriteLine(m.Type);
            data.Parse(m);
            switch (m.Type)
            {
                case MessageType.Init:
                    bgcolor = data.BackgroundColor;
                    ReloadPicturebox();
                    listBox1.Invoke((Action)delegate ()
                    {
                        listBox1.Items.Clear();
                        zones.Clear();
                        pictureBoxWithInterpolationMode1.Image = b2 = new Bitmap(data.Width, data.Height);
                        g2 = Graphics.FromImage(b2);
                        foreach (var item in data.Zones.Values)
                        {
                            var z = new MyZone(item.Id, (int)item.Type);
                            zones.Add(item.Id, z);
                            listBox1.Items.Add(z);
                        }
                    });
                    DrawZone(-1);
                    break;
                case MessageType.ZoneEdit:
                    DrawZone(-1);
                    break;
                case MessageType.ZoneCreate:
                    {
                        int id = -1;
                        listBox1.Invoke((Action)delegate ()
                        {
                            id = m.GetInt(0);
                            var z = new MyZone(id, m.GetInt(1));
                            zones.Add(id, z);
                            listBox1.Items.Add(z);
                        });
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
                    this.Invoke((Action)delegate ()
                    {
                        int l = m.GetInt(1), x = m.GetInt(2), y = m.GetInt(3), id = m.GetInt(4);
                        if (l == 0) b.SetPixel(x, y, GetColor((int)data.Blocks[1, x, y].Id, id, bgcolor));
                        else b.SetPixel(x, y, GetColor(id, (int)data.Blocks[0, x, y].Id, bgcolor));
                        pictureBox1.Invalidate();
                    });
                    break;
                case MessageType.BgColor:
                    bgcolor = m.GetInt(0);
                    ReloadPicturebox();
                    break;
                case MessageType.Clear:
                    bgcolor = -1;
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
                            var p = data.Players[id];
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
                        var p = new MyPlayer(id, m.GetString(1));
                        players.Add(id, p);
                        listBox2.Items.Add(p);
                    });
                    break;
                case MessageType.PlayerExit:
                    this.Invoke((Action)delegate
                    {
                        var id = m.GetInt(0);
                        var p = players[id];
                        players.Remove(id);
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
                    foreach (var item in zones.Keys)
                        if (item != highlight) DrawZone(item, highlight);
                    if (highlight != -1) DrawZone(highlight, highlight);
                }
                else
                {
                    var z = data.Zones[id];
                    var m = z.Map;
                    int w = m.GetLength(0);
                    int h = m.GetLength(1);
                    var t = (int)z.Type;
                    var c = t == 0 ? Color.Goldenrod : Color.Gray;
                    if (id == highlight)
                    {
                        byte max(byte l, byte r) => Math.Max(l, r);
                        const byte b = 48;
                        byte br(byte a) { var d = max(a, (byte)(a + b)); if (a > d) return 255; else return d; }
                        c = Color.FromArgb(br(c.R), br(c.G), br(c.B));
                    }
                    for (int y = 0; y < h; y++)
                        for (int x = 0; x < w; x++)
                        {
                            if (m[x, y]) b2.SetPixel(x, y, c);
                        }
                }
                pictureBoxWithInterpolationMode1.Invalidate();
            });
        }
        private void listBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (listBox1.SelectedItem == null) DrawZone(-1);
            else DrawZone(-1, ((MyZone)listBox1.SelectedItem).id);
        }

        int pid = -1;
        private void listBox2_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (listBox2.SelectedItem == null) pid = -1;
            else pid = ((MyPlayer)listBox2.SelectedItem).id;
        }
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
