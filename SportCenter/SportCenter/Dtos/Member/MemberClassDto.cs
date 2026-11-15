using System;
using System.Collections.Generic;

namespace SportCenter.Dtos
{
    // Một lớp đang tuyển sinh, dành cho học viên xem và tự đăng ký (UC04, UC05).
    // TimeSlotNames: các khung giờ học trong tuần, ví dụ "T2 07:00-10:00".
    public record MemberClassDto(
        int ClassId, string ClassName, string SportName, string CourseName, string? Level,
        string? CoachName, string RoomName, DateOnly StartDate,
        int MaxCapacity, int ActiveMembers, decimal TuitionFee, List<string> TimeSlotNames,
        bool IsEnrolled = false)
    {
        public int SeatsLeft => Math.Max(0, MaxCapacity - ActiveMembers);
        public string ScheduleText => string.Join(", ", TimeSlotNames);
        public bool CanEnroll => !IsEnrolled && SeatsLeft > 0;
        public string EnrollButtonText => IsEnrolled ? "Đã đăng ký" : (SeatsLeft > 0 ? "Đăng ký ngay" : "Hết chỗ");
    }
}
