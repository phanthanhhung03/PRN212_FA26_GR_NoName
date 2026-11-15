using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using SportCenter.Dtos;
using SportCenter.Entities;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace SportCenter.Services
{
    // UC05: Học viên tự đăng ký lớp học
    // UC10: Nhân viên (Staff) ghi danh hộ học viên
    public class EnrollmentService
    {
        // ------------------ UC05: MEMBER TỰ ĐĂNG KÝ ------------------

        // Tạo đăng ký Pending (giữ chỗ). Học viên thanh toán qua PaymentService để chuyển sang Active.
        public async Task<int> EnrollSelfAsync(int classId)
        {
            int memberId = RequireMember();
            return await RunEnrollProcedureAsync(classId, memberId, registeredBy: null);
        }

        // ------------------ UC10: NHÂN VIÊN GHI DANH HỘ ------------------

        // UC06: học viên hủy đăng ký của chính mình trước ngày khai giảng (kiểm tra trong sp_cancel_enrollment).
        public async Task CancelSelfAsync(int classId)
        {
            int memberId = RequireMember();
            using var db = new SportCenterDbContext();
            try
            {
                await db.Database.ExecuteSqlAsync($"EXEC dbo.sp_cancel_enrollment @class_id = {classId}, @member_id = {memberId}");
            }
            catch (SqlException ex)
            {
                throw new InvalidOperationException(ex.Number switch
                {
                    51012 => "Bạn không có đăng ký nào đang hiệu lực ở lớp này.",
                    51013 => "Lớp đã khai giảng, không thể hủy đăng ký.",
                    _ => "Không hủy được đăng ký. Vui lòng thử lại."
                }, ex);
            }
        }

        // Tạo đăng ký Pending cho học viên. Trang gọi tiếp PaymentService để thu tiền.
        public async Task<int> StaffEnrollAsync(int classId, int memberId)
        {
            int staffId = RequireStaffOrManager();
            return await RunEnrollProcedureAsync(classId, memberId, staffId);
        }

        // SYS01/SYS02 kiểm tra trong sp_enroll_member (khóa dòng lớp, Pending + Active giữ chỗ).
        private static async Task<int> RunEnrollProcedureAsync(int classId, int memberId, int? registeredBy)
        {
            await PaymentService.ExpireStalePendingAsync();
            using var db = new SportCenterDbContext();
            var conn = (SqlConnection)db.Database.GetDbConnection();
            if (conn.State != ConnectionState.Open) await conn.OpenAsync();

            using var cmd = conn.CreateCommand();
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.CommandText = "dbo.sp_enroll_member";
            cmd.Parameters.Add("@class_id", SqlDbType.Int).Value = classId;
            cmd.Parameters.Add("@member_id", SqlDbType.Int).Value = memberId;
            cmd.Parameters.Add("@registered_by", SqlDbType.Int).Value = (object?)registeredBy ?? DBNull.Value;
            var output = new SqlParameter("@enrollment_id", SqlDbType.Int) { Direction = ParameterDirection.Output };
            cmd.Parameters.Add(output);

            try
            {
                await cmd.ExecuteNonQueryAsync();
                return (int)output.Value;
            }
            catch (SqlException ex)
            {
                throw new InvalidOperationException(ex.Number switch
                {
                    51001 => "Tài khoản học viên không tồn tại hoặc không hoạt động.",
                    51002 => "Tài khoản không hợp lệ để tự ghi danh.",
                    51003 => "Chỉ Staff đang hoạt động mới được ghi danh hộ.",
                    51004 => "Lớp không tồn tại hoặc không còn tuyển sinh.",
                    51005 => "Học viên đã có đăng ký đối với lớp này.",
                    51006 => "Lớp đã đủ sĩ số.",
                    2601 or 2627 => "Học viên đã được ghi danh vào lớp này.",
                    _ => "Không ghi danh được. Vui lòng thử lại."
                }, ex);
            }
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
            await PaymentService.ExpireStalePendingAsync();
            using var db = new SportCenterDbContext();

            return await db.Classes.AsNoTracking()
                .Where(c => c.Status == "Enrolling")    // sp_enroll_member chỉ nhận lớp Enrolling
                .OrderBy(c => c.Course.Sport.SportName)
                .ThenBy(c => c.ClassName)
                .Select(c => new ClassEnrollOptionDto(
                    c.ClassId,
                    c.ClassName,
                    c.Course.Sport.SportName,
                    c.Course.CourseName,
                    c.Course.TuitionFee,
                    c.MaxCapacity,
                    c.ClassMembers.Count(m => m.Status == "Active" || m.Status == "Pending"),
                    Math.Max(0, c.MaxCapacity - c.ClassMembers.Count(m => m.Status == "Active" || m.Status == "Pending")),
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
                // Mật khẩu ngẫu nhiên không ai biết; học viên tự đặt lại bằng "Quên mật khẩu" (email + SĐT).
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(Convert.ToBase64String(RandomNumberGenerator.GetBytes(18))),
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

