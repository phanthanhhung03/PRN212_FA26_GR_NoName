using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using SportCenter.Dtos;
using SportCenter.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;

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
                    c.ClassId, c.ClassName, c.Course.Sport.SportName, c.Course.Level,
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

        // Thông tin một lớp cho trang chi tiết; null nếu không có lớp này.
        public async Task<ClassDetailDto?> GetClassDetailAsync(int classId)
        {
            RequireManager();
            using var db = new SportCenterDbContext();
            return await db.Classes.AsNoTracking()
                .Where(c => c.ClassId == classId)
                .Select(c => new ClassDetailDto(
                    c.ClassId, c.ClassName, c.Course.Sport.SportName, c.Course.Level,
                    c.Coach != null ? c.Coach.FullName : null,
                    c.Room.RoomName,
                    c.StartDate, c.EndDate,
                    c.MaxCapacity,
                    c.ClassMembers.Count(m => m.Status == "Active"),
                    c.Status,
                    c.Course.TotalSessions))
                .FirstOrDefaultAsync();
        }

        // Các buổi học của một lớp theo thứ tự buổi, kèm phòng, coach và dấu hiệu đổi tạm / buổi bù.
        public async Task<List<SessionItemDto>> GetClassSessionsAsync(int classId)
        {
            RequireManager();
            using var db = new SportCenterDbContext();
            return await db.Sessions.AsNoTracking()
                .Where(s => s.ClassId == classId)
                .OrderBy(s => s.SessionNo)
                .Select(s => new SessionItemDto(
                    s.SessionId, s.SessionNo, s.SessionDate, s.TimeSlot.DayOfWeek,
                    s.TimeSlot.StartTime, s.TimeSlot.EndTime,
                    s.Room.RoomName, s.Coach.FullName,
                    s.Status,
                    s.RoomId != s.Class.RoomId,
                    s.Class.CoachId != null && s.CoachId != s.Class.CoachId,
                    s.SessionNo > s.Class.Course.TotalSessions))
                .ToListAsync();
        }

        public async Task<List<CourseOptionDto>> GetCoursesAsync(int sportId)
        {
            RequireManager();
            using var db = new SportCenterDbContext();
            return await db.Courses.AsNoTracking()
                .Where(c => c.SportId == sportId && c.Status == "Active")
                .OrderBy(c => c.CourseName)
                .Select(c => new CourseOptionDto 
                (
                    c.CourseId, c.CourseName, c.TotalSessions, c.Level
                ))
                .ToListAsync();
        }

        public async Task<List<TimeSlotDto>> GetTimeSlotsAsync()
        {
            RequireManager();
            using var db = new SportCenterDbContext();
            return await db.TimeSlots.AsNoTracking()
                .OrderBy(t => t.DayOfWeek).ThenBy(t => t.StartTime)
                .Select(t => new TimeSlotDto(t.TimeSlotId, t.DayOfWeek, t.StartTime, t.EndTime))
                .ToListAsync();
        }

        public async Task<List<CoachOptionDto>> GetFreeCoachesAsync(int sportId, DateOnly startDate, IEnumerable<int> slotIds, int totalSessions)
        {
            RequireManager();
            using var db = new SportCenterDbContext();
            var conn = (SqlConnection)db.Database.GetDbConnection();
            await conn.OpenAsync();

            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT user_id, full_name FROM dbo.fn_free_coaches(@sport, @start, @slots, @total)";
            cmd.Parameters.AddWithValue("@sport", sportId);
            cmd.Parameters.Add(DateParam("@start", startDate));
            cmd.Parameters.Add(SlotsParam(slotIds));
            cmd.Parameters.AddWithValue("@total", totalSessions);

            var result = new List<CoachOptionDto>();
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
                result.Add(new CoachOptionDto(reader.GetInt32(0), reader.GetString(1)));

            return result;
        }

        private static SqlParameter SlotsParam(IEnumerable<int> slotsId, string name = "@slots")
        {
            var table = new DataTable();
            table.Columns.Add("id", typeof(int));
            foreach (int id  in slotsId.Distinct()) table.Rows.Add(id);
            return new SqlParameter(name, SqlDbType.Structured) { TypeName = "dbo.IdList", Value = table };
        }

        private static SqlParameter DateParam(string name, DateOnly d) => new SqlParameter(name, SqlDbType.Date) { Value = d.ToDateTime(TimeOnly.MinValue) };

        public async Task<List<RoomOptionDto>> GetFreeRoomsAsync(int sportId, int minCapacity, DateOnly startDate, IEnumerable<int> slotIds, int totalSessions)
        {
            RequireManager();
            using var db = new SportCenterDbContext();
            var conn = (SqlConnection)db.Database.GetDbConnection();
            await conn.OpenAsync();

            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT room_id, room_name, capacity FROM dbo.fn_free_rooms(@sport, @minCap, @start, @slots, @total)";
            cmd.Parameters.AddWithValue("@sport", sportId);
            cmd.Parameters.AddWithValue("@minCap", minCapacity);
            cmd.Parameters.Add(DateParam("@start", startDate));
            cmd.Parameters.Add(SlotsParam(slotIds));
            cmd.Parameters.AddWithValue("@total", totalSessions);

            var result = new List<RoomOptionDto>();
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
                result.Add(new RoomOptionDto(reader.GetInt32(0), reader.GetString(1), reader.GetInt32(2)));
            return result;
        } 

        public async Task<List<PlannedDateDto>> PreviewDatesAsync(DateOnly startDate, IEnumerable<int> slotIds, int totalSessions)
        {
            RequireManager();
            using var db = new SportCenterDbContext();
            var conn = (SqlConnection)db.Database.GetDbConnection();
            await conn.OpenAsync();

            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT session_no, session_date, time_slot_id FROM dbo.fn_planned_dates(@start, @slots, @total) ORDER BY session_no";
            cmd.Parameters.Add(DateParam("@start", startDate));
            cmd.Parameters.Add(SlotsParam(slotIds));
            cmd.Parameters.AddWithValue("@total", totalSessions);

            var result = new List<PlannedDateDto>();
            using var reader = await cmd.ExecuteReaderAsync();
            while ( await reader.ReadAsync()) 
                result.Add(new PlannedDateDto((int)reader.GetInt64(0), DateOnly.FromDateTime(reader.GetDateTime(1)), reader.GetInt32(2)));
            return result;
        }

        // Phòng đang hoạt động của một môn (sắp theo tên), kèm sức chứa để chọn cho hợp sĩ số.
        public async Task<List<RoomOptionDto>> GetRoomsBySportAsync(int sportId)
        {
            RequireManager();
            using var db = new SportCenterDbContext();
            return await db.Rooms.AsNoTracking()
                .Where(r => r.SportId == sportId && r.Status == "Active")
                .OrderBy(r => r.RoomName)
                .Select(r => new RoomOptionDto(r.RoomId, r.RoomName, r.Capacity))
                .ToListAsync();
        }

        // Coach đang hoạt động của một môn (mỗi coach thuộc đúng một môn).
        public async Task<List<CoachOptionDto>> GetCoachesBySportAsync(int sportId)
        {
            RequireManager();
            using var db = new SportCenterDbContext();
            return await db.Users.AsNoTracking()
                .Where(u => u.SportId == sportId && u.Status == "Active" && u.Role.RoleName == "Coach")
                .OrderBy(u => u.FullName)
                .Select(u => new CoachOptionDto(u.UserId, u.FullName))
                .ToListAsync();
        }

        // Các khung giờ không chọn được với phòng và coach này, tính theo lịch thật (ngày khai giảng, số buổi)
        // và theo các khung đang chọn. Gọi sp_busy_slots.
        public async Task<List<BusySlotDto>> GetBusySlotsAsync(int roomId, int coachId, DateOnly startDate, IEnumerable<int> selectedSlotIds, int totalSessions)
        {
            RequireManager();
            using var db = new SportCenterDbContext();
            var conn = (SqlConnection)db.Database.GetDbConnection();
            await conn.OpenAsync();

            using var cmd = conn.CreateCommand();
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.CommandText = "dbo.sp_busy_slots";
            cmd.Parameters.AddWithValue("@room_id", roomId);
            cmd.Parameters.AddWithValue("@coach_id", coachId);
            cmd.Parameters.Add(DateParam("@start_date", startDate));
            cmd.Parameters.Add(SlotsParam(selectedSlotIds, "@selected"));
            cmd.Parameters.AddWithValue("@total", totalSessions);

            var result = new List<BusySlotDto>();
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
                result.Add(new BusySlotDto(reader.GetInt32(0), reader.GetString(1)));
            return result;
        }

        // Gọi sp_create_class: kiểm tra lại toàn bộ trong DB, rồi tạo lớp + khung giờ + các buổi học trong một transaction.
        // Trả về class_id của lớp mới; lỗi nghiệp vụ được đổi thành InvalidOperationException với thông báo tiếng Việt.
        public async Task<int> CreateClassAsync(CreateClassRequest request)
        {
            RequireManager();
            using var db = new SportCenterDbContext();
            var conn = (SqlConnection)db.Database.GetDbConnection();
            await conn.OpenAsync();

            using var cmd = conn.CreateCommand();
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.CommandText = "dbo.sp_create_class";
            cmd.Parameters.AddWithValue("@actor_id", CurrentUser.Value!.UserId);
            cmd.Parameters.AddWithValue("@course_id", request.CourseId);
            cmd.Parameters.AddWithValue("@class_name", request.ClassName);
            cmd.Parameters.Add(DateParam("@start_date", request.StartDate));
            cmd.Parameters.AddWithValue("@max_capacity", request.MaxCapacity);
            cmd.Parameters.AddWithValue("@coach_id", request.CoachId);
            cmd.Parameters.AddWithValue("@room_id", request.RoomId);
            cmd.Parameters.Add(SlotsParam(request.SlotIds));
            var classId = new SqlParameter("@class_id", SqlDbType.Int) { Direction = ParameterDirection.Output };
            cmd.Parameters.Add(classId);

            try
            {
                await cmd.ExecuteNonQueryAsync();
            }
            catch (SqlException ex)
            {
                throw new InvalidOperationException(ex.Number switch
                {
                    50001 => "Chỉ Manager đang hoạt động mới được mở lớp.",
                    50002 => "Khóa học không tồn tại hoặc đã ngừng.",
                    50003 => "Coach không hợp lệ (sai môn hoặc không hoạt động).",
                    50004 => "Phòng không hợp lệ (sai môn hoặc không hoạt động).",
                    50005 => "Sĩ số lớn hơn sức chứa của phòng.",
                    50006 => "Coach đã bận vào một buổi trong lịch này.",
                    50007 => "Phòng đã bận vào một buổi trong lịch này.",
                    50008 => "Chưa chọn khung giờ hợp lệ.",
                    2601 or 2627 => "Có người vừa đặt trùng lịch này. Vui lòng chọn lại.",
                    _ => "Không tạo được lớp. Vui lòng thử lại."
                }, ex);
            }

            return (int)classId.Value;
        }

        private static void RequireManager()
        {
            if (CurrentUser.Value?.Role.RoleName != "Manager")
            {
                throw new UnauthorizedAccessException("Chỉ Manager được thực hiện thao tác này.");
            }
        }
    }


}
