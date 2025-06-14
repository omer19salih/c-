using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Media;
using System.IO;

namespace GorselFinalSonOdevv
{
    public partial class WordTarzı : Form
    {
        private SoundPlayer successSound;
        private SoundPlayer errorSound;
        private RichTextBox rtbText;
        private PictureBox pbImage;
        private string currentFilePath;
        private string currentUsername;

        public WordTarzı(string username)
        {
            InitializeComponent();
            currentUsername = username;
            InitializeSounds();
            SetupForm();
            Task.Run(async () => await ShowLoading("Word uygulaması başlatılıyor..."));
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

        private void SetupForm()
        {
            this.Text = "Word Benzeri Uygulama";
            this.Size = new Size(1000, 700);
            this.StartPosition = FormStartPosition.CenterScreen;

            // Menü oluştur
            MenuStrip menuStrip = new MenuStrip();
            ToolStripMenuItem fileMenu = new ToolStripMenuItem("Dosya");
            ToolStripMenuItem editMenu = new ToolStripMenuItem("Düzenle");
            ToolStripMenuItem imageMenu = new ToolStripMenuItem("Resim");

            // Dosya menüsü öğeleri
            fileMenu.DropDownItems.AddRange(new ToolStripItem[]
            {
                new ToolStripMenuItem("Yeni", null, async (s, e) => await NewFile()),
                new ToolStripMenuItem("Aç...", null, async (s, e) => await OpenFile()),
                new ToolStripMenuItem("Kaydet", null, async (s, e) => await SaveFile()),
                new ToolStripMenuItem("Farklı Kaydet...", null, async (s, e) => await SaveFileAs()),
                new ToolStripSeparator(),
                new ToolStripMenuItem("Çıkış", null, (s, e) => this.Close())
            });

            // Düzenle menüsü öğeleri
            editMenu.DropDownItems.AddRange(new ToolStripItem[]
            {
                new ToolStripMenuItem("Kes", null, (s, e) => rtbText.Cut()),
                new ToolStripMenuItem("Kopyala", null, (s, e) => rtbText.Copy()),
                new ToolStripMenuItem("Yapıştır", null, (s, e) => rtbText.Paste()),
                new ToolStripSeparator(),
                new ToolStripMenuItem("Tümünü Seç", null, (s, e) => rtbText.SelectAll())
            });

            // Resim menüsü öğeleri
            imageMenu.DropDownItems.AddRange(new ToolStripItem[]
            {
                new ToolStripMenuItem("Resim Ekle (Gözat)...", null, async (s, e) => await AddImage()),
                new ToolStripMenuItem("Resmi Kaydet...", null, async (s, e) => await SaveImage())
            });

            menuStrip.Items.AddRange(new ToolStripItem[] { fileMenu, editMenu, imageMenu });
            this.Controls.Add(menuStrip);
            this.MainMenuStrip = menuStrip;

            // Araç çubuğu
            ToolStrip toolStrip = new ToolStrip();
            toolStrip.Items.AddRange(new ToolStripItem[]
            {
                new ToolStripButton("Yeni", null, async (s, e) => await NewFile()) { ToolTipText = "Yeni Dosya" },
                new ToolStripButton("Aç", null, async (s, e) => await OpenFile()) { ToolTipText = "Dosya Aç" },
                new ToolStripButton("Kaydet", null, async (s, e) => await SaveFile()) { ToolTipText = "Kaydet" },
                new ToolStripSeparator(),
                new ToolStripButton("Resim Ekle", null, async (s, e) => await AddImage()) { ToolTipText = "Resim Ekle" }
            });
            toolStrip.Location = new Point(0, menuStrip.Height);
            this.Controls.Add(toolStrip);

            // Metin editörü - Konum ve boyut ayarlanıyor
            rtbText = new RichTextBox
            {
                AcceptsTab = true,
                WordWrap = true,
                Font = new Font("Segoe UI", 11),
                Size = new Size(600, 150), // Boyut küçültülüyor
            };

            // Resim kutusu - Konum ve boyut ayarlanıyor
            pbImage = new PictureBox
            {
                Size = new Size(150, 150), // Boyut ayarlanıyor, metin kutusu yüksekliği ile orantılı
                SizeMode = PictureBoxSizeMode.Zoom,
                BorderStyle = BorderStyle.FixedSingle,
            };

            // Kontrolleri forma ekle
            this.Controls.Add(rtbText);
            this.Controls.Add(pbImage);

            // Sürükle-bırak için ayarlar
            pbImage.AllowDrop = true;
            pbImage.DragEnter += PbImage_DragEnter;
            pbImage.DragDrop += PbImage_DragDrop;

            // Kontrolleri yeniden konumlandırmak için Resize olayını kullan
            this.Resize += WordTarzı_Resize;
            // Başlangıçta konumlandırma yap
            PositionControls();

            // Form kapanırken kaydetme kontrolü
            this.FormClosing += async (s, e) =>
            {
                if (rtbText.Modified)
                {
                    var result = MessageBox.Show(
                        "Değişiklikler kaydedilmedi. Kaydetmek ister misiniz?",
                        "Uyarı",
                        MessageBoxButtons.YesNoCancel,
                        MessageBoxIcon.Warning
                    );

                    if (result == DialogResult.Yes)
                    {
                        bool saved = await SaveFile();
                        e.Cancel = !saved;
                    }
                    else if (result == DialogResult.Cancel)
                    {
                        e.Cancel = true;
                    }
                }
            };
        }

        private void WordTarzı_Resize(object sender, EventArgs e)
        {
            PositionControls();
        }

        private void PositionControls()
        {
            // RichTextBox'ı formun alt ortasına konumlandır
            int rtbX = (this.ClientSize.Width - rtbText.Width) / 2;
            int rtbY = this.ClientSize.Height - rtbText.Height - 20; // 20 piksel alttan boşluk
            rtbText.Location = new Point(rtbX, rtbY);

            // PictureBox'ı RichTextBox'ın sağına hizala
            int pbX = rtbX + rtbText.Width + 10; // 10 piksel boşluk
            int pbY = rtbY; // RichTextBox ile aynı yükseklik hizasında
            pbImage.Location = new Point(pbX, pbY);
        }

        private async Task NewFile()
        {
            if (rtbText.Modified)
            {
                var result = MessageBox.Show(
                    "Değişiklikler kaydedilmedi. Kaydetmek ister misiniz?",
                    "Uyarı",
                    MessageBoxButtons.YesNoCancel,
                    MessageBoxIcon.Warning
                );

                if (result == DialogResult.Yes)
                {
                    if (!await SaveFile()) return;
                }
                else if (result == DialogResult.Cancel)
                {
                    return;
                }
            }

            await ShowLoading("Yeni dosya oluşturuluyor...");
            rtbText.Clear();
            pbImage.Image = null;
            currentFilePath = null;
            this.Text = "Word Benzeri Uygulama - Yeni Dosya";
            rtbText.Modified = false;
            try { successSound?.Play(); } catch { }
            LogActivity("Yeni dosya oluşturuldu", true);
        }

        private async Task OpenFile()
        {
            if (rtbText.Modified)
            {
                var result = MessageBox.Show(
                    "Değişiklikler kaydedilmedi. Kaydetmek ister misiniz?",
                    "Uyarı",
                    MessageBoxButtons.YesNoCancel,
                    MessageBoxIcon.Warning
                );

                if (result == DialogResult.Yes)
                {
                    if (!await SaveFile()) return;
                }
                else if (result == DialogResult.Cancel)
                {
                    return;
                }
            }

            using (OpenFileDialog ofd = new OpenFileDialog())
            {
                ofd.Filter = "Metin Dosyaları|*.txt|Tüm Dosyalar|*.*";
                ofd.Title = "Dosya Aç";

                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    await ShowLoading("Dosya açılıyor...");
                    try
                    {
                        rtbText.Text = File.ReadAllText(ofd.FileName);
                        currentFilePath = ofd.FileName;
                        this.Text = $"Word Benzeri Uygulama - {Path.GetFileName(currentFilePath)}";
                        rtbText.Modified = false;
                        try { successSound?.Play(); } catch { }
                        LogActivity($"Dosya açıldı: {Path.GetFileName(currentFilePath)}", true);
                    }
                    catch (Exception ex)
                    {
                        try { errorSound?.Play(); } catch { }
                        MessageBox.Show($"Dosya açılırken hata oluştu: {ex.Message}", 
                            "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        LogActivity($"Dosya açma hatası: {ex.Message}", false);
                    }
                }
            }
        }

        private async Task<bool> SaveFile()
        {
            if (string.IsNullOrEmpty(currentFilePath))
            {
                return await SaveFileAs();
            }

            await ShowLoading("Dosya kaydediliyor...");
            try
            {
                File.WriteAllText(currentFilePath, rtbText.Text);
                rtbText.Modified = false;
                try { successSound?.Play(); } catch { }
                LogActivity($"Dosya kaydedildi: {Path.GetFileName(currentFilePath)}", true);
                return true;
            }
            catch (Exception ex)
            {
                try { errorSound?.Play(); } catch { }
                MessageBox.Show($"Dosya kaydedilirken hata oluştu: {ex.Message}", 
                    "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                LogActivity($"Dosya kaydetme hatası: {ex.Message}", false);
                return false;
            }
        }

        private async Task<bool> SaveFileAs()
        {
            using (SaveFileDialog sfd = new SaveFileDialog())
            {
                sfd.Filter = "Metin Dosyaları|*.txt|Tüm Dosyalar|*.*";
                sfd.Title = "Farklı Kaydet";
                sfd.FileName = currentFilePath ?? "Yeni Dosya.txt";

                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    currentFilePath = sfd.FileName;
                    return await SaveFile();
                }
                return false;
            }
        }

        private async Task AddImage()
        {
            using (OpenFileDialog ofd = new OpenFileDialog())
            {
                ofd.Filter = "Resim Dosyaları|*.jpg;*.jpeg;*.png|Tüm Dosyalar|*.*";
                ofd.Title = "Resim Ekle";

                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    await ShowLoading("Resim yükleniyor...");
                    try
                    {
                        using (var stream = new FileStream(ofd.FileName, FileMode.Open, FileAccess.Read))
                        {
                            pbImage.Image = Image.FromStream(stream);
                        }
                        try { successSound?.Play(); } catch { }
                        LogActivity($"Resim eklendi: {Path.GetFileName(ofd.FileName)}", true);
                    }
                    catch (Exception ex)
                    {
                        try { errorSound?.Play(); } catch { }
                        MessageBox.Show($"Resim yüklenirken hata oluştu: {ex.Message}", 
                            "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        LogActivity($"Resim yükleme hatası: {ex.Message}", false);
                    }
                }
            }
        }

        private async Task<bool> SaveImage()
        {
            if (pbImage.Image == null)
            {
                MessageBox.Show("Kaydedilecek resim bulunamadı!", "Uyarı", 
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            using (SaveFileDialog sfd = new SaveFileDialog())
            {
                sfd.Filter = "JPEG Resim|*.jpg|PNG Resim|*.png";
                sfd.Title = "Resmi Kaydet";
                sfd.FileName = "Resim.jpg";

                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    await ShowLoading("Resim kaydediliyor...");
                    try
                    {
                        string extension = Path.GetExtension(sfd.FileName).ToLower();
                        ImageFormat format = extension == ".png" ? ImageFormat.Png : ImageFormat.Jpeg;
                        pbImage.Image.Save(sfd.FileName, format);
                        try { successSound?.Play(); } catch { }
                        LogActivity($"Resim kaydedildi: {Path.GetFileName(sfd.FileName)}", true);
                        return true;
                    }
                    catch (Exception ex)
                    {
                        try { errorSound?.Play(); } catch { }
                        MessageBox.Show($"Resim kaydedilirken hata oluştu: {ex.Message}", 
                            "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        LogActivity($"Resim kaydetme hatası: {ex.Message}", false);
                        return false;
                    }
                }
                return false;
            }
        }

        private void PbImage_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                if (files.Length == 1)
                {
                    string ext = Path.GetExtension(files[0]).ToLower();
                    if (ext == ".jpg" || ext == ".jpeg" || ext == ".png")
                    {
                        e.Effect = DragDropEffects.Copy;
                        return;
                    }
                }
            }
            e.Effect = DragDropEffects.None;
        }

        private async void PbImage_DragDrop(object sender, DragEventArgs e)
        {
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
            if (files.Length == 1)
            {
                await ShowLoading("Resim yükleniyor...");
                try
                {
                    using (var stream = new FileStream(files[0], FileMode.Open, FileAccess.Read))
                    {
                        pbImage.Image = Image.FromStream(stream);
                    }
                    try { successSound?.Play(); } catch { }
                    LogActivity($"Resim sürükle-bırak ile eklendi: {Path.GetFileName(files[0])}", true);
                }
                catch (Exception ex)
                {
                    try { errorSound?.Play(); } catch { }
                    MessageBox.Show($"Resim yüklenirken hata oluştu: {ex.Message}", 
                        "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    LogActivity($"Resim sürükle-bırak hatası: {ex.Message}", false);
                }
            }
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
    }
}
