using System;

namespace SportCenter.Dtos
{
    // Dữ liệu cho trang quản lý môn thể thao và khóa học (UC11).

    // Một môn: CourseCount = số khóa; ActiveClassCount = số lớp đang tuyển sinh hoặc đang học thuộc môn.
    public record SportManageDto(
        int SportId, string SportName, string? Description, string Status,
        int CourseCount, int ActiveClassCount)
    {
        public bool IsActive => Status == "Active";
    }

    // Một khóa: ClassCount = tổng số lớp đã mở từ khóa (số buổi của khóa không đổi được khi > 0);
    // ActiveClassCount = số lớp đang tuyển sinh hoặc đang học.
    public record CourseManageDto(
        int CourseId, int SportId, string CourseName, string? Level, int TotalSessions, decimal TuitionFee,
        string? Description, string Status, int ClassCount, int ActiveClassCount)
    {
        public bool IsActive => Status == "Active";
    }
}
