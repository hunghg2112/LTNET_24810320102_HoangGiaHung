using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WarehouseAnalytics
{
    // ========================================================
    // 1. MÔ HÌNH DỮ LIỆU TỒN KHO
    // ========================================================
    public class InventoryItem
    {
        public string ItemId { get; set; }
        public string ItemName { get; set; }
        public int Quantity { get; set; }
        public decimal TotalValue { get; set; }
    }

    // ========================================================
    // 2. FORM GIAO DIỆN VÀ XỬ LÝ LOGIC (ASYNC/AWAIT)
    // ========================================================
    public class frmWarehouseAnalytics : Form
    {
        // UI Controls
        private ComboBox cboWarehouse;
        private DateTimePicker dtpFromDate, dtpToDate;
        private Button btnGenerate, btnCancel;
        private ProgressBar pgbStatus;
        private Label lblPercent;
        private DataGridView dgvReportData;

        // Quản lý việc ngắt/hủy tiến trình (Cancellation)
        private CancellationTokenSource cts;

        public frmWarehouseAnalytics()
        {
            TaoGiaoDien();
        }

        private void TaoGiaoDien()
        {
            this.Text = "Bảng Điều Khiển Tổng Hợp Báo Cáo Kho Hàng (Async/Await)";
            this.Size = new Size(800, 550);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Font = new Font("Segoe UI", 9.5F);

            // --- KHU VỰC TIÊU CHÍ TÌM KIẾM ---
            GroupBox gbCriteria = new GroupBox { Text = "Tiêu chí thống kê", Location = new Point(15, 15), Size = new Size(750, 100) };
            
            gbCriteria.Controls.Add(new Label { Text = "Kho hàng:", Location = new Point(15, 33), AutoSize = true });
            cboWarehouse = new ComboBox { Location = new Point(90, 30), Width = 150, DropDownStyle = ComboBoxStyle.DropDownList };
            cboWarehouse.Items.AddRange(new string[] { "Kho Miền Bắc", "Kho Miền Trung", "Kho Miền Nam" });
            cboWarehouse.SelectedIndex = 0;
            gbCriteria.Controls.Add(cboWarehouse);

            gbCriteria.Controls.Add(new Label { Text = "Từ ngày:", Location = new Point(260, 33), AutoSize = true });
            dtpFromDate = new DateTimePicker { Location = new Point(325, 30), Width = 120, Format = DateTimePickerFormat.Short };
            gbCriteria.Controls.Add(dtpFromDate);

            gbCriteria.Controls.Add(new Label { Text = "Đến:", Location = new Point(460, 33), AutoSize = true });
            dtpToDate = new DateTimePicker { Location = new Point(500, 30), Width = 120, Format = DateTimePickerFormat.Short };
            gbCriteria.Controls.Add(dtpToDate);

            // Nút bấm
            btnGenerate = new Button { Text = "Thống Kê Kho", Location = new Point(90, 60), Width = 120, BackColor = Color.LightBlue };
            btnGenerate.Click += BtnGenerate_Click; // Sự kiện Async
            gbCriteria.Controls.Add(btnGenerate);

            btnCancel = new Button { Text = "Hủy Tác Vụ", Location = new Point(220, 60), Width = 100, BackColor = Color.LightCoral, Enabled = false };
            btnCancel.Click += BtnCancel_Click;
            gbCriteria.Controls.Add(btnCancel);

            this.Controls.Add(gbCriteria);

            // --- KHU VỰC TRẠNG THÁI TIẾN TRÌNH ---
            this.Controls.Add(new Label { Text = "Tiến trình xử lý:", Location = new Point(15, 130), AutoSize = true });
            
            pgbStatus = new ProgressBar { Location = new Point(130, 128), Size = new Size(580, 23), Maximum = 100 };
            this.Controls.Add(pgbStatus);

            lblPercent = new Label { Text = "0%", Location = new Point(720, 130), AutoSize = true, Font = new Font("Segoe UI", 9.5F, FontStyle.Bold) };
            this.Controls.Add(lblPercent);

            // --- KHU VỰC HIỂN THỊ KẾT QUẢ ---
            dgvReportData = new DataGridView
            {
                Location = new Point(15, 170),
                Size = new Size(750, 320),
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AllowUserToAddRows = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                BackgroundColor = Color.White
            };
            this.Controls.Add(dgvReportData);
        }

        // ========================================================
        // 3. XỬ LÝ NGHIỆP VỤ BẤT ĐỒNG BỘ (ASYNC / AWAIT)
        // ========================================================

        // YÊU CẦU 1: Gắn từ khóa 'async' vào sự kiện click để sử dụng await bên trong
        private async void BtnGenerate_Click(object sender, EventArgs e)
        {
            // Thiết lập trạng thái giao diện khi bắt đầu chạy
            btnGenerate.Enabled = false;
            btnCancel.Enabled = true;
            dgvReportData.DataSource = null;
            pgbStatus.Value = 0;
            lblPercent.Text = "0%";

            // Khởi tạo Token để quản lý việc Hủy tác vụ
            cts = new CancellationTokenSource();

            // Cơ chế cập nhật Giao diện (UI) từ Background Thread an toàn bằng IProgress
            var progressIndicator = new Progress<int>(percent =>
            {
                pgbStatus.Value = percent;
                lblPercent.Text = $"{percent}%";
            });

            try
            {
                // BẮT ĐẦU CHẠY TASK NGẦM VÀ CHỜ (await) KẾT QUẢ TRẢ VỀ
                // Form sẽ không bị đơ trong quá trình đợi
                List<InventoryItem> reportData = await GenerateWarehouseDataAsync(progressIndicator, cts.Token);

                // Khi chạy xong, đổ dữ liệu lên DataGridView
                dgvReportData.DataSource = reportData;
                ĐinhDangLuoi();
                
                MessageBox.Show("Hoàn tất việc tổng hợp dữ liệu kho!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (OperationCanceledException)
            {
                // Bắt lỗi Hủy tác vụ
                MessageBox.Show("Tác vụ quét kho hàng đã bị người dùng hủy bỏ.", "Hủy tác vụ", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                pgbStatus.Value = 0;
                lblPercent.Text = "0%";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Có lỗi xảy ra: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                // Luôn thực thi đoạn này dù chạy xong hay bị hủy
                btnGenerate.Enabled = true;
                btnCancel.Enabled = false;
                cts?.Dispose();
                cts = null;
            }
        }

        // YÊU CẦU 2: Hủy tác vụ
        private void BtnCancel_Click(object sender, EventArgs e)
        {
            if (cts != null && !cts.IsCancellationRequested)
            {
                // Phát lệnh hủy tới Token
                cts.Cancel(); 
                btnCancel.Enabled = false; // Tránh bấm liên tục
            }
        }

        // ========================================================
        // 4. HÀM CHẠY DƯỚI BACKGROUND THREAD (TASK.RUN)
        // ========================================================
        private Task<List<InventoryItem>> GenerateWarehouseDataAsync(IProgress<int> progress, CancellationToken cancellationToken)
        {
            // Bọc toàn bộ thuật toán nặng vào Task.Run để đẩy xuống Core khác của CPU
            return Task.Run(async () =>
            {
                List<InventoryItem> data = new List<InventoryItem>();

                // Giả lập quá trình quét hàng trăm ngàn dòng dữ liệu (Chạy 100 vòng)
                for (int i = 1; i <= 100; i++)
                {
                    // 1. Kiểm tra xem người dùng có bấm nút Hủy không
                    // Nếu có, dòng này sẽ ném ra lỗi OperationCanceledException và văng ra ngoài
                    cancellationToken.ThrowIfCancellationRequested();

                    // 2. Giả lập công việc nặng tốn 40 mili-giây cho mỗi vòng lặp 
                    // (Tổng = 40ms * 100 = 4000ms = 4 giây)
                    await Task.Delay(40, cancellationToken);

                    // 3. Báo cáo tiến trình (% hoàn thành) ngược lên Giao diện (UI)
                    progress?.Report(i);
                }

                // Sau khi tính toán giả lập xong, tạo dữ liệu kết quả
                data.Add(new InventoryItem { ItemId = "SP001", ItemName = "Bàn phím cơ Logitech", Quantity = 250, TotalValue = 450000000 });
                data.Add(new InventoryItem { ItemId = "SP002", ItemName = "Chuột không dây Dell", Quantity = 520, TotalValue = 156000000 });
                data.Add(new InventoryItem { ItemId = "SP003", ItemName = "Màn hình Samsung 27 inch", Quantity = 85, TotalValue = 510000000 });
                data.Add(new InventoryItem { ItemId = "SP004", ItemName = "Laptop ThinkPad T14", Quantity = 32, TotalValue = 960000000 });

                return data; // Trả về List
            });
        }

        private void ĐinhDangLuoi()
        {
            if (dgvReportData.Columns.Count > 0)
            {
                dgvReportData.Columns["ItemId"].HeaderText = "Mã SP";
                dgvReportData.Columns["ItemName"].HeaderText = "Tên Sản Phẩm";
                dgvReportData.Columns["Quantity"].HeaderText = "Tồn Kho";
                
                dgvReportData.Columns["TotalValue"].HeaderText = "Tổng Giá Trị";
                dgvReportData.Columns["TotalValue"].DefaultCellStyle.Format = "N0";
                dgvReportData.Columns["TotalValue"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            }
        }
    }

    // ========================================================
    // 5. ĐIỂM CHẠY CHƯƠNG TRÌNH
    // ========================================================
    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new frmWarehouseAnalytics());
        }
    }
}