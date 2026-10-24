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

        // Tất cả môn kể cả đã ngừng (bộ lọc danh sách lớp: lớp của môn đã ngừng vẫn còn chạy).
        public async Task<List<SportOptionDto>> GetAllSportsAsync()
        {
            RequireManager();
            using var db = new SportCenterDbContext();
            return await db.Sports.AsNoTracking()
                .OrderBy(s => s.SportName)
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

        // Các coach và phòng có thể chọn khi đổi một buổi (cùng môn với lớp), kèm cờ bận / không đủ chỗ.
        // Chỉ dùng được cho buổi chưa bắt đầu.
        public async Task<ReassignOptionsDto> GetReassignOptionsAsync(int sessionId)
        {
            RequireManager();
            using var db = new SportCenterDbContext();

            var s = await db.Sessions.AsNoTracking()
                .Where(x => x.SessionId == sessionId)
                .Select(x => new
                {
                    x.SessionId, x.SessionNo, x.SessionDate, x.TimeSlotId, x.Status, x.CoachId, x.RoomId,
                    Day = x.TimeSlot.DayOfWeek, x.TimeSlot.StartTime, x.TimeSlot.EndTime,
                    SportId = x.Class.Course.SportId, x.Class.MaxCapacity
                })
                .FirstOrDefaultAsync()
                ?? throw new InvalidOperationException("Không tìm thấy buổi học này.");

            if (!SessionRules.CanModify(s.Status, s.SessionDate, s.StartTime))
            {
                throw new InvalidOperationException("Buổi học đã bắt đầu hoặc không còn sửa được.");
            }

            var (coaches, rooms) = await BuildChoicesAsync(
                db, s.SportId, s.MaxCapacity, s.SessionDate, s.TimeSlotId, s.SessionId, s.CoachId, s.RoomId);

            return new ReassignOptionsDto(
                s.SessionId, s.SessionNo, s.SessionDate, s.Day, s.StartTime, s.EndTime,
                s.CoachId, s.RoomId, coaches, rooms);
        }

        // Đổi coach và/hoặc phòng của một buổi chưa bắt đầu (truyền null cho mục giữ nguyên). Gọi sp_reassign_session.
        public async Task ReassignSessionAsync(int sessionId, int? coachId, int? roomId)
        {
            RequireManager();
            using var db = new SportCenterDbContext();

            var s = await db.Sessions.AsNoTracking()
                .Where(x => x.SessionId == sessionId)
                .Select(x => new { x.Status, x.SessionDate, x.TimeSlot.StartTime })
                .FirstOrDefaultAsync()
                ?? throw new InvalidOperationException("Không tìm thấy buổi học này.");
            if (!SessionRules.CanModify(s.Status, s.SessionDate, s.StartTime))
            {
                throw new InvalidOperationException("Buổi học đã bắt đầu hoặc không còn sửa được.");
            }

            var conn = (SqlConnection)db.Database.GetDbConnection();
            await conn.OpenAsync();

            using var cmd = conn.CreateCommand();
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.CommandText = "dbo.sp_reassign_session";
            cmd.Parameters.AddWithValue("@actor_id", CurrentUser.Value!.UserId);
            cmd.Parameters.AddWithValue("@session_id", sessionId);
            cmd.Parameters.AddWithValue("@coach_id", (object?)coachId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@room_id", (object?)roomId ?? DBNull.Value);

            try
            {
                await cmd.ExecuteNonQueryAsync();
            }
            catch (SqlException ex)
            {
                throw new InvalidOperationException(ex.Number switch
                {
                    50001 => "Chỉ Manager đang hoạt động mới được đổi buổi học.",
                    50003 => "Coach không hợp lệ (sai môn hoặc không hoạt động).",
                    50004 => "Phòng không hợp lệ (sai môn hoặc không hoạt động).",
                    50005 => "Phòng không đủ chỗ cho sĩ số của lớp.",
                    50006 => "Coach đã bận vào khung giờ này. Vui lòng chọn coach khác.",
                    50007 => "Phòng đã bận vào khung giờ này. Vui lòng chọn phòng khác.",
                    50009 => "Buổi học không còn sửa được.",
                    _ => "Không đổi được buổi học. Vui lòng thử lại."
                }, ex);
            }
        }

        // Coach và phòng cùng môn có thể chọn cho một ngày + khung giờ, kèm cờ bận / không đủ chỗ.
        // excludeSessionId: buổi đang sửa (không tính là bận). currentCoachId / currentRoomId: luôn có trong danh sách, dù đã ngừng hoạt động.
        private static async Task<(List<CoachChoiceDto> Coaches, List<RoomChoiceDto> Rooms)> BuildChoicesAsync(
            SportCenterDbContext db, int sportId, int maxCapacity, DateOnly date, int timeSlotId,
            int? excludeSessionId, int? currentCoachId, int? currentRoomId)
        {
            // Coach và phòng đang có buổi khác (chưa hủy) đúng ngày và khung giờ này.
            var others = await db.Sessions.AsNoTracking()
                .Where(x => x.SessionDate == date && x.TimeSlotId == timeSlotId && x.Status != "Cancelled"
                            && (excludeSessionId == null || x.SessionId != excludeSessionId))
                .Select(x => new { x.CoachId, x.RoomId })
                .ToListAsync();
            HashSet<int> busyCoaches = others.Select(o => o.CoachId).ToHashSet();
            HashSet<int> busyRooms = others.Select(o => o.RoomId).ToHashSet();

            var coaches = await db.Users.AsNoTracking()
                .Where(u => (currentCoachId != null && u.UserId == currentCoachId)
                            || (u.SportId == sportId && u.Status == "Active" && u.Role.RoleName == "Coach"))
                .OrderBy(u => u.FullName)
                .Select(u => new { u.UserId, u.FullName })
                .ToListAsync();
            var rooms = await db.Rooms.AsNoTracking()
                .Where(r => (currentRoomId != null && r.RoomId == currentRoomId) || (r.SportId == sportId && r.Status == "Active"))
                .OrderBy(r => r.RoomName)
                .Select(r => new { r.RoomId, r.RoomName, r.Capacity })
                .ToListAsync();

            return (
                coaches.Select(c => new CoachChoiceDto(c.UserId, c.FullName, busyCoaches.Contains(c.UserId), c.UserId == currentCoachId)).ToList(),
                rooms.Select(r => new RoomChoiceDto(r.RoomId, r.RoomName, r.Capacity, busyRooms.Contains(r.RoomId), r.Capacity < maxCapacity, r.RoomId == currentRoomId)).ToList());
        }

        // Hủy một buổi chưa bắt đầu. Hệ thống thử xếp buổi bù tự động; không xếp được (coach hoặc phòng của lớp bận) thì buổi vẫn bị hủy
        // và kết quả không có buổi bù. Gọi sp_cancel_session.
        public async Task<CancelSessionResultDto> CancelSessionAsync(int sessionId)
        {
            RequireManager();
            using var db = new SportCenterDbContext();

            var s = await db.Sessions.AsNoTracking()
                .Where(x => x.SessionId == sessionId)
                .Select(x => new { x.Status, x.SessionDate, x.TimeSlot.StartTime })
                .FirstOrDefaultAsync()
                ?? throw new InvalidOperationException("Không tìm thấy buổi học này.");
            if (!SessionRules.CanModify(s.Status, s.SessionDate, s.StartTime))
            {
                throw new InvalidOperationException("Buổi học đã bắt đầu hoặc không còn hủy được.");
            }

            var conn = (SqlConnection)db.Database.GetDbConnection();
            await conn.OpenAsync();

            using var cmd = conn.CreateCommand();
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.CommandText = "dbo.sp_cancel_session";
            cmd.Parameters.AddWithValue("@actor_id", CurrentUser.Value!.UserId);
            cmd.Parameters.AddWithValue("@session_id", sessionId);
            var makeupId = new SqlParameter("@makeup_session_id", SqlDbType.Int) { Direction = ParameterDirection.Output };
            cmd.Parameters.Add(makeupId);

            try
            {
                await cmd.ExecuteNonQueryAsync();
            }
            catch (SqlException ex)
            {
                throw new InvalidOperationException(ex.Number switch
                {
                    50001 => "Chỉ Manager đang hoạt động mới được hủy buổi học.",
                    50009 => "Buổi học không còn hủy được.",
                    _ => "Không hủy được buổi học. Vui lòng thử lại."
                }, ex);
            }

            if (makeupId.Value is not int newId)
            {
                return new CancelSessionResultDto(null, null, null);
            }

            // Buổi đã hủy xong; nếu chỉ đọc thông tin buổi bù bị lỗi thì vẫn báo hủy thành công (trang sẽ tải lại và hiện buổi bù).
            try
            {
                var makeup = await db.Sessions.AsNoTracking()
                    .Where(x => x.SessionId == newId)
                    .Select(x => new { x.SessionNo, x.SessionDate, x.TimeSlot.DayOfWeek })
                    .FirstOrDefaultAsync();
                return makeup is null
                    ? new CancelSessionResultDto(null, null, null)
                    : new CancelSessionResultDto(makeup.SessionNo, makeup.SessionDate, makeup.DayOfWeek);
            }
            catch (Exception)
            {
                return new CancelSessionResultDto(null, null, null);
            }
        }

        // Thông tin cho hộp thoại "Xếp buổi bù". Báo lỗi nếu lớp không tồn tại, đã hủy hoặc đã đủ số buổi.
        public async Task<MakeupContextDto> GetMakeupContextAsync(int classId)
        {
            RequireManager();
            using var db = new SportCenterDbContext();

            var c = await db.Classes.AsNoTracking()
                .Where(x => x.ClassId == classId)
                .Select(x => new
                {
                    x.ClassId, x.ClassName, x.Status, x.StartDate, x.CoachId, x.RoomId,
                    Total = x.Course.TotalSessions,
                    Valid = x.Sessions.Count(s => s.Status != "Cancelled"),
                    MaxNo = x.Sessions.Max(s => (int?)s.SessionNo) ?? 0
                })
                .FirstOrDefaultAsync()
                ?? throw new InvalidOperationException("Không tìm thấy lớp này.");

            if (c.Status is "Cancelled" or "Completed") throw new InvalidOperationException("Lớp đã kết thúc hoặc bị hủy, không xếp thêm buổi.");
            int missing = c.Total - c.Valid;
            if (missing <= 0) throw new InvalidOperationException("Lớp đã đủ số buổi.");

            return new MakeupContextDto(c.ClassId, c.ClassName, c.StartDate, missing, c.MaxNo + 1, c.CoachId, c.RoomId);
        }

        // Coach và phòng có thể chọn để xếp buổi bù vào một ngày + khung giờ (khung phải đúng thứ của ngày và chưa qua giờ bắt đầu).
        public async Task<MakeupChoicesDto> GetMakeupChoicesAsync(int classId, DateOnly date, int timeSlotId)
        {
            RequireManager();
            using var db = new SportCenterDbContext();

            var c = await db.Classes.AsNoTracking()
                .Where(x => x.ClassId == classId)
                .Select(x => new { SportId = x.Course.SportId, x.MaxCapacity, x.StartDate })
                .FirstOrDefaultAsync()
                ?? throw new InvalidOperationException("Không tìm thấy lớp này.");
            await CheckMakeupSlotAsync(db, date, timeSlotId, c.StartDate);

            var (coaches, rooms) = await BuildChoicesAsync(db, c.SportId, c.MaxCapacity, date, timeSlotId, null, null, null);
            return new MakeupChoicesDto(coaches, rooms);
        }

        // Ngày không được trước ngày khai giảng của lớp; khung giờ phải tồn tại, đúng thứ của ngày và chưa qua giờ bắt đầu.
        private static async Task CheckMakeupSlotAsync(SportCenterDbContext db, DateOnly date, int timeSlotId, DateOnly classStartDate)
        {
            if (date < classStartDate)
            {
                throw new InvalidOperationException("Ngày xếp buổi bù không được trước ngày khai giảng của lớp.");
            }

            var slot = await db.TimeSlots.AsNoTracking()
                .Where(t => t.TimeSlotId == timeSlotId)
                .Select(t => new { t.DayOfWeek, t.StartTime })
                .FirstOrDefaultAsync()
                ?? throw new InvalidOperationException("Không tìm thấy khung giờ này.");

            if (slot.DayOfWeek != SessionRules.DbDayOfWeek(date))
            {
                throw new InvalidOperationException("Khung giờ không khớp với thứ của ngày đã chọn.");
            }
            if (SessionRules.HasStarted(date, slot.StartTime))
            {
                throw new InvalidOperationException("Khung giờ này đã qua, hãy chọn ngày hoặc khung khác.");
            }
        }

        // Xếp thủ công một buổi bù cho lớp đang thiếu buổi. Trả về số buổi mới. Gọi sp_add_makeup_session.
        public async Task<int> AddMakeupSessionAsync(int classId, DateOnly date, int timeSlotId, int coachId, int roomId)
        {
            RequireManager();
            using var db = new SportCenterDbContext();
            DateOnly classStart = await db.Classes.AsNoTracking()
                .Where(x => x.ClassId == classId)
                .Select(x => (DateOnly?)x.StartDate)
                .FirstOrDefaultAsync()
                ?? throw new InvalidOperationException("Không tìm thấy lớp này.");
            await CheckMakeupSlotAsync(db, date, timeSlotId, classStart);

            var conn = (SqlConnection)db.Database.GetDbConnection();
            await conn.OpenAsync();

            using var cmd = conn.CreateCommand();
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.CommandText = "dbo.sp_add_makeup_session";
            cmd.Parameters.AddWithValue("@actor_id", CurrentUser.Value!.UserId);
            cmd.Parameters.AddWithValue("@class_id", classId);
            cmd.Parameters.Add(DateParam("@session_date", date));
            cmd.Parameters.AddWithValue("@time_slot_id", timeSlotId);
            cmd.Parameters.AddWithValue("@coach_id", coachId);
            cmd.Parameters.AddWithValue("@room_id", roomId);
            var newId = new SqlParameter("@session_id", SqlDbType.Int) { Direction = ParameterDirection.Output };
            cmd.Parameters.Add(newId);

            try
            {
                await cmd.ExecuteNonQueryAsync();
            }
            catch (SqlException ex)
            {
                throw new InvalidOperationException(ex.Number switch
                {
                    50001 => "Chỉ Manager đang hoạt động mới được xếp buổi bù.",
                    50003 => "Coach không hợp lệ (sai môn hoặc không hoạt động).",
                    50004 => "Phòng không hợp lệ (sai môn hoặc không hoạt động).",
                    50005 => "Phòng không đủ chỗ cho sĩ số của lớp.",
                    50006 => "Coach đã bận vào khung giờ này. Vui lòng chọn coach khác.",
                    50007 => "Phòng đã bận vào khung giờ này. Vui lòng chọn phòng khác.",
                    50010 => "Khung giờ không khớp với thứ của ngày đã chọn.",
                    50011 => "Lớp đã đủ số buổi hoặc không còn xếp buổi bù được.",
                    _ => "Không xếp được buổi bù. Vui lòng thử lại."
                }, ex);
            }

            // Buổi bù đã tạo xong; nếu chỉ đọc số buổi bị lỗi thì trả 0 (giao diện báo chung chung).
            int id = (int)newId.Value;
            try
            {
                return await db.Sessions.AsNoTracking().Where(x => x.SessionId == id).Select(x => x.SessionNo).FirstAsync();
            }
            catch (Exception)
            {
                return 0;
            }
        }

        public async Task<List<CourseOptionDto>> GetCoursesAsync(int sportId)
        {
            RequireManager();
            using var db = new SportCenterDbContext();
            return await db.Courses.AsNoTracking()
                .Where(c => c.SportId == sportId && c.Status == "Active" && c.Sport.Status == "Active")
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
