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
    }
}
