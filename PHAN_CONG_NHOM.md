# Phân công nhóm – SportCenter (PRN212)

Cập nhật: 2026-10-23 · 17 UC (15 UC gốc + UC16 đăng ký Member + UC17 quên mật khẩu)

## 1. Hiện trạng

| UC | Tình trạng |
|---|---|
| UC01 Đăng nhập / đăng xuất | **Xong** |
| UC12 Mở lớp và phân công Coach | Phần A (danh sách lớp) xong; còn form "Mở lớp mới" |
| UC13 Tạo lịch cho lớp | Gộp vào UC12 (`sp_create_class` sinh buổi học luôn) |
| UC16 Đăng ký Member | Mới có UI, chưa nối DB |
| Còn lại | Chưa làm |

Đã có sẵn: database mới (schema, hàm, thủ tục tạo lớp/hủy buổi/buổi bù, seed), đăng nhập bằng email + BCrypt, khung `StaffShellView` (sidebar lọc theo role) cho Staff/Coach/Manager.

## 2. Phân công

| Người | Cụm | UC | Ghi chú |
|---|---|---|---|
| Hưng | Quản lý lớp, DB | UC12, UC13, UC15 | Nặng nhất: lọc dần môn → khóa → khung giờ → coach rảnh → phòng hợp lệ → `sp_create_class`. UC15 đọc `session`, `attendance` |
| TV1 | Khu vực Member (xem) | UC03, UC04, UC07, UC16, UC17 | Làm `MemberShellView` trước. Chủ yếu đọc dữ liệu |
| TV2 | Ghi danh, thanh toán | UC05, UC06, UC10 | Khó nhất: SYS01 (không vượt sĩ số), SYS02 (không trùng lớp), Pending, QR. UC10 dùng chung logic ghi danh với UC05 |
| TV3 | Coach và quản trị | UC08, UC09, UC11, UC14, UC02 | Đã hoàn thiện mã nguồn trên nhánh tv3. Xem chi tiết tại [KICH_BAN_TEST_TV3.md](KICH_BAN_TEST_TV3.md) |

## 3. Việc cần chốt trước khi bắt đầu

1. **Quyền ghi dữ liệu.** App hiện kết nối bằng `sa`, nên mô hình `app_user` (chỉ ghi qua thủ tục) chưa có tác dụng. DB **chưa có thủ tục ghi** cho: ghi danh, điểm danh, thanh toán, đánh dấu buổi `Completed`, khóa tài khoản, cập nhật hồ sơ. Chọn một:
   - (a) Viết thủ tục ghi cho từng nhóm UC (an toàn hơn, thêm việc ở DB trước).
   - (b) Cho EF Core ghi trực tiếp bằng `sa` (nhanh, nhưng bỏ qua phân quyền).
2. **Ai viết thủ tục DB:** đề xuất Hưng; ưu tiên thứ tự TV2 (ghi danh) → TV3 (điểm danh, tài khoản) → TV1.
3. **`appsettings.json`:** mỗi người tự tạo file của máy mình (không có trong repo, vì chứa mật khẩu).
4. **Thời điểm tạo dòng `attendance`** (khi sinh buổi hay khi Coach điểm danh lần đầu) — chưa quyết định, ảnh hưởng UC09 và UC07.

## 4. Quy ước chung

- Mỗi người một nhánh riêng (`hung`, `tv1`, `tv2`, `tv3`), tạo PR vào `main`. Không push thẳng `main`.
- Phân lớp: **View → Service → DbContext**. Không truy vấn DB trong View.
- Truy vấn chỉ đọc dùng `AsNoTracking()` và chiếu sang DTO (`Select`), tránh N+1.
- Kiểm tra role trong Service (ẩn menu không phải bảo mật). Mẫu: `ClassService.RequireManager()`.
- Không hardcode mật khẩu, connection string, secret vào code; không commit `appsettings.json`.
- Bắt lỗi (`try/catch`) chỉ ở chỗ báo được cho người dùng; không bọc bừa.
- Commit theo Conventional Commits (`feat:`, `fix:`, `refactor:`), mô tả ngắn.
- Trang Staff/Coach/Manager: **thêm đúng một dòng** `NavItem` vào `StaffShellView.AllItems`. Đây là file dễ xung đột nhất, nên ai thêm trước thì người sau `pull` rồi mới thêm.
- `MemberShellView` do TV1 tạo; những người khác không tự tạo khung Member.
- Tạo `UserControl` mới bằng **Add → User Control (WPF)** (không dùng Page).
- Đăng nhập bằng email; DB không có cột `username`. Khi nối UC16, bỏ ô "Tên đăng nhập" ở form đăng ký.

## 5. Gợi ý thứ tự làm

1. Chốt mục 3 (cả nhóm họp ngắn).
2. TV1 làm `MemberShellView` + UC03, UC04 (không phụ thuộc DB mới).
3. Hưng viết thủ tục ghi danh/thanh toán (nếu chọn 3.1a) song song với TV1.
4. TV2 làm UC05 → UC06 → UC10 khi thủ tục ghi danh sẵn sàng.
5. TV3 làm UC08, UC11 (đọc/CRUD đơn giản) trước, rồi UC09, UC14, UC02.
6. Hưng hoàn thành UC12 phần B (form mở lớp), UC15 cuối cùng khi đã có dữ liệu điểm danh.

## 6. Quy tắc hệ thống (không được vi phạm)

- **SYS01:** không ghi danh vượt sĩ số tối đa.
- **SYS02:** không ghi danh trùng một lớp.

