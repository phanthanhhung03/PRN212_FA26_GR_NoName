using System;

namespace SportCenter.Dtos
{
    // Dữ liệu cho trang chi tiết lớp (UC12).
    public record ClassDetailDto(
        int ClassId, string ClassName, string SportName, string? Level,
        string? CoachName, string RoomName,
        DateOnly StartDate, DateOnly? EndDate,
        int MaxCapacity, int ActiveMembers, string Status, int TotalSessions);

    // Một buổi học của lớp. IsRoomChanged / IsCoachChanged: khác phòng / coach gốc của lớp (đã đổi tạm).
    // IsMakeup: buổi bù (số buổi lớn hơn tổng số buổi của khóa).
    public record SessionItemDto(
        int SessionId, int SessionNo, DateOnly SessionDate, byte DayOfWeek,
        TimeOnly StartTime, TimeOnly EndTime, string RoomName, string CoachName,
        string Status, bool IsRoomChanged, bool IsCoachChanged, bool IsMakeup);
}
