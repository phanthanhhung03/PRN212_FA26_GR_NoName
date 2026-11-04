# Kịch bản kiểm thử toàn hệ thống – SportCenter (PRN212)

> **Tài liệu kiểm thử hợp nhất** bao gồm toàn bộ chức năng từ **TV1 đến TV3 và Hưng** (17 Use Cases).  
> Cập nhật: 2026-11-04 · Phiên bản: 1.0

---

## 1. Môi trường & Danh sách tài khoản kiểm thử

### Lệnh khởi chạy ứng dụng:
Mở PowerShell / Terminal tại thư mục gốc dự án và chạy:
```powershell
dotnet run --project .\SportCenter\SportCenter\SportCenter.csproj
```

### Danh sách tài khoản kiểm thử mẫu:
*Tất cả tài khoản dưới đây đều có mật khẩu mặc định là: **`123456`***

| Cổng đăng nhập | Vai trò (Role) | Email đăng nhập | Số điện thoại | Quyền hạn / Bộ môn |
| :--- | :--- | :--- | :--- | :--- |
| **Cổng Nhân viên / Quản lý / HLV** | **Manager** | `manager@sportcenter.local` | `0900000001` | Toàn quyền quản trị: Môn/Khóa, Lớp, Nhân sự, Báo cáo |
| **Cổng Nhân viên / Quản lý / HLV** | **Staff** | `staff1@sportcenter.local` | `0901000001` | Ghi danh hộ tại quầy, quản lý học viên |
| **Cổng Nhân viên / Quản lý / HLV** | **Coach** | `coach.cl1@sportcenter.local` | `0902000001` | HLV Cầu lông (xem lịch dạy, điểm danh) |
| **Cổng Nhân viên / Quản lý / HLV** | **Coach** | `coach.br1@sportcenter.local` | `0902000003` | HLV Bóng rổ (xem lịch dạy, điểm danh) |
| **Cổng Học viên** | **Member** | `member1@sportcenter.local` | `0903000001` | Học viên đã có sẵn lịch học (Cầu lông K01, Bơi K01) |
| **Cổng Học viên** | **Member** | `member3@sportcenter.local` | `0903000003` | Học viên dùng để test tự đăng ký lớp (UC05) |
| **Cổng Học viên** | **Member** | `member4@sportcenter.local` | `0903000004` | Học viên dùng để test Staff ghi danh hộ (UC10) |

---

## 2. Kịch bản kiểm thử chi tiết từng phần

---

### 🟢 PHẦN 1: TV1 – KHU VỰC HỌC VIÊN (MEMBER PORTAL)
*(Phụ trách: UC16, UC03, UC04, UC07, UC17)*

#### 1.1. UC16 – Đăng ký tài khoản học viên mới
1. Mở ứng dụng $\rightarrow$ Bấm chọn cổng **Học viên**.
2. Bấm vào nút **"Đăng ký"** dưới nút đăng nhập.
3. Điền thông tin:
   - Họ và tên: `Vũ Văn Hoàng`
   - Email: `hoang.vv@gmail.com`
   - Số điện thoại: `0907123456` (10 chữ số bắt đầu bằng 0)
   - Ngày sinh: Chọn ngày sinh ($\ge 5$ tuổi)
   - Giới tính: Chọn `Nam`
   - Mật khẩu & Xác nhận: `123456`
4. Bấm **"Đăng ký tài khoản"**.
5. **Kỳ vọng:** Thông báo đăng ký thành công xuất hiện, hệ thống tự động quay lại màn hình đăng nhập. Đăng nhập thử bằng email và mật khẩu vừa tạo thành công.

#### 1.2. UC03 – Xem danh mục Môn và Khóa học
1. Đăng nhập cổng Học viên bằng tài khoản `member1@sportcenter.local` (Pass: `123456`).
2. Chọn mục **"Môn và khóa học"** trên thanh menu bên trái.
3. **Kỳ vọng:** Hiển thị danh sách tất cả các bộ môn đang hoạt động (Cầu lông, Bóng rổ, Bóng đá, Bơi lội, Bóng chuyền) kèm danh sách các khóa học trực thuộc (Beginner, Intermediate, Advanced) cùng học phí và số buổi học.

#### 1.3. UC04 – Xem danh sách Lớp tuyển sinh
1. Chọn mục **"Lớp tuyển sinh"** trên menu bên trái.
2. Thử lọc theo **Môn học** và **Khóa học** bằng 2 ô ComboBox phía trên.
3. **Kỳ vọng:** Bảng hiển thị danh sách các lớp đang mở (`Enrolling`), kèm thông tin: Tên lớp, Phòng tập, Huấn luyện viên, Khai giảng, Lịch học trong tuần, Số chỗ còn lại (`Còn chỗ`) và Học phí.

#### 1.4. UC07 – Xem Lịch học và Điểm danh của bản thân
1. Chọn mục **"Lịch học của tôi"** trên menu bên trái.
2. **Kỳ vọng:** Bảng liệt kê toàn bộ các buổi học của các lớp mà học viên đã ghi danh thành công (`Status = Active`), bao gồm: Ngày học, Giờ học, Lớp, Phòng, HLV và trạng thái điểm danh cá nhân (`Present`, `Absent`, hoặc chưa điểm danh).

#### 1.5. UC17 – Quên mật khẩu & Đặt lại mật khẩu
1. Đăng xuất khỏi hệ thống $\rightarrow$ Tại màn hình đăng nhập Học viên (hoặc Nhân viên), bấm **"Quên mật khẩu?"**.
2. Nhập Email: `member1@sportcenter.local` và Số điện thoại đã đăng ký: `0903000001`.
3. Nhập Mật khẩu mới: `654321` (tối thiểu 6 ký tự) $\rightarrow$ Bấm **"Đặt lại mật khẩu"**.
4. **Kỳ vọng:** Thông báo đổi mật khẩu thành công. Đăng nhập lại với mật khẩu mới `654321` thành công.

---

### 🟡 PHẦN 2: TV2 – GHI DANH & THANH TOÁN (ENROLLMENT & PAYMENT)
*(Phụ trách: UC05, UC06, UC10, Ràng buộc SYS01 & SYS02)*

#### 2.1. UC05 – Học viên tự đăng ký lớp học
- **Tài khoản test:** `member3@sportcenter.local` / Pass: `123456`
1. Đăng nhập cổng **Học viên** $\rightarrow$ Bấm vào mục **"Lớp tuyển sinh"**.
2. **Test Case 1 (Đăng ký thành công lớp còn chỗ):**
   - Tìm lớp `CL-Beginner-K02` (hoặc `BR-Beginner-K01`).
   - Cột Thao tác hiện nút xanh **"Đăng ký ngay"** $\rightarrow$ Click vào nút.
   - Popup xác nhận hiện ra tóm tắt thông tin lớp và học phí $\rightarrow$ Bấm **Yes**.
   - **Kỳ vọng:** Thông báo màu xanh báo thành công; Số chỗ còn lại giảm đi 1; Nút chuyển thành màu xám **"Đã đăng ký"** (khóa không cho bấm tiếp).
3. **Test Case 2 (Kiểm tra lịch học xuất hiện ngay):**
   - Bấm sang mục **"Lịch học của tôi"**.
   - **Kỳ vọng:** Lịch các buổi học của lớp vừa đăng ký xuất hiện đầy đủ trong lịch với trạng thái `Scheduled`.
4. **Test Case 3 (Chặn đăng ký khi lớp đã hết chỗ - SYS01):**
   - Xem lớp `CL-Beginner-K01` (đã đủ 4/4 học viên).
   - **Kỳ vọng:** Cột Còn chỗ hiển thị `0`; Nút bấm hiển thị chữ **"Hết chỗ"** và bị khóa (Disabled).

#### 2.2. UC10 – Nhân viên ghi danh hộ học viên tại quầy
- **Tài khoản test:** `staff1@sportcenter.local` / Pass: `123456`
1. Đăng nhập cổng **Nhân viên / Quản lý / HLV** $\rightarrow$ Bấm mục **"Ghi danh hộ"**.
2. **Test Case 1 (Ghi danh cho học viên có sẵn):**
   - Tại **Bước 1**: Nhập `member4` hoặc SĐT `0903000004` $\rightarrow$ Chọn học viên **Phan Thị Diệu**. Thẻ học viên màu xanh xuất hiện đẹp mắt.
   - Tại **Bước 2**: Chọn lớp `BR-Beginner-K01` (Bóng rổ). Khung tóm tắt hiện đầy đủ học phí, phòng học.
   - Tại **Bước 3**: Giữ nguyên `[x] Đã thu tiền mặt tại quầy` $\rightarrow$ Bấm **"Xác nhận ghi danh"** $\rightarrow$ Bấm **Yes**.
   - **Kỳ vọng:** Báo ghi danh thành công. Bảng "Lượt ghi danh gần đây" bên phải lập tức hiển thị dòng mới của Phan Thị Diệu với trạng thái `Active`.
3. **Test Case 2 (Chặn ghi danh trùng một lớp - SYS02):**
   - Giữ nguyên học viên Phan Thị Diệu và lớp `BR-Beginner-K01` $\rightarrow$ Bấm tiếp **"Xác nhận ghi danh"** $\rightarrow$ Bấm **Yes**.
   - **Kỳ vọng:** Hệ thống chặn lại và bật ngay Popup cảnh báo ⚠️: *"Học viên 'Phan Thị Diệu' đã tham gia lớp này."*, đồng thời hiện thanh viền đỏ cảnh báo.
4. **Test Case 3 (Tạo nhanh học viên mới tại quầy):**
   - Bấm nút **"+ Tạo học viên mới"**. Cửa sổ popup mở ra với thanh cuộn ScrollViewer tiện lợi.
   - Điền thông tin: `Nguyễn Hoàng Long`, SĐT: `0908888777`, Email: `long.nh@gmail.com`, Ngày sinh, Giới tính $\rightarrow$ Bấm **Tạo học viên**.
   - **Kỳ vọng:** Tạo xong tài khoản Member trong CSDL (pass mặc định `123456`) và tự động được chọn vào Bước 1. Bạn chỉ cần chọn lớp và bấm Ghi danh là hoàn tất.

#### 2.3. UC06 – Xử lý học phí & Đối soát doanh thu
1. Khi học viên đăng ký (UC05) hoặc Staff ghi danh thu tiền (UC10), hệ thống tự động trích xuất học phí `tuition_fee`, sinh mã `order_code` và lưu vào bảng `payment`.
2. Đăng nhập tài khoản Quản lý: `manager@sportcenter.local` / Pass: `123456`.
3. Vào mục **"Báo cáo"** $\rightarrow$ Tab **"Doanh thu"**:
   - **Kỳ vọng:** Doanh thu và số lượng đơn thanh toán tăng lên tương ứng với các khoản học phí vừa phát sinh.

---

### 🔵 PHẦN 3: TV3 – HUẤN LUYỆN VIÊN & QUẢN TRỊ (COACH & ADMIN)
*(Phụ trách: UC08, UC09, UC11, UC14, UC02)*

#### 3.1. UC08 – HLV xem lịch giảng dạy & Học viên
- **Tài khoản test:** `coach.cl1@sportcenter.local` (HLV Cầu lông) / Pass: `123456`
1. Đăng nhập cổng **Nhân viên / Quản lý / HLV** $\rightarrow$ Chọn mục **"Lịch dạy"**.
2. **Kỳ vọng:** Bảng bên trái hiển thị các buổi dạy, ngày giờ, phòng tập, tiến độ buổi.
3. Click chọn 1 buổi học bất kỳ:
   - **Kỳ vọng:** Bảng bên phải lập tức hiển thị thông tin tóm tắt và danh sách tất cả học viên trong lớp kèm số điện thoại.
4. Thử đổi các bộ lọc Lớp học, Thời gian (Hôm nay, Tuần này), Trạng thái.

#### 3.2. UC09 – HLV điểm danh học viên & Hoàn thành buổi học
1. Tại màn hình Lịch dạy, bấm nút **"Điểm danh"** ở một buổi học (hoặc bấm mục **"Điểm danh"** trên sidebar).
2. **Thao tác điểm danh:**
   - Bấm **"✓ Tất cả có mặt"** $\rightarrow$ Toàn bộ học viên chuyển thành `Present`.
   - Chọn thủ công 1 học viên thành `Late` (Đi muộn), nhập ghi chú: *"Đến muộn 15 phút"*.
   - Chọn 1 học viên thành `Excused` (Nghỉ có phép).
3. Tích chọn ô `[x] Đánh dấu buổi học đã hoàn thành (Completed)`.
4. Bấm **"Lưu điểm danh"**.
5. **Kỳ vọng:** Thông báo lưu thành công. Cột Giờ điểm danh cập nhật thời gian thực. Bấm quay lại lịch dạy thấy buổi học đã chuyển sang trạng thái `Completed`.

#### 3.3. UC11 – Quản lý Môn thể thao & Khóa học (Manager)
- **Tài khoản test:** `manager@sportcenter.local` / Pass: `123456`
1. Đăng nhập Manager $\rightarrow$ Chọn mục **"Môn và khóa học"**.
2. **Thêm môn mới:** Bấm **"+ Thêm môn"** $\rightarrow$ Nhập Tên môn: *"Bóng bàn"*, Mô tả $\rightarrow$ Bấm **Lưu**.
3. **Thêm khóa học mới:** Bấm **"+ Thêm khóa học"** $\rightarrow$ Chọn môn *"Bóng bàn"*, Tên khóa: *"Bóng bàn Cơ bản"*, Trình độ: `Beginner`, Số buổi: `16`, Học phí: `1200000` $\rightarrow$ Bấm **Lưu**.
4. **Bật/Tắt hoạt động:** Thử bấm nút **"Ngừng hoạt động"** và **"Kích hoạt lại"** trên môn/khóa vừa tạo.

#### 3.4. UC14 – Quản lý Tài khoản nhân sự (Manager)
1. Chọn mục **"Tài khoản"** trên menu bên trái.
2. **Tạo tài khoản Coach mới:** Bấm **"+ Thêm tài khoản"** $\rightarrow$ Chọn vai trò `Coach`, môn chuyên môn *"Cầu lông"*, nhập Họ tên, Email, SĐT $\rightarrow$ Bấm **Lưu**.
3. **Khóa tài khoản:** Chọn 1 tài khoản $\rightarrow$ Bấm **"Khóa"** $\rightarrow$ Trạng thái chuyển thành `Banned` (tài khoản này sẽ không đăng nhập được nữa). Bấm **"Mở khóa"** để mở lại `Active`.
4. **Đặt lại mật khẩu:** Bấm **"Đặt lại MK"** trên một tài khoản $\rightarrow$ Cấp lại mật khẩu mới.

#### 3.5. UC02 – Hồ sơ cá nhân & Đổi mật khẩu (Mọi vai trò)
1. Đăng nhập bất kỳ vai trò nào (Manager, Staff, Coach hoặc Member).
2. Bấm mục **"Hồ sơ cá nhân"** trên sidebar:
   - Thử sửa Số điện thoại hoặc Họ tên $\rightarrow$ Bấm **"Lưu thông tin"** $\rightarrow$ Họ tên trên thanh sidebar được cập nhật ngay.
   - Thử tính năng **Đổi mật khẩu**: Nhập mật khẩu cũ `123456`, mật khẩu mới `654321` $\rightarrow$ Bấm **"Đổi mật khẩu"**. Đăng xuất ra và đăng nhập lại bằng mật khẩu mới thành công.

---

### 🟣 PHẦN 4: HƯNG – QUẢN LÝ LỚP HỌC & BÁO CÁO THỐNG KÊ
*(Phụ trách: UC12, UC13, UC15)*

#### 4.1. UC12 & UC13 – Mở lớp mới & Tự động sinh lịch học
- **Tài khoản test:** `manager@sportcenter.local` / Pass: `123456`
1. Chọn mục **"Lớp học"** trên menu $\rightarrow$ Bấm **"+ Mở lớp mới"**.
2. **Chọn thông tin:**
   - Chọn Môn học $\rightarrow$ Khóa học $\rightarrow$ Nhập Tên lớp: `CL-Demo-2026`.
   - Chọn Ngày khai giảng.
   - Chọn các khung giờ học trong tuần (ví dụ: Thứ Hai 18:00 và Thứ Tư 18:00).
   - Chọn Huấn luyện viên và Phòng học hợp lệ (hệ thống tự động lọc HLV và phòng rảnh).
   - Nhập Sĩ số tối đa (ví dụ: `10`).
3. Bấm **"Tạo lớp"**.
4. **Kỳ vọng:** Lớp mới được tạo thành công, `sp_create_class` tự động sinh đầy đủ tất cả các buổi học (Session) theo đúng số buổi của khóa học.

#### 4.2. Quản lý Buổi học của Lớp (Class Detail Actions)
1. Tại danh sách lớp, bấm vào lớp vừa tạo để mở trang **"Chi tiết lớp"**.
2. **Đổi phòng / HLV tạm thời:** Bấm vào 1 buổi học $\rightarrow$ Chọn **"Đổi HLV/Phòng"** $\rightarrow$ Chọn HLV hoặc phòng thay thế $\rightarrow$ Lưu.
3. **Hủy buổi học:** Chọn 1 buổi $\rightarrow$ Bấm **"Hủy buổi"** $\rightarrow$ Buổi học chuyển sang trạng thái `Cancelled`.
4. **Thêm buổi học bù:** Bấm **"+ Thêm buổi bù"** $\rightarrow$ Chọn ngày và giờ bù $\rightarrow$ Buổi bù được tạo với ký hiệu `(Bù)`.

#### 4.3. UC15 – Báo cáo thống kê & Xuất PDF
1. Chọn mục **"Báo cáo"** trên menu bên trái.
2. **Tab Tổng quan:** Xem các số liệu tổng hợp (Số học viên, Lớp đang mở, Doanh thu, Tỷ lệ chuyên cần).
3. **Tab Doanh thu:** Xem biểu đồ và bảng doanh thu theo từng môn học và khóa học.
4. **Tab Điểm danh:** Xem thống kê tỷ lệ có mặt, đi muộn, vắng mặt của các lớp.
5. **Xuất báo cáo PDF:** Bấm nút **"Xuất báo cáo PDF"** $\rightarrow$ Chọn nơi lưu file $\rightarrow$ Mở file PDF kiểm tra giao diện báo cáo chuyên nghiệp.
