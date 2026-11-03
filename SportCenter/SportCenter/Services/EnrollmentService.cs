using Microsoft.EntityFrameworkCore;
using SportCenter.Dtos;
using SportCenter.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SportCenter.Services
{
    // UC05: Học viên tự đăng ký lớp học
    // UC10: Nhân viên (Staff) ghi danh hộ học viên
    public class EnrollmentService
    {
        // ------------------ UC05: MEMBER TỰ ĐĂNG KÝ ------------------

        public async Task EnrollSelfAsync(int classId)
        {
            int memberId = RequireMember();
            using var db = new SportCenterDbContext();

            var classEntity = await db.Classes
                .Include(c => c.Course)
                .FirstOrDefaultAsync(c => c.ClassId == classId)
                ?? throw new InvalidOperationException("Không tìm thấy lớp học.");

            if (classEntity.Status != "Enrolling")
            {
                throw new InvalidOperationException("Lớp học này hiện không ở trạng thái mở tuyển sinh.");
            }

            // SYS02: Không ghi danh trùng lớp
            bool alreadyEnrolled = await db.ClassMembers
                .AnyAsync(cm => cm.ClassId == classId && cm.MemberId == memberId && cm.Status != "Dropped");
            if (alreadyEnrolled)
            {
                throw new InvalidOperationException("Bạn đã ghi danh vào lớp học này rồi.");
            }

            // SYS01: Không ghi danh vượt quá sĩ số
            int activeCount = await db.ClassMembers
                .CountAsync(cm => cm.ClassId == classId && cm.Status == "Active");
            if (activeCount >= classEntity.MaxCapacity)
            {
                throw new InvalidOperationException("Lớp học đã đủ sĩ số tối đa (hết chỗ).");
            }

            var now = DateTime.Now;
            var enrollment = new ClassMember
            {
                ClassId = classId,
                MemberId = memberId,
                EnrolledAt = now,
                RegisteredBy = null,
                Status = "Active"
            };
            db.ClassMembers.Add(enrollment);

            // Sinh mã hóa đơn thanh toán
            var desc = $"HP {classEntity.ClassName}";
            if (desc.Length > 25) desc = desc[..25];

            var payment = new Payment
            {
                Enrollment = enrollment,
                OrderCode = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                Amount = classEntity.Course.TuitionFee,
                Description = desc,
                Status = "Paid",
                PaidAt = now,
                CreatedBy = memberId,
                CreatedAt = now
            };
            db.Payments.Add(payment);

            await db.SaveChangesAsync();
        }

        // ------------------ UC10: NHÂN VIÊN GHI DANH HỘ ------------------

        public async Task<int> StaffEnrollAsync(int classId, int memberId, bool isPaidAtCounter)
        {
            int staffId = RequireStaffOrManager();
            using var db = new SportCenterDbContext();

            var member = await db.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.UserId == memberId && u.Role.RoleName == "Member" && u.Status == "Active")
                ?? throw new InvalidOperationException("Học viên không tồn tại hoặc tài khoản đang bị khóa.");

            var classEntity = await db.Classes
                .Include(c => c.Course)
                .FirstOrDefaultAsync(c => c.ClassId == classId)
                ?? throw new InvalidOperationException("Không tìm thấy lớp học.");

            if (classEntity.Status != "Enrolling" && classEntity.Status != "Ongoing")
            {
                throw new InvalidOperationException("Lớp học này hiện không nhận học viên.");
            }

            // SYS02: Không ghi danh trùng một lớp
            bool alreadyEnrolled = await db.ClassMembers
                .AnyAsync(cm => cm.ClassId == classId && cm.MemberId == memberId && cm.Status != "Dropped");
            if (alreadyEnrolled)
            {
                throw new InvalidOperationException($"Học viên '{member.FullName}' đã tham gia lớp này.");
            }

            // SYS01: Không ghi danh vượt quá sĩ số
            int activeCount = await db.ClassMembers
                .CountAsync(cm => cm.ClassId == classId && cm.Status == "Active");
            if (activeCount >= classEntity.MaxCapacity)
            {
                throw new InvalidOperationException($"Lớp học đã đủ sĩ số tối đa ({classEntity.MaxCapacity} học viên).");
            }

            var now = DateTime.Now;
            var enrollment = new ClassMember
            {
                ClassId = classId,
                MemberId = memberId,
                EnrolledAt = now,
                RegisteredBy = staffId,
                Status = isPaidAtCounter ? "Active" : "Pending"
            };
            db.ClassMembers.Add(enrollment);

            var desc = $"HP {classEntity.ClassName}";
            if (desc.Length > 25) desc = desc[..25];

            var payment = new Payment
            {
                Enrollment = enrollment,
                OrderCode = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                Amount = classEntity.Course.TuitionFee,
                Description = desc,
                Status = isPaidAtCounter ? "Paid" : "Pending",
                PaidAt = isPaidAtCounter ? now : null,
                CreatedBy = staffId,
                CreatedAt = now
            };
            db.Payments.Add(payment);

            await db.SaveChangesAsync();
            return enrollment.EnrollmentId;
        }

        // Tìm kiếm nhanh học viên theo Tên, SĐT, Email
        public async Task<List<MemberSearchOptionDto>> SearchMembersAsync(string? keyword = null)
        {
            RequireStaffOrManager();
            using var db = new SportCenterDbContext();

            var query = db.Users.AsNoTracking()
                .Where(u => u.Role.RoleName == "Member" && u.Status == "Active");

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                var kw = keyword.Trim().ToLower();
                query = query.Where(u => u.FullName.ToLower().Contains(kw)
                                      || u.Email.ToLower().Contains(kw)
                                      || (u.Phone != null && u.Phone.Contains(kw)));
            }

            return await query
                .OrderBy(u => u.FullName)
                .Take(30)
                .Select(u => new MemberSearchOptionDto(
                    u.UserId, u.FullName, u.Email, u.Phone, u.BirthDate, u.Gender))
                .ToListAsync();
        }

        // Danh sách lớp đang tuyển sinh cho Staff chọn
        public async Task<List<ClassEnrollOptionDto>> GetOpenClassesForStaffAsync()
        {
            RequireStaffOrManager();
            using var db = new SportCenterDbContext();

            return await db.Classes.AsNoTracking()
                .Where(c => c.Status == "Enrolling" || c.Status == "Ongoing")
                .OrderBy(c => c.Course.Sport.SportName)
                .ThenBy(c => c.ClassName)
                .Select(c => new ClassEnrollOptionDto(
                    c.ClassId,
                    c.ClassName,
                    c.Course.Sport.SportName,
                    c.Course.CourseName,
                    c.Course.TuitionFee,
                    c.MaxCapacity,
                    c.ClassMembers.Count(m => m.Status == "Active"),
                    Math.Max(0, c.MaxCapacity - c.ClassMembers.Count(m => m.Status == "Active")),
                    string.Join(", ", c.TimeSlots.OrderBy(t => t.DayOfWeek).ThenBy(t => t.StartTime).Select(t => t.SlotName)),
                    c.Room.RoomName,
                    c.Coach != null ? c.Coach.FullName : "Chưa phân công"))
                .ToListAsync();
        }

        // Tạo nhanh học viên mới tại quầy
        public async Task<int> QuickCreateMemberAsync(string fullName, string email, string phone, DateOnly birthDate, string gender)
        {
            int staffId = RequireStaffOrManager();
            fullName = fullName.Trim();
            email = email.Trim();
            phone = phone.Trim();

            if (fullName.Length == 0) throw new InvalidOperationException("Vui lòng nhập họ và tên.");
            if (!ProfileService.IsValidEmail(email)) throw new InvalidOperationException("Email không đúng định dạng.");
            if (!ProfileService.IsValidPhone(phone)) throw new InvalidOperationException("Số điện thoại gồm 10 số và bắt đầu bằng 0.");

            using var db = new SportCenterDbContext();
            if (await db.Users.AnyAsync(u => u.Email == email))
            {
                throw new InvalidOperationException("Email này đã được sử dụng bởi tài khoản khác.");
            }

            int memberRoleId = await db.Roles.Where(r => r.RoleName == "Member").Select(r => r.RoleId).FirstAsync();
            var user = new User
            {
                RoleId = memberRoleId,
                FullName = fullName,
                Email = email,
                Phone = phone,
                BirthDate = birthDate,
                Gender = gender,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("123456"),
                Status = "Active",
                CreatedAt = DateTime.Now,
                CreatedBy = staffId
            };

            db.Users.Add(user);
            await db.SaveChangesAsync();
            return user.UserId;
        }

        // Danh sách lượt ghi danh gần đây
        public async Task<List<RecentEnrollmentDto>> GetRecentEnrollmentsAsync(int limit = 15)
        {
            RequireStaffOrManager();
            using var db = new SportCenterDbContext();

            return await db.ClassMembers.AsNoTracking()
                .OrderByDescending(cm => cm.EnrolledAt)
                .Take(limit)
                .Select(cm => new RecentEnrollmentDto(
                    cm.EnrollmentId,
                    cm.Member.FullName,
                    cm.Member.Phone ?? "",
                    cm.Class.ClassName,
                    cm.Class.Course.Sport.SportName,
                    cm.Class.Course.TuitionFee,
                    cm.Status,
                    cm.RegisteredByNavigation != null ? cm.RegisteredByNavigation.FullName : "Tự đăng ký",
                    cm.EnrolledAt))
                .ToListAsync();
        }

        private static int RequireMember()
        {
            if (CurrentUser.Value is not { Role.RoleName: "Member" } user)
            {
                throw new UnauthorizedAccessException("Chỉ học viên mới có quyền tự đăng ký lớp học.");
            }
            return user.UserId;
        }

        private static int RequireStaffOrManager()
        {
            if (CurrentUser.Value is not { Role.RoleName: "Staff" or "Manager" } user)
            {
                throw new UnauthorizedAccessException("Chỉ nhân viên hoặc quản lý mới có quyền ghi danh hộ.");
            }
            return user.UserId;
        }
    }
}

