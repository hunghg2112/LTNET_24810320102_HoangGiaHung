using System;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace QuanLyKhachSan
{
    // ========================================================
    // 1. MÔ HÌNH DỮ LIỆU (BOOKING MODEL)
    // Thực thi INotifyPropertyChanged để tự động đồng bộ 2 chiều
    // ========================================================
    public class BookingModel : INotifyPropertyChanged
    {
        private string bookingId;
        private string customerName;
        private string identityCard;
        private string roomType;
        private DateTime checkInDate;
        private int days;
        private decimal pricePerNight;

        public string BookingId
        {
            get => bookingId;
            set { bookingId = value; OnPropertyChanged(nameof(BookingId)); }
        }

        public string CustomerName
        {
            get => customerName;
            set { customerName = value; OnPropertyChanged(nameof(CustomerName)); }
        }

        public string IdentityCard
        {
            get => identityCard;
            set { identityCard = value; OnPropertyChanged(nameof(IdentityCard)); }
        }

        public string RoomType
        {
            get => roomType;
            set { roomType = value; OnPropertyChanged(nameof(RoomType)); }
        }

        public DateTime CheckInDate
        {
            get => checkInDate;
            set { checkInDate = value; OnPropertyChanged(nameof(CheckInDate)); }
        }

        public int Days
        {
            get => days;
            set { days = value; OnPropertyChanged(nameof(Days)); }
        }

        public decimal PricePerNight
        {
            get => pricePerNight;
            set { pricePerNight = value; OnPropertyChanged(nameof(PricePerNight)); }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    // ========================================================
    // 2. FORM QUẢN LÝ ĐẶT PHÒNG (frmBookingManager)
    // ========================================================
    public class frmBookingManager : Form
    {
        // Các Controls khu vực nhập liệu
        private TextBox txtBookingId, txtCustomerName, txtIdentityCard, txtPricePerNight;
        private ComboBox cboRoomType;
        private DateTimePicker dtpCheckIn;
        private NumericUpDown numDays;

        // Các nút bấm
        private Button btnAdd, btnUpdate, btnDelete, btnRefresh;

        // DataGridView & Validation
        private DataGridView dgvBookings;
        private ErrorProvider errorProvider;

        // Quản lý trạng thái đồng bộ 2 chiều
        private BindingList<BookingModel> bookingList = new BindingList<BookingModel>();
        private BindingSource bindingSource = new BindingSource();

        // Biến tự động tăng Mã đặt phòng (BK001, BK002...)
        private int autoIncrementId = 1;

        public frmBookingManager()
        {
            TaoGiaoDien();
            KhoiTaoGrid();
            KhoiTaoBinding();
            TaoMaBookingTuDong();
        }

        // --- DỰNG GIAO DIỆN BẰNG CODE ---
        private void TaoGiaoDien()
        {
            this.Text = "Quản Lý Đặt Phòng Khách Sạn (frmBookingManager)";
            this.Size = new Size(880, 560);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Font = new Font("Segoe UI", 9.5F);

            errorProvider = new ErrorProvider { BlinkStyle = ErrorBlinkStyle.NeverBlink };

            // Panel Nhập liệu
            GroupBox gbInput = new GroupBox
            {
                Text = "Thông tin đặt phòng",
                Location = new Point(15, 10),
                Size = new Size(835, 150)
            };

            // Hàng 1
            gbInput.Controls.Add(new Label { Text = "Mã đặt:", Location = new Point(15, 28), AutoSize = true });
            txtBookingId = new TextBox { Location = new Point(100, 25), Width = 150, ReadOnly = true };
            gbInput.Controls.Add(txtBookingId);

            gbInput.Controls.Add(new Label { Text = "Tên khách:", Location = new Point(280, 28), AutoSize = true });
            txtCustomerName = new TextBox { Location = new Point(370, 25), Width = 180 };
            gbInput.Controls.Add(txtCustomerName);

            gbInput.Controls.Add(new Label { Text = "CCCD/CMND:", Location = new Point(570, 28), AutoSize = true });
            txtIdentityCard = new TextBox { Location = new Point(660, 25), Width = 140 };
            gbInput.Controls.Add(txtIdentityCard);

            // Hàng 2
            gbInput.Controls.Add(new Label { Text = "Loại phòng:", Location = new Point(15, 68), AutoSize = true });
            cboRoomType = new ComboBox { Location = new Point(100, 65), Width = 150, DropDownStyle = ComboBoxStyle.DropDownList };
            cboRoomType.Items.AddRange(new string[] { "Standard", "Deluxe", "VIP" });
            cboRoomType.SelectedIndex = 0;
            gbInput.Controls.Add(cboRoomType);

            gbInput.Controls.Add(new Label { Text = "Ngày nhận:", Location = new Point(280, 68), AutoSize = true });
            dtpCheckIn = new DateTimePicker { Location = new Point(370, 65), Width = 180, Format = DateTimePickerFormat.Short };
            gbInput.Controls.Add(dtpCheckIn);

            gbInput.Controls.Add(new Label { Text = "Số ngày ở:", Location = new Point(570, 68), AutoSize = true });
            numDays = new NumericUpDown { Location = new Point(660, 65), Width = 140, Minimum = 1, Maximum = 365, Value = 1 };
            gbInput.Controls.Add(numDays);

            // Hàng 3
            gbInput.Controls.Add(new Label { Text = "Giá/Đêm:", Location = new Point(15, 108), AutoSize = true });
            txtPricePerNight = new TextBox { Location = new Point(100, 105), Width = 150 };
            gbInput.Controls.Add(txtPricePerNight);

            this.Controls.Add(gbInput);

            // Panel Chức năng (Buttons)
            btnAdd = new Button { Text = "Thêm đặt phòng", Location = new Point(15, 170), Width = 130, Height = 35 };
            btnAdd.Click += BtnAdd_Click;
            this.Controls.Add(btnAdd);

            btnUpdate = new Button { Text = "Cập nhật", Location = new Point(155, 170), Width = 100, Height = 35 };
            btnUpdate.Click += BtnUpdate_Click;
            this.Controls.Add(btnUpdate);

            btnDelete = new Button { Text = "Xóa", Location = new Point(265, 170), Width = 100, Height = 35 };
            btnDelete.Click += BtnDelete_Click;
            this.Controls.Add(btnDelete);

            btnRefresh = new Button { Text = "Làm mới", Location = new Point(375, 170), Width = 100, Height = 35 };
            btnRefresh.Click += BtnRefresh_Click;
            this.Controls.Add(btnRefresh);

            // DataGridView
            dgvBookings = new DataGridView
            {
                Location = new Point(15, 215),
                Size = new Size(835, 280),
                ReadOnly = true,
                MultiSelect = false,
                AllowUserToAddRows = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };
            dgvBookings.SelectionChanged += DgvBookings_SelectionChanged;
            this.Controls.Add(dgvBookings);
        }

        // --- YÊU CẦU 1: KHỞI TẠO GRID BẰNG CODE (AutoGenerateColumns = false) ---
        private void KhoiTaoGrid()
        {
            dgvBookings.AutoGenerateColumns = false;
            dgvBookings.Columns.Clear();

            dgvBookings.Columns.Add(new DataGridViewTextBoxColumn
            {
                HeaderText = "Mã Đặt",
                DataPropertyName = "BookingId",
                Width = 90
            });

            dgvBookings.Columns.Add(new DataGridViewTextBoxColumn
            {
                HeaderText = "Tên Khách",
                DataPropertyName = "CustomerName",
                Width = 160
            });

            dgvBookings.Columns.Add(new DataGridViewTextBoxColumn
            {
                HeaderText = "CCCD",
                DataPropertyName = "IdentityCard",
                Width = 120
            });

            dgvBookings.Columns.Add(new DataGridViewTextBoxColumn
            {
                HeaderText = "Loại Phòng",
                DataPropertyName = "RoomType",
                Width = 100
            });

            dgvBookings.Columns.Add(new DataGridViewTextBoxColumn
            {
                HeaderText = "Ngày Nhận",
                DataPropertyName = "CheckInDate",
                DefaultCellStyle = new DataGridViewCellStyle { Format = "dd/MM/yyyy" },
                Width = 110
            });

            dgvBookings.Columns.Add(new DataGridViewTextBoxColumn
            {
                HeaderText = "Số Đêm",
                DataPropertyName = "Days",
                Width = 80
            });

            dgvBookings.Columns.Add(new DataGridViewTextBoxColumn
            {
                HeaderText = "Giá/Đêm",
                DataPropertyName = "PricePerNight",
                DefaultCellStyle = new DataGridViewCellStyle { Format = "N0" },
                Width = 120
            });
        }

        // --- YÊU CẦU ĐỒNG BỘ DATA BINDING 2 CHIỀU ---
        private void KhoiTaoBinding()
        {
            bindingSource.DataSource = bookingList;
            dgvBookings.DataSource = bindingSource;

            // Thêm dữ liệu mẫu để thử nghiệm
            bookingList.Add(new BookingModel
            {
                BookingId = "BK001",
                CustomerName = "Nguyễn Văn An",
                IdentityCard = "012345678901",
                RoomType = "Deluxe",
                CheckInDate = DateTime.Now,
                Days = 3,
                PricePerNight = 1200000
            });
            autoIncrementId = 2;
        }

        // --- YÊU CẦU 2: KIỂM TRA HỢP LỆ (ERRORPROVIDER) ---
        private bool ValidateInputs()
        {
            errorProvider.Clear();
            bool isValid = true;

            // 1. Tên khách hàng không được để trống
            if (string.IsNullOrWhiteSpace(txtCustomerName.Text))
            {
                errorProvider.SetError(txtCustomerName, "Tên khách hàng không được để trống!");
                isValid = false;
            }

            // 2. CCCD phải đủ 9 hoặc 12 chữ số
            string identityPattern = @"^\d{9}$|^\d{12}$";
            if (!Regex.IsMatch(txtIdentityCard.Text.Trim(), identityPattern))
            {
                errorProvider.SetError(txtIdentityCard, "Số CCCD/CMND phải gồm đúng 9 hoặc 12 chữ số!");
                isValid = false;
            }

            // 3. Giá phòng phải là số lớn hơn 0
            if (!decimal.TryParse(txtPricePerNight.Text.Trim(), out decimal price) || price <= 0)
            {
                errorProvider.SetError(txtPricePerNight, "Giá phòng phải là một số lớn hơn 0!");
                isValid = false;
            }

            return isValid;
        }

        // Tự động tạo mã đặt phòng dạng BK001, BK002...
        private void TaoMaBookingTuDong()
        {
            txtBookingId.Text = $"BK{autoIncrementId:D3}";
        }

        // --- THAO TÁC THÊM ĐẶT PHÒNG ---
        private void BtnAdd_Click(object sender, EventArgs e)
        {
            if (!ValidateInputs()) return;

            BookingModel booking = new BookingModel
            {
                BookingId = txtBookingId.Text,
                CustomerName = txtCustomerName.Text.Trim(),
                IdentityCard = txtIdentityCard.Text.Trim(),
                RoomType = cboRoomType.SelectedItem.ToString(),
                CheckInDate = dtpCheckIn.Value,
                Days = (int)numDays.Value,
                PricePerNight = decimal.Parse(txtPricePerNight.Text.Trim())
            };

            bookingList.Add(booking);
            autoIncrementId++;

            MessageBox.Show("Thêm đặt phòng thành công!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
            ClearInputForm();
        }

        // --- YÊU CẦU 3: CẬP NHẬT DỮ LIỆU ĐỒNG BỘ XUỐNG BINDINGLIST ---
        private void BtnUpdate_Click(object sender, EventArgs e)
        {
            if (bindingSource.Current == null)
            {
                MessageBox.Show("Vui lòng chọn một bản ghi trên bảng để cập nhật!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!ValidateInputs()) return;

            BookingModel selectedBooking = (BookingModel)bindingSource.Current;

            // Cập nhật giá trị vào thuộc tính của Object -> INotifyPropertyChanged sẽ tự phát tín hiệu cập nhật lên DataGridView
            selectedBooking.CustomerName = txtCustomerName.Text.Trim();
            selectedBooking.IdentityCard = txtIdentityCard.Text.Trim();
            selectedBooking.RoomType = cboRoomType.SelectedItem.ToString();
            selectedBooking.CheckInDate = dtpCheckIn.Value;
            selectedBooking.Days = (int)numDays.Value;
            selectedBooking.PricePerNight = decimal.Parse(txtPricePerNight.Text.Trim());

            bindingSource.ResetCurrentItem(); // Làm mới hiển thị trên Grid
            MessageBox.Show("Cập nhật thông tin thành công!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        // --- YÊU CẦU 3: XÓA CÓ MESSAGEBOX XÁC NHẬN CẢNH BÁO ---
        private void BtnDelete_Click(object sender, EventArgs e)
        {
            if (bindingSource.Current == null)
            {
                MessageBox.Show("Vui lòng chọn một phòng cần xóa!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            BookingModel selectedBooking = (BookingModel)bindingSource.Current;

            // Cảnh báo Yes/No
            DialogResult confirm = MessageBox.Show(
                $"Bạn có chắc chắn muốn xóa đơn đặt phòng '{selectedBooking.BookingId}' của khách hàng '{selectedBooking.CustomerName}'?",
                "Cảnh báo xóa",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning
            );

            if (confirm == DialogResult.Yes)
            {
                bookingList.Remove(selectedBooking);
                ClearInputForm();
                MessageBox.Show("Xóa thành công!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        // --- YÊU CẦU 3: CHỌN MỘT DÒNG ĐẨY NGHỊCH LÊN FORM ---
        private void DgvBookings_SelectionChanged(object sender, EventArgs e)
        {
            if (dgvBookings.SelectedRows.Count > 0 && bindingSource.Current != null)
            {
                BookingModel selected = (BookingModel)bindingSource.Current;

                txtBookingId.Text = selected.BookingId;
                txtCustomerName.Text = selected.CustomerName;
                txtIdentityCard.Text = selected.IdentityCard;
                cboRoomType.SelectedItem = selected.RoomType;
                dtpCheckIn.Value = selected.CheckInDate;
                numDays.Value = selected.Days;
                txtPricePerNight.Text = selected.PricePerNight.ToString("0");

                errorProvider.Clear();
            }
        }

        // LÀM MỚI FORM NHẬP LIỆU
        private void BtnRefresh_Click(object sender, EventArgs e)
        {
            ClearInputForm();
        }

        private void ClearInputForm()
        {
            dgvBookings.ClearSelection();
            TaoMaBookingTuDong();
            txtCustomerName.Clear();
            txtIdentityCard.Clear();
            cboRoomType.SelectedIndex = 0;
            dtpCheckIn.Value = DateTime.Now;
            numDays.Value = 1;
            txtPricePerNight.Clear();
            errorProvider.Clear();
            txtCustomerName.Focus();
        }
    }

    // ========================================================
    // 3. ĐIỂM CHẠY CHƯƠNG TRÌNH
    // ========================================================
    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new frmBookingManager());
        }
    }
}