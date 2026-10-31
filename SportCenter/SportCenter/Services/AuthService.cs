using Microsoft.EntityFrameworkCore;
using SportCenter.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SportCenter.Services
{
    public class AuthService
    {
        public async Task<User?> LoginAsync(string email, string password, bool isMemberPortal)
        {
            using var db = new SportCenterDbContext();
            var user = await db.Users
                    .AsNoTracking()
                    .Include(x => x.Role)
                    .FirstOrDefaultAsync(u => u.Email == email.Trim());

            if (user is null || user.Status != "Active") return null;
            if (!BCrypt.Net.BCrypt.Verify(password, user.PasswordHash)) return null;

            bool isMember = user.Role.RoleName == "Member";
            return isMember == isMemberPortal ? user : null;
        }

        // Quên mật khẩu: xác minh bằng email + SĐT đã đăng ký rồi đặt mật khẩu mới. Mọi vai trò dùng chung.
        // Sai email hay sai SĐT đều báo cùng một câu để không lộ email nào đã đăng ký.
        // shortcut: không giới hạn số lần thử và SĐT dễ đoán, nâng lên mã OTP gửi qua email/SMS khi có dịch vụ gửi.
        public async Task ResetPasswordAsync(string email, string phone, string newPassword)
        {
            email = email.Trim();
            phone = phone.Trim();
            if (email.Length == 0 || phone.Length == 0)
                throw new InvalidOperationException("Vui lòng nhập email và số điện thoại.");
            ValidatePassword(newPassword);

            using var db = new SportCenterDbContext();
            User user = await db.Users.FirstOrDefaultAsync(u => u.Email == email && u.Phone == phone && u.Status == "Active")
                ?? throw new InvalidOperationException("Email hoặc số điện thoại không khớp với tài khoản nào.");

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
            await db.SaveChangesAsync();
        }

        // Dùng chung cho đăng ký và đặt lại mật khẩu.
        public static void ValidatePassword(string password)
        {
            if (password.Length < ProfileService.MinPasswordLength)
                throw new InvalidOperationException($"Mật khẩu tối thiểu {ProfileService.MinPasswordLength} ký tự.");
            if (Encoding.UTF8.GetByteCount(password) > ProfileService.MaxPasswordBytes)
                throw new InvalidOperationException("Mật khẩu quá dài.");
        }
    }
}
