using System;

namespace SportCenter.Dtos
{
    public record MemberSearchOptionDto(
        int MemberId,
        string FullName,
        string Email,
        string? Phone,
        DateOnly? BirthDate,
        string? Gender)
    {
        public string DisplayText => $"{FullName} · {Phone ?? "Chưa có SĐT"} ({Email})";
    }

    public record ClassEnrollOptionDto(
        int ClassId,
        string ClassName,
        string SportName,
        string CourseName,
        decimal TuitionFee,
        int MaxCapacity,
        int ActiveMembers,
        int SeatsLeft,
        string ScheduleText,
        string RoomName,
        string? CoachName)
    {
        public string DisplayText => $"{ClassName} ({SportName}) · Còn {SeatsLeft}/{MaxCapacity} chỗ · {TuitionFee:N0} đ";
    }

    public record RecentEnrollmentDto(
        int EnrollmentId,
        string MemberName,
        string MemberPhone,
        string ClassName,
        string SportName,
        decimal TuitionFee,
        string Status,
        string? RegisteredByName,
        DateTime EnrolledAt)
    {
        public string EnrolledAtText => EnrolledAt.ToString("dd/MM/yyyy HH:mm");
        public string FeeText => $"{TuitionFee:N0} ₫";
    }
}

