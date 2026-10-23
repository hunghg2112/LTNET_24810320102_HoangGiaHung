using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace QuanLyTruongHocMDI
{
    // ========================================================
    // 1. CÁC FORM CON (CHILD FORMS)
    // Thiết kế đơn giản để minh họa kiến trúc MDI
    // ========================================================
    public class frmStudentList : Form
    {
        public frmStudentList()
        {
            this.Text = "Danh Sách Sinh Viên";
            this.Size = new Size(600, 400);
            this.BackColor = Color.LightCyan; // Phân biệt màu sắc
            this.Controls.Add(new Label { Text = "QUẢN LÝ HỒ SƠ SINH VIÊN", Font = new Font("Segoe UI", 16, FontStyle.Bold), AutoSize = true, Location = new Point(20, 20) });
        }
    }

    public class frmTeacherList : Form
    {
        public frmTeacherList()
        {
            this.Text = "Danh Sách Giảng Viên";
            this.Size = new Size(600, 400);
            this.BackColor = Color.LightYellow;
            this.Controls.Add(new Label { Text = "QUẢN LÝ HỒ SƠ GIẢNG VIÊN", Font = new Font("Segoe UI", 16, FontStyle.Bold), AutoSize = true, Location = new Point(20, 20) });
        }
    }

    public class frmCourseList : Form
    {
        public frmCourseList()
        {
            this.Text = "Danh Mục Môn Học";
            this.Size = new Size(600, 400);
            this.BackColor = Color.LightGreen;
            this.Controls.Add(new Label { Text = "QUẢN LÝ DANH MỤC MÔN HỌC", Font = new Font("Segoe UI", 16, FontStyle.Bold), AutoSize = true, Location = new Point(20, 20) });
        }
    }

    // ========================================================
    // 2. FORM CHA (MDI CONTAINER)
    // ========================================================
    public class frmSchoolMain : Form
    {
        private MenuStrip menuStrip;
        private StatusStrip statusStrip;
        private ToolStripStatusLabel lblUserInfo;
        private ToolStripProgressBar progressBar;

        public frmSchoolMain()
        {
            TaoGiaoDien();
        }

        private void TaoGiaoDien()
        {
            this.Text = "Hệ Thống Quản Lý Trường Học (MDI Architecture)";
            this.Size = new Size(1000, 700);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Font = new Font("Segoe UI", 9.5F);
            
            // YÊU CẦU QUAN TRỌNG NHẤT: Biến Form này thành MDI Container
            this.IsMdiContainer = true; 

            // --- 1. THIẾT KẾ MENUSTRIP ---
            menuStrip = new MenuStrip();

            // Menu Hệ thống
            ToolStripMenuItem menuSystem = new ToolStripMenuItem("Hệ thống");
            menuSystem.DropDownItems.Add("Đăng xuất", null, (s, e) => MessageBox.Show("Đã đăng xuất!"));
            menuSystem.DropDownItems.Add("Đổi mật khẩu", null, (s, e) => MessageBox.Show("Đang mở đổi mật khẩu..."));
            menuSystem.DropDownItems.Add("-"); // Đường gạch ngang
            menuSystem.DropDownItems.Add("Thoát", null, (s, e) => Application.Exit());

            // Menu Quản lý (Gọi các Form con)
            ToolStripMenuItem menuManage = new ToolStripMenuItem("Quản lý");
            menuManage.DropDownItems.Add("Danh sách Sinh viên", null, (s, e) => OpenOrActivateForm<frmStudentList>());
            menuManage.DropDownItems.Add("Danh sách Giảng viên", null, (s, e) => OpenOrActivateForm<frmTeacherList>());
            menuManage.DropDownItems.Add("Danh mục Môn học", null, (s, e) => OpenOrActivateForm<frmCourseList>());

            // Menu Cửa sổ (Quản lý các cửa sổ MDI)
            ToolStripMenuItem menuWindow = new ToolStripMenuItem("Cửa sổ");
            menuWindow.DropDownItems.Add("Xếp tầng (Cascade)", null, (s, e) => this.LayoutMdi(MdiLayout.Cascade));
            menuWindow.DropDownItems.Add("Xếp ngang (Tile Horizontal)", null, (s, e) => this.LayoutMdi(MdiLayout.TileHorizontal));
            menuWindow.DropDownItems.Add("Xếp dọc (Tile Vertical)", null, (s, e) => this.LayoutMdi(MdiLayout.TileVertical));
            menuWindow.DropDownItems.Add("-");
            menuWindow.DropDownItems.Add("Đóng tất cả", null, CloseAllChildForms);

            // Gắn các menu lớn vào MenuStrip
            menuStrip.Items.AddRange(new ToolStripItem[] { menuSystem, menuManage, menuWindow });
            
            // Tính năng cao cấp: Tự động liệt kê các cửa sổ đang mở vào trong menu "Cửa sổ"
            menuStrip.MdiWindowListItem = menuWindow; 

            this.Controls.Add(menuStrip);
            this.MainMenuStrip = menuStrip;

            // --- 2. THIẾT KẾ STATUSSTRIP ---
            statusStrip = new StatusStrip();
            
            lblUserInfo = new ToolStripStatusLabel("👤 Cán bộ quản lý: Admin (Phòng Đào Tạo)");
            
            // Tạo một thanh tiến trình ảo
            progressBar = new ToolStripProgressBar
            {
                Value = 100, // Full 100%
                Size = new Size(150, 16),
                Alignment = ToolStripItemAlignment.Right // Đẩy về góc phải
            };

            statusStrip.Items.AddRange(new ToolStripItem[] { lblUserInfo, new ToolStripStatusLabel { Spring = true }, progressBar });
            this.Controls.Add(statusStrip);
        }

        // ========================================================
        // 3. CÁC HÀM XỬ LÝ NGHIỆP VỤ MDI
        // ========================================================

        /// <summary>
        /// Hàm Generic để mở Form. 
        /// Nếu Form đã mở -> Đưa nó lên trên cùng (Activate).
        /// Nếu Form chưa mở -> Khởi tạo mới và hiển thị.
        /// </summary>
        /// <typeparam name="T">Kiểu của Form (ví dụ: frmStudentList)</typeparam>
        private void OpenOrActivateForm<T>() where T : Form, new()
        {
            // 1. Kiểm tra xem trong các Form con (MdiChildren) có form nào thuộc kiểu T không
            foreach (Form child in this.MdiChildren)
            {
                if (child is T)
                {
                    // Nếu đã mở, chỉ cần kích hoạt (đưa lên mặt trước)
                    child.Activate();
                    
                    // (Tùy chọn) Có thể phóng to lại nếu người dùng đã thu nhỏ nó
                    if (child.WindowState == FormWindowState.Minimized)
                        child.WindowState = FormWindowState.Normal;
                        
                    return; // Kết thúc hàm, không tạo mới nữa
                }
            }

            // 2. Nếu vòng lặp chạy xong mà không tìm thấy (chưa mở), tiến hành tạo mới
            T newForm = new T();
            newForm.MdiParent = this; // Gán Form cha là Form hệ thống hiện tại
            newForm.Show();
        }

        /// <summary>
        /// Đóng tất cả các form con đang mở
        /// </summary>
        private void CloseAllChildForms(object sender, EventArgs e)
        {
            foreach (Form child in this.MdiChildren)
            {
                child.Close();
            }
        }
    }

    // ========================================================
    // 4. ĐIỂM CHẠY CHƯƠNG TRÌNH
    // ========================================================
    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new frmSchoolMain());
        }
    }
}