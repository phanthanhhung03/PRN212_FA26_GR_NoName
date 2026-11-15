using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using SportCenter.Dtos;
using SportCenter.Entities;
using System.Data;

namespace SportCenter.Services;

/// <summary>UC05 (Member tự ghi danh) và UC10 (Staff ghi danh hộ).</summary>
public sealed class EnrollmentService
{
    public async Task<int> EnrollCurrentMemberAsync(int classId)
    {
        if (CurrentUser.Value?.Role.RoleName != "Member")
            throw new UnauthorizedAccessException("Chỉ Member mới được tự ghi danh lớp.");

        return await EnrollAsync(classId, CurrentUser.Value.UserId, registeredBy: null);
    }

    public async Task<int> EnrollMemberByStaffAsync(int classId, int memberId)
    {
        if (CurrentUser.Value?.Role.RoleName != "Staff")
            throw new UnauthorizedAccessException("Chỉ Staff mới được ghi danh hộ học viên.");

        return await EnrollAsync(classId, memberId, CurrentUser.Value.UserId);
    }

    private static async Task<int> EnrollAsync(int classId, int memberId, int? registeredBy)
    {
        using var db = new SportCenterDbContext();
        var conn = (SqlConnection)db.Database.GetDbConnection();
        if (conn.State != ConnectionState.Open) await conn.OpenAsync();

        using var cmd = conn.CreateCommand();
        cmd.CommandType = CommandType.StoredProcedure;
        cmd.CommandText = "dbo.sp_enroll_member";
        cmd.Parameters.Add("@class_id", SqlDbType.Int).Value = classId;
        cmd.Parameters.Add("@member_id", SqlDbType.Int).Value = memberId;
        cmd.Parameters.Add("@registered_by", SqlDbType.Int).Value = (object?)registeredBy ?? DBNull.Value;
        var output = new SqlParameter("@enrollment_id", SqlDbType.Int) { Direction = ParameterDirection.Output };
        cmd.Parameters.Add(output);

        try
        {
            await cmd.ExecuteNonQueryAsync();
            return (int)output.Value;
        }
        catch (SqlException ex)
        {
            throw MapEnrollmentError(ex);
        }
    }

    public async Task<List<MemberLookupDto>> SearchMembersAsync(string keyword)
    {
        RequireStaff();
        if (string.IsNullOrWhiteSpace(keyword)) return new();
        using var db = new SportCenterDbContext();
        string term = keyword.Trim();
        return await db.Users.AsNoTracking()
            .Where(u => u.Status == "Active" && u.Role.RoleName == "Member"
                && (u.Email.Contains(term) || u.FullName.Contains(term) || (u.Phone != null && u.Phone.Contains(term))))
            .OrderBy(u => u.FullName)
            .Take(30)
            .Select(u => new MemberLookupDto(u.UserId, u.FullName, u.Email, u.Phone))
            .ToListAsync();
    }

    public async Task<List<MemberClassDto>> GetEnrollingClassesAsync()
    {
        RequireStaff();
        using var db = new SportCenterDbContext();
        return await db.Classes.AsNoTracking()
            .Where(c => c.Status == "Enrolling")
            .OrderBy(c => c.StartDate)
            .Select(c => new MemberClassDto(
                c.ClassId, c.ClassName, c.Course.Sport.SportName, c.Course.CourseName, c.Course.Level,
                c.Coach != null ? c.Coach.FullName : null, c.Room.RoomName, c.StartDate,
                c.MaxCapacity,
                c.ClassMembers.Count(m => m.Status == "Pending" || m.Status == "Active"),
                c.Course.TuitionFee,
                c.TimeSlots.OrderBy(t => t.DayOfWeek).ThenBy(t => t.StartTime).Select(t => t.SlotName).ToList()))
            .ToListAsync();
    }

    public async Task<EnrollmentResultDto> GetEnrollmentAsync(int enrollmentId)
    {
        using var db = new SportCenterDbContext();
        var q = db.ClassMembers.AsNoTracking().Where(e => e.EnrollmentId == enrollmentId);
        if (CurrentUser.Value?.Role.RoleName == "Member")
            q = q.Where(e => e.MemberId == CurrentUser.Value.UserId);
        else if (CurrentUser.Value?.Role.RoleName == "Staff")
            q = q.Where(e => e.RegisteredBy == CurrentUser.Value.UserId);
        else
            throw new UnauthorizedAccessException("Không có quyền xem đăng ký này.");

        return await q.Select(e => new EnrollmentResultDto(
                e.EnrollmentId, e.ClassId, e.Class.ClassName, e.Member.FullName, e.Status))
            .FirstOrDefaultAsync()
            ?? throw new InvalidOperationException("Không tìm thấy đăng ký.");
    }

    private static void RequireStaff()
    {
        if (CurrentUser.Value?.Role.RoleName != "Staff")
            throw new UnauthorizedAccessException("Chỉ Staff được sử dụng chức năng này.");
    }

    private static InvalidOperationException MapEnrollmentError(SqlException ex) => new(ex.Number switch
    {
        51001 => "Tài khoản học viên không tồn tại hoặc không hoạt động.",
        51002 => "Tài khoản không hợp lệ để tự ghi danh.",
        51003 => "Chỉ Staff đang hoạt động mới được ghi danh hộ.",
        51004 => "Lớp không tồn tại hoặc không còn tuyển sinh.",
        51005 => "Học viên đã có đăng ký đối với lớp này.",
        51006 => "Lớp đã đủ sĩ số.",
        2601 or 2627 => "Học viên đã được ghi danh vào lớp này.",
        _ => "Không ghi danh được. Vui lòng thử lại."
    }, ex);
}
