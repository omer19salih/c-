using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Media;
using System.IO;

namespace GorselFinalSonOdevv
{
    public partial class PaintTarzı : Form
    {
        private SoundPlayer successSound;
        private SoundPlayer errorSound;
        private Bitmap drawingBitmap;
        private Graphics drawingGraphics;
        private Point lastPoint;
        private bool isDrawing = false;
        private Color currentColor = Color.Black;
        private int currentPenSize = 2;
        private string currentUsername;
        private string currentFilePath;
        private Panel drawingPanel;

        public PaintTarzı(string username)
        {
            InitializeComponent();
            currentUsername = username;
            InitializeSounds();
            SetupForm();
            Task.Run(async () => await ShowLoading("Paint uygulaması başlatılıyor..."));
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
            this.Text = "Paint Benzeri Uygulama";
            this.Size = new Size(1000, 700);
            this.StartPosition = FormStartPosition.CenterScreen;

            // Menü oluştur
            MenuStrip menuStrip = new MenuStrip();
            ToolStripMenuItem fileMenu = new ToolStripMenuItem("Dosya");
            ToolStripMenuItem editMenu = new ToolStripMenuItem("Düzenle");
            ToolStripMenuItem toolsMenu = new ToolStripMenuItem("Araçlar");

            // Dosya menüsü öğeleri
            fileMenu.DropDownItems.AddRange(new ToolStripItem[]
            {
                new ToolStripMenuItem("Yeni", null, async (s, e) => await NewDrawing()),
                new ToolStripMenuItem("Aç...", null, async (s, e) => await OpenImage()),
                new ToolStripMenuItem("Kaydet", null, async (s, e) => await SaveImage()),
                new ToolStripMenuItem("Farklı Kaydet...", null, async (s, e) => await SaveImageAs()),
                new ToolStripSeparator(),
                new ToolStripMenuItem("Çıkış", null, (s, e) => this.Close())
            });

            // Düzenle menüsü öğeleri
            editMenu.DropDownItems.AddRange(new ToolStripItem[]
            {
                new ToolStripMenuItem("Temizle", null, async (s, e) => await ClearDrawing())
            });

            // Araçlar menüsü öğeleri
            toolsMenu.DropDownItems.AddRange(new ToolStripItem[]
            {
                new ToolStripMenuItem("Renk Seç...", null, (s, e) => SelectColor()),
                new ToolStripSeparator(),
                new ToolStripMenuItem("Kalem Boyutu: 1px", null, (s, e) => SetPenSize(1)),
                new ToolStripMenuItem("Kalem Boyutu: 2px", null, (s, e) => SetPenSize(2)),
                new ToolStripMenuItem("Kalem Boyutu: 4px", null, (s, e) => SetPenSize(4)),
                new ToolStripMenuItem("Kalem Boyutu: 8px", null, (s, e) => SetPenSize(8))
            });

            menuStrip.Items.AddRange(new ToolStripItem[] { fileMenu, editMenu, toolsMenu });
            this.Controls.Add(menuStrip);
            this.MainMenuStrip = menuStrip;

            // Araç çubuğu
            ToolStrip toolStrip = new ToolStrip();
            toolStrip.Items.AddRange(new ToolStripItem[]
            {
                new ToolStripButton("Yeni", null, async (s, e) => await NewDrawing()) { ToolTipText = "Yeni Çizim" },
                new ToolStripButton("Aç", null, async (s, e) => await OpenImage()) { ToolTipText = "Resim Aç" },
                new ToolStripButton("Kaydet", null, async (s, e) => await SaveImage()) { ToolTipText = "Kaydet" },
                new ToolStripSeparator(),
                new ToolStripButton("Renk", null, (s, e) => SelectColor()) { ToolTipText = "Renk Seç" },
                new ToolStripSeparator(),
                new ToolStripButton("Temizle", null, async (s, e) => await ClearDrawing()) { ToolTipText = "Çizimi Temizle" }
            });
            toolStrip.Location = new Point(0, menuStrip.Height);
            this.Controls.Add(toolStrip);

            // Çizim alanı
            drawingPanel = new Panel
            {
                Dock = DockStyle.Fill,
                Location = new Point(0, menuStrip.Height + toolStrip.Height),
                BackColor = Color.White
            };

            // Çizim alanı olayları
            drawingPanel.Paint += DrawingPanel_Paint;
            drawingPanel.MouseDown += DrawingPanel_MouseDown;
            drawingPanel.MouseMove += DrawingPanel_MouseMove;
            drawingPanel.MouseUp += DrawingPanel_MouseUp;
            drawingPanel.Resize += DrawingPanel_Resize;

            this.Controls.Add(drawingPanel);

            // İlk çizim bitmap'ini oluştur
            InitializeDrawingBitmap(drawingPanel.Width, drawingPanel.Height);
        }

        private void InitializeDrawingBitmap(int width, int height)
        {
            if (drawingBitmap != null)
            {
                drawingBitmap.Dispose();
                drawingGraphics.Dispose();
            }

            drawingBitmap = new Bitmap(width, height);
            drawingGraphics = Graphics.FromImage(drawingBitmap);
            drawingGraphics.SmoothingMode = SmoothingMode.AntiAlias;
            drawingGraphics.Clear(Color.White);
        }

        private void DrawingPanel_Resize(object sender, EventArgs e)
        {
            if (drawingPanel != null && drawingPanel.Width > 0 && drawingPanel.Height > 0)
            {
                InitializeDrawingBitmap(drawingPanel.Width, drawingPanel.Height);
                drawingPanel.Invalidate();
            }
        }

        private void DrawingPanel_Paint(object sender, PaintEventArgs e)
        {
            if (drawingBitmap != null)
            {
                e.Graphics.DrawImage(drawingBitmap, 0, 0);
            }
        }

        private void DrawingPanel_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                isDrawing = true;
                lastPoint = e.Location;
            }
        }

        private void DrawingPanel_MouseMove(object sender, MouseEventArgs e)
        {
            if (isDrawing)
            {
                using (Pen pen = new Pen(currentColor, currentPenSize))
                {
                    pen.StartCap = LineCap.Round;
                    pen.EndCap = LineCap.Round;
                    drawingGraphics.DrawLine(pen, lastPoint, e.Location);
                }
                lastPoint = e.Location;
                ((Panel)sender).Invalidate();
            }
        }

        private void DrawingPanel_MouseUp(object sender, MouseEventArgs e)
        {
            isDrawing = false;
        }

        private async Task NewDrawing()
        {
            if (drawingBitmap != null)
            {
                var result = MessageBox.Show(
                    "Mevcut çizim kaydedilmedi. Kaydetmek ister misiniz?",
                    "Uyarı",
                    MessageBoxButtons.YesNoCancel,
                    MessageBoxIcon.Warning
                );

                if (result == DialogResult.Yes)
                {
                    if (!await SaveImage()) return;
                }
                else if (result == DialogResult.Cancel)
                {
                    return;
                }
            }

            await ShowLoading("Yeni çizim oluşturuluyor...");
            drawingGraphics.Clear(Color.White);
            currentFilePath = null;
            this.Text = "Paint Benzeri Uygulama - Yeni Çizim";
            try { successSound?.Play(); } catch { }
            LogActivity("Yeni çizim oluşturuldu", true);
        }

        private async Task OpenImage()
        {
            if (drawingBitmap != null)
            {
                var result = MessageBox.Show(
                    "Mevcut çizim kaydedilmedi. Kaydetmek ister misiniz?",
                    "Uyarı",
                    MessageBoxButtons.YesNoCancel,
                    MessageBoxIcon.Warning
                );

                if (result == DialogResult.Yes)
                {
                    if (!await SaveImage()) return;
                }
                else if (result == DialogResult.Cancel)
                {
                    return;
                }
            }

            using (OpenFileDialog ofd = new OpenFileDialog())
            {
                ofd.Filter = "Resim Dosyaları|*.jpg;*.jpeg;*.png|Tüm Dosyalar|*.*";
                ofd.Title = "Resim Aç";

                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    await ShowLoading("Resim açılıyor...");
                    try
                    {
                        using (var stream = new FileStream(ofd.FileName, FileMode.Open, FileAccess.Read))
                        {
                            var image = Image.FromStream(stream);
                            InitializeDrawingBitmap(image.Width, image.Height);
                            drawingGraphics.DrawImage(image, 0, 0);
                            currentFilePath = ofd.FileName;
                            this.Text = $"Paint Benzeri Uygulama - {Path.GetFileName(currentFilePath)}";
                            try { successSound?.Play(); } catch { }
                            LogActivity($"Resim açıldı: {Path.GetFileName(currentFilePath)}", true);
                        }
                    }
                    catch (Exception ex)
                    {
                        try { errorSound?.Play(); } catch { }
                        MessageBox.Show($"Resim açılırken hata oluştu: {ex.Message}", 
                            "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        LogActivity($"Resim açma hatası: {ex.Message}", false);
                    }
                }
            }
        }

        private async Task<bool> SaveImage()
        {
            if (string.IsNullOrEmpty(currentFilePath))
            {
                return await SaveImageAs();
            }

            await ShowLoading("Resim kaydediliyor...");
            try
            {
                string extension = Path.GetExtension(currentFilePath).ToLower();
                ImageFormat format = extension == ".png" ? ImageFormat.Png : ImageFormat.Jpeg;
                drawingBitmap.Save(currentFilePath, format);
                try { successSound?.Play(); } catch { }
                LogActivity($"Resim kaydedildi: {Path.GetFileName(currentFilePath)}", true);
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

        private async Task<bool> SaveImageAs()
        {
            using (SaveFileDialog sfd = new SaveFileDialog())
            {
                sfd.Filter = "JPEG Resim|*.jpg|PNG Resim|*.png";
                sfd.Title = "Resmi Kaydet";
                sfd.FileName = currentFilePath ?? "Çizim.jpg";

                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    currentFilePath = sfd.FileName;
                    return await SaveImage();
                }
                return false;
            }
        }

        private async Task ClearDrawing()
        {
            var result = MessageBox.Show(
                "Çizimi temizlemek istediğinizden emin misiniz?",
                "Onay",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question
            );

            if (result == DialogResult.Yes)
            {
                await ShowLoading("Çizim temizleniyor...");
                drawingGraphics.Clear(Color.White);
                try { successSound?.Play(); } catch { }
                LogActivity("Çizim temizlendi", true);
            }
        }

        private void SelectColor()
        {
            using (ColorDialog colorDialog = new ColorDialog())
            {
                colorDialog.Color = currentColor;
                if (colorDialog.ShowDialog() == DialogResult.OK)
                {
                    currentColor = colorDialog.Color;
                    try { successSound?.Play(); } catch { }
                }
            }
        }

        private void SetPenSize(int size)
        {
            currentPenSize = size;
            try { successSound?.Play(); } catch { }
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

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (drawingBitmap != null)
            {
                var result = MessageBox.Show(
                    "Çizim kaydedilmedi. Kaydetmek ister misiniz?",
                    "Uyarı",
                    MessageBoxButtons.YesNoCancel,
                    MessageBoxIcon.Warning
                );

                if (result == DialogResult.Yes)
                {
                    bool saved = SaveImage().GetAwaiter().GetResult();
                    e.Cancel = !saved;
                }
                else if (result == DialogResult.Cancel)
                {
                    e.Cancel = true;
                }

                if (!e.Cancel)
                {
                    drawingBitmap.Dispose();
                    drawingGraphics.Dispose();
                }
            }
            base.OnFormClosing(e);
        }
    }
}
