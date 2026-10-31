using Microsoft.EntityFrameworkCore;
using SportCenter.Dtos;
using SportCenter.Entities;

namespace SportCenter.Services
{
    // Cho học viên xem trung tâm có những môn và khóa học nào đang mở.
    public class MemberCatalogService
    {
        public async Task<List<MemberSportDto>> GetCatalogAsync()
        {
            RequireMember();
            using var db = new SportCenterDbContext();

            return await db.Sports
                .AsNoTracking()
                .Where(s => s.Status == "Active")
                .OrderBy(s => s.SportName)
                .Select(s => new MemberSportDto(
                    s.SportId, s.SportName, s.Description,
                    s.Courses
                        .Where(c => c.Status == "Active")
                        .OrderBy(c => c.CourseName)
                        .Select(c => new MemberCourseDto(
                            c.CourseId, c.CourseName, c.Level, c.TotalSessions, c.TuitionFee))
                        .ToList()))
                .ToListAsync();
        }

        private static void RequireMember()
        {
            if (CurrentUser.Value?.Role.RoleName != "Member")
            {
                throw new UnauthorizedAccessException("Chỉ học viên được xem danh sách này.");
            }
        }
    }
}