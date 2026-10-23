using System;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace IoTMonitorSystem
{
    // ========================================================
    // 1. FORM CHÍNH (GIAO DIỆN & XỬ LÝ LỖI CROSS-THREAD)
    // ========================================================
    public class frmIoTMonitor : Form
    {
        // Khai báo công cụ giao diện
        private Button btnStart, btnStop;
        private Label lblTempTitle, lblTemperature;
        private DataGridView dgvLogs;
        private StatusStrip statusStrip;
        private ToolStripStatusLabel lblConnectionStatus, lblSignalCount;

        // Quản lý luồng (Thread)
        private CancellationTokenSource cts;
        private Random random = new Random();

        public frmIoTMonitor()
        {
            TaoGiaoDien();
        }

        private void TaoGiaoDien()
        {
            this.Text = "Hệ Thống Giám Sát Cảnh Báo IoT Real-Time (Cross-Thread Safety)";
            this.Size = new Size(700, 500);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Font = new Font("Segoe UI", 9.5F);

            // --- 1. KHU VỰC ĐIỀU KHIỂN ---
            GroupBox gbControl = new GroupBox { Text = "Điều khiển kết nối", Location = new Point(15, 15), Size = new Size(250, 100) };
            
            btnStart = new Button { Text = "Bắt đầu Giám sát", Location = new Point(15, 35), Width = 110, Height = 40, BackColor = Color.LightGreen };
            btnStart.Click += BtnStart_Click;
            gbControl.Controls.Add(btnStart);

            btnStop = new Button { Text = "Dừng Giám sát", Location = new Point(130, 35), Width = 110, Height = 40, BackColor = Color.LightCoral, Enabled = false };
            btnStop.Click += BtnStop_Click;
            gbControl.Controls.Add(btnStop);

            this.Controls.Add(gbControl);

            // --- 2. KHU VỰC HIỂN THỊ NHIỆT ĐỘ (REAL-TIME) ---
            GroupBox gbDisplay = new GroupBox { Text = "Thông số thời gian thực", Location = new Point(15, 130), Size = new Size(250, 290) };
            
            lblTempTitle = new Label { Text = "NHIỆT ĐỘ CẢM BIẾN", Location = new Point(50, 60), AutoSize = true, Font = new Font("Segoe UI", 12F, FontStyle.Bold) };
            gbDisplay.Controls.Add(lblTempTitle);

            // Label hiển thị nhiệt độ (Kích thước chữ khổng lồ)
            lblTemperature = new Label 
            { 
                Text = "-- °C", 
                Location = new Point(30, 110), 
                Size = new Size(190, 80), 
                Font = new Font("Consolas", 36F, FontStyle.Bold), 
                TextAlign = ContentAlignment.MiddleCenter,
                ForeColor = Color.Gray
            };
            gbDisplay.Controls.Add(lblTemperature);

            this.Controls.Add(gbDisplay);

            // --- 3. KHU VỰC NHẬT KÝ (LOGS) ---
            dgvLogs = new DataGridView
            {
                Location = new Point(285, 23),
                Size = new Size(385, 397),
                ReadOnly = true,
                AllowUserToAddRows = false,
                RowHeadersVisible = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                BackgroundColor = Color.White
            };
            // Khởi tạo các cột
            dgvLogs.Columns.Add("Time", "Thời gian");
            dgvLogs.Columns.Add("Temp", "Nhiệt độ");
            dgvLogs.Columns.Add("Status", "Trạng thái");
            
            this.Controls.Add(dgvLogs);

            // --- 4. STATUS STRIP ---
            statusStrip = new StatusStrip();
            lblConnectionStatus = new ToolStripStatusLabel("Trạng thái: Đã ngắt kết nối");
            lblSignalCount = new ToolStripStatusLabel("Số lần nhận: 0") { Alignment = ToolStripItemAlignment.Right };
            
            statusStrip.Items.AddRange(new ToolStripItem[] { lblConnectionStatus, new ToolStripStatusLabel { Spring = true }, lblSignalCount });
            this.Controls.Add(statusStrip);
        }

        // ========================================================
        // THAO TÁC NÚT BẤM VÀ TASK LOOP
        // ========================================================

        private async void BtnStart_Click(object sender, EventArgs e)
        {
            btnStart.Enabled = false;
            btnStop.Enabled = true;
            lblConnectionStatus.Text = "Trạng thái: ĐANG KẾT NỐI IOT...";
            lblConnectionStatus.ForeColor = Color.Green;
            dgvLogs.Rows.Clear(); // Xóa log cũ

            // Khởi tạo Token để kiểm soát việc ngắt luồng
            cts = new CancellationTokenSource();

            try
            {
                // Gọi chạy luồng ngầm (Background Task)
                await Task.Run(() => MonitoringLoop(cts.Token));
            }
            catch (OperationCanceledException)
            {
                // Tiến trình đã bị hủy an toàn
            }
        }

        private void BtnStop_Click(object sender, EventArgs e)
        {
            // Bấm dừng -> Yêu cầu Token ngắt luồng ngầm
            if (cts != null && !cts.IsCancellationRequested)
            {
                cts.Cancel();
            }

            btnStart.Enabled = true;
            btnStop.Enabled = false;
            lblConnectionStatus.Text = "Trạng thái: Đã ngắt kết nối";
            lblConnectionStatus.ForeColor = Color.Red;
        }

        // ========================================================
        // HÀM CHẠY NGẦM (BACKGROUND LOOP) THU THẬP DỮ LIỆU
        // ========================================================
        private async Task MonitoringLoop(CancellationToken token)
        {
            int loopCount = 0;

            // Vòng lặp vô hạn (chỉ dừng khi Token bị Cancel)
            while (!token.IsCancellationRequested)
            {
                // 1. Sinh nhiệt độ ngẫu nhiên từ 30.0 đến 42.0
                // (random.NextDouble() sinh số từ 0.0 -> 1.0)
                double currentTemp = 30.0 + (random.NextDouble() * 12.0);
                loopCount++;

                // 2. Gửi dữ liệu về UI (YÊU CẦU BẮT BUỘC DÙNG THREAD SAFETY)
                CapNhatGiaoDien(currentTemp, loopCount);

                // 3. Chu kỳ nghỉ 2 giây (Chờ tín hiệu tiếp theo)
                try
                {
                    await Task.Delay(2000, token);
                }
                catch (TaskCanceledException)
                {
                    break; // Bị ngắt khi đang delay, thoát vòng lặp
                }
            }
        }

        // ========================================================
        // HÀM XỬ LÝ AN TOÀN LUỒNG (THREAD SAFETY - CROSS-THREAD)
        // ========================================================
        private void CapNhatGiaoDien(double temp, int count)
        {
            // YÊU CẦU 2: Kiểm tra xem luồng đang gọi có phải là luồng UI không?
            if (this.InvokeRequired)
            {
                // Nếu KHÔNG PHẢI luồng UI (đang ở Background) -> Đóng gói lệnh vào Action và nhờ UI thực thi dùm
                this.Invoke(new Action(() => CapNhatGiaoDien(temp, count)));
                return; // Phải return ngay lập tức để luồng ngầm không chạy tiếp xuống dưới
            }

            // --- BẮT ĐẦU TỪ ĐÂY, TA ĐANG Ở LUỒNG GIAO DIỆN (UI THREAD) AN TOÀN CHUẨN 100% ---
            
            // 1. Cập nhật Label nhiệt độ
            lblTemperature.Text = $"{temp:F1} °C";

            string statusText = "";
            if (temp > 37.0)
            {
                lblTemperature.ForeColor = Color.Red; // Quá nhiệt
                statusText = "QUÁ NHIỆT";
            }
            else
            {
                lblTemperature.ForeColor = Color.Green; // Bình thường
                statusText = "Bình thường";
            }

            // 2. Cập nhật StatusStrip
            lblSignalCount.Text = $"Số lần nhận: {count}";

            // 3. Thêm vào DataGridView Log (Thêm vào dòng đầu tiên - Index 0 để log mới nhất đẩy lên trên)
            string currentTime = DateTime.Now.ToString("HH:mm:ss");
            dgvLogs.Rows.Insert(0, currentTime, $"{temp:F1} °C", statusText);

            // Bôi màu dòng cảnh báo trong DataGridView
            if (temp > 37.0)
            {
                dgvLogs.Rows[0].DefaultCellStyle.ForeColor = Color.Red;
                dgvLogs.Rows[0].DefaultCellStyle.Font = new Font(dgvLogs.Font, FontStyle.Bold);
            }
        }
    }

    // ========================================================
    // ĐIỂM CHẠY CHƯƠNG TRÌNH
    // ========================================================
    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new frmIoTMonitor());
        }
    }
}