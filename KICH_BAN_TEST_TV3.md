# Kịch bản test của TV3 – Cụm Coach và Quản trị (SportCenter PRN212)

Cập nhật: 2026-10-31 · Phụ trách: **TV3** (UC08, UC09, UC11, UC14, UC02)

---

## 1. Chuẩn bị môi trường kiểm thử

### Lệnh khởi chạy ứng dụng:

Mở Terminal / PowerShell tại thư mục gốc của dự án và chạy:

```powershell
dotnet run --project .\SportCenter\SportCenter\SportCenter.csproj
```

### Danh sách tài khoản kiểm thử mặc định:

- **Tài khoản Huấn luyện viên (Coach):**
  - Email: `coach.cl1@sportcenter.local` (HLV Cầu lông)
  - Email: `coach.br1@sportcenter.local` (HLV Bóng rổ)
  - Mật khẩu mặc định: `123456`
- **Tài khoản Quản lý (Manager):**
  - Email: `manager@sportcenter.local`
  - Mật khẩu mặc định: `123456`
- **Tài khoản Nhân viên (Staff):**
  - Email: `staff1@sportcenter.local`
  - Mật khẩu mặc định: `123456`

---

## 2. Chi tiết các kịch bản kiểm thử (Test Scenarios)

### 🧪 Kịch bản 1: UC08 – Xem lịch dạy & Danh sách học viên

- **Mục tiêu:** Kiểm tra HLV có thể xem được toàn bộ danh sách các buổi dạy được phân công, lọc theo lớp/thời gian và xem được danh sách học viên trong từng buổi học.
- **Tài khoản test:** `coach.cl1@sportcenter.local` / Mật khẩu: `123456`
- **Các bước thực hiện:**
  1. Tại màn hình chọn cổng đăng nhập, chọn **Nhân viên / Quản lý / HLV**.
  2. Nhập email `coach.cl1@sportcenter.local` và mật khẩu `123456` -> Bấm **Đăng nhập**.
  3. Bấm vào mục **"Lịch dạy"** trên thanh menu bên trái.
  4. **Kiểm tra hiển thị danh sách buổi dạy:**
     - Bảng bên trái hiển thị các cột: Ngày học, Thứ trong tuần, Giờ học, Lớp học, Môn học, Phòng tập, Tiến độ buổi (vd: `1/24`), Trạng thái (`Scheduled`, `Completed`).
  5. **Kiểm tra các bộ lọc:**
     - Chọn bộ lọc **Lớp học**: Chọn thử lớp `CL-Beginner-K01` -> Bảng chỉ hiển thị các buổi của lớp này.
     - Chọn bộ lọc **Thời gian**: Thử chọn `Hôm nay`, `Tuần này`, `Tháng này` -> Dữ liệu lọc tương ứng.
     - Chọn bộ lọc **Trạng thái**: Chọn `Scheduled` hoặc `Completed`.
  6. **Kiểm tra xem danh sách học viên:**
     - Click chọn 1 dòng buổi học bất kỳ trên bảng danh sách.
     - Quan sát khung chi tiết bên phải:
       - Hiển thị tóm tắt: Tên lớp, Môn học, Thời gian, Phòng tập, Trạng thái.
       - Bảng danh sách học viên: Hiển thị đầy đủ Họ tên, SĐT và trạng thái điểm danh hiện tại của từng học viên trong lớp.
       - Nút **"Mở bảng điểm danh đầy đủ"** chuyển sang trạng thái khả dụng.

---

### 🧪 Kịch bản 2: UC09 – Điểm danh học viên & Hoàn thành buổi học

- **Mục tiêu:** Kiểm tra HLV có thể điểm danh cho từng học viên trong buổi học, ghi chú, đánh dấu nhanh và lưu vào CSDL (bảng `Attendance`), đồng thời có tùy chọn cập nhật buổi học thành `Completed`.
- **Tài khoản test:** `coach.cl1@sportcenter.local` / Mật khẩu: `123456`
- **Các bước thực hiện:**
  1. Tại màn hình **Lịch dạy**, bấm nút **"Điểm danh"** trực tiếp ở một dòng buổi học (hoặc bấm mục **"Điểm danh"** trên sidebar).
  2. Trang **Điểm danh học viên** hiển thị với thông tin buổi học được chọn.
  3. **Kiểm tra tính năng đánh dấu nhanh:**
     - Bấm **"✓ Tất cả có mặt"** -> Toàn bộ học viên trong bảng chuyển trạng thái sang `✓ Có mặt (Present)`.
     - Bấm **"✗ Tất cả vắng mặt"** -> Toàn bộ học viên chuyển trạng thái sang `✗ Vắng mặt (Absent)`.
  4. **Chỉnh sửa điểm danh chi tiết từng học viên:**
     - Tại cột **Trạng thái điểm danh**, chọn thủ công các trạng thái khác nhau cho từng học viên:
       - Học viên 1: `✓ Có mặt (Present)`
       - Học viên 2: `⏰ Đi muộn (Late)`
       - Học viên 3: `📝 Có phép (Excused)`
       - Học viên 4: `✗ Vắng mặt (Absent)`
     - Tại cột **Ghi chú**, nhập ghi chú tương ứng (ví dụ: _"Đến trễ 15 phút do kẹt xe"_, _"Có đơn xin phép vắng"_).
  5. **Đánh dấu hoàn thành buổi học:**
     - Tích chọn ô `[x] Đánh dấu buổi học đã hoàn thành (Completed)`.
  6. **Lưu dữ liệu:**
     - Bấm nút **"Lưu điểm danh"**.
     - Thông báo màu xanh: _"Đã lưu điểm danh thành công!"_ xuất hiện trên NoticeBar.
     - Cột **Giờ điểm danh** tự động cập nhật thời gian check-in.
  7. **Kiểm tra điều hướng:**
     - Bấm nút **"← Quay lại lịch dạy"**.
     - Kiểm tra trạng thái của buổi học vừa điểm danh trong danh sách đã chuyển thành `Completed`.

---

### 🧪 Kịch bản 3: UC11 – Quản lý Môn thể thao và Khóa học

- **Mục tiêu:** Quản lý môn thể thao và các khóa học thuộc quyền của Manager.
- **Tài khoản test:** `manager@sportcenter.local` / Mật khẩu: `123456`
- **Các bước thực hiện:**
  1. Đăng nhập với tài khoản Manager -> Chọn mục **"Môn và khóa học"** trên menu.
  2. **Thêm môn mới:**
     - Bấm **"+ Thêm môn"**.
     - Nhập Tên môn: _"Bóng bàn"_, Mô tả: _"Bộ môn bóng bàn trong nhà"_.
     - Bấm **Lưu** -> Môn mới xuất hiện trong danh sách với trạng thái `Active`.
  3. **Thêm khóa học mới:**
     - Bấm **"+ Thêm khóa học"**.
     - Chọn Môn: _"Bóng bàn"_.
     - Nhập Tên khóa: _"Bóng bàn Cơ bản"_, Trình độ: `Beginner`, Số buổi: `16`, Học phí: `1200000`.
     - Bấm **Lưu** -> Khóa học mới hiển thị đúng dưới bộ môn tương ứng.
  4. **Sửa và Ngừng hoạt động:**
     - Chọn môn hoặc khóa học vừa tạo -> Bấm **Sửa** -> Cập nhật thông tin -> Bấm **Lưu**.
     - Bấm **"Ngừng hoạt động"** -> Trạng thái chuyển sang `Inactive`.
     - Bấm **"Kích hoạt lại"** -> Trạng thái chuyển về `Active`.

---

### 🧪 Kịch bản 4: UC14 – Quản lý Tài khoản (Staff / Coach)

- **Mục tiêu:** Manager tạo tài khoản nhân sự mới, phân công môn cho HLV, khóa/mở khóa tài khoản và cấp lại mật khẩu.
- **Tài khoản test:** `manager@sportcenter.local` / Mật khẩu: `123456`
- **Các bước thực hiện:**
  1. Đăng nhập tài khoản Manager -> Chọn mục **"Tài khoản"** trên menu.
  2. **Tạo tài khoản Coach mới:**
     - Bấm **"+ Thêm tài khoản"**.
     - Chọn vai trò: `Coach`.
     - Chọn chuyên môn: _"Cầu lông"_.
     - Điền Họ tên: _"HLV Thử Nghiệm"_, Email: `coach.test@sportcenter.local`, SĐT: `0909999888`.
     - Bấm **Lưu** -> Tài khoản mới được thêm với trạng thái `Active`.
  3. **Kiểm tra tính năng Khóa / Mở khóa:**
     - Chọn tài khoản `coach.test@sportcenter.local` -> Bấm nút **"Khóa"**.
     - Xác nhận dialog -> Trạng thái tài khoản chuyển sang `Banned`.
     - Thử đăng xuất và đăng nhập bằng tài khoản `coach.test@sportcenter.local` -> Hệ thống từ chối đăng nhập.
     - Đăng nhập lại bằng Manager -> Chọn tài khoản trên và bấm **"Mở khóa"** -> Trạng thái phục hồi `Active`.
  4. **Đặt lại mật khẩu:**
     - Bấm **"Đặt lại MK"** trên tài khoản đó -> Nhập mật khẩu mới (vd: `111222`) -> Xác nhận -> Đăng nhập bằng mật khẩu mới thành công.

---

### 🧪 Kịch bản 5: UC02 – Hồ sơ cá nhân & Đổi mật khẩu

- **Mục tiêu:** Mọi người dùng (Manager, Staff, Coach) có thể xem, cập nhật thông tin cá nhân và đổi mật khẩu của chính mình.
- **Tài khoản test:** Có thể dùng bất kỳ tài khoản nào (vd: `coach.cl1@sportcenter.local`).
- **Các bước thực hiện:**
  1. Đăng nhập tài khoản -> Chọn mục **"Hồ sơ cá nhân"** trên thanh menu.
  2. **Cập nhật thông tin:**
     - Chỉnh sửa trường **Họ và tên** (vd: _"Trần Quốc Bảo - Pro"_) và **Số điện thoại**.
     - Bấm **"Lưu thông tin"** -> Thông báo thành công và tên hiển thị ở góc sidebar lập tức cập nhật theo tên mới.
  3. **Đổi mật khẩu:**
     - Nhập Mật khẩu hiện tại: `123456`.
     - Nhập Mật khẩu mới: `654321` (tối thiểu 6 ký tự).
     - Xác nhận lại mật khẩu mới: `654321`.
     - Bấm **"Đổi mật khẩu"** -> Thông báo _"Đổi mật khẩu thành công!"_.
  4. **Kiểm tra đăng nhập lại:**
     - Bấm **"Đăng xuất"**.
     - Đăng nhập bằng mật khẩu cũ `123456` -> Báo lỗi sai mật khẩu.
     - Đăng nhập bằng mật khẩu mới `654321` -> Đăng nhập thành công.
