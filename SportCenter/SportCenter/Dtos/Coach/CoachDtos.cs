using System;

namespace SportCenter.Dtos
{
    // DTO cho ComboBox lọc lớp của Coach
    public record CoachClassOptionDto(int ClassId, string ClassName, string SportName);

    // DTO hiển thị danh sách buổi học của Coach (UC08)
    public record CoachSessionDto(
        int SessionId,
        int ClassId,
        string ClassName,
        string SportName,
        int SessionNo,
        int TotalSessions,
        DateOnly SessionDate,
        byte DayOfWeek,
        TimeOnly StartTime,
        TimeOnly EndTime,
        string RoomName,
        string Status,
        int MemberCount,
        int PresentCount)
    {
        public string TimeDisplay => $"{StartTime:HH\\:mm} - {EndTime:HH\\:mm}";
        public string DayOfWeekDisplay => DayOfWeek switch
        {
            2 => "Thứ Hai",
            3 => "Thứ Ba",
            4 => "Thứ Tư",
            5 => "Thứ Năm",
            6 => "Thứ Sáu",
            7 => "Thứ Bảy",
            1 => "Chủ Nhật",
            _ => $"Thứ {DayOfWeek}"
        };
        public string ProgressDisplay => $"{SessionNo}/{TotalSessions}";
    }

    // DTO chi tiết học viên trong buổi học để điểm danh (UC09)
    public class SessionMemberAttendanceDto
    {
        public int MemberId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string Status { get; set; } = "Present"; // Present, Absent, Late, Excused
        public string? Remarks { get; set; }
        public DateTime? CheckInTime { get; set; }

        public bool IsPresent
        {
            get => Status == "Present";
            set { if (value) Status = "Present"; }
        }

        public bool IsAbsent
        {
            get => Status == "Absent";
            set { if (value) Status = "Absent"; }
        }

        public bool IsLate
        {
            get => Status == "Late";
            set { if (value) Status = "Late"; }
        }

        public bool IsExcused
        {
            get => Status == "Excused";
            set { if (value) Status = "Excused"; }
        }
    }

    public record AttendanceItemUpdateDto(
        int MemberId,
        string Status,
        string? Remarks);
}

