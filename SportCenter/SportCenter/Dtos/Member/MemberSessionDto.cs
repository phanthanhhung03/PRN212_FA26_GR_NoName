namespace SportCenter.Dtos
{
    // Một buổi học của học viên kèm kết quả điểm danh của chính học viên đó.
    // AttendanceStatus = null nghĩa là Coach chưa điểm danh buổi này.
    public record MemberSessionDto(
        string ClassName, int SessionNo, DateOnly SessionDate, TimeOnly StartTime, TimeOnly EndTime,
        string RoomName, string CoachName, string SessionStatus, string? AttendanceStatus, string? Remarks)
    {
        public string TimeText => $"{StartTime:HH\\:mm}-{EndTime:HH\\:mm}";

        public string SessionStatusText => SessionStatus switch
        {
            "Scheduled" => "Sắp diễn ra",
            "Completed" => "Đã học",
            "Cancelled" => "Đã hủy",
            _ => SessionStatus
        };

        // Buổi đã hủy thì không có điểm danh.
        public string AttendanceText => SessionStatus == "Cancelled" ? "—" : AttendanceStatus switch
        {
            null => "Chưa điểm danh",
            "Present" => "Có mặt",
            "Absent" => "Vắng",
            "Late" => "Đi trễ",
            "Excused" => "Vắng có phép",
            _ => AttendanceStatus
        };
    }
}
