# Kịch bản kiểm thử UC05 (Học viên tự đăng ký) & UC10 (Nhân viên ghi danh hộ)

Cập nhật: 2026-11-03 · Phụ trách: **Cụm Ghi danh & Thanh toán (UC05, UC10)**

---

## 1. Chuẩn bị môi trường & Tài khoản kiểm thử

### Lệnh chạy ứng dụng:
```powershell
dotnet run --project .\SportCenter\SportCenter\SportCenter.csproj
```

### Danh sách tài khoản kiểm thử:
- **Tài khoản Học viên (Member):**
  - Email: `member3@sportcenter.local` (Chưa đăng ký lớp Cầu lông K02, Bóng rổ K01)
  - Email: `member4@sportcenter.local`
  - Mật khẩu: `123456`
- **Tài khoản Nhân viên quầy (Staff):**
  - Email: `staff1@sportcenter.local`
  - Mật khẩu: `123456`

---

## 2. Kịch bản kiểm thử chi tiết

### 🧪 PHẦN 1: UC05 – Học viên tự đăng ký lớp học (Member Portal)

#### Bước chuẩn bị:
1. Mở ứng dụng, chọn cổng **Học viên (Member)**.
2. Đăng nhập bằng tài khoản: `member3@sportcenter.local` / Mật khẩu: `123456`.
3. Bấm vào mục **"Lớp tuyển sinh"** trên menu bên trái.

#### Test Case 1.1: Đăng ký thành công lớp còn chỗ
- **Thực hiện:**
  1. Tìm một lớp đang mở tuyển sinh và còn chỗ (ví dụ: `CL-Beginner-K02` hoặc `BR-Beginner-K01`).
  2. Quan sát nút bấm ở cột **Thao tác**: Nút hiển thị màu xanh với chữ **"Đăng ký ngay"**.
  3. Bấm vào nút **"Đăng ký ngay"**.
  4. Hộp thoại xác nhận hiện ra tóm tắt thông tin lớp (Tên lớp, Khóa, Môn, Lịch học, Học phí).
  5. Bấm **Yes (Đồng ý)**.
- **Kết quả kỳ vọng:**
  - Thông báo màu xanh: *"Đăng ký thành công lớp {Tên lớp}!"*.
  - Số chỗ còn lại (`Còn chỗ`) giảm đi 1.
  - Nút bấm của lớp đó tự động chuyển sang màu xám mờ với chữ **"Đã đăng ký"** (không cho bấm tiếp).

#### Test Case 1.2: Kiểm tra lịch học xuất hiện ngay sau khi đăng ký
- **Thực hiện:**
  1. Bấm vào mục **"Lịch học của tôi"** trên menu bên trái.
- **Kết quả kỳ vọng:**
  - Danh sách các buổi học của lớp vừa đăng ký xuất hiện đầy đủ trong lịch với trạng thái `Scheduled`.

#### Test Case 1.3: Kiểm tra lớp đã hết chỗ (Quy tắc SYS01)
- **Thực hiện:**
  1. Quay lại trang **"Lớp tuyển sinh"**.
  2. Tìm lớp đã đủ sĩ số tối đa (ví dụ lớp `CL-Beginner-K01` đã đủ 4/4 học viên).
- **Kết quả kỳ vọng:**
  - Cột `Còn chỗ` hiển thị `0`.
  - Nút bấm hiển thị chữ **"Hết chỗ"** và bị vô hiệu hóa (disabled), không cho học viên đăng ký.

---

### 🧪 PHẦN 2: UC10 – Nhân viên ghi danh hộ tại quầy (Staff Portal)

#### Bước chuẩn bị:
1. Đăng xuất khỏi Member (hoặc mở ứng dụng), chọn cổng **Nhân viên / Quản lý / HLV**.
2. Đăng nhập bằng tài khoản: `staff1@sportcenter.local` / Mật khẩu: `123456`.
3. Bấm vào mục **"Ghi danh hộ"** trên menu bên trái.

#### Test Case 2.1: Ghi danh cho học viên đã có tài khoản
- **Thực hiện:**
  1. Tại ô tìm kiếm học viên (Bước 1): Nhập `member4` hoặc gõ số điện thoại `0903000004`.
  2. Danh sách gợi ý hiển thị: Chọn học viên **Phan Thị Diệu**.
  3. Quan sát khung thông tin học viên màu xám xanh hiển thị đúng họ tên, SĐT, Email.
  4. Tại mục chọn lớp (Bước 2): Chọn lớp `BR-Beginner-K01` (Bóng rổ). Khung thông tin lớp hiển thị học phí, phòng, số chỗ còn trống.
  5. Tại mục xác nhận (Bước 3): Giữ nguyên tùy chọn `[x] Đã thu tiền mặt tại quầy (kích hoạt ngay)`.
  6. Bấm nút **"Xác nhận ghi danh"** -> Bấm **Yes** trên popup xác nhận.
- **Kết quả kỳ vọng:**
  - Thông báo màu xanh: *"Đã ghi danh thành công cho 'Phan Thị Diệu' vào lớp 'BR-Beginner-K01'!"*.
  - Bảng **"Lượt ghi danh gần đây"** ở cột bên phải lập tức xuất hiện dòng mới nhất với tên học viên, lớp học, học phí, trạng thái `Active`, người ghi danh là `Lê Thị Thu` (Staff1).

#### Test Case 2.2: Kiểm tra chặn ghi danh trùng lớp (Quy tắc SYS02)
- **Thực hiện:**
  1. Vẫn giữ nguyên học viên `Phan Thị Diệu` và lớp `BR-Beginner-K01` vừa ghi danh.
  2. Bấm lại nút **"Xác nhận ghi danh"** -> Bấm **Yes**.
- **Kết quả kỳ vọng:**
  - Hệ thống chặn lại và thông báo lỗi: *"Lỗi ghi danh: Học viên 'Phan Thị Diệu' đã tham gia lớp này."*.

#### Test Case 2.3: Tạo nhanh học viên mới tại quầy và ghi danh
- **Thực hiện:**
  1. Bấm vào nút **"+ Tạo học viên mới"** ở góc phải Bước 1.
  2. Cửa sổ popup mở ra:
     - Họ và tên: `Nguyễn Hoàng Long`
     - Email: `long.nh@gmail.com`
     - Số điện thoại: `0908888777`
     - Ngày sinh & Giới tính: Giữ mặc định hoặc chỉnh sửa hợp lệ.
     - Bấm **"Tạo học viên"**.
  3. Cửa sổ đóng lại, học viên `Nguyễn Hoàng Long` được tự động điền và chọn sẵn trong form ghi danh.
  4. Chọn một lớp còn chỗ (ví dụ: `BD-Beginner-K01` môn Bóng đá).
  5. Bấm **"Xác nhận ghi danh"** -> Bấm **Yes**.
- **Kết quả kỳ vọng:**
  - Vừa tạo thành công tài khoản học viên mới trong DB (mật khẩu mặc định là `123456`), vừa ghi danh thành công vào lớp.
  - Lượt ghi danh của `Nguyễn Hoàng Long` xuất hiện ngay trên bảng danh sách bên phải.

---

### 🧪 PHẦN 3: Kiểm tra bổ sung UC02 (Hồ sơ cá nhân phía Member)
1. Đăng nhập lại cổng **Học viên (Member)** với bất kỳ tài khoản học viên nào.
2. Bấm mục **"Hồ sơ cá nhân"** trên sidebar.
3. Màn hình thông tin hồ sơ và đổi mật khẩu hiển thị đầy đủ (đã thay thế hoàn toàn màn hình Placeholder trước đây).
