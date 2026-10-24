using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using SportCenter.Entities;
using System;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SportCenter.Services
{
    // UC02: cập nhật hồ sơ cá nhân và đổi mật khẩu của người đang đăng nhập.
    // Lỗi nghiệp vụ ném InvalidOperationException với thông báo tiếng Việt; người gọi không phải chính chủ thì ném UnauthorizedAccessException.
    public class ProfileService
    {
        private static readonly Regex EmailRegex = new(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled);
        private static readonly Regex PhoneRegex = new(@"^0\d{9}$", RegexOptions.Compiled);

        public const int MinPasswordLength = 6;
        public const int MaxPasswordBytes = 72;        // BCrypt chỉ dùng 72 byte đầu của mật khẩu

        // Dùng chung cho giao diện và Service để hai nơi kiểm tra giống nhau.
        public static bool IsValidEmail(string email) => EmailRegex.IsMatch(email);
        public static bool IsValidPhone(string phone) => PhoneRegex.IsMatch(phone);

        public async Task<User> GetAsync(int userId)
        {
            RequireSelf(userId);
            using var db = new SportCenterDbContext();
            return await db.Users
                .AsNoTracking()
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.UserId == userId)
                ?? throw new InvalidOperationException("Không tìm thấy tài khoản.");
        }

        // Số điện thoại có thể để trống (lưu NULL); nếu nhập thì phải đúng 10 số, bắt đầu bằng 0.
        public async Task UpdateAsync(int userId, string fullName, string email, string? phone)
        {
            RequireSelf(userId);

            fullName = fullName.Trim();
            email = email.Trim();
            phone = string.IsNullOrWhiteSpace(phone) ? null : phone.Trim();

            if (fullName.Length == 0) throw new InvalidOperationException("Vui lòng nhập họ và tên.");
            if (email.Length == 0) throw new InvalidOperationException("Vui lòng nhập email.");
            if (!IsValidEmail(email)) throw new InvalidOperationException("Email không đúng định dạng.");
            if (phone is not null && !IsValidPhone(phone))
                throw new InvalidOperationException("Số điện thoại gồm 10 số và bắt đầu bằng 0.");

            using var db = new SportCenterDbContext();

            bool taken = await db.Users.AnyAsync(u => u.UserId != userId && u.Email == email);
            if (taken) throw new InvalidOperationException("Email này đã được dùng bởi tài khoản khác.");

            User user = await db.Users.FirstOrDefaultAsync(u => u.UserId == userId)
                ?? throw new InvalidOperationException("Không tìm thấy tài khoản.");
            user.FullName = fullName;
            user.Email = email;
            user.Phone = phone;

            try
            {
                await db.SaveChangesAsync();
            }
            catch (DbUpdateException ex) when (ex.InnerException is SqlException { Number: 2601 or 2627 })
            {
                // Có người vừa lưu cùng email giữa lúc kiểm tra và lúc lưu.
                throw new InvalidOperationException("Email này đã được dùng bởi tài khoản khác.", ex);
            }

            if (CurrentUser.Value is { } current && current.UserId == userId)
            {
                current.FullName = user.FullName;
                current.Email = user.Email;
                current.Phone = user.Phone;
            }
        }

        public async Task ChangePasswordAsync(int userId, string currentPassword, string newPassword)
        {
            RequireSelf(userId);

            if (newPassword.Length < MinPasswordLength)
                throw new InvalidOperationException($"Mật khẩu tối thiểu {MinPasswordLength} ký tự.");
            if (Encoding.UTF8.GetByteCount(newPassword) > MaxPasswordBytes)
                throw new InvalidOperationException("Mật khẩu quá dài.");
            if (newPassword == currentPassword)
                throw new InvalidOperationException("Mật khẩu mới phải khác mật khẩu hiện tại.");

            using var db = new SportCenterDbContext();
            User user = await db.Users.FirstOrDefaultAsync(u => u.UserId == userId)
                ?? throw new InvalidOperationException("Không tìm thấy tài khoản.");

            if (!BCrypt.Net.BCrypt.Verify(currentPassword, user.PasswordHash))
                throw new InvalidOperationException("Mật khẩu hiện tại không đúng.");

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
            await db.SaveChangesAsync();

            if (CurrentUser.Value is { } current && current.UserId == userId)
            {
                current.PasswordHash = user.PasswordHash;
            }
        }

        // Chỉ được xem và sửa hồ sơ của chính mình.
        private static void RequireSelf(int userId)
        {
            if (CurrentUser.Value?.UserId != userId)
            {
                throw new UnauthorizedAccessException("Bạn chỉ được xem và sửa hồ sơ của chính mình.");
            }
        }
    }
}
