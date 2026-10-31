using Microsoft.EntityFrameworkCore;
using SportCenter.Dtos;
using SportCenter.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SportCenter.Services
{
    // UC08: Coach xem lịch dạy và danh sách học viên
    // UC09: Coach điểm danh học viên và đánh dấu hoàn thành buổi học
    public class CoachScheduleService
    {
        public async Task<List<CoachClassOptionDto>> GetMyClassesAsync()
        {
            int coachId = RequireCoach();
            using var db = new SportCenterDbContext();

            return await db.Classes.AsNoTracking()
                .Where(c => c.CoachId == coachId || c.Sessions.Any(s => s.CoachId == coachId))
                .OrderBy(c => c.ClassName)
                .Select(c => new CoachClassOptionDto(c.ClassId, c.ClassName, c.Course.Sport.SportName))
                .ToListAsync();
        }

        public async Task<List<CoachSessionDto>> GetMySessionsAsync(
            int? classId = null,
            DateOnly? fromDate = null,
            DateOnly? toDate = null,
            string? status = null)
        {
            int coachId = RequireCoach();
            using var db = new SportCenterDbContext();

            var query = db.Sessions.AsNoTracking()
                .Where(s => s.CoachId == coachId);

            if (classId.HasValue && classId.Value > 0)
            {
                query = query.Where(s => s.ClassId == classId.Value);
            }

            if (fromDate.HasValue)
            {
                query = query.Where(s => s.SessionDate >= fromDate.Value);
            }

            if (toDate.HasValue)
            {
                query = query.Where(s => s.SessionDate <= toDate.Value);
            }

            if (!string.IsNullOrWhiteSpace(status) && status != "All")
            {
                query = query.Where(s => s.Status == status);
            }

            return await query
                .OrderBy(s => s.SessionDate)
                .ThenBy(s => s.TimeSlot.StartTime)
                .Select(s => new CoachSessionDto(
                    s.SessionId,
                    s.ClassId,
                    s.Class.ClassName,
                    s.Class.Course.Sport.SportName,
                    s.SessionNo,
                    s.Class.Course.TotalSessions,
                    s.SessionDate,
                    s.TimeSlot.DayOfWeek,
                    s.TimeSlot.StartTime,
                    s.TimeSlot.EndTime,
                    s.Room.RoomName,
                    s.Status,
                    s.Class.ClassMembers.Count(m => m.Status == "Active"),
                    s.Attendances.Count(a => a.Status == "Present" || a.Status == "Late")))
                .ToListAsync();
        }

        public async Task<Session?> GetSessionDetailAsync(int sessionId)
        {
            int coachId = RequireCoach();
            using var db = new SportCenterDbContext();

            return await db.Sessions
                .AsNoTracking()
                .Include(s => s.Class)
                    .ThenInclude(c => c.Course)
                        .ThenInclude(co => co.Sport)
                .Include(s => s.Room)
                .Include(s => s.TimeSlot)
                .FirstOrDefaultAsync(s => s.SessionId == sessionId && s.CoachId == coachId);
        }

        public async Task<List<SessionMemberAttendanceDto>> GetSessionMembersAsync(int sessionId)
        {
            int coachId = RequireCoach();
            using var db = new SportCenterDbContext();

            var session = await db.Sessions
                .AsNoTracking()
                .Include(s => s.Class)
                .FirstOrDefaultAsync(s => s.SessionId == sessionId && s.CoachId == coachId)
                ?? throw new InvalidOperationException("Không tìm thấy buổi học hoặc bạn không được phân công buổi này.");

            var activeMembers = await db.ClassMembers
                .AsNoTracking()
                .Include(cm => cm.Member)
                .Where(cm => cm.ClassId == session.ClassId && cm.Status == "Active")
                .OrderBy(cm => cm.Member.FullName)
                .ToListAsync();

            var attendances = await db.Attendances
                .AsNoTracking()
                .Where(a => a.SessionId == sessionId)
                .ToDictionaryAsync(a => a.MemberId);

            var result = new List<SessionMemberAttendanceDto>();
            foreach (var cm in activeMembers)
            {
                attendances.TryGetValue(cm.MemberId, out var att);
                result.Add(new SessionMemberAttendanceDto
                {
                    MemberId = cm.MemberId,
                    FullName = cm.Member.FullName,
                    Email = cm.Member.Email,
                    Phone = cm.Member.Phone,
                    Status = att?.Status ?? "Present",
                    Remarks = att?.Remarks,
                    CheckInTime = att?.CheckInTime
                });
            }

            return result;
        }

        public async Task SaveAttendanceAsync(int sessionId, List<AttendanceItemUpdateDto> updates, bool markCompleted)
        {
            int coachId = RequireCoach();
            using var db = new SportCenterDbContext();

            var session = await db.Sessions
                .FirstOrDefaultAsync(s => s.SessionId == sessionId && s.CoachId == coachId)
                ?? throw new InvalidOperationException("Không tìm thấy buổi học hoặc bạn không có quyền điểm danh buổi này.");

            if (session.Status == "Cancelled")
            {
                throw new InvalidOperationException("Không thể điểm danh cho buổi học đã bị hủy.");
            }

            var existingAttendances = await db.Attendances
                .Where(a => a.SessionId == sessionId)
                .ToListAsync();

            var now = DateTime.Now;

            foreach (var update in updates)
            {
                var att = existingAttendances.FirstOrDefault(a => a.MemberId == update.MemberId);
                if (att is null)
                {
                    att = new Attendance
                    {
                        SessionId = sessionId,
                        MemberId = update.MemberId,
                        Status = update.Status,
                        Remarks = string.IsNullOrWhiteSpace(update.Remarks) ? null : update.Remarks.Trim(),
                        MarkedBy = coachId,
                        UpdatedAt = now,
                        CheckInTime = (update.Status == "Present" || update.Status == "Late") ? now : null
                    };
                    db.Attendances.Add(att);
                }
                else
                {
                    att.Status = update.Status;
                    att.Remarks = string.IsNullOrWhiteSpace(update.Remarks) ? null : update.Remarks.Trim();
                    att.MarkedBy = coachId;
                    att.UpdatedAt = now;
                    if ((update.Status == "Present" || update.Status == "Late") && att.CheckInTime is null)
                    {
                        att.CheckInTime = now;
                    }
                }
            }

            if (markCompleted && session.Status == "Scheduled")
            {
                session.Status = "Completed";
            }

            await db.SaveChangesAsync();
        }

        private static int RequireCoach()
        {
            if (CurrentUser.Value is not { Role.RoleName: "Coach" } user)
            {
                throw new UnauthorizedAccessException("Chỉ Huấn luyện viên mới có quyền truy cập chức năng này.");
            }
            return user.UserId;
        }
    }
}

