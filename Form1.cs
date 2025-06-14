using System;
using System.Media;
using System.Windows.Forms;
using System.Drawing;
using System.Threading.Tasks;
using Timer = System.Windows.Forms.Timer;
using System.IO;

namespace GorselFinalSonOdevv
{
    public partial class Form1 : Form
    {
        private SoundPlayer successSound;
        private SoundPlayer errorSound;
        private string currentUsername;

        public Form1(string username)
        {
            InitializeComponent();
            currentUsername = username;
            InitializeSounds();
            SetupMenuForm();
            Task.Run(async () => await ShowLoading("Ana menü yükleniyor..."));
        }

        private void InitializeSounds()
        {
            try
            {
                if (File.Exists("basarilises.wav") && File.Exists("hatalieylem.wav"))
                {
                    successSound = new SoundPlayer("basarilises.wav");
                    errorSound = new SoundPlayer("hatalieylem.wav");
                }
            }
            catch (Exception)
            {
                successSound = null;
                errorSound = null;
            }
        }

        private void SetupMenuForm()
        {
            // Form ayarları
            this.Text = "Ana Menü";
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Size = new Size(800, 600);
            this.BackColor = Color.White;

            // Başlık
            Label lblTitle = new Label
            {
                Text = "Hoş Geldiniz, " + currentUsername,
                Font = new Font("Segoe UI", 16, FontStyle.Bold),
                AutoSize = true,
                Location = new Point(20, 20)
            };

            // Menü paneli
            Panel menuPanel = new Panel
            {
                Location = new Point(20, 80),
                Size = new Size(740, 400),
                BorderStyle = BorderStyle.FixedSingle
            };

            // Paint butonu
            Button btnPaint = new Button
            {
                Text = "Paint Benzeri Uygulama",
                Size = new Size(300, 100),
                Location = new Point(50, 50),
                Font = new Font("Segoe UI", 12),
                BackColor = Color.LightBlue,
                FlatStyle = FlatStyle.Flat
            };

            // Word butonu
            Button btnWord = new Button
            {
                Text = "Word Benzeri Uygulama",
                Size = new Size(300, 100),
                Location = new Point(390, 50),
                Font = new Font("Segoe UI", 12),
                BackColor = Color.LightGreen,
                FlatStyle = FlatStyle.Flat
            };

            // Çıkış butonu
            Button btnLogout = new Button
            {
                Text = "Çıkış Yap",
                Size = new Size(200, 40),
                Location = new Point(270, 300),
                Font = new Font("Segoe UI", 10),
                BackColor = Color.LightCoral,
                FlatStyle = FlatStyle.Flat
            };

            // Buton olayları
            btnPaint.Click += async (s, e) =>
            {
                await ShowLoading("Paint uygulaması açılıyor...");
                try { successSound?.Play(); } catch { }
                PaintTarzı paintForm = new PaintTarzı(currentUsername);
                paintForm.Show();
                LogActivity("Paint uygulaması açıldı", true);
            };

            btnWord.Click += async (s, e) =>
            {
                await ShowLoading("Word uygulaması açılıyor...");
                try { successSound?.Play(); } catch { }
                WordTarzı wordForm = new WordTarzı(currentUsername);
                wordForm.Show();
                LogActivity("Word uygulaması açıldı", true);
            };

            btnLogout.Click += async (s, e) =>
            {
                var result = MessageBox.Show(
                    "Çıkış yapmak istediğinizden emin misiniz?",
                    "Çıkış Onayı",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question
                );

                if (result == DialogResult.Yes)
                {
                    await ShowLoading("Çıkış yapılıyor...");
                    try { successSound?.Play(); } catch { }
                    LogActivity("Çıkış yapıldı", true);
                    Application.Restart();
                }
            };

            // Kontrolleri panellere ekle
            menuPanel.Controls.AddRange(new Control[] { btnPaint, btnWord, btnLogout });
            this.Controls.AddRange(new Control[] { lblTitle, menuPanel });

            // Form kapanırken
            this.FormClosing += (s, e) =>
            {
                if (e.CloseReason == CloseReason.UserClosing)
                {
                    var result = MessageBox.Show(
                        "Çıkış yapmak istediğinizden emin misiniz?",
                        "Çıkış Onayı",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Question
                    );

                    if (result == DialogResult.No)
                    {
                        e.Cancel = true;
                    }
                    else
                    {
                        LogActivity("Programdan çıkış yapıldı", true);
                    }
                }
            };
        }

        private async Task ShowLoading(string message)
        {
            Form loadingForm = new Form
            {
                Size = new Size(300, 100),
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.None,
                BackColor = Color.White
            };

            Label lblLoading = new Label
            {
                Text = message,
                AutoSize = true,
                Location = new Point(20, 20)
            };

            ProgressBar progressBar = new ProgressBar
            {
                Style = ProgressBarStyle.Marquee,
                Location = new Point(20, 50),
                Size = new Size(260, 20)
            };

            loadingForm.Controls.AddRange(new Control[] { lblLoading, progressBar });
            loadingForm.Show(this);

            await Task.Delay(1000);
            loadingForm.Close();
        }

        private void LogActivity(string action, bool isSuccess)
        {
            ActivityLog log = new ActivityLog(currentUsername, action, isSuccess);
            string logPath = "activity_log.txt";
            File.AppendAllText(logPath, log.ToString() + Environment.NewLine);
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }
    }
}
