using Microsoft.EntityFrameworkCore;
using SportCenter.Dtos;
using SportCenter.Entities;

namespace SportCenter.Services
{
    // Học viên xem lịch học và điểm danh của chính mình.
    public class MemberScheduleService
    {
        // Chỉ các lớp học viên đã đăng ký xong (Active); đăng ký đang chờ thanh toán chưa có lịch.
        public async Task<List<MemberSessionDto>> GetMySessionsAsync()
        {
            int memberId = RequireMember();
            using var db = new SportCenterDbContext();

            return await db.Sessions
                .AsNoTracking()
                .Where(s => s.Class.ClassMembers.Any(m => m.MemberId == memberId && m.Status == "Active"))
                .OrderBy(s => s.SessionDate).ThenBy(s => s.TimeSlot.StartTime)
                .Select(s => new MemberSessionDto(
                    s.Class.ClassName, s.SessionNo, s.SessionDate, s.TimeSlot.StartTime, s.TimeSlot.EndTime,
                    s.Room.RoomName, s.Coach.FullName, s.Status,
                    s.Attendances.Where(a => a.MemberId == memberId).Select(a => a.Status).FirstOrDefault(),
                    s.Attendances.Where(a => a.MemberId == memberId).Select(a => a.Remarks).FirstOrDefault()))
                .ToListAsync();
        }

        private static int RequireMember()
        {
            if (CurrentUser.Value is not { Role.RoleName: "Member" } user)
            {
                throw new UnauthorizedAccessException("Chỉ học viên được xem lịch học.");
            }
            return user.UserId;
        }
    }
}
