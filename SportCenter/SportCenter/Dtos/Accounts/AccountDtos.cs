using System;

namespace SportCenter.Dtos
{
    // Một dòng trong trang quản lý tài khoản (UC14).
    // ActiveClassCount: số lớp đang tuyển sinh hoặc đang học mà người này là coach (0 với role khác).
    // UpcomingSessionCount: số buổi chưa diễn ra được xếp cho người này (kể cả dạy thay).
    public record AccountDto(
        int UserId, string FullName, string Email, string RoleName, string? Phone,
        int? SportId, string? SportName, string Status, DateTime CreatedAt, int ActiveClassCount, int UpcomingSessionCount)
    {
        public bool IsActive => Status == "Active";
        public bool HasWork => ActiveClassCount > 0 || UpcomingSessionCount > 0;      // đang có việc: không đổi role / môn
        public bool IsEditable => RoleName is "Staff" or "Coach";      // chỉ Staff và Coach sửa / đổi role được từ trang này
    }
}
