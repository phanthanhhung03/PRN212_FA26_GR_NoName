using Microsoft.EntityFrameworkCore;
using SportCenter.Dtos;
using SportCenter.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SportCenter.Services
{
    // UC11: quản lý môn thể thao và khóa học (thêm, sửa, ngừng / kích hoạt lại). Chỉ Manager.
    // Lỗi nghiệp vụ ném InvalidOperationException với thông báo tiếng Việt.
    public class CatalogService
    {
        public static readonly string[] Levels = { "Beginner", "Intermediate", "Advanced" };

        public const int MaxSportNameLength = 100;
        public const int MaxCourseNameLength = 150;
        public const int MaxDescriptionLength = 1000;
        public const int MaxSessions = 100;                    // hàm sinh lịch (fn_planned_dates) chỉ phủ khoảng 140 buổi cho một khung/tuần
        public const decimal MaxTuitionFee = 100_000_000m;

        private const string Active = "Active";
        private const string Inactive = "Inactive";

        // ---------- Đọc ----------

        // Tất cả môn (kể cả đã ngừng), kèm số khóa và số lớp đang chạy.
        public async Task<List<SportManageDto>> GetSportsAsync()
        {
            RequireManager();
            using var db = new SportCenterDbContext();
            return await db.Sports.AsNoTracking()
                .OrderBy(s => s.SportName)
                .Select(s => new SportManageDto(
                    s.SportId, s.SportName, s.Description, s.Status,
                    s.Courses.Count,
                    s.Courses.SelectMany(c => c.Classes).Count(k => k.Status == "Enrolling" || k.Status == "Ongoing")))
                .ToListAsync();
        }

        // Các khóa của một môn (kể cả đã ngừng), theo level rồi theo tên.
        public async Task<List<CourseManageDto>> GetCoursesAsync(int sportId)
        {
            RequireManager();
            using var db = new SportCenterDbContext();
            return await db.Courses.AsNoTracking()
                .Where(c => c.SportId == sportId)
                .OrderBy(c => c.Level == "Beginner" ? 0 : c.Level == "Intermediate" ? 1 : c.Level == "Advanced" ? 2 : 3)
                .ThenBy(c => c.CourseName)
                .Select(c => new CourseManageDto(
                    c.CourseId, c.SportId, c.CourseName, c.Level, c.TotalSessions, c.TuitionFee,
                    c.Description, c.Status,
                    c.Classes.Count,
                    c.Classes.Count(k => k.Status == "Enrolling" || k.Status == "Ongoing")))
                .ToListAsync();
        }

        // ---------- Môn ----------

        public async Task<int> CreateSportAsync(string name, string? description)
        {
            RequireManager();
            (name, description) = CleanSport(name, description);

            using var db = new SportCenterDbContext();
            await EnsureSportNameFreeAsync(db, name, exceptSportId: null);

            var sport = new Sport { SportName = name, Description = description, Status = Active };
            db.Sports.Add(sport);
            await db.SaveChangesAsync();
            return sport.SportId;
        }

        public async Task UpdateSportAsync(int sportId, string name, string? description)
        {
            RequireManager();
            (name, description) = CleanSport(name, description);

            using var db = new SportCenterDbContext();
            Sport sport = await db.Sports.FirstOrDefaultAsync(s => s.SportId == sportId)
                ?? throw new InvalidOperationException("Không tìm thấy môn này.");
            await EnsureSportNameFreeAsync(db, name, exceptSportId: sportId);

            sport.SportName = name;
            sport.Description = description;
            await db.SaveChangesAsync();
        }

        // Ngừng hoặc kích hoạt lại một môn. Ngừng không đụng tới các lớp đang chạy: chỉ không mở lớp mới được.
        public async Task SetSportActiveAsync(int sportId, bool active)
        {
            RequireManager();
            using var db = new SportCenterDbContext();
            Sport sport = await db.Sports.FirstOrDefaultAsync(s => s.SportId == sportId)
                ?? throw new InvalidOperationException("Không tìm thấy môn này.");
            sport.Status = active ? Active : Inactive;
            await db.SaveChangesAsync();
        }

        // ---------- Khóa ----------

        public async Task<int> CreateCourseAsync(
            int sportId, string name, string? level, int totalSessions, decimal tuitionFee, string? description)
        {
            RequireManager();
            (name, description) = CleanCourse(name, level, totalSessions, tuitionFee, description);

            using var db = new SportCenterDbContext();
            Sport sport = await db.Sports.AsNoTracking().FirstOrDefaultAsync(s => s.SportId == sportId)
                ?? throw new InvalidOperationException("Không tìm thấy môn này.");
            if (sport.Status != Active) throw new InvalidOperationException("Môn này đang ngừng, hãy kích hoạt lại môn trước khi thêm khóa.");
            await EnsureCourseNameFreeAsync(db, sportId, name, exceptCourseId: null);

            var course = new Course
            {
                SportId = sportId, CourseName = name, Level = level, TotalSessions = totalSessions,
                TuitionFee = tuitionFee, Description = description, Status = Active
            };
            db.Courses.Add(course);
            await db.SaveChangesAsync();
            return course.CourseId;
        }

        // Sửa một khóa. Môn của khóa không đổi. Số buổi chỉ đổi được khi chưa có lớp nào mở từ khóa này
        // (lịch của lớp và việc tính "thiếu buổi / buổi bù" đều dựa vào số buổi của khóa).
        public async Task UpdateCourseAsync(
            int courseId, string name, string? level, int totalSessions, decimal tuitionFee, string? description)
        {
            RequireManager();
            (name, description) = CleanCourse(name, level, totalSessions, tuitionFee, description);

            using var db = new SportCenterDbContext();
            Course course = await db.Courses.FirstOrDefaultAsync(c => c.CourseId == courseId)
                ?? throw new InvalidOperationException("Không tìm thấy khóa này.");

            if (course.TotalSessions != totalSessions && await db.Classes.AnyAsync(k => k.CourseId == courseId))
            {
                throw new InvalidOperationException("Khóa đã có lớp nên không đổi được số buổi.");
            }
            await EnsureCourseNameFreeAsync(db, course.SportId, name, exceptCourseId: courseId);

            course.CourseName = name;
            course.Level = level;
            course.TotalSessions = totalSessions;
            course.TuitionFee = tuitionFee;
            course.Description = description;
            await db.SaveChangesAsync();
        }

        // Ngừng hoặc kích hoạt lại một khóa. Kích hoạt lại chỉ được khi môn của khóa đang hoạt động.
        public async Task SetCourseActiveAsync(int courseId, bool active)
        {
            RequireManager();
            using var db = new SportCenterDbContext();
            Course course = await db.Courses.Include(c => c.Sport).FirstOrDefaultAsync(c => c.CourseId == courseId)
                ?? throw new InvalidOperationException("Không tìm thấy khóa này.");

            if (active && course.Sport.Status != Active)
            {
                throw new InvalidOperationException("Môn của khóa này đang ngừng, hãy kích hoạt lại môn trước.");
            }
            course.Status = active ? Active : Inactive;
            await db.SaveChangesAsync();
        }

        // ---------- Kiểm tra dữ liệu ----------

        private static (string Name, string? Description) CleanSport(string name, string? description)
        {
            name = (name ?? "").Trim();
            if (name.Length == 0) throw new InvalidOperationException("Vui lòng nhập tên môn.");
            if (name.Length > MaxSportNameLength) throw new InvalidOperationException($"Tên môn tối đa {MaxSportNameLength} ký tự.");
            return (name, CleanDescription(description));
        }

        private static (string Name, string? Description) CleanCourse(
            string name, string? level, int totalSessions, decimal tuitionFee, string? description)
        {
            name = (name ?? "").Trim();
            if (name.Length == 0) throw new InvalidOperationException("Vui lòng nhập tên khóa.");
            if (name.Length > MaxCourseNameLength) throw new InvalidOperationException($"Tên khóa tối đa {MaxCourseNameLength} ký tự.");
            if (level is null || !Levels.Contains(level)) throw new InvalidOperationException("Vui lòng chọn level.");
            if (totalSessions < 1 || totalSessions > MaxSessions)
                throw new InvalidOperationException($"Số buổi phải từ 1 đến {MaxSessions}.");
            if (tuitionFee < 0 || tuitionFee > MaxTuitionFee)
                throw new InvalidOperationException($"Học phí phải từ 0 đến {MaxTuitionFee:N0}.");
            return (name, CleanDescription(description));
        }

        private static string? CleanDescription(string? description)
        {
            description = description?.Trim();
            if (string.IsNullOrEmpty(description)) return null;
            if (description.Length > MaxDescriptionLength) throw new InvalidOperationException($"Mô tả tối đa {MaxDescriptionLength} ký tự.");
            return description;
        }

        // Tên môn không trùng (không phân biệt hoa thường: theo collation của DB).
        private static async Task EnsureSportNameFreeAsync(SportCenterDbContext db, string name, int? exceptSportId)
        {
            bool taken = await db.Sports.AnyAsync(s => s.SportName == name && (exceptSportId == null || s.SportId != exceptSportId));
            if (taken) throw new InvalidOperationException("Đã có môn trùng tên này.");
        }

        // Tên khóa không trùng trong cùng một môn.
        private static async Task EnsureCourseNameFreeAsync(SportCenterDbContext db, int sportId, string name, int? exceptCourseId)
        {
            bool taken = await db.Courses.AnyAsync(c => c.SportId == sportId && c.CourseName == name
                                                        && (exceptCourseId == null || c.CourseId != exceptCourseId));
            if (taken) throw new InvalidOperationException("Môn này đã có khóa trùng tên.");
        }

        private static void RequireManager()
        {
            if (CurrentUser.Value?.Role.RoleName != "Manager")
            {
                throw new UnauthorizedAccessException("Chỉ Manager được quản lý môn và khóa học.");
            }
        }
    }
}
