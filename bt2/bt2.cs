using System;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace QuanLyNhanSu
{
    // ==========================================
    // 1. MÔ HÌNH DỮ LIỆU NHÂN VIÊN
    // ==========================================
    public class EmployeeModel
    {
        public string EmpId { get; set; }
        public string FullName { get; set; }
        public string Position { get; set; }
        public decimal BaseSalary { get; set; }
        public string AvatarPath { get; set; }
    }

    // ==========================================
    // 2. FORM CHÍNH (GIAO DIỆN & LOGIC)
    // ==========================================
    public class frmEmployeeManager : Form
    {
        // Thanh điều khiển
        private MenuStrip menuStrip;
        private ToolStrip toolStrip;
        private StatusStrip statusStrip;
        private ToolStripStatusLabel lblTotalEmployees, lblTotalSalary, lblCurrentTime;

        // Khu vực nhập liệu
        private TextBox txtEmpId, txtFullName, txtPosition;
        private NumericUpDown numBaseSalary;
        private PictureBox picAvatar;
        private Button btnChooseImage, btnAdd, btnDelete;
        
        // Khu vực hiển thị
        private DataGridView dgvEmployees;

        // Dữ liệu
        private BindingList<EmployeeModel> empList = new BindingList<EmployeeModel>();
        private BindingSource bindingSource = new BindingSource();
        private Timer timerClock;
        private string currentAvatarPath = "";

        public frmEmployeeManager()
        {
            TaoGiaoDien();
            KhoiTaoDuLieu();
            CapNhatStatusStrip();
        }

        private void TaoGiaoDien()
        {
            this.Text = "Quản Lý Hồ Sơ Nhân Sự (System Dialogs & File I/O)";
            this.Size = new Size(950, 600);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Font = new Font("Segoe UI", 9.5F);

            // --- 1. MENUSTRIP ---
            menuStrip = new MenuStrip();
            ToolStripMenuItem menuFile = new ToolStripMenuItem("File");
            ToolStripMenuItem itemOpenCsv = new ToolStripMenuItem("Mở file CSV", null, BtnOpenCSV_Click);
            ToolStripMenuItem itemSaveCsv = new ToolStripMenuItem("Lưu file CSV", null, BtnSaveCSV_Click);
            ToolStripMenuItem itemExit = new ToolStripMenuItem("Thoát", null, (s, e) => Application.Exit());
            menuFile.DropDownItems.AddRange(new ToolStripItem[] { itemOpenCsv, itemSaveCsv, new ToolStripSeparator(), itemExit });

            ToolStripMenuItem menuAction = new ToolStripMenuItem("Tác vụ");
            ToolStripMenuItem itemAdd = new ToolStripMenuItem("Thêm nhân viên", null, BtnAdd_Click);
            ToolStripMenuItem itemDelete = new ToolStripMenuItem("Xóa nhân viên", null, BtnDelete_Click);
            menuAction.DropDownItems.AddRange(new ToolStripItem[] { itemAdd, itemDelete });

            menuStrip.Items.AddRange(new ToolStripItem[] { menuFile, menuAction });
            this.MainMenuStrip = menuStrip;
            this.Controls.Add(menuStrip);

            // --- 2. TOOLSTRIP ---
            toolStrip = new ToolStrip { Location = new Point(0, 24) };
            ToolStripButton btnTsOpen = new ToolStripButton("📂 Mở CSV") { DisplayStyle = ToolStripItemDisplayStyle.Text };
            btnTsOpen.Click += BtnOpenCSV_Click;
            ToolStripButton btnTsSave = new ToolStripButton("💾 Lưu CSV") { DisplayStyle = ToolStripItemDisplayStyle.Text };
            btnTsSave.Click += BtnSaveCSV_Click;
            ToolStripButton btnTsAdd = new ToolStripButton("➕ Thêm mới") { DisplayStyle = ToolStripItemDisplayStyle.Text };
            btnTsAdd.Click += BtnAdd_Click;

            toolStrip.Items.AddRange(new ToolStripItem[] { btnTsOpen, btnTsSave, new ToolStripSeparator(), btnTsAdd });
            this.Controls.Add(toolStrip);

            // --- 3. STATUSSTRIP ---
            statusStrip = new StatusStrip();
            lblTotalEmployees = new ToolStripStatusLabel("Tổng NV: 0");
            lblTotalSalary = new ToolStripStatusLabel("Tổng quỹ lương: 0 VNĐ");
            lblCurrentTime = new ToolStripStatusLabel(DateTime.Now.ToString("HH:mm:ss")) { Spring = true, TextAlign = ContentAlignment.MiddleRight };
            
            statusStrip.Items.AddRange(new ToolStripItem[] { lblTotalEmployees, new ToolStripStatusLabel(" | "), lblTotalSalary, lblCurrentTime });
            this.Controls.Add(statusStrip);

            // Đồng hồ thời gian thực
            timerClock = new Timer { Interval = 1000 };
            timerClock.Tick += (s, e) => lblCurrentTime.Text = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss");
            timerClock.Start();

            // --- 4. KHU VỰC NHẬP LIỆU (Bên trái) ---
            GroupBox gbInput = new GroupBox { Text = "Thông tin chi tiết", Location = new Point(15, 60), Size = new Size(300, 480) };
            
            gbInput.Controls.Add(new Label { Text = "Mã NV:", Location = new Point(15, 30), AutoSize = true });
            txtEmpId = new TextBox { Location = new Point(90, 27), Width = 190 };
            gbInput.Controls.Add(txtEmpId);

            gbInput.Controls.Add(new Label { Text = "Họ Tên:", Location = new Point(15, 70), AutoSize = true });
            txtFullName = new TextBox { Location = new Point(90, 67), Width = 190 };
            gbInput.Controls.Add(txtFullName);

            gbInput.Controls.Add(new Label { Text = "Chức vụ:", Location = new Point(15, 110), AutoSize = true });
            txtPosition = new TextBox { Location = new Point(90, 107), Width = 190 };
            gbInput.Controls.Add(txtPosition);

            gbInput.Controls.Add(new Label { Text = "Lương:", Location = new Point(15, 150), AutoSize = true });
            numBaseSalary = new NumericUpDown { Location = new Point(90, 147), Width = 190, Maximum = 1000000000, Increment = 1000000 };
            gbInput.Controls.Add(numBaseSalary);

            gbInput.Controls.Add(new Label { Text = "Ảnh đại diện:", Location = new Point(15, 190), AutoSize = true });
            picAvatar = new PictureBox { Location = new Point(90, 190), Size = new Size(120, 160), BorderStyle = BorderStyle.FixedSingle, SizeMode = PictureBoxSizeMode.Zoom };
            gbInput.Controls.Add(picAvatar);

            btnChooseImage = new Button { Text = "Chọn ảnh...", Location = new Point(90, 360), Width = 120 };
            btnChooseImage.Click += BtnChooseImage_Click;
            gbInput.Controls.Add(btnChooseImage);

            btnAdd = new Button { Text = "Thêm NV", Location = new Point(30, 420), Width = 110, Height = 35 };
            btnAdd.Click += BtnAdd_Click;
            gbInput.Controls.Add(btnAdd);

            btnDelete = new Button { Text = "Xóa NV", Location = new Point(150, 420), Width = 110, Height = 35 };
            btnDelete.Click += BtnDelete_Click;
            gbInput.Controls.Add(btnDelete);

            this.Controls.Add(gbInput);

            // --- 5. DATA GRID VIEW (Bên phải) ---
            dgvEmployees = new DataGridView
            {
                Location = new Point(330, 68),
                Size = new Size(585, 470),
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                AllowUserToAddRows = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };
            dgvEmployees.SelectionChanged += DgvEmployees_SelectionChanged;
            this.Controls.Add(dgvEmployees);
        }

        private void KhoiTaoDuLieu()
        {
            // Thiết lập Binding
            bindingSource.DataSource = empList;
            dgvEmployees.DataSource = bindingSource;

            // Định dạng cột Grid
            dgvEmployees.Columns["BaseSalary"].DefaultCellStyle.Format = "N0";
            dgvEmployees.Columns["BaseSalary"].HeaderText = "Mức lương";
            dgvEmployees.Columns["EmpId"].HeaderText = "Mã NV";
            dgvEmployees.Columns["FullName"].HeaderText = "Họ và Tên";
            dgvEmployees.Columns["Position"].HeaderText = "Chức vụ";
            dgvEmployees.Columns["AvatarPath"].HeaderText = "Đường dẫn ảnh";

            empList.ListChanged += (s, e) => CapNhatStatusStrip(); // Tự động cập nhật khi list thay đổi
        }

        // ==========================================
        // CÁC HÀM XỬ LÝ NGHIỆP VỤ (YÊU CẦU 1, 2, 3)
        // ==========================================

        // YÊU CẦU 3: CẬP NHẬT STATUS STRIP
        private void CapNhatStatusStrip()
        {
            int totalEmp = empList.Count;
            decimal totalSalary = empList.Sum(e => e.BaseSalary);

            lblTotalEmployees.Text = $"Tổng NV: {totalEmp}";
            lblTotalSalary.Text = $"Tổng quỹ lương: {totalSalary:N0} VNĐ";
        }

        // YÊU CẦU 1: XỬ LÝ ẢNH (OPEN FILE DIALOG)
        private void BtnChooseImage_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog ofd = new OpenFileDialog())
            {
                ofd.Title = "Chọn ảnh chân dung";
                ofd.Filter = "Image Files (*.jpg;*.png)|*.jpg;*.png|All Files (*.*)|*.*";
                
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    currentAvatarPath = ofd.FileName;
                    try
                    {
                        picAvatar.Image = Image.FromFile(currentAvatarPath);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Lỗi hiển thị ảnh: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        // THÊM NHÂN VIÊN
        private void BtnAdd_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtEmpId.Text) || string.IsNullOrWhiteSpace(txtFullName.Text))
            {
                MessageBox.Show("Vui lòng nhập đủ Mã và Tên nhân viên!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            empList.Add(new EmployeeModel
            {
                EmpId = txtEmpId.Text.Trim(),
                FullName = txtFullName.Text.Trim(),
                Position = txtPosition.Text.Trim(),
                BaseSalary = numBaseSalary.Value,
                AvatarPath = currentAvatarPath
            });

            txtEmpId.Clear(); txtFullName.Clear(); txtPosition.Clear(); 
            numBaseSalary.Value = 0; currentAvatarPath = "";
            picAvatar.Image = null;
            txtEmpId.Focus();
        }

        // XÓA NHÂN VIÊN
        private void BtnDelete_Click(object sender, EventArgs e)
        {
            if (bindingSource.Current != null)
            {
                EmployeeModel emp = (EmployeeModel)bindingSource.Current;
                if (MessageBox.Show($"Bạn có chắc muốn xóa nhân viên {emp.FullName}?", "Xác nhận", MessageBoxButtons.YesNo) == DialogResult.Yes)
                {
                    empList.Remove(emp);
                }
            }
        }

        // HIỂN THỊ DỮ LIỆU LÊN FORM KHI CLICK VÀO GRID
        private void DgvEmployees_SelectionChanged(object sender, EventArgs e)
        {
            if (dgvEmployees.SelectedRows.Count > 0 && bindingSource.Current != null)
            {
                EmployeeModel emp = (EmployeeModel)bindingSource.Current;
                txtEmpId.Text = emp.EmpId;
                txtFullName.Text = emp.FullName;
                txtPosition.Text = emp.Position;
                numBaseSalary.Value = emp.BaseSalary;
                currentAvatarPath = emp.AvatarPath;

                if (!string.IsNullOrEmpty(emp.AvatarPath) && File.Exists(emp.AvatarPath))
                {
                    try { picAvatar.Image = Image.FromFile(emp.AvatarPath); }
                    catch { picAvatar.Image = null; }
                }
                else
                {
                    picAvatar.Image = null;
                }
            }
        }

        // YÊU CẦU 2: LƯU FILE CSV (SAVE FILE DIALOG)
        private void BtnSaveCSV_Click(object sender, EventArgs e)
        {
            using (SaveFileDialog sfd = new SaveFileDialog())
            {
                sfd.Title = "Lưu danh sách nhân sự";
                sfd.Filter = "CSV File (*.csv)|*.csv";
                sfd.FileName = "DanhSachNhanSu.csv";

                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        using (StreamWriter sw = new StreamWriter(sfd.FileName))
                        {
                            // Viết dòng tiêu đề
                            sw.WriteLine("EmpId,FullName,Position,BaseSalary,AvatarPath");
                            // Viết dữ liệu
                            foreach (var emp in empList)
                            {
                                sw.WriteLine($"{emp.EmpId},{emp.FullName},{emp.Position},{emp.BaseSalary},{emp.AvatarPath}");
                            }
                        }
                        MessageBox.Show("Lưu file thành công!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Lỗi ghi file: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        // YÊU CẦU 2: ĐỌC FILE CSV (OPEN FILE DIALOG)
        private void BtnOpenCSV_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog ofd = new OpenFileDialog())
            {
                ofd.Title = "Mở file danh sách nhân sự";
                ofd.Filter = "CSV File (*.csv)|*.csv";

                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        empList.Clear();
                        string[] lines = File.ReadAllLines(ofd.FileName);

                        // Bỏ qua dòng đầu tiên (tiêu đề)
                        for (int i = 1; i < lines.Length; i++)
                        {
                            if (string.IsNullOrWhiteSpace(lines[i])) continue;

                            string[] data = lines[i].Split(',');
                            if (data.Length >= 4)
                            {
                                empList.Add(new EmployeeModel
                                {
                                    EmpId = data[0],
                                    FullName = data[1],
                                    Position = data[2],
                                    BaseSalary = decimal.Parse(data[3]),
                                    AvatarPath = data.Length > 4 ? data[4] : ""
                                });
                            }
                        }
                        MessageBox.Show("Nạp dữ liệu thành công!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Lỗi đọc file: Đảm bảo file đúng định dạng.\n" + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }
    }

    // ==========================================
    // 3. ĐIỂM CHẠY CHƯƠNG TRÌNH
    // ==========================================
    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new frmEmployeeManager());
        }
    }
}