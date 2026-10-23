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
    public class ClassService
    {

        public async Task<List<ClassListItemDto>> GetClassAsync(int? sportId, string? status)
        {
            RequireManager();
            using var db = new SportCenterDbContext();

            var query = db.Classes.AsNoTracking();
            if (sportId is not null) query = query.Where(c => c.Course.SportId == sportId);
            if (status is not null) query = query.Where(c => c.Status == status);

            return await query
                .OrderBy(c => c.StartDate)
                .Select(c => new ClassListItemDto(
                    c.ClassId, c.ClassName, c.Course.Sport.SportName, c.Course.CourseName,
                    c.Coach != null ? c.Coach.FullName : null,
                    c.Room.RoomName,
                    c.StartDate, c.EndDate, c.MaxCapacity,
                    c.ClassMembers.Count(m => m.Status == "Active"),
                    c.Status))
                .ToListAsync();
        }

        public async Task<List<SportOptionDto>> GetSportsAsync()
        {
            RequireManager();
            using var db = new SportCenterDbContext();
            return await db.Sports.AsNoTracking()
                .Where(s => s.Status == "Active")
                .Select(s => new SportOptionDto(s.SportId, s.SportName))
                .ToListAsync();
        }

        public async Task<List<String>> GetClassStatusesAsync()
        {
            RequireManager();
            using var db = new SportCenterDbContext();
            return await db.Classes
                .AsNoTracking()
                .Select(c => c.Status)
                .Distinct()
                .OrderBy(s => s)
                .ToListAsync();
        }

        private static void RequireManager()
        {
            if (CurrentUser.Value?.Role.RoleName != "Manager")
            {
                throw new UnauthorizedAccessException("Chỉ Manager được xem danh sách lớp.");
            }
        }
    }


}
