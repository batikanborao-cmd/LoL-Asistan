using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace LolAsistan
{
    // LoL istemcisine benzeyen renkler
    static class Theme
    {
        public static readonly Color Bg = Color.FromArgb(10, 20, 40);
        public static readonly Color BgDark = Color.FromArgb(1, 10, 19);
        public static readonly Color Gold = Color.FromArgb(200, 170, 110);
        public static readonly Color GoldDark = Color.FromArgb(120, 90, 40);
        public static readonly Color GoldLight = Color.FromArgb(240, 230, 210);
        public static readonly Color Teal = Color.FromArgb(10, 200, 185);
        public static readonly Color Grey = Color.FromArgb(160, 155, 140);
        public static readonly Color Red = Color.FromArgb(232, 64, 87);
        public static Font F(float size, FontStyle st = FontStyle.Regular) { return new Font("Segoe UI", size, st); }
        public static Font Semi(float size) { return new Font("Segoe UI Semibold", size); }
    }

    // Altin cerceveli onay kutusu
    class GoldCheck : Control
    {
        bool isChecked, hover;
        public event EventHandler CheckedChanged;

        public GoldCheck(string text, bool on)
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
            Text = text;
            isChecked = on;
            Font = Theme.F(9.5f);
            ForeColor = Theme.GoldLight;
            BackColor = Theme.Bg;
            Cursor = Cursors.Hand;
            Size = new Size(TextRenderer.MeasureText(text, Font).Width + 30, 24);
        }

        public bool Checked
        {
            get { return isChecked; }
            set { if (isChecked == value) return; isChecked = value; Invalidate(); if (CheckedChanged != null) CheckedChanged(this, EventArgs.Empty); }
        }

        protected override void OnClick(EventArgs e) { base.OnClick(e); Checked = !Checked; }
        protected override void OnMouseEnter(EventArgs e) { hover = true; Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { hover = false; Invalidate(); base.OnMouseLeave(e); }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.Clear(BackColor);
            g.SmoothingMode = SmoothingMode.AntiAlias;
            var box = new Rectangle(1, (Height - 16) / 2, 16, 16);
            using (var b = new SolidBrush(isChecked ? Theme.Teal : Theme.BgDark)) g.FillRectangle(b, box);
            using (var p = new Pen(hover ? Theme.GoldLight : Theme.Gold, 1.5f)) g.DrawRectangle(p, box);
            if (isChecked)
                using (var p = new Pen(Theme.BgDark, 2.2f) { StartCap = LineCap.Round, EndCap = LineCap.Round, LineJoin = LineJoin.Round })
                    g.DrawLines(p, new[] { new PointF(box.X + 3.5f, box.Y + 8.5f), new PointF(box.X + 6.8f, box.Y + 11.8f), new PointF(box.X + 12.5f, box.Y + 4.5f) });
            TextRenderer.DrawText(g, Text, Font, new Point(24, (Height - Font.Height) / 2), isChecked ? Theme.GoldLight : Theme.Grey);
        }
    }

    // "SECILECEKLER ─────" tarzi bolum basligi
    class Section : Control
    {
        public Section(string text, int width)
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint, true);
            Text = text;
            Font = Theme.Semi(9.5f);
            BackColor = Theme.Bg;
            Size = new Size(width, 22);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.Clear(BackColor);
            var sz = TextRenderer.MeasureText(Text, Font);
            TextRenderer.DrawText(e.Graphics, Text, Font, new Point(0, 2), Theme.Gold);
            using (var b = new LinearGradientBrush(new Rectangle(sz.Width + 6, 0, Width - sz.Width - 6, 2), Theme.GoldDark, Theme.Bg, 0f))
                e.Graphics.FillRectangle(b, sz.Width + 6, Height / 2, Width - sz.Width - 6, 1);
        }
    }

    // Durum gostergesi: renkli nokta + yazi
    class StatusPill : Control
    {
        bool ok;
        public StatusPill()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
            Font = Theme.Semi(9f);
            BackColor = Theme.BgDark;
            Size = new Size(210, 26);
        }

        public void Set(string text, bool isOk) { Text = text; ok = isOk; Invalidate(); }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.Clear(BackColor);
            g.SmoothingMode = SmoothingMode.AntiAlias;
            var tw = TextRenderer.MeasureText(Text, Font).Width;
            int w = tw + 30, x = Width - w - 1;
            using (var path = Rounded(new Rectangle(x, 1, w, Height - 3), 11))
            {
                using (var b = new SolidBrush(Theme.Bg)) g.FillPath(b, path);
                using (var p = new Pen(Theme.GoldDark)) g.DrawPath(p, path);
            }
            var dot = ok ? Theme.Teal : Theme.Grey;
            using (var b = new SolidBrush(Color.FromArgb(60, dot))) g.FillEllipse(b, x + 7, Height / 2 - 7, 13, 13);
            using (var b = new SolidBrush(dot)) g.FillEllipse(b, x + 10, Height / 2 - 4, 7, 7);
            TextRenderer.DrawText(g, Text, Font, new Point(x + 22, (Height - Font.Height) / 2 - 1), ok ? Theme.GoldLight : Theme.Grey);
        }

        static GraphicsPath Rounded(Rectangle r, int rad)
        {
            var p = new GraphicsPath();
            int d = rad * 2;
            p.AddArc(r.X, r.Y, d, d, 180, 90);
            p.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            p.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            p.CloseFigure();
            return p;
        }
    }

    // Altin kenarli metin kutusu
    class GoldInput : Panel
    {
        public readonly TextBox Box;
        public GoldInput(int width)
        {
            Size = new Size(width, 30);
            Padding = new Padding(8, 6, 6, 4);
            BackColor = Theme.BgDark;
            Box = new TextBox
            {
                BorderStyle = BorderStyle.None, BackColor = Theme.BgDark, ForeColor = Theme.GoldLight,
                Font = Theme.F(10f), Dock = DockStyle.Fill
            };
            Controls.Add(Box);
            Box.GotFocus += (s, e) => Invalidate();
            Box.LostFocus += (s, e) => Invalidate();
            Click += (s, e) => Box.Focus();
            Cursor = Cursors.IBeam;
            SetStyle(ControlStyles.ResizeRedraw | ControlStyles.OptimizedDoubleBuffer, true);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            using (var p = new Pen(Box.Focused ? Theme.Gold : Theme.GoldDark))
                e.Graphics.DrawRectangle(p, 0, 0, Width - 1, Height - 1);
        }
    }

    class MainForm : Form
    {
        [DllImport("user32.dll")] static extern bool ReleaseCapture();
        [DllImport("user32.dll")] static extern IntPtr SendMessage(IntPtr h, int msg, IntPtr w, IntPtr l);

        readonly Engine engine = new Engine();
        readonly System.Windows.Forms.Timer timer = new System.Windows.Forms.Timer();
        int busy;
        bool loading = true, suppress;

        StatusPill status;
        GoldCheck chkActive, chkAccept, chkPick, chkLock, chkBan;
        readonly TextBox[] pickBoxes = new TextBox[3], banBoxes = new TextBox[3];
        readonly Label[] pickInfo = new Label[3], banInfo = new Label[3];
        Panel suggestHost;
        ListBox suggest;
        TextBox suggestFor;
        TextBox logBox;

        const int W = 440;

        protected override CreateParams CreateParams
        {
            get
            {
                var cp = base.CreateParams;
                cp.Style |= 0x00020000; // WS_MINIMIZEBOX: gorev cubugundan kucultme calissin
                return cp;
            }
        }

        public MainForm()
        {
            Text = "LoL Asistan";
            FormBorderStyle = FormBorderStyle.None;
            ClientSize = new Size(W, 640);
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = Theme.Bg;
            ForeColor = Theme.GoldLight;
            Font = Theme.F(9.5f);
            DoubleBuffered = true;
            try { Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath); } catch { }

            BuildHeader();

            chkActive = new GoldCheck("Asistan açık", true) { Location = new Point(20, 72) };
            chkAccept = new GoldCheck("Maçı otomatik kabul et", true) { Location = new Point(170, 72) };
            Controls.AddRange(new Control[] { chkActive, chkAccept });

            int y = 114;
            Controls.Add(new Section("SEÇİLECEKLER", W - 40) { Location = new Point(20, y) });
            y = AddRows(y + 30, pickBoxes, pickInfo);
            chkPick = new GoldCheck("Otomatik seç", true) { Location = new Point(20, y) };
            chkLock = new GoldCheck("Kilitle", true) { Location = new Point(150, y) };
            Controls.AddRange(new Control[] { chkPick, chkLock });
            Controls.Add(new Label { Text = "ARAM'da bench'ten de alır", AutoSize = true, ForeColor = Theme.Grey, Font = Theme.F(8.5f), Location = new Point(240, y + 4) });

            y += 44;
            Controls.Add(new Section("BANLANACAKLAR", W - 40) { Location = new Point(20, y) });
            y = AddRows(y + 30, banBoxes, banInfo);
            chkBan = new GoldCheck("Otomatik banla", true) { Location = new Point(20, y) };
            Controls.Add(chkBan);

            y += 44;
            Controls.Add(new Section("KAYIT", W - 40) { Location = new Point(20, y) });
            var logHost = new GoldInput(W - 40) { Location = new Point(20, y + 28), Height = ClientSize.Height - y - 48, Cursor = Cursors.Default };
            logBox = logHost.Box;
            logBox.Multiline = true;
            logBox.ReadOnly = true;
            logBox.BackColor = Theme.BgDark;
            logBox.ForeColor = Theme.Grey;
            logBox.Font = Theme.F(8.75f);
            logBox.TabStop = false;
            Controls.Add(logHost);

            suggestHost = new Panel { Visible = false, BackColor = Theme.Gold, Padding = new Padding(1) };
            suggest = new ListBox
            {
                BorderStyle = BorderStyle.None, BackColor = Theme.BgDark, ForeColor = Theme.GoldLight, Font = Theme.F(10f),
                IntegralHeight = false, TabStop = false, Dock = DockStyle.Fill, DrawMode = DrawMode.OwnerDrawFixed, ItemHeight = 24
            };
            suggest.DrawItem += DrawSuggestItem;
            suggest.MouseDown += (s, e) =>
            {
                int i = suggest.IndexFromPoint(e.Location);
                if (i >= 0 && suggestFor != null) Accept(suggestFor, (Champ)suggest.Items[i]);
            };
            suggest.MouseMove += (s, e) =>
            {
                int i = suggest.IndexFromPoint(e.Location);
                if (i >= 0 && i != suggest.SelectedIndex) suggest.SelectedIndex = i;
            };
            suggestHost.Controls.Add(suggest);
            Controls.Add(suggestHost);
            suggestHost.BringToFront();

            engine.Log = m => Ui(() => AddLog(m));
            engine.Status = (t, ok) => Ui(() => status.Set(t, ok));
            engine.ChampsChanged = () => Ui(RefreshAll);

            LoadSettings();
            Champs.LoadCache();
            RefreshAll();

            foreach (var cb in new[] { chkActive, chkAccept, chkPick, chkLock, chkBan }) cb.CheckedChanged += (s, e) => SaveSettings();
            chkActive.CheckedChanged += (s, e) => AddLog(chkActive.Checked ? "Asistan açıldı." : "Asistan kapatıldı (sadece izliyor).");
            loading = false;

            Task.Run(() =>
            {
                bool ok = Champs.LoadDDragon();
                Ui(() => { RefreshAll(); if (!ok && Champs.Count == 0) AddLog("Şampiyon listesi indirilemedi; LoL açılınca istemciden alınacak."); });
            });

            AddLog("Başlatıldı.");
            timer.Interval = 1000;
            timer.Tick += (s, e) => OnTimer();
            Shown += (s, e) => { timer.Start(); OnTimer(); };
            FormClosing += (s, e) => { timer.Stop(); SaveSettings(); };
        }

        void BuildHeader()
        {
            var header = new Panel { Location = new Point(0, 0), Size = new Size(W, 56), BackColor = Theme.BgDark };
            MouseEventHandler drag = (s, e) =>
            {
                if (e.Button != MouseButtons.Left) return;
                ReleaseCapture();
                SendMessage(Handle, 0xA1, (IntPtr)2, IntPtr.Zero); // basliktan surukle
            };
            header.MouseDown += drag;

            var logo = new PictureBox { Location = new Point(16, 12), Size = new Size(32, 32), SizeMode = PictureBoxSizeMode.Zoom, BackColor = Theme.BgDark };
            try
            {
                var st = typeof(MainForm).Assembly.GetManifestResourceStream("icon.png");
                if (st != null) logo.Image = Image.FromStream(st);
            }
            catch { }
            logo.MouseDown += drag;

            var title = new Label { Text = "LOL ASİSTAN", AutoSize = true, Location = new Point(56, 16), Font = new Font("Segoe UI", 13f, FontStyle.Bold), ForeColor = Theme.GoldLight, BackColor = Theme.BgDark };
            title.MouseDown += drag;

            status = new StatusPill { Location = new Point(W - 290, 15) };
            status.Set("LoL bekleniyor", false);
            status.MouseDown += drag;

            var min = HeaderButton("—", W - 72);
            min.Click += (s, e) => WindowState = FormWindowState.Minimized;
            var close = HeaderButton("✕", W - 38);
            close.Click += (s, e) => Close();

            var line = new Panel { Location = new Point(0, 55), Size = new Size(W, 1), BackColor = Theme.GoldDark };
            header.Controls.AddRange(new Control[] { logo, title, status, min, close, line });
            Controls.Add(header);
        }

        Label HeaderButton(string text, int x)
        {
            var b = new Label
            {
                Text = text, Location = new Point(x, 13), Size = new Size(30, 30), TextAlign = ContentAlignment.MiddleCenter,
                ForeColor = Theme.Grey, BackColor = Theme.BgDark, Font = Theme.F(11f), Cursor = Cursors.Hand
            };
            b.MouseEnter += (s, e) => { b.ForeColor = Theme.GoldLight; b.BackColor = Theme.Bg; };
            b.MouseLeave += (s, e) => { b.ForeColor = Theme.Grey; b.BackColor = Theme.BgDark; };
            return b;
        }

        int AddRows(int y, TextBox[] boxes, Label[] infos)
        {
            for (int i = 0; i < 3; i++)
            {
                var num = new Label
                {
                    Text = (i + 1).ToString(), Location = new Point(20, y + i * 38), Size = new Size(22, 30),
                    TextAlign = ContentAlignment.MiddleCenter, ForeColor = Theme.Gold, Font = Theme.Semi(11f)
                };
                var input = new GoldInput(210) { Location = new Point(48, y + i * 38) };
                var info = new Label { AutoSize = true, Location = new Point(270, y + i * 38 + 6), Font = Theme.Semi(9.5f) };
                var tb = input.Box;
                boxes[i] = tb; infos[i] = info;
                tb.TextChanged += (s, e) => { UpdateInfo(tb, info); if (!suppress) { ShowSuggest(tb); SaveSettings(); } };
                tb.KeyDown += (s, e) => OnBoxKey(tb, e);
                tb.Leave += (s, e) => BeginInvoke((Action)(() =>
                {
                    if (suggestFor == tb && !suggest.Focused) HideSuggest();
                    var c = Champs.Resolve(tb.Text);
                    if (c != null && tb.Text != c.Name) SetText(tb, c.Name);
                }));
                Controls.AddRange(new Control[] { num, input, info });
            }
            return y + 3 * 38 + 4;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            using (var p = new Pen(Theme.GoldDark)) e.Graphics.DrawRectangle(p, 0, 0, ClientSize.Width - 1, ClientSize.Height - 1);
        }

        void DrawSuggestItem(object sender, DrawItemEventArgs e)
        {
            if (e.Index < 0) return;
            bool sel = (e.State & DrawItemState.Selected) != 0;
            using (var b = new SolidBrush(sel ? Theme.Bg : Theme.BgDark)) e.Graphics.FillRectangle(b, e.Bounds);
            if (sel) using (var b = new SolidBrush(Theme.Teal)) e.Graphics.FillRectangle(b, e.Bounds.X, e.Bounds.Y + 4, 3, e.Bounds.Height - 8);
            TextRenderer.DrawText(e.Graphics, suggest.Items[e.Index].ToString(), suggest.Font,
                new Point(e.Bounds.X + 10, e.Bounds.Y + (e.Bounds.Height - suggest.Font.Height) / 2), sel ? Theme.GoldLight : Theme.Grey);
        }

        void UpdateInfo(TextBox tb, Label info)
        {
            if (tb.Text.Trim().Length == 0) { info.Text = ""; return; }
            if (Champs.Count == 0) { info.Text = "liste yükleniyor..."; info.ForeColor = Theme.Grey; return; }
            var c = Champs.Resolve(tb.Text);
            if (c != null) { info.Text = "✓ " + c.Name; info.ForeColor = Theme.Teal; }
            else { info.Text = "✗ bulunamadı"; info.ForeColor = Theme.Red; }
        }

        void RefreshAll()
        {
            for (int i = 0; i < 3; i++) { UpdateInfo(pickBoxes[i], pickInfo[i]); UpdateInfo(banBoxes[i], banInfo[i]); }
        }

        void SetText(TextBox tb, string t)
        {
            suppress = true;
            tb.Text = t;
            tb.SelectionStart = t.Length;
            suppress = false;
            SaveSettings();
        }

        void ShowSuggest(TextBox tb)
        {
            if (!tb.Focused || tb.Text.Trim().Length == 0) { HideSuggest(); return; }
            var list = Champs.Suggest(tb.Text, 30);
            if (list.Count == 0) { HideSuggest(); return; }
            suggestFor = tb;
            suggest.BeginUpdate();
            suggest.Items.Clear();
            foreach (var c in list) suggest.Items.Add(c);
            suggest.SelectedIndex = 0;
            suggest.EndUpdate();
            var host = tb.Parent;
            var p = PointToClient(host.Parent.PointToScreen(new Point(host.Left, host.Bottom - 1)));
            suggestHost.Location = p;
            suggestHost.Size = new Size(host.Width, Math.Min(list.Count, 7) * suggest.ItemHeight + 2);
            suggestHost.Visible = true;
            suggestHost.BringToFront();
        }

        void HideSuggest() { suggestHost.Visible = false; suggestFor = null; }

        void Accept(TextBox tb, Champ c)
        {
            SetText(tb, c.Name);
            HideSuggest();
            tb.Focus();
        }

        void OnBoxKey(TextBox tb, KeyEventArgs e)
        {
            bool open = suggestHost.Visible && suggestFor == tb;
            if (e.KeyCode == Keys.Down && open) { if (suggest.SelectedIndex < suggest.Items.Count - 1) suggest.SelectedIndex++; e.Handled = true; }
            else if (e.KeyCode == Keys.Up && open) { if (suggest.SelectedIndex > 0) suggest.SelectedIndex--; e.Handled = true; }
            else if (e.KeyCode == Keys.Enter)
            {
                if (open && suggest.SelectedItem != null) Accept(tb, (Champ)suggest.SelectedItem);
                else { var c = Champs.Resolve(tb.Text); if (c != null) SetText(tb, c.Name); }
                e.Handled = e.SuppressKeyPress = true;
            }
            else if (e.KeyCode == Keys.Escape) { HideSuggest(); e.Handled = e.SuppressKeyPress = true; }
        }

        void Ui(Action a)
        {
            if (IsDisposed || !IsHandleCreated) return;
            try { BeginInvoke(a); } catch { }
        }

        void AddLog(string m) { logBox.AppendText(DateTime.Now.ToString("HH:mm:ss") + "   " + m + "\r\n"); }

        List<int> Ids(TextBox[] boxes)
        {
            var r = new List<int>();
            foreach (var tb in boxes) { var c = Champs.Resolve(tb.Text); if (c != null && !r.Contains(c.Id)) r.Add(c.Id); }
            return r;
        }

        void OnTimer()
        {
            if (Interlocked.CompareExchange(ref busy, 1, 0) != 0) return;
            var cfg = new Cfg
            {
                Active = chkActive.Checked, Accept = chkAccept.Checked, PickOn = chkPick.Checked,
                Lock = chkLock.Checked, BanOn = chkBan.Checked, Picks = Ids(pickBoxes), Bans = Ids(banBoxes)
            };
            Task.Run(() =>
            {
                try { engine.Tick(cfg); } catch { }
                finally { Interlocked.Exchange(ref busy, 0); }
            });
        }

        void LoadSettings()
        {
            try
            {
                if (!File.Exists(Paths.Settings)) return;
                var a = J.S.Deserialize<Ayarlar>(File.ReadAllText(Paths.Settings, Encoding.UTF8));
                chkActive.Checked = a.active; chkAccept.Checked = a.accept; chkPick.Checked = a.pickOn;
                chkLock.Checked = a.lockIn; chkBan.Checked = a.banOn;
                suppress = true;
                for (int i = 0; i < 3; i++)
                {
                    if (a.picks != null && i < a.picks.Length) pickBoxes[i].Text = a.picks[i] ?? "";
                    if (a.bans != null && i < a.bans.Length) banBoxes[i].Text = a.bans[i] ?? "";
                }
                suppress = false;
            }
            catch { suppress = false; }
        }

        void SaveSettings()
        {
            if (loading) return;
            try
            {
                var a = new Ayarlar
                {
                    active = chkActive.Checked, accept = chkAccept.Checked, pickOn = chkPick.Checked,
                    lockIn = chkLock.Checked, banOn = chkBan.Checked,
                    picks = pickBoxes.Select(t => t.Text).ToArray(), bans = banBoxes.Select(t => t.Text).ToArray()
                };
                Directory.CreateDirectory(Paths.Dir);
                File.WriteAllText(Paths.Settings, J.S.Serialize(a), Encoding.UTF8);
            }
            catch { }
        }
    }
}

