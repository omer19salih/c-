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
using System.IO;

namespace GorselFinalSonOdevv
{
    public partial class AdminPanel : Form
    {
        private SoundPlayer successSound;
        private SoundPlayer errorSound;
        private List<ActivityLog> logs;
        private DataGridView dgvLogs;
        private TextBox txtSearch;
        private Button btnSearch;
        private Button btnDelete;
        private Button btnRefresh;
        private Button btnExport;
        private DateTimePicker dtpStart;
        private DateTimePicker dtpEnd;

        public AdminPanel()
        {
            InitializeComponent();
            InitializeSounds();
            SetupForm();
            LoadLogs();
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
            this.Text = "Admin Panel - Log Yönetimi";
            this.Size = new Size(1000, 600);
            this.StartPosition = FormStartPosition.CenterScreen;

            // Arama paneli
            Panel searchPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 60,
                BorderStyle = BorderStyle.FixedSingle
            };

            Label lblSearch = new Label
            {
                Text = "Kullanıcı Ara:",
                Location = new Point(10, 20),
                AutoSize = true
            };

            txtSearch = new TextBox
            {
                Location = new Point(100, 17),
                Size = new Size(150, 20)
            };

            Label lblDateRange = new Label
            {
                Text = "Tarih Aralığı:",
                Location = new Point(270, 20),
                AutoSize = true
            };

            dtpStart = new DateTimePicker
            {
                Location = new Point(360, 17),
                Size = new Size(150, 20),
                Format = DateTimePickerFormat.Short
            };

            Label lblTo = new Label
            {
                Text = "-",
                Location = new Point(520, 20),
                AutoSize = true
            };

            dtpEnd = new DateTimePicker
            {
                Location = new Point(540, 17),
                Size = new Size(150, 20),
                Format = DateTimePickerFormat.Short
            };

            btnSearch = new Button
            {
                Text = "Ara",
                Location = new Point(700, 15),
                Size = new Size(80, 25)
            };

            btnRefresh = new Button
            {
                Text = "Yenile",
                Location = new Point(790, 15),
                Size = new Size(80, 25)
            };

            searchPanel.Controls.AddRange(new Control[] { 
                lblSearch, txtSearch, lblDateRange, dtpStart, lblTo, dtpEnd, 
                btnSearch, btnRefresh 
            });

            // DataGridView
            dgvLogs = new DataGridView
            {
                Dock = DockStyle.Fill,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };

            // Buton paneli
            Panel buttonPanel = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 50,
                BorderStyle = BorderStyle.FixedSingle
            };

            btnDelete = new Button
            {
                Text = "Seçili Kaydı Sil",
                Location = new Point(10, 10),
                Size = new Size(120, 30)
            };

            btnExport = new Button
            {
                Text = "Excel'e Aktar",
                Location = new Point(140, 10),
                Size = new Size(120, 30)
            };

            buttonPanel.Controls.AddRange(new Control[] { btnDelete, btnExport });

            // Form kontrollerini ekle
            this.Controls.AddRange(new Control[] { searchPanel, dgvLogs, buttonPanel });

            // Olayları bağla
            btnSearch.Click += async (s, e) => await SearchLogs();
            btnRefresh.Click += async (s, e) => await RefreshLogs();
            btnDelete.Click += async (s, e) => await DeleteSelectedLog();
            btnExport.Click += async (s, e) => await ExportToExcel();
            dgvLogs.CellDoubleClick += DgvLogs_CellDoubleClick;
        }

        private async Task LoadLogs()
        {
            await ShowLoading("Loglar yükleniyor...");
            logs = new List<ActivityLog>();
            string logPath = "activity_log.txt";

            if (File.Exists(logPath))
            {
                string[] lines = File.ReadAllLines(logPath);
                foreach (string line in lines)
                {
                    try
                    {
                        // [Timestamp] User: username, Action: action, Success: true/false, Details: details
                        string[] parts = line.Split(new[] { '[', ']', ':', ',' }, StringSplitOptions.RemoveEmptyEntries);
                        if (parts.Length >= 6)
                        {
                            DateTime timestamp = DateTime.Parse(parts[0].Trim());
                            string username = parts[1].Trim().Replace("User", "").Trim();
                            string action = parts[2].Trim().Replace("Action", "").Trim();
                            bool success = bool.Parse(parts[3].Trim().Replace("Success", "").Trim());
                            string details = parts[4].Trim().Replace("Details", "").Trim();

                            logs.Add(new ActivityLog(username, action, success, details) 
                            { 
                                Timestamp = timestamp 
                            });
                        }
                    }
                    catch { /* Hatalı log satırlarını atla */ }
                }
            }

            RefreshDataGridView();
        }

        private void RefreshDataGridView()
        {
            dgvLogs.DataSource = null;
            dgvLogs.DataSource = logs.Select(l => new
            {
                Tarih = l.Timestamp,
                Kullanıcı = l.Username,
                İşlem = l.Action,
                Durum = l.IsSuccess ? "Başarılı" : "Başarısız",
                Detay = l.Details
            }).ToList();

            // Sütun başlıklarını Türkçeleştir
            if (dgvLogs.Columns.Count > 0)
            {
                dgvLogs.Columns["Tarih"].DefaultCellStyle.Format = "dd.MM.yyyy HH:mm:ss";
                dgvLogs.Columns["Durum"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            }
        }

        private async Task SearchLogs()
        {
            await ShowLoading("Arama yapılıyor...");
            string searchText = txtSearch.Text.ToLower();
            DateTime startDate = dtpStart.Value.Date;
            DateTime endDate = dtpEnd.Value.Date.AddDays(1).AddSeconds(-1);

            var filteredLogs = logs.Where(l =>
                (string.IsNullOrEmpty(searchText) || l.Username.ToLower().Contains(searchText)) &&
                l.Timestamp >= startDate &&
                l.Timestamp <= endDate
            ).ToList();

            dgvLogs.DataSource = null;
            dgvLogs.DataSource = filteredLogs.Select(l => new
            {
                Tarih = l.Timestamp,
                Kullanıcı = l.Username,
                İşlem = l.Action,
                Durum = l.IsSuccess ? "Başarılı" : "Başarısız",
                Detay = l.Details
            }).ToList();

            try { successSound?.Play(); } catch { }
        }

        private async Task RefreshLogs()
        {
            await LoadLogs();
            try { successSound?.Play(); } catch { }
        }

        private async Task DeleteSelectedLog()
        {
            if (dgvLogs.SelectedRows.Count == 0)
            {
                MessageBox.Show("Lütfen silinecek bir kayıt seçin!", "Uyarı", 
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var result = MessageBox.Show("Seçili kaydı silmek istediğinizden emin misiniz?", 
                "Onay", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                await ShowLoading("Kayıt siliniyor...");
                var selectedLog = (dynamic)dgvLogs.SelectedRows[0].DataBoundItem;
                logs.RemoveAll(l => 
                    l.Timestamp == selectedLog.Tarih && 
                    l.Username == selectedLog.Kullanıcı);

                // Log dosyasını güncelle
                File.WriteAllLines("activity_log.txt", 
                    logs.Select(l => l.ToString()));

                RefreshDataGridView();
                try { successSound?.Play(); } catch { }
            }
        }

        private async Task ExportToExcel()
        {
            if (logs.Count == 0)
            {
                MessageBox.Show("Dışa aktarılacak kayıt bulunamadı!", "Uyarı", 
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            SaveFileDialog saveDialog = new SaveFileDialog
            {
                Filter = "Excel Dosyası|*.xlsx",
                Title = "Excel'e Aktar",
                FileName = $"Log_Kayıtları_{DateTime.Now:yyyyMMdd_HHmmss}"
            };

            if (saveDialog.ShowDialog() == DialogResult.OK)
            {
                await ShowLoading("Excel'e aktarılıyor...");
                try
                {
                    // Excel'e aktarma işlemi burada yapılacak
                    // Örnek olarak CSV formatında kaydedelim
                    string csvContent = "Tarih,Kullanıcı,İşlem,Durum,Detay\r\n";
                    csvContent += string.Join("\r\n", logs.Select(l => 
                        $"{l.Timestamp:dd.MM.yyyy HH:mm:ss},{l.Username},{l.Action}," +
                        $"{(l.IsSuccess ? "Başarılı" : "Başarısız")},{l.Details}"));

                    File.WriteAllText(saveDialog.FileName.Replace(".xlsx", ".csv"), 
                        csvContent, Encoding.UTF8);

                    try { successSound?.Play(); } catch { }
                    MessageBox.Show("Kayıtlar başarıyla dışa aktarıldı!", "Başarılı", 
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    try { errorSound?.Play(); } catch { }
                    MessageBox.Show($"Dışa aktarma sırasında hata oluştu: {ex.Message}", 
                        "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void DgvLogs_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0)
            {
                var selectedLog = (dynamic)dgvLogs.Rows[e.RowIndex].DataBoundItem;
                MessageBox.Show(
                    $"Tarih: {selectedLog.Tarih:dd.MM.yyyy HH:mm:ss}\r\n" +
                    $"Kullanıcı: {selectedLog.Kullanıcı}\r\n" +
                    $"İşlem: {selectedLog.İşlem}\r\n" +
                    $"Durum: {selectedLog.Durum}\r\n" +
                    $"Detay: {selectedLog.Detay}",
                    "Log Detayı",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information
                );
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

            await Task.Delay(1000); // 1 saniye bekle
            loadingForm.Close();
        }
    }
}
