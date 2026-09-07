using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Net.NetworkInformation;
using System.Security.Principal;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DNSDegistirici
{
    public class CmdResult
    {
        public int ExitCode;
        public string Output;
        public CmdResult(int c, string o) { ExitCode = c; Output = o; }
    }

    public class MainForm : Form
    {
        private ListView lvAdapters;
        private Button btnManual, btnQuad9, btnGoogle, btnCloudflare, btnAuto, btnFlush, btnRefresh;
        private TextBox txtLog;
        private Label lblStatus, lblStatusDot;
        private Panel headerPanel, statusPanel, cardAdapters, cardDns, cardLog;
        private ToolTip tooltip;

        private readonly string manualPrimary = "127.0.2.2";
        private readonly string manualSecondary = "127.0.2.3";
        private readonly string quad9Primary = "9.9.9.10";
        private readonly string quad9Secondary = "149.112.112.10";
        private readonly string googlePrimary = "8.8.8.8";
        private readonly string googleSecondary = "8.8.4.4";
        private readonly string cfPrimary = "1.1.1.1";
        private readonly string cfSecondary = "1.0.0.1";

        public MainForm()
        {
            InitUI();
            CheckAdmin();
            RefreshAdapters();
        }

        private bool IsAdmin()
        {
            WindowsIdentity id = WindowsIdentity.GetCurrent();
            WindowsPrincipal p = new WindowsPrincipal(id);
            return p.IsInRole(WindowsBuiltInRole.Administrator);
        }

        private void CheckAdmin()
        {
            if (!IsAdmin())
            {
                lblStatus.Text = "✕  Yonetici yetkisi yok  -  DNS degistirmek icin yonetici gerekli";
                lblStatus.ForeColor = Color.FromArgb(255, 205, 210);
                lblStatusDot.Text = "✕";
                lblStatusDot.ForeColor = Color.White;
                lblStatusDot.BackColor = Color.FromArgb(211, 47, 47);
                statusPanel.BackColor = Color.FromArgb(60, 20, 20);
                Log("UYARI: Yonetici yetkisi yok.");
                Log("Cozum: Sag tik > Yonetici olarak calistir");
                DialogResult res = MessageBox.Show("DNS degistirmek icin yonetici yetkisi gerekli.\nYonetici olarak yeniden baslatilsin mi?", "Yetki Gerekli", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                if (res == DialogResult.Yes)
                {
                    try
                    {
                        ProcessStartInfo psi = new ProcessStartInfo(Application.ExecutablePath);
                        psi.UseShellExecute = true;
                        psi.Verb = "runas";
                        Process.Start(psi);
                        Application.Exit();
                    }
                    catch { Log("Yukseltme iptal edildi."); }
                }
            }
            else
            {
                lblStatus.Text = "✓  Yonetici yetkisi aktif  -  DNS degisiklikleri yapilabilir";
                lblStatus.ForeColor = Color.FromArgb(165, 214, 167);
                lblStatusDot.Text = "✓";
                lblStatusDot.ForeColor = Color.White;
                lblStatusDot.BackColor = Color.FromArgb(46, 125, 50);
                statusPanel.BackColor = Color.FromArgb(30, 50, 35);
            }
        }

        private void InitUI()
        {
            try { Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath); } catch { }
            tooltip = new ToolTip();
            Text = "DNS Degistirici";
            Size = new Size(800, 700);
            MinimumSize = new Size(800, 700);
            MaximumSize = new Size(800, 900);
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = Color.FromArgb(18, 18, 20);
            ForeColor = Color.White;
            Font = new Font("Segoe UI", 9f);
            DoubleBuffered = true;

            // Header with gradient
            headerPanel = new Panel();
            headerPanel.Dock = DockStyle.Top;
            headerPanel.Height = 72;
            headerPanel.Padding = new Padding(20, 0, 20, 0);
            headerPanel.Paint += HeaderPaint;
            Controls.Add(headerPanel);

            Label lblTitle = new Label();
            lblTitle.Text = "DNS Degistirici";
            lblTitle.Font = new Font("Segoe UI", 16f, FontStyle.Bold);
            lblTitle.ForeColor = Color.White;
            lblTitle.Location = new Point(20, 14);
            lblTitle.AutoSize = true;
            lblTitle.BackColor = Color.Transparent;
            headerPanel.Controls.Add(lblTitle);

            Label lblSub = new Label();
            lblSub.Text = "Hizli  •  Guvenli  •  Tek tikla DNS yonetimi";
            lblSub.Font = new Font("Segoe UI", 8.5f);
            lblSub.ForeColor = Color.FromArgb(200, 220, 255);
            lblSub.Location = new Point(22, 42);
            lblSub.AutoSize = true;
            lblSub.BackColor = Color.Transparent;
            headerPanel.Controls.Add(lblSub);

            Label lblVer = new Label();
            lblVer.Text = "v2.1  •  Quad9  •  Google  •  Cloudflare";
            lblVer.Font = new Font("Segoe UI", 7f);
            lblVer.ForeColor = Color.FromArgb(160, 180, 220);
            lblVer.AutoSize = true;
            lblVer.BackColor = Color.Transparent;
            lblVer.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            lblVer.Location = new Point(600, 28);
            headerPanel.Controls.Add(lblVer);

            // Status pill
            statusPanel = new Panel();
            statusPanel.Dock = DockStyle.Top;
            statusPanel.Height = 28;
            statusPanel.BackColor = Color.FromArgb(45, 45, 48);
            statusPanel.Padding = new Padding(12, 0, 12, 0);
            Controls.Add(statusPanel);
            statusPanel.BringToFront();
            headerPanel.BringToFront();

            lblStatusDot = new Label();
            lblStatusDot.Size = new Size(18, 18);
            lblStatusDot.Location = new Point(12, 5);
            lblStatusDot.BackColor = Color.Gray;
            lblStatusDot.ForeColor = Color.White;
            lblStatusDot.Font = new Font("Segoe UI", 8f, FontStyle.Bold);
            lblStatusDot.Text = "...";
            lblStatusDot.TextAlign = ContentAlignment.MiddleCenter;
            statusPanel.Controls.Add(lblStatusDot);
            lblStatusDot.Paint += delegate(object s, PaintEventArgs e) {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using (GraphicsPath path = RoundedRect(new Rectangle(0,0,lblStatusDot.Width-1,lblStatusDot.Height-1), 6))
                using (SolidBrush b = new SolidBrush(lblStatusDot.BackColor))
                    e.Graphics.FillPath(b, path);
            };

            lblStatus = new Label();
            lblStatus.Text = "Hazir";
            lblStatus.Location = new Point(36, 5);
            lblStatus.AutoSize = true;
            lblStatus.ForeColor = Color.Silver;
            lblStatus.Font = new Font("Segoe UI", 8.5f);
            lblStatus.BackColor = Color.Transparent;
            statusPanel.Controls.Add(lblStatus);

            // Card: Adapters
            cardAdapters = CreateCard(16, 112, 768, 190, "AG BAGDASTIRICILARI");
            Controls.Add(cardAdapters);

            lvAdapters = new ListView();
            lvAdapters.Location = new Point(12, 32);
            lvAdapters.Size = new Size(744, 146);
            lvAdapters.View = View.Details;
            lvAdapters.FullRowSelect = true;
            lvAdapters.GridLines = false;
            lvAdapters.BackColor = Color.FromArgb(30, 30, 32);
            lvAdapters.ForeColor = Color.FromArgb(230, 230, 230);
            lvAdapters.BorderStyle = BorderStyle.None;
            lvAdapters.HeaderStyle = ColumnHeaderStyle.Nonclickable;
            lvAdapters.Font = new Font("Segoe UI", 8.5f);
            lvAdapters.Columns.Add("  Ag Adaptoru", 230);
            lvAdapters.Columns.Add("Durum", 90);
            lvAdapters.Columns.Add("DNS Sunuculari", 410);
            lvAdapters.OwnerDraw = false;
            cardAdapters.Controls.Add(lvAdapters);

            // Small refresh button inside card header
            btnRefresh = new Button();
            btnRefresh.Text = "Yenile";
            btnRefresh.Size = new Size(70, 22);
            btnRefresh.Location = new Point(686, 6);
            btnRefresh.BackColor = Color.FromArgb(60, 60, 65);
            btnRefresh.ForeColor = Color.White;
            btnRefresh.FlatStyle = FlatStyle.Flat;
            btnRefresh.FlatAppearance.BorderSize = 0;
            btnRefresh.Font = new Font("Segoe UI", 7.5f, FontStyle.Bold);
            btnRefresh.Cursor = Cursors.Hand;
            btnRefresh.Click += (s, e) => RefreshAdapters();
            cardAdapters.Controls.Add(btnRefresh);
            btnRefresh.BringToFront();

            // Card: DNS
            cardDns = CreateCard(16, 314, 768, 148, "DNS SAGLAYICILARI  —  Tek tikla degistir");
            Controls.Add(cardDns);

            // DNS buttons - 5 in a row, beautiful
            int bw = 140, bh = 58, gap = 12, x0 = 12, y0 = 36;
            btnManual = CreateDnsButton("MANUEL", "127.0.2.2", Color.FromArgb(21, 101, 192), Color.FromArgb(13, 71, 161), x0, y0, bw, bh, "Lokal  •  Ozel");
            btnQuad9 = CreateDnsButton("Quad9", "9.9.9.10", Color.FromArgb(106, 27, 154), Color.FromArgb(74, 20, 140), x0 + (bw + gap), y0, bw, bh, "Guvenli  •  Gizlilik");
            btnGoogle = CreateDnsButton("Google", "8.8.8.8", Color.FromArgb(25, 118, 210), Color.FromArgb(21, 101, 192), x0 + (bw + gap) * 2, y0, bw, bh, "Hizli  •  Yaygin");
            btnCloudflare = CreateDnsButton("Cloudflare", "1.1.1.1", Color.FromArgb(239, 108, 0), Color.FromArgb(230, 81, 0), x0 + (bw + gap) * 3, y0, bw, bh, "En hizli  •  Modern");
            btnAuto = CreateDnsButton("OTOMATIK", "DHCP", Color.FromArgb(46, 125, 50), Color.FromArgb(27, 94, 32), x0 + (bw + gap) * 4, y0, bw, bh, "Varsayilan");

            tooltip.SetToolTip(btnManual, "127.0.2.2 / 127.0.2.3 - Lokal DNS");
            tooltip.SetToolTip(btnQuad9, "9.9.9.10 / 149.112.112.10 - Quad9 Guvenli DNS");
            tooltip.SetToolTip(btnGoogle, "8.8.8.8 / 8.8.4.4 - Google Public DNS");
            tooltip.SetToolTip(btnCloudflare, "1.1.1.1 / 1.0.0.1 - Cloudflare DNS");
            tooltip.SetToolTip(btnAuto, "DNS'i otomatik al (DHCP)");

            btnManual.Click += async (s, e) => await SetDnsAsync(manualPrimary, manualSecondary, "Manuel");
            btnQuad9.Click += async (s, e) => await SetDnsAsync(quad9Primary, quad9Secondary, "Quad9");
            btnGoogle.Click += async (s, e) => await SetDnsAsync(googlePrimary, googleSecondary, "Google");
            btnCloudflare.Click += async (s, e) => await SetDnsAsync(cfPrimary, cfSecondary, "Cloudflare");
            btnAuto.Click += async (s, e) => await SetAutoAsync();

            cardDns.Controls.Add(btnManual);
            cardDns.Controls.Add(btnQuad9);
            cardDns.Controls.Add(btnGoogle);
            cardDns.Controls.Add(btnCloudflare);
            cardDns.Controls.Add(btnAuto);

            // Bottom actions row inside DNS card
            btnFlush = new Button();
            btnFlush.Text = "  DNS Cache Temizle";
            btnFlush.Location = new Point(12, 104);
            btnFlush.Size = new Size(744, 32);
            btnFlush.BackColor = Color.FromArgb(45, 45, 48);
            btnFlush.ForeColor = Color.FromArgb(255, 183, 77);
            btnFlush.FlatStyle = FlatStyle.Flat;
            btnFlush.FlatAppearance.BorderSize = 1;
            btnFlush.FlatAppearance.BorderColor = Color.FromArgb(80, 60, 20);
            btnFlush.Font = new Font("Segoe UI", 8.5f, FontStyle.Bold);
            btnFlush.Cursor = Cursors.Hand;
            btnFlush.Click += async (s, e) => await FlushAsync();
            cardDns.Controls.Add(btnFlush);

            // Card: Log
            cardLog = CreateCard(16, 474, 768, 168, "ISLEM GUNLUGU");
            Controls.Add(cardLog);

            txtLog = new TextBox();
            txtLog.Location = new Point(12, 32);
            txtLog.Size = new Size(744, 124);
            txtLog.Multiline = true;
            txtLog.ScrollBars = ScrollBars.Vertical;
            txtLog.ReadOnly = true;
            txtLog.BackColor = Color.FromArgb(24, 24, 26);
            txtLog.ForeColor = Color.FromArgb(200, 200, 200);
            txtLog.BorderStyle = BorderStyle.None;
            txtLog.Font = new Font("Consolas", 8f);
            cardLog.Controls.Add(txtLog);
        }

        private Panel CreateCard(int x, int y, int w, int h, string title)
        {
            Panel p = new Panel();
            p.Location = new Point(x, y);
            p.Size = new Size(w, h);
            p.BackColor = Color.FromArgb(37, 37, 40);
            p.Paint += (s, e) => {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using (Pen pen = new Pen(Color.FromArgb(55, 55, 60), 1))
                {
                    Rectangle r = new Rectangle(0, 0, p.Width - 1, p.Height - 1);
                    int rad = 8;
                    using (GraphicsPath path = RoundedRect(r, rad))
                    {
                        e.Graphics.DrawPath(pen, path);
                    }
                }
            };
            Label lbl = new Label();
            lbl.Text = title;
            lbl.Font = new Font("Segoe UI", 7.5f, FontStyle.Bold);
            lbl.ForeColor = Color.FromArgb(130, 130, 135);
            lbl.Location = new Point(12, 8);
            lbl.AutoSize = true;
            lbl.BackColor = Color.Transparent;
            p.Controls.Add(lbl);

            // underline
            Panel line = new Panel();
            line.Location = new Point(12, 24);
            line.Size = new Size(w - 24, 1);
            line.BackColor = Color.FromArgb(55, 55, 60);
            p.Controls.Add(line);

            return p;
        }

        private GraphicsPath RoundedRect(Rectangle r, int rad)
        {
            GraphicsPath path = new GraphicsPath();
            path.AddArc(r.X, r.Y, rad, rad, 180, 90);
            path.AddArc(r.Right - rad, r.Y, rad, rad, 270, 90);
            path.AddArc(r.Right - rad, r.Bottom - rad, rad, rad, 0, 90);
            path.AddArc(r.X, r.Bottom - rad, rad, rad, 90, 90);
            path.CloseFigure();
            return path;
        }

        private void HeaderPaint(object sender, PaintEventArgs e)
        {
            Rectangle r = headerPanel.ClientRectangle;
            using (LinearGradientBrush br = new LinearGradientBrush(r, Color.FromArgb(13, 71, 161), Color.FromArgb(21, 101, 192), 0f))
            {
                e.Graphics.FillRectangle(br, r);
            }
            // subtle bottom line
            using (Pen pen = new Pen(Color.FromArgb(30, 30, 30), 1))
                e.Graphics.DrawLine(pen, 0, r.Bottom - 1, r.Right, r.Bottom - 1);
        }

        private Button CreateDnsButton(string title, string sub, Color c1, Color c2, int x, int y, int w, int h, string tag)
        {
            Button b = new Button();
            b.Text = title + "\n" + sub;
            b.Location = new Point(x, y);
            b.Size = new Size(w, h);
            b.BackColor = c1;
            b.ForeColor = Color.White;
            b.FlatStyle = FlatStyle.Flat;
            b.FlatAppearance.BorderSize = 0;
            b.Font = new Font("Segoe UI", 8f, FontStyle.Bold);
            b.Cursor = Cursors.Hand;
            b.TextAlign = ContentAlignment.MiddleCenter;
            b.UseVisualStyleBackColor = false;
            // Tag for secondary label
            Label hint = new Label();
            hint.Text = tag;
            hint.Font = new Font("Segoe UI", 6.5f);
            hint.ForeColor = Color.FromArgb(220, 220, 220);
            hint.AutoSize = true;
            hint.BackColor = Color.Transparent;
            hint.Location = new Point(5, h - 14);
            // Can't add label to button easily, use tooltip instead
            b.Paint += delegate(object s, PaintEventArgs e) {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                Rectangle r = new Rectangle(0, 0, b.Width - 1, b.Height - 1);
                using (LinearGradientBrush br = new LinearGradientBrush(r, c1, c2, 90f))
                using (GraphicsPath path = RoundedRect(r, 8))
                {
                    e.Graphics.FillPath(br, path);
                    using (Pen pen = new Pen(Color.FromArgb(60, 255, 255, 255), 1))
                        e.Graphics.DrawPath(pen, path);
                }
                TextRenderer.DrawText(e.Graphics, title, new Font("Segoe UI", 9f, FontStyle.Bold), new Rectangle(0, 10, w, 18), Color.White, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                TextRenderer.DrawText(e.Graphics, sub, new Font("Segoe UI", 7.5f), new Rectangle(0, 28, w, 14), Color.FromArgb(230, 230, 230), TextFormatFlags.HorizontalCenter);
                TextRenderer.DrawText(e.Graphics, tag, new Font("Segoe UI", 6.5f), new Rectangle(0, 42, w, 12), Color.FromArgb(200, 220, 255), TextFormatFlags.HorizontalCenter);
            };
            // hover
            b.MouseEnter += delegate { b.BackColor = ControlPaint.Light(c1, 0.15f); b.Invalidate(); };
            b.MouseLeave += delegate { b.BackColor = c1; b.Invalidate(); };
            return b;
        }

        private void Log(string msg)
        {
            if (InvokeRequired) { Invoke(new Action<string>(Log), msg); return; }
            txtLog.AppendText("[" + DateTime.Now.ToString("HH:mm:ss") + "] " + msg + "\r\n");
            txtLog.SelectionStart = txtLog.TextLength;
            txtLog.ScrollToCaret();
        }

        private void SetButtonsEnabled(bool enabled)
        {
            if (InvokeRequired) { Invoke(new Action<bool>(SetButtonsEnabled), enabled); return; }
            btnManual.Enabled = enabled;
            btnQuad9.Enabled = enabled;
            btnGoogle.Enabled = enabled;
            btnCloudflare.Enabled = enabled;
            btnAuto.Enabled = enabled;
            btnFlush.Enabled = enabled;
            btnRefresh.Enabled = enabled;
        }

        private void RefreshAdapters()
        {
            lvAdapters.Items.Clear();
            try
            {
                var all = NetworkInterface.GetAllNetworkInterfaces();
                List<NetworkInterface> nics = all.Where(n => n.NetworkInterfaceType != NetworkInterfaceType.Loopback && n.OperationalStatus == OperationalStatus.Up).ToList();
                if (nics.Count == 0)
                    nics = all.Where(n => n.NetworkInterfaceType != NetworkInterfaceType.Loopback).ToList();

                foreach (NetworkInterface nic in nics)
                {
                    IPInterfaceProperties props = nic.GetIPProperties();
                    var dns = props.DnsAddresses;
                    string dnsStr;
                    if (dns.Count > 0)
                        dnsStr = string.Join(", ", dns.Cast<System.Net.IPAddress>().Select(ip => ip.ToString()).ToArray());
                    else
                        dnsStr = "(Otomatik / Bos)";
                    dnsStr = dnsStr.Replace("::ffff:", "");
                    ListViewItem item = new ListViewItem("  " + nic.Name);
                    item.SubItems.Add(nic.OperationalStatus.ToString());
                    item.SubItems.Add(dnsStr);
                    if (dnsStr.Contains(manualPrimary) || dnsStr.Contains(quad9Primary) || dnsStr.Contains(googlePrimary) || dnsStr.Contains(cfPrimary))
                        item.ForeColor = Color.FromArgb(129, 199, 132);
                    else if (dnsStr.Contains("(Otomatik"))
                        item.ForeColor = Color.FromArgb(144, 164, 174);
                    lvAdapters.Items.Add(item);
                }
                Log("Adaptorler yenilendi - " + lvAdapters.Items.Count + " adaptor bulundu.");
                if (lvAdapters.Items.Count == 0) Log("Aktif adaptor bulunamadi!");
            }
            catch (Exception ex) { Log("HATA yenileme: " + ex.Message); }
        }

        private async Task SetDnsAsync(string primary, string secondary, string label)
        {
            if (!IsAdmin()) { MessageBox.Show("Yonetici yetkisi gerekli!", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error); return; }
            SetButtonsEnabled(false);
            Log(label + " DNS ayarlaniyor (" + primary + " / " + secondary + ")...");
            int ok = 0, fail = 0;
            List<string> names = lvAdapters.Items.Cast<ListViewItem>().Select(i => i.Text.Trim()).ToList();
            if (names.Count == 0) names = await GetAdapterNamesViaPowerShell();
            foreach (string name in names)
            {
                CmdResult r1 = await RunNetsh("interface ip set dns \"" + name + "\" static " + primary);
                if (r1.ExitCode != 0) { Log("[HATA] " + name + " : " + r1.Output); fail++; continue; }
                CmdResult r2 = await RunNetsh("interface ip add dns \"" + name + "\" " + secondary + " index=2");
                if (r2.ExitCode != 0) Log("[UYARI] " + name + " yedek eklenemedi: " + r2.Output);
                Log("[OK] " + name + " -> " + primary + ", " + secondary);
                ok++;
            }
            await FlushInternal();
            Log("Tamamlandi: " + ok + " basarili, " + fail + " hatali");
            RefreshAdapters();
            SetButtonsEnabled(true);
        }

        private async Task SetAutoAsync()
        {
            if (!IsAdmin()) { MessageBox.Show("Yonetici yetkisi gerekli!", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error); return; }
            SetButtonsEnabled(false);
            Log("Otomatik (DHCP) ayarlaniyor...");
            int ok = 0, fail = 0;
            List<string> names = lvAdapters.Items.Cast<ListViewItem>().Select(i => i.Text.Trim()).ToList();
            if (names.Count == 0) names = await GetAdapterNamesViaPowerShell();
            foreach (string name in names)
            {
                CmdResult r = await RunNetsh("interface ip set dns \"" + name + "\" dhcp");
                if (r.ExitCode != 0) { Log("[HATA] " + name + ": " + r.Output); fail++; }
                else { Log("[OK] " + name + " -> DHCP"); ok++; }
            }
            await FlushInternal();
            Log("Tamamlandi: " + ok + " basarili, " + fail + " hatali");
            RefreshAdapters();
            SetButtonsEnabled(true);
        }

        private async Task FlushAsync()
        {
            SetButtonsEnabled(false);
            Log("DNS Cache temizleniyor...");
            CmdResult r = await RunCmd("ipconfig", "/flushdns");
            Log(r.Output.Trim());
            if (r.ExitCode == 0) Log("[OK] Cache temizlendi");
            else Log("[HATA] Cache temizlenemedi");
            SetButtonsEnabled(true);
        }

        private async Task FlushInternal()
        {
            CmdResult r = await RunCmd("ipconfig", "/flushdns");
            Log(r.Output.Trim());
        }

        private async Task<List<string>> GetAdapterNamesViaPowerShell()
        {
            CmdResult r = await RunCmd("powershell", "-NoProfile -Command \"Get-NetAdapter | Where-Object {$_.Status -eq 'Up'} | Select-Object -ExpandProperty Name\"");
            string[] parts = r.Output.Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            List<string> list = new List<string>();
            foreach (string s in parts)
            {
                string t = s.Trim();
                if (!string.IsNullOrWhiteSpace(t)) list.Add(t);
            }
            return list;
        }

        private Task<CmdResult> RunNetsh(string args) { return RunCmd("netsh", args); }

        private Task<CmdResult> RunCmd(string file, string args)
        {
            return Task.Run(() =>
            {
                try
                {
                    ProcessStartInfo psi = new ProcessStartInfo(file, args);
                    psi.UseShellExecute = false;
                    psi.RedirectStandardOutput = true;
                    psi.RedirectStandardError = true;
                    psi.CreateNoWindow = true;
                    using (Process p = Process.Start(psi))
                    {
                        string outp = p.StandardOutput.ReadToEnd();
                        string err = p.StandardError.ReadToEnd();
                        p.WaitForExit();
                        string combined = (outp + " " + err).Trim();
                        return new CmdResult(p.ExitCode, combined);
                    }
                }
                catch (Exception ex) { return new CmdResult(-1, ex.Message); }
            });
        }
    }
}
