using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Media;

namespace GorselFinalSonOdevv
{
    public partial class KayıtOl : Form
    {
        private SoundPlayer successSound;
        private SoundPlayer errorSound;

        public KayıtOl()
        {
            InitializeComponent();
            InitializeSounds();
            SetupForm();
        }

        private void InitializeSounds()
        {
            try
            {
                if (File.Exists("success.wav") && File.Exists("error.wav"))
                {
                    successSound = new SoundPlayer("success.wav");
                    errorSound = new SoundPlayer("error.wav");
                }
            }
            catch (Exception)
            {
                // Ses dosyaları yüklenemezse sessiz devam et
                successSound = null;
                errorSound = null;
            }
        }

        private void SetupForm()
        {
            // Form ayarları
            this.Text = "Kayıt Ol";
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Size = new Size(400, 500);

            // Kullanıcı adı
            Label lblUsername = new Label
            {
                Text = "Kullanıcı Adı:",
                Location = new Point(50, 50),
                AutoSize = true
            };

            TextBox txtUsername = new TextBox
            {
                Location = new Point(50, 80),
                Size = new Size(280, 20)
            };

            // Şifre
            Label lblPassword = new Label
            {
                Text = "Şifre:",
                Location = new Point(50, 120),
                AutoSize = true
            };

            TextBox txtPassword = new TextBox
            {
                Location = new Point(50, 150),
                Size = new Size(280, 20),
                PasswordChar = '●'
            };

            // Şifre tekrar
            Label lblPasswordConfirm = new Label
            {
                Text = "Şifre Tekrar:",
                Location = new Point(50, 190),
                AutoSize = true
            };

            TextBox txtPasswordConfirm = new TextBox
            {
                Location = new Point(50, 220),
                Size = new Size(280, 20),
                PasswordChar = '●'
            };

            // Kayıt ol butonu
            Button btnRegister = new Button
            {
                Text = "Kayıt Ol",
                Location = new Point(50, 280),
                Size = new Size(280, 40)
            };

            // Kontrolleri forma ekle
            this.Controls.AddRange(new Control[] { 
                lblUsername, txtUsername,
                lblPassword, txtPassword,
                lblPasswordConfirm, txtPasswordConfirm,
                btnRegister
            });

            // Kayıt ol butonu tıklama olayı
            btnRegister.Click += async (s, e) =>
            {
                await ShowLoading("Kayıt yapılıyor...");

                if (string.IsNullOrWhiteSpace(txtUsername.Text) || 
                    string.IsNullOrWhiteSpace(txtPassword.Text))
                {
                    try { errorSound?.Play(); } catch { }
                    MessageBox.Show("Kullanıcı adı ve şifre boş olamaz!", "Hata", 
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                if (txtPassword.Text != txtPasswordConfirm.Text)
                {
                    try { errorSound?.Play(); } catch { }
                    MessageBox.Show("Şifreler eşleşmiyor!", "Hata", 
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                var users = User.LoadUsers();
                if (users.Any(u => u.Username == txtUsername.Text))
                {
                    try { errorSound?.Play(); } catch { }
                    MessageBox.Show("Bu kullanıcı adı zaten kullanılıyor!", "Hata", 
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                // Yeni kullanıcıyı ekle
                users.Add(new User(txtUsername.Text, txtPassword.Text));
                User.SaveUsers(users);

                try { successSound?.Play(); } catch { }
                LogActivity(txtUsername.Text, "Yeni kullanıcı kaydı", true);
                MessageBox.Show("Kayıt başarıyla tamamlandı!", "Başarılı", 
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                
                this.Close();
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

            await Task.Delay(2000); // 2 saniye bekle
            loadingForm.Close();
        }

        private void LogActivity(string username, string action, bool isSuccess)
        {
            ActivityLog log = new ActivityLog(username, action, isSuccess);
            string logPath = "activity_log.txt";
            File.AppendAllText(logPath, log.ToString() + Environment.NewLine);
        }
    }
}
