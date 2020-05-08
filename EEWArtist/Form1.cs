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
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.IO;
using System.Threading;
using WorldData = EEWData.WorldData;
using System.Drawing.Drawing2D;
using System.IO.Compression;
using System.Collections.Concurrent;

namespace eewartist
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        int x, y;
        Color[] palette;
        //Dictionary<int, List<int>> palettebid;
        Dictionary<int, ushort> palettebid;
        Color[,] image;
        ushort[,] idgrid;
        int imgw, imgh;
        byte mode;
        Stopwatch stopwatch_gcc = new Stopwatch();
        Color GetClosestColor(Color c)
        {
            stopwatch_gcc.Start();
            Color result;
            switch (mode)
            {
                case 1:
                    result = palette[ColorsHelper.closestColor1(palette, c)];
                    break;
                case 2:
                    result = palette[ColorsHelper.closestColor2(palette, c)];
                    break;
                case 3:
                    result = palette[ColorsHelper.closestColor3(palette, c)];
                    break;
                default: throw new InvalidOperationException();
            }
            stopwatch_gcc.Stop();
            return result;
        }

        const string WIDPATH = "wid.txt";
        const string CBSTATEPATH = ".cbstate";
        const string TOKENPATH = "token.txt";
        const string CREDSPATH = "creds.txt";
        const string CACHEPATH = "cache.db";
        ConcurrentDictionary<int, ushort> cache;
        bool cacheModified = false;
        ConcurrentDictionary<int, ushort> ReadCache()
        {
            if (!File.Exists(CACHEPATH)) return new ConcurrentDictionary<int, ushort>();
            ConcurrentDictionary<int, ushort> d = new ConcurrentDictionary<int, ushort>();
            using (var fs = new FileStream(CACHEPATH, FileMode.Open))
            using (var ds = new DeflateStream(fs, CompressionMode.Decompress))
            using (var ms = new MemoryStream())
            using (var br = new BinaryReader(ms))
            {
                ds.CopyTo(ms);
                ms.Position = 0;

                while (ms.Position < ms.Length)
                    d.Add(br.ReadInt32(), br.ReadUInt16());
            }
            return d;
        }
        void SaveCache(ConcurrentDictionary<int, ushort> d)
        {
            if (!cacheModified) return;
            int uncompressedBytes = 0;
            using (var fs = new FileStream(CACHEPATH, FileMode.Create))
            using (var ds = new DeflateStream(fs, CompressionMode.Compress))
            using (var ms = new MemoryStream())
            using (var bw = new BinaryWriter(ms))
            {
                foreach (var item in d)
                {
                    bw.Write(item.Key);
                    bw.Write(item.Value);
                    uncompressedBytes += 6;
                }
                ms.Position = 0;
                ms.CopyTo(ds);
                fs.Flush();
            }
            var str = "uncompressed bytes: " + uncompressedBytes;
            Console.WriteLine(str);
            Trace.WriteLine(str);
        }

        void EditPath(string path)
        {
            Process.Start(Environment.OSVersion.Platform == PlatformID.Win32NT ? "notepad.exe" : "nano", path);
        }
        private void Form1_Load(object sender, EventArgs e)
        {
            cache = ReadCache();
            ////debug only
            //int w = 50, h = 50;
            //pictureBox1.Image = new Bitmap(w, h);
            //originalSize = new Size(w, h);
            //Graphics.FromImage(pictureBox1.Image).Clear(Color.SkyBlue);
            //pictureBox1.Size = new Size(w, h);
            //pictureBox1.Invalidate();
            //trackBar3.Enabled = true;
            ////

            EEWClient.StartEditor = false;
            ConnectionExtensions.UseAsync = true;
            ConnectionExtensions.UseLocking = false;
            trackBar1.Value = trackBar1.Maximum / 2;
            trackBar2.Value = trackBar2.Maximum / 2;
            if (File.Exists(WIDPATH)) textBox1.Text = File.ReadAllText(WIDPATH);
            var cbfileexists = File.Exists(CBSTATEPATH);
            var fileexists = File.Exists(cbfileexists ? TOKENPATH : CREDSPATH);
            checkBox1.CheckedChanged -= checkBox1_CheckedChanged;
            checkBox1.Checked = cbfileexists;
            if (!fileexists) checkBox1_CheckedChanged(this, null);
            checkBox1.CheckedChanged += checkBox1_CheckedChanged;
            EEWClient.TokenPath = cbfileexists ? TOKENPATH : CREDSPATH;
            radioButton2.PerformClick();
            palette = WorldData.BlockColors.Values.Where(x => x >= 0).Select(x => Color.FromArgb(EEUData.WorldData.FromBlockColorToArgb(x))).Distinct().ToArray();
            //palettebid = new Dictionary<int, List<int>>();
            palettebid = new Dictionary<int, ushort>();
            //foreach (var item in WorldData.BlockColors.Where(x => x.Value >= 0).Select(x => new KeyValuePair<int, int>(EEWData.WorldData.FromBlockColorToArgb(x.Value), x.Key)))
            foreach (var item in WorldData.BlockColors.Where(x => x.Value >= 0).Select(x => (EEUData.WorldData.FromBlockColorToArgb(x.Value), (ushort)x.Key)))
            {
                //if (!palettebid.ContainsKey(item.Key)) palettebid.Add(item.Key, new List<int>() { item.Value });
                if (!palettebid.ContainsKey(item.Item1)) palettebid[item.Item1] = item.Item2;
                //else palettebid[item.Key].Add(item.Value);
            }

            //Console.WriteLine("A");
            //foreach (var item in palette)
            //{
            //    Console.WriteLine(item.ToArgb().ToString("X6"));
            //}
            //foreach (var item in cache)
            //{
            //    Console.WriteLine(item.Key.ToString("X6"));
            //}
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (openFileDialog1.ShowDialog() != DialogResult.OK) return;
            this.AllowDrop = false;
            trackBar3.Enabled = false;
            Bitmap m;
            using (var s = openFileDialog1.OpenFile())
            {
                m = new Bitmap(s);
            }
            panelMain.Enabled = false;
            Task.Run(() => ProcessImage(m));
        }
        int maxParallelism = Math.Max(1, Environment.ProcessorCount - 1);
        void ProcessImage(Bitmap m)
        {
            try
            {
                this.InvokeX(() => { panelMain.Enabled = false; });
                void ProcessUsingLockbitsAndParallel(Bitmap processedBitmap)
                {//this was also copied with modifications
                    BitmapData bitmapData = processedBitmap.LockBits(new Rectangle(0, 0, processedBitmap.Width, processedBitmap.Height), ImageLockMode.ReadWrite, processedBitmap.PixelFormat);

                    int bytesPerPixel = Image.GetPixelFormatSize(processedBitmap.PixelFormat) / 8;
                    int byteCount = bitmapData.Stride * processedBitmap.Height;
                    int heightInPixels = bitmapData.Height;
                    int widthInBytes = bitmapData.Width * bytesPerPixel;
                    byte[] pixels = new byte[byteCount];
                    IntPtr ptrFirstPixel = bitmapData.Scan0;
                    Marshal.Copy(ptrFirstPixel, pixels, 0, pixels.Length);

                    Parallel.For(0, heightInPixels, new ParallelOptions() { MaxDegreeOfParallelism = maxParallelism }, y =>
                    {
                        int currentLine = y * bitmapData.Stride;
                        for (int x = 0; x < widthInBytes; x = x + bytesPerPixel)
                        {
                            byte oldBlue = pixels[currentLine + x];
                            byte oldGreen = pixels[currentLine + x + 1];
                            byte oldRed = pixels[currentLine + x + 2];
                            byte oldAlpha = 255;
                            if (bytesPerPixel > 3) oldAlpha = pixels[currentLine + x + 3];

                            // calculate new pixel value
                            if (oldAlpha < 127) oldAlpha = 0;
                            else oldAlpha = 255;
                            var oldc = Color.FromArgb(oldAlpha, oldRed, oldGreen, oldBlue);
                            var oldargb = oldc.ToArgb();
                            Color c;

                            if (oldAlpha == 0)
                            {
                                //c = Color.FromArgb(WorldData.FromBlockColorToArgb(WorldData.BlockColors[(ushort)BlockId.Black]));
                                //c = Color.Transparent;
                                c = Color.FromArgb(0, 0, 0, 0);
                            }
                            else
                            {
                                Trace.Assert(oldAlpha == 255);
                                if (cache.ContainsKey(oldargb))
                                {
                                    c = Color.FromArgb(WorldData.FromBlockColorToArgb(WorldData.BlockColors[cache[oldargb]]));
                                }
                                else
                                {
                                    c = GetClosestColor(oldc);
                                    if (!cache.TryAdd(oldargb, palettebid[c.ToArgb()]))
                                        Console.WriteLine("someone was faster than me! " + oldargb.ToString("X6"));
                                    cacheModified = true;
                                }
                            }

                            image[x / bytesPerPixel, y] = c;
                            //Console.WriteLine($"{x / bytesPerPixel},{y}");
                            oldRed = c.R;
                            oldGreen = c.G;
                            oldBlue = c.B;

                            pixels[currentLine + x] = oldBlue;
                            pixels[currentLine + x + 1] = oldGreen;
                            pixels[currentLine + x + 2] = oldRed;
                            if (bytesPerPixel > 3) pixels[currentLine + x + 3] = oldAlpha;
                        }
                    });
                    // copy modified bytes back
                    Marshal.Copy(pixels, 0, ptrFirstPixel, pixels.Length);
                    processedBitmap.UnlockBits(bitmapData);
                }

                imgw = m.Width; imgh = m.Height;
                image = new Color[imgw, imgh];
                Stopwatch stopwatch = Stopwatch.StartNew();
                stopwatch_gcc.Reset();
                ColorsHelper.ResetTimers();
                ProcessUsingLockbitsAndParallel(m);
                stopwatch_gcc.Stop();
                stopwatch.Stop();
                Console.WriteLine($"in GetClosestColor: {stopwatch_gcc.Elapsed}");
                Console.WriteLine($"total: {stopwatch.Elapsed}");
                ColorsHelper.PrintTimers();
                this.InvokeX(() =>
                {
                    pictureBox1.Image?.Dispose();
                    pictureBox1.Image = m;
                    originalSize = new Size(imgw, imgh);
                    trackBar3_Scroll(this, null);
                    pictureBox1.Location = new Point(0, 0);
                    panelMain.Enabled = true;
                    this.AllowDrop = true;
                    button2.Enabled = true;
                    trackBar3.Enabled = true;
                    button3.Enabled = true;
                });
            }
            catch
            {
                Debugger.Break();
                throw;
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            if (pictureBox1.Image == null) return;
            if (saveFileDialog1.ShowDialog() != DialogResult.OK) return;
            using (var s = saveFileDialog1.OpenFile()) pictureBox1.Image.Save(s, ImageFormat.Png);
        }

        private void trackBar1_Scroll(object sender, EventArgs e)
        {//sat
            ColorsHelper.factorSat = (float)trackBar1.Value / trackBar1.Maximum;
        }
        private void trackBar2_Scroll(object sender, EventArgs e)
        {//bri
            ColorsHelper.factorBri = (float)trackBar2.Value / trackBar2.Maximum;
        }

        private void radioButton1_CheckedChanged(object sender, EventArgs e)
        {
            splitContainer1.Enabled = false;
            mode = 1;
        }
        private void radioButton2_CheckedChanged(object sender, EventArgs e)
        {
            splitContainer1.Enabled = false;
            mode = 2;
        }
        private void radioButton3_CheckedChanged(object sender, EventArgs e)
        {
            splitContainer1.Enabled = true;
            mode = 3;
        }

        bool hold = false;
        Point holdpos;
        private void pictureBox1_MouseDown(object sender, MouseEventArgs e)
        {
            //holdpos = pictureBox1.PointToScreen(e.Location);
            holdpos = e.Location;
            hold = true;
        }
        private void pictureBox1_MouseUp(object sender, MouseEventArgs e)
        {
            hold = false;
        }
        private void pictureBox1_MouseMove(object sender, MouseEventArgs e)
        {
            if (hold)
            {
                pictureBox1.Location = new Point(e.X + pictureBox1.Left - holdpos.X, e.Y + pictureBox1.Top - holdpos.Y);
            }
        }

        private void panel1_MouseClick(object sender, MouseEventArgs e)
        {
            pictureBox1.Location = new Point(0, 0);
        }

        private void Form1_DragEnter(object sender, DragEventArgs e)
        {
            var formats = e.Data.GetFormats();
            foreach (var item in formats)
            {
                Console.WriteLine(item);
            }
            Console.WriteLine();
            ;
            if (e.Data.GetDataPresent(DataFormats.Bitmap) ||
                e.Data.GetDataPresent(DataFormats.FileDrop))
                e.Effect = DragDropEffects.Copy;
        }

        private void Form1_DragDrop(object sender, DragEventArgs e)
        {
            Bitmap bmp;

            bmp = (Bitmap)e.Data.GetData(DataFormats.Bitmap, true);
            if (bmp == null)
            {
                string[] paths = null; string path = null;
                paths = ((string[])e.Data.GetData(DataFormats.FileDrop));
                if (paths != null) path = paths[0];
                if (path != null) bmp = new Bitmap(path);//Bitmap.FromFile(path);
            }

            Console.WriteLine(bmp != null);
            if (bmp != null) { this.AllowDrop = false; Task.Run(() => ProcessImage(bmp)); }
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            var check = checkBox1.Checked;
            var path = EEWClient.TokenPath = check ? TOKENPATH : CREDSPATH;
            if (check)
            {
                #region i got bored
                if (!File.Exists(CBSTATEPATH)) File.WriteAllText(CBSTATEPATH, new String(new ushort[] { 9617, 9617, 9617, 9617, 9604, 9604, 9604, 9604, 9600, 9600, 9600, 9600, 9600, 9600, 9600, 9600, 9604, 9604, 9604, 9604, 9604, 9604, 13, 10, 9617, 9617, 9617, 9617, 9608, 9617, 9617, 9617, 9617, 9618, 9618, 9618, 9618, 9618, 9618, 9618, 9618, 9618, 9618, 9618, 9618, 9617, 9617, 9600, 9600, 9604, 13, 10, 9617, 9617, 9617, 9608, 9617, 9617, 9617, 9618, 9618, 9618, 9618, 9618, 9618, 9617, 9617, 9617, 9617, 9617, 9617, 9617, 9617, 9618, 9618, 9618, 9617, 9617, 9608, 13, 10, 9617, 9617, 9608, 9617, 9617, 9617, 9617, 9617, 9617, 9604, 9608, 9608, 9600, 9604, 9604, 9617, 9617, 9617, 9617, 9617, 9604, 9604, 9604, 9617, 9617, 9617, 9608, 13, 10, 9617, 9600, 9618, 9604, 9604, 9604, 9618, 9617, 9608, 9600, 9600, 9600, 9600, 9604, 9604, 9608, 9617, 9617, 9617, 9608, 9608, 9604, 9604, 9608, 9617, 9617, 9617, 9608, 13, 10, 9608, 9618, 9608, 9618, 9604, 9617, 9600, 9604, 9604, 9604, 9600, 9617, 9617, 9617, 9617, 9617, 9617, 9617, 9617, 9608, 9617, 9617, 9617, 9618, 9618, 9618, 9618, 9618, 9608, 13, 10, 9608, 9618, 9608, 9617, 9608, 9600, 9604, 9604, 9617, 9617, 9617, 9617, 9617, 9608, 9600, 9617, 9617, 9617, 9617, 9600, 9604, 9617, 9617, 9604, 9600, 9600, 9600, 9604, 9618, 9608, 13, 10, 9617, 9608, 9600, 9604, 9617, 9608, 9604, 9617, 9608, 9600, 9604, 9604, 9617, 9600, 9617, 9600, 9600, 9617, 9604, 9604, 9600, 9617, 9617, 9617, 9617, 9608, 9617, 9617, 9608, 13, 10, 9617, 9617, 9608, 9617, 9617, 9600, 9604, 9600, 9608, 9604, 9604, 9617, 9608, 9600, 9600, 9600, 9604, 9604, 9604, 9604, 9600, 9600, 9608, 9600, 9608, 9608, 9617, 9608, 13, 10, 9617, 9617, 9617, 9608, 9617, 9617, 9608, 9608, 9617, 9617, 9600, 9608, 9604, 9604, 9604, 9608, 9604, 9604, 9608, 9604, 9608, 9608, 9608, 9608, 9617, 9608, 13, 10, 9617, 9617, 9617, 9617, 9608, 9617, 9617, 9617, 9600, 9600, 9604, 9617, 9608, 9617, 9617, 9617, 9608, 9617, 9608, 9608, 9608, 9608, 9608, 9608, 9608, 9617, 9608, 13, 10, 9617, 9617, 9617, 9617, 9617, 9600, 9604, 9617, 9617, 9617, 9600, 9600, 9604, 9604, 9604, 9608, 9604, 9608, 9604, 9608, 9604, 9608, 9604, 9600, 9617, 9617, 9608, 13, 10, 9617, 9617, 9617, 9617, 9617, 9617, 9617, 9600, 9604, 9604, 9617, 9618, 9618, 9618, 9618, 9617, 9617, 9617, 9617, 9617, 9617, 9617, 9617, 9617, 9617, 9608, 13, 10, 9617, 9617, 9617, 9617, 9617, 9617, 9617, 9617, 9617, 9617, 9600, 9600, 9604, 9604, 9617, 9618, 9618, 9618, 9618, 9618, 9618, 9618, 9618, 9618, 9618, 9617, 9608, 13, 10, 9617, 9617, 9617, 9617, 9617, 9617, 9617, 9617, 9617, 9617, 9617, 9617, 9617, 9617, 9600, 9604, 9604, 9604, 9604, 9604, 9617, 9617, 9617, 9617, 9617, 9608 }.Select(x => (char)x).ToArray()));
                #endregion
                if (!File.Exists(path)) File.WriteAllText(path, "ur token here");
            }
            else
            {
                if (File.Exists(CBSTATEPATH)) File.Delete(CBSTATEPATH);
                if (!File.Exists(path)) File.WriteAllLines(path, new[] { "username", "password" });
            }
            /*if (e != null)*/ EditPath(path);
        }

        EEWClient cli;
        Connection con;
        string cachedToken;
        private void button2_Click(object sender, EventArgs e)
        {
            if (image == null && idgrid == null) return;
            panelMain.Enabled = false;
            x = (int)numericUpDown1.Value;
            y = (int)numericUpDown2.Value;
            var wid = textBox1.Text;
            File.WriteAllText(WIDPATH, wid);
            var useToken = checkBox1.Checked;
            if (image != null)
            {
                Stopwatch s = Stopwatch.StartNew();
                var w = image.GetLength(0);
                var h = image.GetLength(1);
                idgrid = new ushort[w, h];
                for (int j = 0; j < h; j++)
                    for (int i = 0; i < w; i++)
                    {
                        var argb = image[i, j].ToArgb();
                        if (argb != 0) idgrid[i, j] = palettebid[argb];
                        else idgrid[i, j] = (ushort)BlockId.Black;//Secret also works here (or any block with -1 color)
                    }
                image = null;
                s.Stop();
                Console.WriteLine($"moving/converting image[,] to idgrid[,]: {s.Elapsed}");
            }
            Task.Run(() => TryConnect(wid, useToken, 3, 1500));
        }

        //string username;
        //AutoResetEvent re = new AutoResetEvent(false);
        ManualResetEventSlim re = new ManualResetEventSlim(false, 0);
        void TryConnect(string wid, bool useToken, int retries, int timeout)
        {
            re.Reset();
            if (retries < 0)
            {
                this.InvokeX(() =>
                {
                    MessageBox.Show(this, "Timed out, max retry limit reached. Try again?", "Timeout", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                    panelMain.Enabled = true;
                });
                return;
            }
            if (cachedToken == null)
                if (useToken) cli = new EEWClient(File.ReadAllText(EEWClient.TokenPath));
                else
                {
                    var creds = File.ReadAllLines(EEWClient.TokenPath);
                    cli = new EEWClient(creds[0], creds[1], out cachedToken);
                }
            else cli = new EEWClient(cachedToken);
            //cli.OnMessage += (o, e) =>
            //{
            //    if (e.Type == MessageType.SelfInfo) username = e.GetString(0);
            //};
            cli.OnDisconnect += OnDisconnect;
            try { cli.Connect(); }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "UWU", MessageBoxButtons.OK, MessageBoxIcon.Error);
                panelMain.InvokeX(() => panelMain.Enabled = true);
                return;
            }
            con = (Connection)cli.CreateWorldConnection(wid);
            con.OnMessage += OnMessage;
            con.SendAsync(MessageType.Init, 0);
            //if (!re.WaitOne(timeout))
            if (!re.Wait(timeout))
            {
                cli?.Dispose();
                TryConnect(wid, useToken, retries - 1, timeout + 750);
            }
        }

        private void OnDisconnect(object sender, CloseEventArgs e)
        {
            //if (re.WaitOne(0))
            if (re.IsSet)
                panelMain.InvokeX(() => panelMain.Enabled = true);
        }

        bool bg;
        private void checkBox2_CheckedChanged(object sender, EventArgs e)
        {
            bg = checkBox2.Checked;
        }

        bool glass;
        private void checkBox3_CheckedChanged(object sender, EventArgs e)
        {
            glass = checkBox3.Checked;
        }

        WorldData world;

        Size originalSize;
        private void trackBar3_Scroll(object sender, EventArgs e)
        {//zoom
            var value = trackBar3.Value + 10d;
            double scale = value / 10d;

            Size oldSize = pictureBox1.Size;
            Size newSize = new Size((int)(originalSize.Width * scale),
                           (int)(originalSize.Height * scale));

            pictureBox1.Size = newSize;

            //todo: make this zoom centered on the middle point of panel where picturebox resides instead
            pictureBox1.Left = pictureBox1.Left + (oldSize.Width - newSize.Width) / 2;
            pictureBox1.Top = pictureBox1.Top + (oldSize.Height - newSize.Height) / 2;
        }

        bool canClose = true;
        bool savingCache = false;
        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (canClose)
            {
                if (!savingCache)
                {
                    e.Cancel = true;
                    canClose = false;
                    savingCache = true;
                    panelMain.Enabled = false;
                    Task.Run(() => SaveCache(cache)).ContinueWith((_) => { canClose = true; this.InvokeX(() => this.Close()); });
                }
            }
            else e.Cancel = true;
        }

        CountdownEvent ce = new CountdownEvent(1);
        private void OnMessage(object sender, EEUniverse.Library.Message m)
        {
            void die() { cli.DisposeAsync(); panelMain.InvokeX(() => panelMain.Enabled = true); }
            switch (m.Type)
            {
                case MessageType.Init:
                    re.Set();
                    if (m.GetString(7) != m.GetString(1))
                    {
                        die();
                        MessageBox.Show("not world owner", ".w.", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }

                    world = new WorldData();
                    world.Parse(m);
                    //int ww = m.GetInt(9), wh = m.GetInt(10);
                    int ww = world.Width, wh = world.Height;
                    Stopwatch s = Stopwatch.StartNew();
                    ce.Reset();
                    Task.Run(() =>
                    {
                        //var w = idgrid.GetLength(0);
                        //var h = idgrid.GetLength(1);
                        //HashSet<Task> tasks = new HashSet<Task>();
                        //void PlaceBlock(int layer, int x, int y, int id) => tasks.Add(con.SendAsync(MessageType.PlaceBlock, layer, x, y, id).);

                        //int num = 0;
                        void PlaceBlock(int layer, int x, int y, int id)
                        {
                            //Interlocked.Increment(ref num);
                            //con.SendAsync(MessageType.PlaceBlock, layer, x, y, id).ContinueWith((_) => Interlocked.Decrement(ref num));//.ContinueWith((t) => t.Dispose());
                            ce.AddCount();
                            con.SendAsync(MessageType.PlaceBlock, layer, x, y, id).ContinueWith((_) => ce.Signal());
                        }
                        for (int j = 0; j < imgh; j++)//y
                        {
                            int yy = j + y;
                            if (yy >= wh) break;//out of bounds
                            for (int i = 0; i < imgw; i++)//x
                            {
                                int xx = i + x;
                                if (xx >= ww) break;//out of bounds
                                int b = idgrid[i, j];
                                int l = ((BlockId)b).ToString().StartsWith("Bg") ? 0 : bg ? 0 : 1; //CustomBlockId doesn't have backgrounds yet so we're ok

                                if (world[l, xx, yy].Id != b) PlaceBlock(l, xx, yy, b);
                                if (l == 0)
                                {
                                    if (glass) { if (world[1, xx, yy].Id != (int)BlockId.Clear) PlaceBlock(1, xx, yy, (int)BlockId.Clear); }
                                    else if (world[1, xx, yy].Id != 0) PlaceBlock(1, xx, yy, 0);
                                }
                                //else if (i == 1)
                                //{
                                //    PlaceBlock(0, xx, yy, 0);//get rid of bg behind fg
                                //}
                            }
                        }
                        Console.WriteLine(s.Elapsed);
                        //Task.WhenAll(tasks).Wait();

                        //while (num != 0) Task.Delay(10).Wait();
                        ce.Signal();
                        ce.Wait();
                        s.Stop();
                        Console.WriteLine(s.Elapsed);
                        die();
                    });
                    break;
                    //case MessageType.PlaceBlock:
                    //    {
                    //        int xx = m.GetInt(2), yy = m.GetInt(3);
                    //        if (xx == x + imgw && yy == y + imgh)
                    //        {
                    //            Task.Delay(100).Wait();//bad code. bad.
                    //            die();
                    //        }
                    //    }
                    //    break;
            }
        }
    }
    public static class Extensions
    {
        public static void InvokeX(this Control c, Action act)
        {
            if (c.InvokeRequired)
                c.Invoke(act);
            else
                act();
        }
    }
    public class ColorsHelper
    {//yes this is from stackoverflow (with modifications)
     // closed match for hues only:
        public static Dictionary<string, Stopwatch> timers;
        public static void ResetTimers() { foreach (var item in timers.Values) item.Reset(); }
        public static void PrintTimers() { foreach (var item in timers) Console.WriteLine($"{item.Key}: {item.Value.Elapsed}"); }
        static ColorsHelper()
        {
            timers = new Dictionary<string, Stopwatch>();
            foreach (var item in new[] { nameof(closestColor1), nameof(closestColor2), nameof(closestColor3), nameof(ColorDiff), nameof(ColorNum) })
            {
                timers.Add(item, new Stopwatch());
            }
        }
        public static float factorSat = .5f;
        public static float factorBri = .5f;
        public static int closestColor1(IEnumerable<Color> colors, Color target)
        {
            var t = timers[nameof(closestColor1)];
            t.Start();
            var hue1 = target.GetHue();
            var diffs = colors.Select(n => getHueDistance(n.GetHue(), hue1));
            var diffMin = diffs.Min(n => n);
            var result = diffs.ToList().FindIndex(n => n == diffMin);
            t.Stop();
            return result;
        }

        //closed match in RGB space
        //public static int closestColor2(List<Color> colors, Color target)
        //{/*total: 00:00:03.8944648
        //   in GetClosestColor: 00:00:03.7436237*/
        // //00:00:06.9715584
        //    var t = timers[nameof(closestColor2)];
        //    t.Start();
        //    var colorDiffs = colors.Select(n => ColorDiff(n, target)).Min(n => n);
        //    var result = colors.FindIndex(n => ColorDiff(n, target) == colorDiffs);
        //    t.Stop();
        //    return result;
        //}

        public static int closestColor2(IEnumerable<Color> colors, Color target)
        {/*total: 
           in GetClosestColor: */
            //var colorDiffs = colors.Select(n => ColorDiff(n, target)).Min(n => n);
            //return colors.FindIndex(n => ColorDiff(n, target) == colorDiffs);
            //00:00:03.9186904
            var t = timers[nameof(closestColor2)];
            t.Start();
            (double n, int index) min = (int.MaxValue, -1);
            int i = 0;
            foreach (var item in colors)
            {
                (double n, int index) a = (ColorDiff(item, target), i++);
                if (a.n < min.n) min = a;
            }
            t.Stop();
            return min.index;
        }

        // weighed distance using hue, saturation and brightness
        public static int closestColor3(IEnumerable<Color> colors, Color target)
        {
            var t = timers[nameof(closestColor3)];
            t.Start();
            float hue1 = target.GetHue();
            var num1 = ColorNum(target);
            var diffs = colors.Select(n => Math.Abs(ColorNum(n) - num1) +
                                           getHueDistance(n.GetHue(), hue1));
            var diffMin = diffs.Min(x => x);
            var result = diffs.ToList().FindIndex(n => n == diffMin);
            t.Stop();
            return result;
        }

        // color brightness as perceived:
        public static float getBrightness(Color c)
        { return (c.R * 0.299f + c.G * 0.587f + c.B * 0.114f) / 256f; }

        // distance between two hues:
        public static float getHueDistance(float hue1, float hue2)
        {
            float d = Math.Abs(hue1 - hue2); return d > 180 ? 360 - d : d;
        }

        //  weighed only by saturation and brightness (from my trackbars)
        public static float ColorNum(Color c)
        {
            var t = timers[nameof(ColorNum)];
            t.Start();
            var result = c.GetSaturation() * factorSat +
                        getBrightness(c) * factorBri;
            t.Stop();
            return result;
        }

        // distance in RGB space
        public static double ColorDiff(Color c1, Color c2)
        {
            var t = timers[nameof(ColorDiff)];
            t.Start();
            //var result = Math.Sqrt((c1.R - c2.R) * (c1.R - c2.R)
            //                       + (c1.G - c2.G) * (c1.G - c2.G)
            //                       + (c1.B - c2.B) * (c1.B - c2.B));//ColorDiff: 00:00:11.1644311 ColorDiff: 00:00:10.8104914 ColorDiff: 00:00:06.7707368
            //var result = (c1.R - c2.R) * (c1.R - c2.R)
            //           + (c1.G - c2.G) * (c1.G - c2.G)
            //           + (c1.B - c2.B) * (c1.B - c2.B);//ColorDiff: 00:00:11.4789996 00:00:12.7942789 00:00:12.3954112 00:00:05.2996942
            var result = (c1.R - c2.R) * (double)(c1.R - c2.R)
                       + (c1.G - c2.G) * (double)(c1.G - c2.G)
                       + (c1.B - c2.B) * (double)(c1.B - c2.B);//00:00:05.6199386
            //todo: do we actually need sqrt?

            //Console.WriteLine(result);
            t.Stop();
            return result;
        }
    }
    public class PictureBoxWithInterpolationMode : PictureBox
    {
        public InterpolationMode InterpolationMode { get; set; }

        protected override void OnPaint(PaintEventArgs paintEventArgs)
        {
            paintEventArgs.Graphics.InterpolationMode = InterpolationMode;
            base.OnPaint(paintEventArgs);
        }
    }
}
