using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using SportCenter.Dtos;
using SportCenter.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SportCenter.Services
{
    // UC14: Manager tạo tài khoản Staff / Coach, sửa họ tên / SĐT / môn của Coach, khóa / mở khóa, đặt lại mật khẩu.
    // Lỗi nghiệp vụ ném InvalidOperationException với thông báo tiếng Việt.
    public class AccountService
    {
        public static readonly string[] CreatableRoles = { "Staff", "Coach" };

        public const int MaxNameLength = 100;
        public const int MaxEmailLength = 100;

        private const string Active = "Active";
        private const string Banned = "Banned";

        // ---------- Đọc ----------

        // Mọi tài khoản (kể cả Member và đã khóa); lọc / tìm kiếm làm ở giao diện.
        public async Task<List<AccountDto>> GetAccountsAsync()
        {
            RequireManager();
            using var db = new SportCenterDbContext();
            var today = DateOnly.FromDateTime(DateTime.Today);
            return await db.Users.AsNoTracking()
                .OrderBy(u => u.Role.RoleName).ThenBy(u => u.FullName)
                .Select(u => new AccountDto(
                    u.UserId, u.FullName, u.Email, u.Role.RoleName, u.Phone,
                    u.SportId, u.Sport == null ? null : u.Sport.SportName, u.Status, u.CreatedAt,
                    u.Classes.Count(k => k.Status == "Enrolling" || k.Status == "Ongoing"),
                    u.Sessions.Count(x => x.Status == "Scheduled" && x.SessionDate >= today)))
                .ToListAsync();
        }

        // Các môn đang hoạt động để gán cho Coach.
        public async Task<List<SportOptionDto>> GetSportsAsync()
        {
            RequireManager();
            using var db = new SportCenterDbContext();
            return await db.Sports.AsNoTracking()
                .Where(s => s.Status == "Active")
                .OrderBy(s => s.SportName)
                .Select(s => new SportOptionDto(s.SportId, s.SportName))
                .ToListAsync();
        }

        // ---------- Ghi ----------

        public async Task<int> CreateAsync(
            string fullName, string email, string? phone, string roleName, int? sportId, string password)
        {
            RequireManager();
            (fullName, email, phone) = CleanProfile(fullName, email, phone);
            ValidateRole(roleName, sportId);
            ValidatePassword(password);

            using var db = new SportCenterDbContext();
            await RequireActiveManagerAsync(db);
            if (await db.Users.AnyAsync(u => u.Email == email)) throw new InvalidOperationException("Email này đã được dùng bởi tài khoản khác.");
            if (roleName == "Coach") await EnsureSportActiveAsync(db, sportId!.Value);

            var user = new User
            {
                RoleId = await RoleIdAsync(db, roleName),
                FullName = fullName, Email = email, Phone = phone,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
                SportId = roleName == "Coach" ? sportId : null,
                Status = Active,
                CreatedBy = CurrentUser.Value!.UserId,
                CreatedAt = DateTime.Now
            };
            db.Users.Add(user);
            await SaveAsync(db);
            return user.UserId;
        }

        // Sửa họ tên, SĐT và môn của Coach. Role (Staff / Coach) và email không đổi ở đây.
        public async Task UpdateAsync(int userId, string fullName, string? phone, string roleName, int? sportId)
        {
            RequireManager();
            fullName = CleanName(fullName);
            phone = CleanPhone(phone);
            ValidateRole(roleName, sportId);

            using var db = new SportCenterDbContext();
            await RequireActiveManagerAsync(db);
            User user = await db.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.UserId == userId)
                ?? throw new InvalidOperationException("Không tìm thấy tài khoản này.");
            if (user.Role.RoleName is not ("Staff" or "Coach"))
                throw new InvalidOperationException("Chỉ sửa được tài khoản Staff hoặc Coach.");

            if (user.Role.RoleName != roleName) throw new InvalidOperationException("Không đổi được role của tài khoản.");
            bool sportChanged = user.SportId != (roleName == "Coach" ? sportId : null);
            var today = DateOnly.FromDateTime(DateTime.Today);
            if (sportChanged && user.Role.RoleName == "Coach"
                && (await db.Classes.AnyAsync(k => k.CoachId == userId && (k.Status == "Enrolling" || k.Status == "Ongoing"))
                    || await db.Sessions.AnyAsync(x => x.CoachId == userId && x.Status == "Scheduled" && x.SessionDate >= today)))
            {
                throw new InvalidOperationException("Coach đang phụ trách lớp hoặc buổi học sắp tới nên không đổi được môn.");
            }
            if (roleName == "Coach" && sportChanged) await EnsureSportActiveAsync(db, sportId!.Value);

            user.FullName = fullName;
            user.Phone = phone;
            user.RoleId = await RoleIdAsync(db, roleName);
            user.SportId = roleName == "Coach" ? sportId : null;
            await SaveAsync(db);
        }

        // Khóa (Banned) hoặc mở khóa (Active). Không tự khóa mình; không khóa Manager hoạt động cuối cùng.
        public async Task SetLockedAsync(int userId, bool locked)
        {
            RequireManager();
            if (userId == CurrentUser.Value!.UserId) throw new InvalidOperationException("Bạn không thể đổi trạng thái tài khoản của chính mình.");

            using var db = new SportCenterDbContext();
            await RequireActiveManagerAsync(db);
            // Serializable: hai Manager khóa nhau cùng lúc không thể cùng thấy "còn Manager khác" rồi để lại 0 Manager hoạt động.
            await using var tx = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
            User user = await db.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.UserId == userId)
                ?? throw new InvalidOperationException("Không tìm thấy tài khoản này.");

            if (locked && user.Role.RoleName == "Manager" && user.Status == Active
                && !await db.Users.AnyAsync(u => u.UserId != userId && u.Role.RoleName == "Manager" && u.Status == Active))
            {
                throw new InvalidOperationException("Không thể khóa Manager hoạt động cuối cùng.");
            }
            user.Status = locked ? Banned : Active;
            await SaveAsync(db);
            await tx.CommitAsync();
        }

        // Manager đặt mật khẩu mới cho người khác (người đó tự đổi lại ở Hồ sơ cá nhân). Mật khẩu của chính mình đổi ở Hồ sơ.
        public async Task ResetPasswordAsync(int userId, string newPassword)
        {
            RequireManager();
            if (userId == CurrentUser.Value!.UserId) throw new InvalidOperationException("Hãy đổi mật khẩu của bạn ở Hồ sơ cá nhân.");
            ValidatePassword(newPassword);

            using var db = new SportCenterDbContext();
            await RequireActiveManagerAsync(db);
            User user = await db.Users.FirstOrDefaultAsync(u => u.UserId == userId)
                ?? throw new InvalidOperationException("Không tìm thấy tài khoản này.");
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
            await SaveAsync(db);
        }

        // ---------- Kiểm tra dữ liệu ----------

        private static (string FullName, string Email, string? Phone) CleanProfile(string fullName, string email, string? phone)
        {
            fullName = CleanName(fullName);
            email = (email ?? "").Trim();
            if (email.Length == 0) throw new InvalidOperationException("Vui lòng nhập email.");
            if (email.Length > MaxEmailLength) throw new InvalidOperationException($"Email tối đa {MaxEmailLength} ký tự.");
            if (!ProfileService.IsValidEmail(email)) throw new InvalidOperationException("Email không đúng định dạng.");
            return (fullName, email, CleanPhone(phone));
        }

        private static string CleanName(string fullName)
        {
            fullName = (fullName ?? "").Trim();
            if (fullName.Length == 0) throw new InvalidOperationException("Vui lòng nhập họ và tên.");
            if (fullName.Length > MaxNameLength) throw new InvalidOperationException($"Họ và tên tối đa {MaxNameLength} ký tự.");
            return fullName;
        }

        private static string? CleanPhone(string? phone)
        {
            phone = string.IsNullOrWhiteSpace(phone) ? null : phone.Trim();
            if (phone is not null && !ProfileService.IsValidPhone(phone))
                throw new InvalidOperationException("Số điện thoại gồm 10 số và bắt đầu bằng 0.");
            return phone;
        }

        private static void ValidateRole(string roleName, int? sportId)
        {
            if (!CreatableRoles.Contains(roleName)) throw new InvalidOperationException("Chỉ được chọn role Staff hoặc Coach.");
            if (roleName == "Coach" && sportId is null) throw new InvalidOperationException("Coach phải thuộc một môn thể thao.");
        }

        private static void ValidatePassword(string password)
        {
            if (string.IsNullOrWhiteSpace(password) || password.Length < ProfileService.MinPasswordLength)
                throw new InvalidOperationException($"Mật khẩu tối thiểu {ProfileService.MinPasswordLength} ký tự.");
            if (Encoding.UTF8.GetByteCount(password) > ProfileService.MaxPasswordBytes)
                throw new InvalidOperationException("Mật khẩu quá dài.");
        }

        private static async Task EnsureSportActiveAsync(SportCenterDbContext db, int sportId)
        {
            Sport sport = await db.Sports.AsNoTracking().FirstOrDefaultAsync(s => s.SportId == sportId)
                ?? throw new InvalidOperationException("Không tìm thấy môn này.");
            if (sport.Status != Active) throw new InvalidOperationException("Môn này đang ngừng, hãy chọn môn khác.");
        }

        private static async Task<int> RoleIdAsync(SportCenterDbContext db, string roleName) =>
            await db.Roles.Where(r => r.RoleName == roleName).Select(r => r.RoleId).FirstAsync();

        private static async Task SaveAsync(SportCenterDbContext db)
        {
            try
            {
                await db.SaveChangesAsync();
            }
            catch (DbUpdateException ex) when (ex.InnerException is SqlException { Number: 2601 or 2627 })
            {
                throw new InvalidOperationException("Email này đã được dùng bởi tài khoản khác.", ex);
            }
        }

        // Người thao tác phải còn là Manager đang hoạt động trong DB (CurrentUser chỉ được nạp lúc đăng nhập, có thể đã bị khóa từ đó).
        private static async Task RequireActiveManagerAsync(SportCenterDbContext db)
        {
            RequireManager();
            int actorId = CurrentUser.Value!.UserId;
            if (!await db.Users.AnyAsync(u => u.UserId == actorId && u.Role.RoleName == "Manager" && u.Status == Active))
            {
                throw new InvalidOperationException("Tài khoản của bạn không còn quyền Manager hoạt động.");
            }
        }

        private static void RequireManager()
        {
            if (CurrentUser.Value?.Role.RoleName != "Manager")
            {
                throw new UnauthorizedAccessException("Chỉ Manager được quản lý tài khoản.");
            }
        }
    }
}
