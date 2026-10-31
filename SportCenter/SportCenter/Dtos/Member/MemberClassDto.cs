namespace SportCenter.Dtos
{
    // Một lớp đang tuyển sinh, dành cho học viên xem.
    // TimeSlotNames: các khung giờ học trong tuần, ví dụ "T2 07:00-10:00".
    public record MemberClassDto(
        int ClassId, string ClassName, string SportName, string CourseName, string? Level,
        string? CoachName, string RoomName, DateOnly StartDate,
        int MaxCapacity, int ActiveMembers, decimal TuitionFee, List<string> TimeSlotNames)
    {
        public int SeatsLeft => MaxCapacity - ActiveMembers;
        public string ScheduleText => string.Join(", ", TimeSlotNames);
    }
}