namespace SportCenter.Dtos
{
    // Một lớp đang tuyển sinh, dành cho học viên xem.
    // TimeSlotNames: các khung giờ học trong tuần, ví dụ "T2 07:00-10:00".
    public record MemberClassDto(
        int ClassId, string ClassName, string SportName, string CourseName, string? Level,
        string? CoachName, string RoomName, DateOnly StartDate,
<<<<<<< Updated upstream
        int MaxCapacity, int ActiveMembers, decimal TuitionFee, List<string> TimeSlotNames)
=======
        int MaxCapacity, int ActiveMembers, decimal TuitionFee, List<string> TimeSlotNames,
        bool IsEnrolled = false, bool IsPaid = false, bool HasStarted = false)
>>>>>>> Stashed changes
    {
        public int SeatsLeft => MaxCapacity - ActiveMembers;
        public string ScheduleText => string.Join(", ", TimeSlotNames);
<<<<<<< Updated upstream
=======
        // Đã đăng ký thì nút dùng để hủy (UC06); chưa đăng ký thì dùng để đăng ký khi còn chỗ.
        public bool CannotCancel => IsEnrolled && IsPaid && HasStarted;
        public bool CanEnroll => IsEnrolled || SeatsLeft > 0;
        public string EnrollButtonText => IsEnrolled
            ? (CannotCancel ? "Đã khai giảng" : "Hủy đăng ký")
            : (SeatsLeft > 0 ? "Đăng ký ngay" : "Hết chỗ");
>>>>>>> Stashed changes
    }
}