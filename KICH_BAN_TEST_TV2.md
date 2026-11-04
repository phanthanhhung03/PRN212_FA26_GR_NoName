# Kịch bản kiểm thử toàn diện – TV2 (Ghi danh & Thanh toán)

Cập nhật: 2026-11-04 · Phụ trách: **TV2** (UC05, UC06, UC10)

---

## 1. Chuẩn bị môi trường kiểm thử

### Lệnh khởi chạy ứng dụng:
Mở PowerShell / Terminal tại thư mục dự án và chạy:
```powershell
dotnet run --project .\SportCenter\SportCenter\SportCenter.csproj
```

### Danh sách tài khoản kiểm thử:
- **Tài khoản Học viên (Member):**
  - Email: `member3@sportcenter.local` (Mật khẩu: `123456`)
  - Email: `member4@sportcenter.local` (Mật khẩu: `123456`)
- **Tài khoản Nhân viên quầy (Staff):**
  - Email: `staff1@sportcenter.local` (Mật khẩu: `123456`)
- **Tài khoản Quản trị viên (Manager) để đối soát doanh thu:**
  - Email: `manager@sportcenter.local` (Mật khẩu: `123456`)

---

## 2. Chi tiết các kịch bản kiểm thử theo từng Use Case

---

### 🧪 PHẦN 1: UC05 – Học viên tự đăng ký lớp học (Member Portal)

- **Mục tiêu:** Kiểm tra học viên có thể tự chọn lớp đang mở tuyển sinh, bấm đăng ký, hệ thống tự động sinh hóa đơn học phí và cập nhật ngay vào lịch học của học viên; đồng thời kiểm tra 2 quy tắc vàng **SYS01** (không vượt sĩ số) và **SYS02** (không trùng lớp).
- **Tài khoản test:** `member3@sportcenter.local` / Mật khẩu: `123456`

#### Các bước thực hiện:
1. Tại màn hình khởi động của ứng dụng, chọn cổng **Học viên**.
2. Nhập Email: `member3@sportcenter.local` / Mật khẩu: `123456` $\rightarrow$ Bấm **Đăng nhập**.
3. Bấm vào mục **"Lớp tuyển sinh"** trên menu bên trái.

#### Các tình huống kiểm tra (Test Cases):
- **Test Case 1.1: Đăng ký thành công lớp còn chỗ**
  - Tìm lớp `CL-Beginner-K02` (Môn Cầu lông) hoặc `BR-Beginner-K01` (Môn Bóng rổ).
  - Quan sát nút bấm ở cột **Thao tác**: Nút hiển thị màu xanh với chữ **"Đăng ký ngay"**.
  - Bấm vào nút **"Đăng ký ngay"**.
  - Popup xác nhận hiện ra tóm tắt thông tin lớp (Tên lớp, Khóa học, Môn, Ngày khai giảng, Giờ học, Học phí).
  - Bấm **Yes (Đồng ý)**.
  - **Kỳ vọng:**
    - Thông báo màu xanh: *"Đăng ký thành công lớp {Tên lớp}!"*.
    - Số chỗ còn trống (`Còn chỗ`) giảm đi 1.
    - Nút bấm của lớp đó tự động chuyển sang màu xám mờ với chữ **"Đã đăng ký"** (khóa không cho bấm tiếp).
- **Test Case 1.2: Lịch học xuất hiện ngay sau khi đăng ký**
  - Bấm vào mục **"Lịch học của tôi"** trên thanh menu bên trái.
  - **Kỳ vọng:** Toàn bộ danh sách các buổi học của lớp bạn vừa đăng ký đã xuất hiện đầy đủ trong lịch với trạng thái `Scheduled`.
- **Test Case 1.3: Chặn đăng ký khi lớp đã đủ sĩ số tối đa (Quy tắc SYS01)**
  - Quay lại mục **"Lớp tuyển sinh"**.
  - Tìm lớp `CL-Beginner-K01` (lớp này đã đủ 4/4 học viên từ dữ liệu mẫu).
  - **Kỳ vọng:** Cột `Còn chỗ` hiển thị `0`. Nút bấm hiển thị chữ **"Hết chỗ"** và bị vô hiệu hóa (disabled), không cho phép đăng ký.

---

### 🧪 PHẦN 2: UC10 – Nhân viên (Staff) ghi danh hộ học viên tại quầy

- **Mục tiêu:** Kiểm tra nhân viên quầy có thể tìm kiếm học viên, tạo nhanh học viên mới nếu khách vãng lai chưa có tài khoản, chọn lớp học và ghi nhận ghi danh thành công.
- **Tài khoản test:** `staff1@sportcenter.local` / Mật khẩu: `123456`

#### Các bước thực hiện:
1. Đăng xuất hoặc khởi động app, chọn cổng **Nhân viên / Quản lý / HLV**.
2. Nhập Email: `staff1@sportcenter.local` / Mật khẩu: `123456` $\rightarrow$ Bấm **Đăng nhập**.
3. Bấm vào mục **"Ghi danh hộ"** trên thanh menu bên trái.

#### Các tình huống kiểm tra (Test Cases):
- **Test Case 2.1: Ghi danh cho học viên đã có tài khoản trong hệ thống**
  - Tại **Bước 1 (Chọn học viên)**: Nhập vào ô tìm kiếm `member4` hoặc SĐT `0903000004` $\rightarrow$ Danh sách gợi ý hiển thị: Click chọn **Phan Thị Diệu**.
  - Khung tóm tắt màu xanh nhạt hiển thị chính xác họ tên, SĐT và email của học viên.
  - Tại **Bước 2 (Chọn lớp học)**: Chọn lớp `BR-Beginner-K01` (Bóng rổ). Khung chi tiết hiển thị học phí, phòng học, HLV và số chỗ còn trống.
  - Tại **Bước 3 (Xác nhận & Thu phí)**: Tích chọn `[x] Đã thu tiền mặt tại quầy (kích hoạt ngay)`.
  - Bấm nút **"Xác nhận ghi danh"** $\rightarrow$ Bấm **Yes** trên popup xác nhận.
  - **Kỳ vọng:**
    - Thông báo màu xanh: *"Đã ghi danh thành công cho 'Phan Thị Diệu' vào lớp 'BR-Beginner-K01'!"*.
    - Bảng **"Lượt ghi danh gần đây"** ở cột bên phải lập tức hiển thị dòng mới nhất với tên học viên `Phan Thị Diệu`, lớp học, học phí, trạng thái `Active`, người ghi danh là `Lê Thị Thu` (Staff1).
- **Test Case 2.2: Chặn ghi danh trùng một lớp (Quy tắc SYS02)**
  - Vẫn giữ nguyên học viên `Phan Thị Diệu` và lớp `BR-Beginner-K01` vừa ghi danh xong.
  - Bấm lại nút **"Xác nhận ghi danh"** $\rightarrow$ Bấm **Yes**.
  - **Kỳ vọng:** Hệ thống từ chối và báo lỗi: *"Lỗi ghi danh: Học viên 'Phan Thị Diệu' đã tham gia lớp này."*.
- **Test Case 2.3: Tạo nhanh học viên mới tại quầy và ghi danh ngay**
  - Bấm nút **"+ Tạo học viên mới"** ở góc trên Bước 1.
  - Popup hiện ra, nhập thông tin:
    - Họ và tên: `Nguyễn Hoàng Long`
    - Email: `long.nh@gmail.com`
    - Số điện thoại: `0908888777`
    - Ngày sinh: Chọn ngày sinh hợp lệ ($\ge 5$ tuổi)
    - Giới tính: `Nam`
    - Bấm **"Tạo học viên"**.
  - **Kỳ vọng:** Popup đóng lại, tài khoản học viên mới được tạo thành công trong CSDL (mật khẩu mặc định là `123456`) và tự động được chọn sẵn vào Bước 1.
  - Tiếp tục chọn một lớp học còn chỗ (ví dụ: `BD-Beginner-K01`) $\rightarrow$ Bấm **"Xác nhận ghi danh"**.
  - **Kỳ vọng:** Ghi danh thành công và dòng ghi danh của `Nguyễn Hoàng Long` xuất hiện ngay trên bảng danh sách bên phải.

---

### 🧪 PHẦN 3: UC06 – Xử lý & Quản lý Thanh toán học phí (Payment)

- **Mục tiêu:** Kiểm tra số tiền học phí được tính toán chuẩn xác từ khóa học, mã đơn hàng được tạo tự động và doanh thu được ghi nhận đúng vào hệ thống báo cáo tài chính.

#### Các tình huống kiểm tra (Test Cases):
- **Test Case 3.1: Kiểm tra tính học phí tự động & Lưu bảng Payment**
  - Khi học viên tự đăng ký (UC05) hoặc Staff ghi danh có thu tiền mặt (UC10), hệ thống tự động:
    - Lấy đúng số tiền học phí `tuition_fee` của khóa học đó gán vào trường `amount` trong bảng `payment`.
    - Sinh mã đơn hàng duy nhất `order_code` theo Unix timestamp.
    - Trạng thái thanh toán được lưu là `Paid`, ngày thanh toán `paid_at` là thời điểm bấm ghi danh.
- **Test Case 3.2: Kiểm tra đối soát doanh thu phía Quản lý (Manager)**
  - Đăng xuất khỏi Staff $\rightarrow$ Đăng nhập cổng **Nhân viên / Quản lý / HLV** bằng tài khoản Quản lý:
    - Email: `manager@sportcenter.local` / Mật khẩu: `123456`.
  - Bấm vào mục **"Báo cáo"** trên thanh menu bên trái.
  - Chọn tab **"Doanh thu"**:
    - **Kỳ vọng:** Tổng doanh thu và số lượng đơn thanh toán đã tăng lên tương ứng với các khoản học phí của các lớp bạn vừa đăng ký thành công ở UC05 và UC10.
    - Bấm nút **"Xuất PDF"** $\rightarrow$ File báo cáo PDF được tạo ra chứa đầy đủ các khoản thu vừa phát sinh.
