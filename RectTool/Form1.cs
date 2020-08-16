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

namespace RectTool
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
        Properties.Settings prop = Properties.Settings.Default;
        byte whitelistMode;
        bool cbt;
        HashSet<string> whitelisted = new HashSet<string>();
        Dictionary<int, MyPlayer> players = new Dictionary<int, MyPlayer>();

        class MyPlayer
        {
            public MyPlayer(Player p) => this.p = p;
            public Player p;
            public byte mode, state;
            public Point locfrom;
            public Block blockfrom;
            public int layerfrom;
            public bool processing;
            public bool showmsgs = true;
            public bool keepsel;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (cli?.Connected == true)
            {
                cli?.DisposeAsync();
                button1.Text = "Connect";
                return;
            }
            cli = new EEUClient(Clipboard.GetText());
            cli.OnDisconnect += Cli_OnDisconnect;
            cli.Connect();
            data = new RoomData();
            con = cli.CreateWorldConnection(textBox1.Text);
            con.OnMessage += Con_OnMessage;
            con.ChatPrefix = "[RectTool]: ";
            con.Init();
            button1.Text = "Disconnect";
        }

        private void Cli_OnDisconnect(object sender, CloseEventArgs e)
        {
            button1.BeginInvoke((Action)(() =>
            {
                if (!e.WasClean) MessageBox.Show(this, e.ToString(), "Disconnected");
                button1.Text = "Connect";
            }));
        }

        private void Con_OnMessage(object sender, EEUniverse.Library.Message m)
        {
            data.Parse(m);
            switch (m.Type)
            {
                case MessageType.Init:
                    {
                        players.Add(m.GetInt(0), new MyPlayer(data[m.GetInt(0)]));
                        con.Chat("Connected! Type .help for available commands.");
                        break;
                    }
                case MessageType.PlayerJoin:
                case MessageType.PlayerAdd:
                    players.Add(m.GetInt(0), new MyPlayer(data[m.GetInt(0)]));
                    break;
                case MessageType.PlayerExit:
                    players.Remove(m.GetInt(0));
                    break;
                case MessageType.Chat:
                case MessageType.ChatPMFrom:
                    {
                        bool wasPm = m.Type == MessageType.ChatPMFrom;

                        const string PREFIXES = ".,!";
                        var id = m.GetInt(0);
                        var player = players[id];
                        var username = player.p.Username;
                        string message = m.GetString(1);
                        string[] args = message.Split(' ');
                        char prefix;
                        if (PREFIXES.Contains(args[0][0]))
                        {
                            prefix = args[0][0];
                            args[0] = args[0].Substring(1);
                        }
                        else break;

                        switch (args[0])
                        {
                            case "help":
                                {
                                    const string TXT = "help: show this message, f: fill tool, r: replace tool, o: outline tool, c: cancel, k: keep first selection, h: show/hide messages";
                                    if (wasPm) con.ChatDM(username, TXT); else con.ChatRespond(username, TXT);
                                    break;
                                }
                            case "k":
                            case "keep":
                                {
                                    player.keepsel = !player.keepsel;
                                    con.ChatDM(username, player.keepsel ? "now keeping the first selection. type .c to deselect" : "no longer keeping first selection");
                                    break;
                                }
                            case "c":
                            case "cancel":
                                {
                                    player.mode = player.state = 0;
                                    if (player.showmsgs) con.ChatDM(username, "no tool selected.");
                                    break;
                                }
                            case "h":
                            case "hide":
                                {
                                    player.showmsgs = !player.showmsgs;
                                    con.ChatDM(username, player.showmsgs ? "now displaying tool messages" : "no longer displaying tool messages");
                                    break;
                                }
                            case "f":
                            case "fill":
                                {
                                    if (!CheckPermission(username) && !cbt) { con.ChatDM(username, "no permission!"); break; }
                                    //if (player.processing) { con.ChatDM(username, "please wait for current operation to finish"); break; }
                                    player.mode = 1;
                                    player.state = 1;
                                    if (player.showmsgs) con.ChatDM(username, "place a block at the first location");
                                    break;
                                }
                            case "r":
                            case "replace":
                                {
                                    if (!CheckPermission(username) && !cbt) { con.ChatDM(username, "no permission!"); break; }
                                    player.mode = 2;
                                    player.state = 1;
                                    if (player.showmsgs) con.ChatDM(username, "place a block you want replaced at the first location");
                                    break;
                                }
                            case "o":
                            case "outline":
                                {
                                    if (!CheckPermission(username) && !cbt) { con.ChatDM(username, "no permission!"); break; }
                                    player.mode = 3;
                                    player.state = 1;
                                    if (player.showmsgs) con.ChatDM(username, "place a block at the first corner");
                                    break;
                                }
                        }
                        break;
                    }
                case MessageType.PlaceBlock:
                    {
                        var player = players[m.GetInt(0)];
                        if (player.state == 0) break;
                        var username = player.p.Username;
                        if (!CheckPermission(username) && !cbt)
                        {
                            player.state = player.mode = 0;
                            break;
                        }
                        int l = m.GetInt(1), x = m.GetInt(2), y = m.GetInt(3);

                        switch (player.state)
                        {
                            case 1:
                                {
                                    player.locfrom = new Point(x, y);
                                    player.layerfrom = l;
                                    player.blockfrom = data[l, x, y];
                                    player.state = 2;
                                    if (player.showmsgs)
                                        switch (player.mode)
                                        {
                                            case 1://fill
                                                con.ChatDM(username, "now place a block you want to be filled with at the second location");
                                                break;
                                            case 2://replace
                                                con.ChatDM(username, "now place a block you want to replace with at the second location");
                                                break;
                                            case 3://outline
                                                con.ChatDM(username, "now place a block at the second corner");
                                                break;
                                        }
                                    break;
                                }
                            case 2:
                                {
                                    int prevlayer = player.layerfrom;
                                    var prevloc = player.locfrom;
                                    var blockfrom = player.blockfrom;
                                    var onlyZones = cbt && !CheckPermission(username);
                                    var mode = player.mode;
                                    var b = data[l, x, y];
                                    bool[,] zonemap = new bool[data.Width, data.Height];
                                    foreach (var zone in data.Zones.Values)
                                    {
                                        if (zone.Type == ZoneType.Edit)
                                            for (int yy = 0; yy < data.Height; yy++)
                                                for (int xx = 0; xx < data.Width; xx++)
                                                {
                                                    zonemap[xx, yy] |= zone.Map[xx, yy];
                                                }
                                    }
                                    player.processing = true;
                                    if (!player.keepsel) player.mode = player.state = 0;
                                    Task.Run(() =>
                                    {
                                        int x1 = Math.Min(prevloc.X, x), y1 = Math.Min(prevloc.Y, y), x2 = Math.Max(prevloc.X, x), y2 = Math.Max(prevloc.Y, y);
                                        switch (mode)
                                        {
                                            case 1://fill
                                                {
                                                    for (int yy = y1; yy < y2 + 1; yy++)
                                                        for (int xx = x1; xx < x2 + 1; xx++)
                                                        {
                                                            if (onlyZones && !zonemap[xx, yy]) continue;
                                                            if (data[l, xx, yy] != b) con.PlaceBlock(l, xx, yy, b);
                                                        }
                                                    break;
                                                }
                                            case 2://replace
                                                {
                                                    for (int yy = y1; yy < y2 + 1; yy++)
                                                        for (int xx = x1; xx < x2 + 1; xx++)
                                                        {
                                                            if (data[prevlayer, xx, yy] == blockfrom)
                                                            {
                                                                if (onlyZones && !zonemap[xx, yy]) continue;
                                                                if (prevlayer != l && data[prevlayer, xx, yy].Id != 0) con.PlaceBlock(prevlayer, xx, yy, 0);
                                                                if (data[l, xx, yy] != b) con.PlaceBlock(l, xx, yy, b);
                                                            }
                                                        }
                                                    break;
                                                }
                                            case 3://outline
                                                {
                                                    //top
                                                    for (int xx = x1; xx < x2 + 1; xx++)
                                                    {
                                                        if (onlyZones && !zonemap[xx, y1]) continue;
                                                        if (data[l, xx, y1] != b) con.PlaceBlock(l, xx, y1, b);
                                                    }
                                                    //right
                                                    for (int yy = y1 + 1; yy < y2 + 1; yy++)
                                                    {
                                                        if (onlyZones && !zonemap[x2, yy]) continue;
                                                        if (data[l, x2, yy] != b) con.PlaceBlock(l, x2, yy, b);
                                                    }
                                                    //bottom
                                                    for (int xx = x2 - 1; xx > x1 - 1; xx--)
                                                    {
                                                        if (onlyZones && !zonemap[xx, y2]) continue;
                                                        if (data[l, xx, y2] != b) con.PlaceBlock(l, xx, y2, b);
                                                    }
                                                    //left
                                                    for (int yy = y2 - 1; yy > y1; yy--)
                                                    {
                                                        if (onlyZones && !zonemap[x1, yy]) continue;
                                                        if (data[l, x1, yy] != b) con.PlaceBlock(l, x1, yy, b);
                                                    }
                                                    break;
                                                }
                                        }
                                        player.processing = false;
                                        if (player.showmsgs && !player.keepsel) con.ChatDM(username, "done!");
                                    });
                                    break;
                                }
                        }
                        break;
                    }
            }
        }

        private bool CheckPermission(string username)
        {
            username = username.ToLowerInvariant();
            if (whitelistMode == 2//any
            || (whitelistMode == 1 && whitelisted.Contains(username))//whitelist
            || (whitelistMode == 0 && username == data.OwnerUsername.ToLowerInvariant())//owneronly
               ) return true;

            return false;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            whitelistMode = prop.WhitelistMode;
            textBox1.Text = prop.WorldId;
            cbt = checkBox1.Checked = prop.cbt;
            switch (whitelistMode)
            {
                case 0: radioButton1.PerformClick(); break;
                case 1: radioButton2.PerformClick(); break;
                case 2: radioButton3.PerformClick(); break;
            }
            var c = prop.WhitelistedUsers;
            if (c != null) foreach (var item in c) { listBox1.Items.Add(item); whitelisted.Add(item); }
        }

        private void radioButton1_CheckedChanged(object sender, EventArgs e) { whitelistMode = 0; }
        private void radioButton2_CheckedChanged(object sender, EventArgs e) { whitelistMode = 1; }
        private void radioButton3_CheckedChanged(object sender, EventArgs e) { whitelistMode = 2; }
        private void checkBox1_CheckedChanged(object sender, EventArgs e) { cbt = checkBox1.Checked; }

        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            prop.WorldId = textBox1.Text;
            prop.WhitelistMode = whitelistMode;
            prop.cbt = cbt;
            prop.WhitelistedUsers = new System.Collections.Specialized.StringCollection();
            prop.WhitelistedUsers.AddRange(whitelisted.ToArray());
            prop.Save();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            var t = textBox2.Text.ToLowerInvariant();
            if (!String.IsNullOrWhiteSpace(t))
            {
                if (whitelisted.Add(t)) listBox1.Items.Add(t);
            }
            textBox2.Text = "";
        }

        private void button3_Click(object sender, EventArgs e)
        {
            var i = listBox1.SelectedIndices;
            if (i.Count == 0) return;
            var s = i.Cast<int>();
            foreach (var index in s.OrderByDescending(x => x))
            {
                var item = (string)listBox1.Items[index];
                whitelisted.Remove(item);
                listBox1.Items.RemoveAt(index);
            }
        }

        private void Form1_Shown(object sender, EventArgs e)
        {
            FormBorderStyle = FormBorderStyle.SizableToolWindow;
        }
    }
}
