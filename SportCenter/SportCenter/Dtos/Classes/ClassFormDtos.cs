using System;
using System.Collections.Generic;

namespace SportCenter.Dtos
{
    // Dữ liệu cho form "Mở lớp mới" (UC12, UC13).
    public record CourseOptionDto(int CourseId, string CourseName, int TotalSessions);
    public record TimeSlotDto(int TimeSlotId, byte DayOfWeek, TimeOnly StartTime, TimeOnly EndTime);
    public record CoachOptionDto(int UserId, string FullName);
    public record RoomOptionDto(int RoomId, string RoomName, int Capacity);
    // Khung giờ không chọn được; Reason là "Room", "Coach" hoặc "Both".
    public record BusySlotDto(int TimeSlotId, string Reason);
    public record PlannedDateDto(int SessionNo, DateOnly SessionDate, int TimeSlotId);
    public record CreateClassRequest(int CourseId, string ClassName, DateOnly StartDate, int MaxCapacity, int CoachId, int RoomId, List<int> SlotIds);
}
