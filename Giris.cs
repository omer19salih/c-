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
    public partial class Giris : Form
    {
        private SoundPlayer successSound;
        private SoundPlayer errorSound;
        private List<User> users;

        public Giris()
        {
            InitializeComponent();
            InitializeSounds();
            LoadUsers();
            SetupForm();
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
                // Ses dosyaları yüklenemezse sessiz devam et
                successSound = null;
                errorSound = null;
            }
        }

        private void LoadUsers()
        {
            users = User.LoadUsers();
        }

        private void SetupForm()
        {
            // Form ayarları
            this.Text = "Giriş Ekranı";
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

            // Giriş butonu
            Button btnLogin = new Button
            {
                Text = "Giriş Yap",
                Location = new Point(50, 200),
                Size = new Size(280, 40)
            };

            // Kayıt ol butonu
            Button btnRegister = new Button
            {
                Text = "Kayıt Ol",
                Location = new Point(50, 260),
                Size = new Size(280, 40)
            };

            // Kontrolleri forma ekle
            this.Controls.AddRange(new Control[] { 
                lblUsername, txtUsername,
                lblPassword, txtPassword,
                btnLogin, btnRegister
            });

            // Giriş butonu tıklama olayı
            btnLogin.Click += (s, e) =>
            {
                string username = txtUsername.Text.Trim();
                string password = txtPassword.Text;

                if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
                {
                    try { errorSound?.Play(); } catch { }
                    MessageBox.Show("Kullanıcı adı ve şifre boş olamaz!", "Hata", 
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    LogActivity(username, "Boş kullanıcı adı veya şifre ile giriş denemesi", false);
                    return;
                }

                // Yükleme formunu oluştur
                var loadingForm = new Form
                {
                    Size = new Size(300, 100),
                    StartPosition = FormStartPosition.CenterParent,
                    FormBorderStyle = FormBorderStyle.None,
                    BackColor = Color.White,
                    TopMost = true
                };

                var lblLoading = new Label
                {
                    Text = "Giriş yapılıyor...",
                    AutoSize = true,
                    Location = new Point(20, 20),
                    Font = new Font("Segoe UI", 10, FontStyle.Regular)
                };

                var progressBar = new ProgressBar
                {
                    Style = ProgressBarStyle.Marquee,
                    Location = new Point(20, 50),
                    Size = new Size(260, 20),
                    MarqueeAnimationSpeed = 30
                };

                loadingForm.Controls.AddRange(new Control[] { lblLoading, progressBar });

                // Yükleme formunu göster (modal değil)
                loadingForm.Show(this);
                Application.DoEvents(); // UI'ın güncellenmesini sağla
                try
                {
                    // Admin kontrolü
                    if (username == "admin" && password == "admin")
                    {
                        try { successSound?.Play(); } catch { }
                        LogActivity("admin", "Admin girişi başarılı", true);
                        
                        loadingForm.Close();
                        loadingForm.Dispose();

                        // UI thread\'inde form geçişini zamanla: Yeni formu göster, sonra bu formu kapat
                        this.BeginInvoke((MethodInvoker)delegate
                        {
                            var adminPanel = new AdminPanel();
                            adminPanel.ShowDialog(); // Admin paneli modal olarak göster
                            this.Close(); // Admin paneli kapatıldıktan sonra giriş formunu kapat
                        });
                    }
                    // Normal kullanıcı kontrolü
                    else
                    {
                        var user = users.FirstOrDefault(u => u.Username == username && u.Password == password);
                        if (user != null)
                        {
                            try { successSound?.Play(); } catch { }
                            LogActivity(user.Username, "Kullanıcı girişi başarılı", true);
                            
                            loadingForm.Close();
                            loadingForm.Dispose();

                            // UI thread\'inde form geçişini zamanla: Yeni formu göster, sonra bu formu kapat
                            this.BeginInvoke((MethodInvoker)delegate
                            {
                                var mainForm = new Form1(user.Username);
                                mainForm.ShowDialog(); // Ana formu modal olarak göster
                                this.Close(); // Ana form kapatıldıktan sonra giriş formunu kapat
                            });
                        }
                        else
                        {
                            try { errorSound?.Play(); } catch { }
                            loadingForm.Close();
                            loadingForm.Dispose();

                            // UI thread\'inde mesaj kutusunu göster
                            this.BeginInvoke((MethodInvoker)delegate
                            {
                                MessageBox.Show("Kullanıcı adı veya şifre hatalı!", "Hata", 
                                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                            });
                            LogActivity(username, "Başarısız giriş denemesi", false);
                        }
                    }
                }
                catch (Exception ex)
                {
                    // UI thread\'inde hata mesajını göster ve loading form\'u kapat
                    this.BeginInvoke((MethodInvoker)delegate
                    {
                         if (!loadingForm.IsDisposed) loadingForm.Close();
                         if (!loadingForm.IsDisposed) loadingForm.Dispose();
                         MessageBox.Show($"Bir hata oluştu: {ex.Message}", "Hata", 
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                    });
                }
            };

            // Kayıt ol butonu tıklama olayı
            btnRegister.Click += (s, e) =>
            {
                 // UI thread\'inde kayıt form\'unu göster
                 this.BeginInvoke((MethodInvoker)delegate
                 {
                    var registerForm = new KayıtOl();
                    registerForm.ShowDialog();
                    LoadUsers(); // Kullanıcı listesini yenile
                 });
            };
        }

        private void LogActivity(string username, string action, bool isSuccess)
        {
            ActivityLog log = new ActivityLog(username, action, isSuccess);
            string logPath = "activity_log.txt";
            File.AppendAllText(logPath, log.ToString() + Environment.NewLine);
        }
    }
}
