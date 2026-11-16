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
        public async Task<List<MemberClassDto>> GetOpenClassesAsync(int? sportId, int? courseId)
        {
            RequireMember();
<<<<<<< Updated upstream
=======
            int currentMemberId = CurrentUser.Value?.UserId ?? 0;
            var today = DateOnly.FromDateTime(DateTime.Today);
            await PaymentService.ExpireStalePendingAsync();   // trả chỗ của các Pending quá hạn trước khi đếm
>>>>>>> Stashed changes
            using var db = new SportCenterDbContext();

            var query = db.Classes.AsNoTracking().Where(c => c.Status == "Enrolling");
            if (sportId is not null) query = query.Where(c => c.Course.SportId == sportId);
            if (courseId is not null) query = query.Where(c => c.CourseId == courseId);

            return await query
                .OrderBy(c => c.StartDate)
                .Select(c => new MemberClassDto(
                    c.ClassId, c.ClassName, c.Course.Sport.SportName, c.Course.CourseName, c.Course.Level,
                    c.Coach != null ? c.Coach.FullName : null,
                    c.Room.RoomName, c.StartDate,
                    c.MaxCapacity,
                    c.ClassMembers.Count(m => m.Status == "Active"),
                    c.Course.TuitionFee,
                    c.TimeSlots.OrderBy(t => t.DayOfWeek).ThenBy(t => t.StartTime)
<<<<<<< Updated upstream
                        .Select(t => t.SlotName).ToList()))
=======
                        .Select(t => t.SlotName).ToList(),
                    c.ClassMembers.Any(m => m.MemberId == currentMemberId && m.Status != "Dropped"),
                    c.ClassMembers.Any(m => m.MemberId == currentMemberId && m.Status == "Active"),   // IsPaid
                    c.StartDate <= today))                                                            // HasStarted
>>>>>>> Stashed changes
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