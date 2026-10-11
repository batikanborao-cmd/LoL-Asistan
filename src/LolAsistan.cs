using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Management;
using System.Net;
using System.Net.Security;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Windows.Forms;

[assembly: System.Reflection.AssemblyTitle("LoL Asistan")]
[assembly: System.Reflection.AssemblyProduct("LoL Asistan")]
[assembly: System.Reflection.AssemblyVersion("1.0.0.0")]

namespace LolAsistan
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            bool created;
            using (var mutex = new Mutex(true, "LolAsistan_tek_ornek", out created))
            {
                if (!created) { MessageBox.Show("LoL Asistan zaten açık.", "LoL Asistan"); return; }
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                // LoL istemcisi 127.0.0.1 uzerinde kendi imzali sertifika kullaniyor; sadece ona izin ver.
                ServicePointManager.ServerCertificateValidationCallback = delegate(object sender,
                    System.Security.Cryptography.X509Certificates.X509Certificate cert,
                    System.Security.Cryptography.X509Certificates.X509Chain chain, SslPolicyErrors errors)
                {
                    var req = sender as HttpWebRequest;
                    if (req != null && req.RequestUri.Host == "127.0.0.1") return true;
                    return errors == SslPolicyErrors.None;
                };
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new MainForm());
            }
        }
    }

    // ---------------- JSON yardimcilari ----------------
    static class J
    {
        public static readonly JavaScriptSerializer S = new JavaScriptSerializer { MaxJsonLength = int.MaxValue };
        public static object Get(object o, string k)
        {
            var d = o as IDictionary<string, object>;
            object v;
            return d != null && d.TryGetValue(k, out v) ? v : null;
        }
        public static int Int(object o) { if (o == null) return 0; try { return Convert.ToInt32(o); } catch { return 0; } }
        public static bool Bool(object o) { return o is bool && (bool)o; }
        public static string Str(object o) { return o == null ? "" : o.ToString(); }
        public static IEnumerable<object> Arr(object o)
        {
            var a = o as IEnumerable;
            if (a == null || o is string || o is IDictionary) yield break;
            foreach (var x in a) yield return x;
        }
    }

    static class Paths
    {
        public static readonly string Dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "LolAsistan");
        public static readonly string Settings = Path.Combine(Dir, "ayarlar.json");
        public static readonly string ChampCache = Path.Combine(Dir, "sampiyonlar.json");
    }

    // ---------------- sampiyon verisi ----------------
    class Champ
    {
        public int Id;
        public string Name;
        public string Alias;
        public override string ToString() { return Name; }
    }

    static class Champs
    {
        static readonly object Lk = new object();
        static readonly Dictionary<int, Champ> byId = new Dictionary<int, Champ>();
        static readonly Dictionary<string, Champ> byKey = new Dictionary<string, Champ>();
        static readonly Dictionary<string, string> nick = new Dictionary<string, string>
        {
            {"mf","MissFortune"},{"tf","TwistedFate"},{"j4","JarvanIV"},{"jarvan","JarvanIV"},{"asol","AurelionSol"},
            {"cho","Chogath"},{"kog","KogMaw"},{"lb","Leblanc"},{"ww","Warwick"},{"mundo","DrMundo"},{"tk","TahmKench"},
            {"tahm","TahmKench"},{"xin","XinZhao"},{"yi","MasterYi"},{"gp","Gangplank"},{"vel","Velkoz"},{"kassa","Kassadin"},
            {"rek","RekSai"},{"heimer","Heimerdinger"},{"fiddle","Fiddlesticks"},{"blitz","Blitzcrank"},{"cass","Cassiopeia"},
            {"kha","Khazix"},{"lee","LeeSin"},{"morde","Mordekaiser"},{"naut","Nautilus"},{"noc","Nocturne"},{"ori","Orianna"},
            {"sej","Sejuani"},{"trynd","Tryndamere"},{"voli","Volibear"},{"wu","MonkeyKing"},{"ez","Ezreal"},{"cait","Caitlyn"},
            {"mao","Maokai"},{"panth","Pantheon"},{"kata","Katarina"},{"malph","Malphite"},{"yas","Yasuo"},{"ali","Alistar"},
            {"ani","Anivia"},{"fizz","Fizz"},{"kayn","Kayn"},{"seju","Sejuani"},{"aatrox","Aatrox"},{"nida","Nidalee"},
            {"zil","Zilean"},{"sion","Sion"},{"gnar","Gnar"},{"brand","Brand"},{"zyra","Zyra"},{"vlad","Vladimir"},{"ksante","KSante"}
        };

        public static string Norm(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            var sb = new StringBuilder();
            foreach (char ch0 in s.ToLowerInvariant())
            {
                char ch = ch0;
                switch (ch)
                {
                    case 'ç': ch = 'c'; break;
                    case 'ş': ch = 's'; break;
                    case 'ğ': ch = 'g'; break;
                    case 'ı': ch = 'i'; break;
                    case 'ö': ch = 'o'; break;
                    case 'ü': ch = 'u'; break;
                }
                if ((ch >= 'a' && ch <= 'z') || (ch >= '0' && ch <= '9')) sb.Append(ch);
            }
            return sb.ToString();
        }

        public static int Count { get { lock (Lk) return byId.Count; } }

        public static void Add(int id, string name, string alias)
        {
            if (id <= 0 || string.IsNullOrEmpty(name)) return;
            lock (Lk)
            {
                Champ c;
                if (!byId.TryGetValue(id, out c)) { c = new Champ { Id = id, Name = name, Alias = alias }; byId[id] = c; }
                else { c.Name = name; if (!string.IsNullOrEmpty(alias)) c.Alias = alias; }
                byKey[Norm(name)] = c;
                if (!string.IsNullOrEmpty(alias)) byKey[Norm(alias)] = c;
            }
        }

        public static string NameOf(int id)
        {
            lock (Lk) { Champ c; return byId.TryGetValue(id, out c) ? c.Name : ("#" + id); }
        }

        static int Lev(string a, string b)
        {
            var d = new int[a.Length + 1, b.Length + 1];
            for (int i = 0; i <= a.Length; i++) d[i, 0] = i;
            for (int j = 0; j <= b.Length; j++) d[0, j] = j;
            for (int i = 1; i <= a.Length; i++)
                for (int j = 1; j <= b.Length; j++)
                {
                    int cost = a[i - 1] == b[j - 1] ? 0 : 1;
                    d[i, j] = Math.Min(Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1), d[i - 1, j - 1] + cost);
                    if (i > 1 && j > 1 && a[i - 1] == b[j - 2] && a[i - 2] == b[j - 1])
                        d[i, j] = Math.Min(d[i, j], d[i - 2, j - 2] + 1);
                }
            return d[a.Length, b.Length];
        }

        // Yazilan metni sampiyona cevir: tam eslesme -> kisaltma -> tek on ek -> kucuk yazim hatasi
        public static Champ Resolve(string text)
        {
            var k = Norm(text);
            if (k.Length == 0) return null;
            lock (Lk)
            {
                Champ c;
                if (byKey.TryGetValue(k, out c)) return c;
                string n;
                if (nick.TryGetValue(k, out n) && byKey.TryGetValue(Norm(n), out c)) return c;
                var pref = byKey.Where(p => p.Key.StartsWith(k)).Select(p => p.Value).Distinct().ToList();
                if (pref.Count == 1) return pref[0];
                if (k.Length >= 4)
                {
                    Champ best = null; int bestD = 99; bool tie = false;
                    foreach (var p in byKey)
                    {
                        int dist = Lev(k, p.Key);
                        if (dist < bestD) { bestD = dist; best = p.Value; tie = false; }
                        else if (dist == bestD && p.Value != best) tie = true;
                    }
                    if (best != null && !tie && bestD <= 2) return best;
                }
            }
            return null;
        }

        public static List<Champ> Suggest(string text, int max)
        {
            var k = Norm(text);
            lock (Lk)
            {
                var all = byId.Values.ToList();
                var r = all.Where(c => Norm(c.Name).StartsWith(k) || Norm(c.Alias).StartsWith(k)).OrderBy(c => c.Name).ToList();
                r.AddRange(all.Where(c => !r.Contains(c) && (Norm(c.Name).Contains(k) || Norm(c.Alias).Contains(k))).OrderBy(c => c.Name));
                var res = Resolve(text);
                if (res != null) { r.Remove(res); r.Insert(0, res); }
                return r.Take(max).ToList();
            }
        }

        public static void LoadCache()
        {
            try
            {
                if (!File.Exists(Paths.ChampCache)) return;
                foreach (var c in J.Arr(J.S.DeserializeObject(File.ReadAllText(Paths.ChampCache, Encoding.UTF8))))
                    Add(J.Int(J.Get(c, "id")), J.Str(J.Get(c, "name")), J.Str(J.Get(c, "alias")));
            }
            catch { }
        }

        public static void SaveCache()
        {
            try
            {
                List<Dictionary<string, object>> list;
                lock (Lk) list = byId.Values.Select(c => new Dictionary<string, object> { { "id", c.Id }, { "name", c.Name }, { "alias", c.Alias } }).ToList();
                Directory.CreateDirectory(Paths.Dir);
                File.WriteAllText(Paths.ChampCache, J.S.Serialize(list), Encoding.UTF8);
            }
            catch { }
        }

        public static bool LoadDDragon()
        {
            try
            {
                using (var wc = new WebClient())
                {
                    wc.Encoding = Encoding.UTF8;
                    var versions = J.S.DeserializeObject(wc.DownloadString("https://ddragon.leagueoflegends.com/api/versions.json"));
                    var v = J.Str(J.Arr(versions).First());
                    var data = J.S.DeserializeObject(wc.DownloadString("https://ddragon.leagueoflegends.com/cdn/" + v + "/data/en_US/champion.json"));
                    var d = J.Get(data, "data") as IDictionary<string, object>;
                    if (d == null) return false;
                    foreach (var kv in d)
                        Add(J.Int(J.Str(J.Get(kv.Value, "key"))), J.Str(J.Get(kv.Value, "name")), J.Str(J.Get(kv.Value, "id")));
                }
                SaveCache();
                return true;
            }
            catch { return false; }
        }
    }

    // ---------------- LoL istemcisi (LCU) ----------------
    class Lcu
    {
        string baseUrl, auth;

        public static Lcu Find()
        {
            try
            {
                using (var s = new ManagementObjectSearcher("SELECT CommandLine FROM Win32_Process WHERE Name='LeagueClientUx.exe'"))
                    foreach (ManagementObject o in s.Get())
                    {
                        var cl = o["CommandLine"] as string;
                        if (cl == null) continue;
                        var port = Regex.Match(cl, "--app-port=(\\d+)").Groups[1].Value;
                        var tok = Regex.Match(cl, "--remoting-auth-token=([\\w-]+)").Groups[1].Value;
                        if (port.Length == 0 || tok.Length == 0) continue;
                        return new Lcu
                        {
                            baseUrl = "https://127.0.0.1:" + port,
                            auth = "Basic " + Convert.ToBase64String(Encoding.ASCII.GetBytes("riot:" + tok))
                        };
                    }
            }
            catch { }
            return null;
        }

        public object Call(string method, string path, object body)
        {
            var req = (HttpWebRequest)WebRequest.Create(baseUrl + path);
            req.Method = method;
            req.Timeout = 4000;
            req.ReadWriteTimeout = 4000;
            req.Headers["Authorization"] = auth;
            req.Accept = "application/json";
            if (body != null)
            {
                var b = Encoding.UTF8.GetBytes(J.S.Serialize(body));
                req.ContentType = "application/json";
                req.ContentLength = b.Length;
                using (var st = req.GetRequestStream()) st.Write(b, 0, b.Length);
            }
            else if (method != "GET") req.ContentLength = 0;
            using (var resp = (HttpWebResponse)req.GetResponse())
            using (var r = new StreamReader(resp.GetResponseStream(), Encoding.UTF8))
            {
                var t = r.ReadToEnd();
                return string.IsNullOrWhiteSpace(t) ? null : J.S.DeserializeObject(t);
            }
        }

        public object Get(string path) { return Call("GET", path, null); }
    }

    class Cfg
    {
        public bool Active, Accept, PickOn, Lock, BanOn;
        public List<int> Picks = new List<int>();
        public List<int> Bans = new List<int>();
    }

    // ---------------- otomasyon ----------------
    class Engine
    {
        Lcu lcu;
        string lastPhase = "";
        DateTime nextConnect = DateTime.MinValue, nextSwap = DateTime.MinValue;
        readonly HashSet<string> done = new HashSet<string>();
        public Action<string> Log;
        public Action<string, bool> Status;
        public Action ChampsChanged;

        static readonly Dictionary<string, string> phaseText = new Dictionary<string, string>
        {
            {"None","Ana menü"},{"Lobby","Lobide"},{"Matchmaking","Sırada, maç aranıyor..."},{"ReadyCheck","Maç bulundu!"},
            {"ChampSelect","Şampiyon seçimi"},{"GameStart","Oyun başlıyor"},{"InProgress","Oyunda"},{"Reconnect","Yeniden bağlan"},
            {"WaitingForStats","Oyun bitti"},{"PreEndOfGame","Oyun bitti"},{"EndOfGame","Oyun sonu ekranı"}
        };

        static Dictionary<string, object> Body(int champ, bool? completed)
        {
            var d = new Dictionary<string, object> { { "championId", champ } };
            if (completed.HasValue) d["completed"] = completed.Value;
            return d;
        }

        public void Tick(Cfg cfg)
        {
            if (lcu == null)
            {
                if (DateTime.Now < nextConnect) return;
                nextConnect = DateTime.Now.AddSeconds(3);
                var l = Lcu.Find();
                if (l == null) { Status("LoL istemcisi bekleniyor...", false); return; }
                try
                {
                    var sum = l.Get("/lol-game-data/assets/v1/champion-summary.json");
                    foreach (var c in J.Arr(sum)) Champs.Add(J.Int(J.Get(c, "id")), J.Str(J.Get(c, "name")), J.Str(J.Get(c, "alias")));
                    Champs.SaveCache();
                    ChampsChanged();
                    l.Get("/lol-gameflow/v1/gameflow-phase");
                }
                catch { Status("LoL istemcisi açılıyor...", false); return; }
                lcu = l;
                lastPhase = null;
                Log("İstemciye bağlanıldı.");
            }

            string phase;
            try { phase = J.Str(lcu.Get("/lol-gameflow/v1/gameflow-phase")); }
            catch { lcu = null; lastPhase = ""; Log("İstemci bağlantısı koptu, tekrar bağlanılacak."); Status("LoL istemcisi bekleniyor...", false); return; }

            if (phase != lastPhase)
            {
                lastPhase = phase;
                if (phase != "ChampSelect") done.Clear();
                string t;
                Status(phaseText.TryGetValue(phase, out t) ? t : phase, true);
            }
            if (!cfg.Active) return;

            if (phase == "ReadyCheck" && cfg.Accept)
            {
                try
                {
                    var rc = lcu.Get("/lol-matchmaking/v1/ready-check");
                    if (J.Str(J.Get(rc, "playerResponse")) == "None")
                    {
                        lcu.Call("POST", "/lol-matchmaking/v1/ready-check/accept", null);
                        Log("Maç kabul edildi.");
                    }
                }
                catch { }
            }
            else if (phase == "ChampSelect")
            {
                try { ChampSelect(cfg); } catch { }
            }
        }

        void ChampSelect(Cfg cfg)
        {
            var s = lcu.Get("/lol-champ-select/v1/session");
            int me = J.Int(J.Get(s, "localPlayerCellId"));
            if (J.Bool(J.Get(s, "benchEnabled"))) { Aram(s, me, cfg); return; }

            var all = new List<object>();
            foreach (var g in J.Arr(J.Get(s, "actions"))) foreach (var a in J.Arr(g)) all.Add(a);

            var banned = new HashSet<int>();
            var taken = new HashSet<int>();
            var mates = new HashSet<int>();
            var bans = J.Get(s, "bans");
            foreach (var x in J.Arr(J.Get(bans, "myTeamBans"))) banned.Add(J.Int(x));
            foreach (var x in J.Arr(J.Get(bans, "theirTeamBans"))) banned.Add(J.Int(x));
            foreach (var a in all)
            {
                int cid = J.Int(J.Get(a, "championId"));
                if (!J.Bool(J.Get(a, "completed")) || cid <= 0) continue;
                var type = J.Str(J.Get(a, "type"));
                if (type == "ban") banned.Add(cid);
                else if (type == "pick" && J.Int(J.Get(a, "actorCellId")) != me) taken.Add(cid);
            }
            foreach (var m in J.Arr(J.Get(s, "myTeam")))
            {
                if (J.Int(J.Get(m, "cellId")) == me) continue;
                int c1 = J.Int(J.Get(m, "championId")), c2 = J.Int(J.Get(m, "championPickIntent"));
                if (c1 > 0) mates.Add(c1);
                if (c2 > 0) mates.Add(c2);
            }

            HashSet<int> pickable = null;
            foreach (var a in all)
            {
                if (J.Int(J.Get(a, "actorCellId")) != me || J.Bool(J.Get(a, "completed"))) continue;
                int id = J.Int(J.Get(a, "id"));
                string type = J.Str(J.Get(a, "type"));
                bool inProgress = J.Bool(J.Get(a, "isInProgress"));
                int cur = J.Int(J.Get(a, "championId"));
                string path = "/lol-champ-select/v1/session/actions/" + id;

                if (type == "ban" && inProgress && cfg.BanOn)
                {
                    int c = cfg.Bans.FirstOrDefault(x => !banned.Contains(x) && !mates.Contains(x) && !taken.Contains(x));
                    if (c > 0)
                    {
                        try { lcu.Call("PATCH", path, Body(c, true)); Log("Banlandı: " + Champs.NameOf(c)); } catch { }
                    }
                    else if (done.Add("noban" + id))
                        Log(cfg.Bans.Count == 0 ? "Ban listesi boş, ban sende." : "Ban listesindeki şampiyonların hiçbiri uygun değil, ban sende.");
                }
                else if (type == "pick" && cfg.PickOn)
                {
                    if (pickable == null)
                    {
                        pickable = new HashSet<int>();
                        foreach (var x in J.Arr(lcu.Get("/lol-champ-select/v1/pickable-champion-ids"))) pickable.Add(J.Int(x));
                    }
                    int c = cfg.Picks.FirstOrDefault(x => pickable.Contains(x) && !banned.Contains(x) && !taken.Contains(x) && !mates.Contains(x));
                    if (c <= 0)
                    {
                        if (inProgress && done.Add("nopick" + id))
                            Log(cfg.Picks.Count == 0 ? "Seçim listesi boş, seçim sende." : "Seçim listesindeki şampiyonların hiçbiri uygun değil, seçim sende.");
                        continue;
                    }
                    if (inProgress)
                    {
                        if (cfg.Lock)
                        {
                            try { lcu.Call("PATCH", path, Body(c, true)); Log("Seçildi ve kilitlendi: " + Champs.NameOf(c)); } catch { }
                        }
                        else if (cur != c)
                        {
                            try { lcu.Call("PATCH", path, Body(c, null)); Log("Seçildi (kilitlemek sende): " + Champs.NameOf(c)); } catch { }
                        }
                    }
                    else if (cur != c && done.Add("hover" + id + "_" + c))
                    {
                        try { lcu.Call("PATCH", path, Body(c, null)); Log("Ön seçim (hover): " + Champs.NameOf(c)); } catch { }
                    }
                }
            }
        }

        void Aram(object s, int me, Cfg cfg)
        {
            if (!cfg.PickOn || cfg.Picks.Count == 0) return;
            int mine = 0;
            foreach (var m in J.Arr(J.Get(s, "myTeam")))
                if (J.Int(J.Get(m, "cellId")) == me) mine = J.Int(J.Get(m, "championId"));
            Func<int, int> rank = x => { int i = cfg.Picks.IndexOf(x); return i < 0 ? 999 : i; };
            int best = 0, bestRank = rank(mine);
            foreach (var b in J.Arr(J.Get(s, "benchChampions")))
            {
                int id = J.Int(J.Get(b, "championId"));
                int r = rank(id);
                if (r < bestRank) { best = id; bestRank = r; }
            }
            if (best > 0 && DateTime.Now > nextSwap)
            {
                nextSwap = DateTime.Now.AddSeconds(3);
                try { lcu.Call("POST", "/lol-champ-select/v1/session/bench/swap/" + best, null); Log("ARAM: bench'ten alındı: " + Champs.NameOf(best)); } catch { }
            }
        }
    }

    // ---------------- ayarlar ----------------
    public class Ayarlar
    {
        public bool active { get; set; }
        public bool accept { get; set; }
        public bool pickOn { get; set; }
        public bool lockIn { get; set; }
        public bool banOn { get; set; }
        public string[] picks { get; set; }
        public string[] bans { get; set; }
        public Ayarlar() { active = accept = pickOn = lockIn = banOn = true; picks = new string[3]; bans = new string[3]; }
    }

    // ---------------- arayuz ----------------
    class MainForm : Form
    {
        readonly Engine engine = new Engine();
        readonly System.Windows.Forms.Timer timer = new System.Windows.Forms.Timer();
        int busy;
        bool loading = true, suppress;

        Label statusLbl;
        CheckBox chkActive, chkAccept, chkPick, chkLock, chkBan;
        readonly TextBox[] pickBoxes = new TextBox[3], banBoxes = new TextBox[3];
        readonly Label[] pickInfo = new Label[3], banInfo = new Label[3];
        ListBox suggest;
        TextBox suggestFor;
        TextBox logBox;

        static readonly Color Ok = Color.FromArgb(0, 120, 60), Bad = Color.FromArgb(200, 40, 40);

        public MainForm()
        {
            Text = "LoL Asistan";
            Font = new Font("Segoe UI", 9f);
            ClientSize = new Size(420, 590);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            StartPosition = FormStartPosition.CenterScreen;
            try { Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath); } catch { }

            var title = new Label { Text = "LoL Asistan", AutoSize = true, Location = new Point(14, 10), Font = new Font("Segoe UI", 14f, FontStyle.Bold) };
            statusLbl = new Label { AutoSize = true, Location = new Point(16, 44), Font = new Font("Segoe UI", 9f, FontStyle.Bold), ForeColor = Color.Gray, Text = "LoL istemcisi bekleniyor..." };
            chkActive = new CheckBox { Text = "Asistan açık", AutoSize = true, Location = new Point(16, 70), Checked = true };
            chkAccept = new CheckBox { Text = "Maçı otomatik kabul et", AutoSize = true, Location = new Point(16, 94), Checked = true };
            Controls.AddRange(new Control[] { title, statusLbl, chkActive, chkAccept });

            var gPick = MakeGroup("Seçilecek şampiyonlar (öncelik sırasıyla)", 126, pickBoxes, pickInfo);
            chkPick = new CheckBox { Text = "Otomatik seç", AutoSize = true, Location = new Point(14, 128), Checked = true };
            chkLock = new CheckBox { Text = "Kilitle", AutoSize = true, Location = new Point(130, 128), Checked = true };
            var hint = new Label { Text = "ARAM: bench'ten bu sıraya göre alır", AutoSize = true, Location = new Point(200, 129), ForeColor = Color.Gray };
            gPick.Controls.AddRange(new Control[] { chkPick, chkLock, hint });

            var gBan = MakeGroup("Banlanacak şampiyonlar (öncelik sırasıyla)", 296, banBoxes, banInfo);
            chkBan = new CheckBox { Text = "Otomatik banla", AutoSize = true, Location = new Point(14, 128), Checked = true };
            gBan.Controls.Add(chkBan);
            Controls.AddRange(new Control[] { gPick, gBan });

            logBox = new TextBox { Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Vertical, BackColor = Color.White, Location = new Point(14, 468), Size = new Size(392, 108) };
            Controls.Add(logBox);

            suggest = new ListBox { Visible = false, Width = 200, Height = 140, IntegralHeight = false, TabStop = false };
            suggest.MouseDown += (s, e) =>
            {
                int i = suggest.IndexFromPoint(e.Location);
                if (i >= 0 && suggestFor != null) Accept(suggestFor, (Champ)suggest.Items[i]);
            };
            Controls.Add(suggest);
            suggest.BringToFront();

            engine.Log = m => Ui(() => AddLog(m));
            engine.Status = (t, ok) => Ui(() => { statusLbl.Text = t; statusLbl.ForeColor = ok ? Ok : Color.Gray; });
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

        GroupBox MakeGroup(string title, int y, TextBox[] boxes, Label[] infos)
        {
            var g = new GroupBox { Text = title, Location = new Point(14, y), Size = new Size(392, 160) };
            for (int i = 0; i < 3; i++)
            {
                g.Controls.Add(new Label { Text = (i + 1) + ".", AutoSize = true, Location = new Point(14, 30 + i * 32) });
                var tb = new TextBox { Location = new Point(36, 27 + i * 32), Width = 180 };
                var info = new Label { AutoSize = true, Location = new Point(224, 30 + i * 32) };
                boxes[i] = tb; infos[i] = info;
                int idx = i;
                tb.TextChanged += (s, e) => { UpdateInfo(tb, info); if (!suppress) { ShowSuggest(tb); SaveSettings(); } };
                tb.KeyDown += (s, e) => OnBoxKey(tb, e);
                tb.Leave += (s, e) => BeginInvoke((Action)(() =>
                {
                    if (suggestFor == tb && !suggest.Focused) HideSuggest();
                    var c = Champs.Resolve(tb.Text);
                    if (c != null && tb.Text != c.Name) SetText(tb, c.Name);
                }));
                g.Controls.Add(tb);
                g.Controls.Add(info);
            }
            return g;
        }

        void UpdateInfo(TextBox tb, Label info)
        {
            if (tb.Text.Trim().Length == 0) { info.Text = ""; return; }
            if (Champs.Count == 0) { info.Text = "liste yükleniyor..."; info.ForeColor = Color.Gray; return; }
            var c = Champs.Resolve(tb.Text);
            if (c != null) { info.Text = "✓ " + c.Name; info.ForeColor = Ok; }
            else { info.Text = "✗ bulunamadı"; info.ForeColor = Bad; }
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
            var p = PointToClient(tb.Parent.PointToScreen(new Point(tb.Left, tb.Bottom)));
            suggest.Location = p;
            suggest.Width = tb.Width;
            suggest.Height = Math.Min(list.Count, 8) * suggest.ItemHeight + 6;
            suggest.Visible = true;
            suggest.BringToFront();
        }

        void HideSuggest() { suggest.Visible = false; suggestFor = null; }

        void Accept(TextBox tb, Champ c)
        {
            SetText(tb, c.Name);
            HideSuggest();
            tb.Focus();
        }

        void OnBoxKey(TextBox tb, KeyEventArgs e)
        {
            bool open = suggest.Visible && suggestFor == tb;
            if (e.KeyCode == Keys.Down && open) { if (suggest.SelectedIndex < suggest.Items.Count - 1) suggest.SelectedIndex++; e.Handled = true; }
            else if (e.KeyCode == Keys.Up && open) { if (suggest.SelectedIndex > 0) suggest.SelectedIndex--; e.Handled = true; }
            else if (e.KeyCode == Keys.Enter || e.KeyCode == Keys.Tab && open)
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

        void AddLog(string m) { logBox.AppendText(DateTime.Now.ToString("HH:mm:ss") + "  " + m + "\r\n"); }

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
