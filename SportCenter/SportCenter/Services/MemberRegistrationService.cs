using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using SportCenter.Entities;

namespace SportCenter.Services
{
    // Người lạ tự đăng ký tài khoản học viên (chưa đăng nhập nên không kiểm tra CurrentUser).
    // Lỗi nghiệp vụ ném InvalidOperationException với thông báo tiếng Việt.
    public class MemberRegistrationService
    {
        public const int MinAge = 5;
        private static readonly string[] Genders = { "Male", "Female", "Other" };

        public async Task RegisterAsync(
            string fullName, string email, string phone, DateOnly birthDate, string gender, string password)
        {
            fullName = fullName.Trim();
            email = email.Trim();
            phone = phone.Trim();

            if (fullName.Length == 0) throw new InvalidOperationException("Vui lòng nhập họ và tên.");
            if (!ProfileService.IsValidEmail(email)) throw new InvalidOperationException("Email không đúng định dạng.");
            if (!ProfileService.IsValidPhone(phone)) throw new InvalidOperationException("Số điện thoại gồm 10 số và bắt đầu bằng 0.");
            if (!Genders.Contains(gender)) throw new InvalidOperationException("Giới tính không hợp lệ.");
            ValidateBirthDate(birthDate);
            AuthService.ValidatePassword(password);

            using var db = new SportCenterDbContext();
            if (await db.Users.AnyAsync(u => u.Email == email))
                throw new InvalidOperationException("Email này đã được dùng bởi tài khoản khác.");

            db.Users.Add(new User
            {
                RoleId = await db.Roles.Where(r => r.RoleName == "Member").Select(r => r.RoleId).FirstAsync(),
                FullName = fullName, Email = email, Phone = phone,
                BirthDate = birthDate, Gender = gender,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
                Status = "Active",
                CreatedAt = DateTime.Now               // CreatedBy để trống: học viên tự đăng ký
            });

            try
            {
                await db.SaveChangesAsync();
            }
            catch (DbUpdateException ex) when (ex.InnerException is SqlException { Number: 2601 or 2627 })
            {
                // Có người vừa đăng ký cùng email giữa lúc kiểm tra và lúc lưu.
                throw new InvalidOperationException("Email này đã được dùng bởi tài khoản khác.", ex);
            }
        }

        private static void ValidateBirthDate(DateOnly birthDate)
        {
            DateOnly today = DateOnly.FromDateTime(DateTime.Today);
            if (birthDate > today) throw new InvalidOperationException("Ngày sinh không được ở tương lai.");
            if (birthDate > today.AddYears(-MinAge)) throw new InvalidOperationException($"Học viên phải từ {MinAge} tuổi trở lên.");
        }
    }
}
