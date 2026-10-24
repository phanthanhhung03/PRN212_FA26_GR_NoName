using System;
using System.Collections.Generic;

namespace SportCenter.Dtos
{
    // Dữ liệu cho trang báo cáo của Manager (UC15). Tất cả theo một tháng được chọn, trừ danh sách khoản chờ thanh toán.

    // Một tháng để chọn trong ô lọc.
    public record MonthOption(int Year, int Month)
    {
        public string Label => $"{Month:00}/{Year}";
    }

    // Thẻ tổng quan. FillRate = học viên đang học / tổng sĩ số của các lớp đang tuyển sinh hoặc đang học (null khi chưa có lớp).
    public record OverviewDto(
        int ActiveMembers, int RunningClasses, double? FillRate,
        decimal Revenue, int PendingPaymentCount, int CancelledSessions);

    // Một dòng doanh thu (theo môn hoặc theo khóa).
    public record RevenueRowDto(string Name, int PaymentCount, decimal Amount);

    // Doanh thu của một tháng trong biểu đồ 6 tháng gần nhất.
    public record RevenueMonthDto(int Year, int Month, decimal Amount)
    {
        public string Label => $"{Month:00}/{Year}";
    }

    // Khoản chưa thanh toán, DaysWaiting = số ngày kể từ lúc tạo.
    public record PendingPaymentDto(
        int PaymentId, string MemberName, string ClassName, decimal Amount, DateTime CreatedAt, int DaysWaiting);

    public record RevenueReportDto(
        decimal Total, IReadOnlyList<RevenueRowDto> BySport, IReadOnlyList<RevenueRowDto> ByCourse,
        IReadOnlyList<RevenueMonthDto> Trend, IReadOnlyList<PendingPaymentDto> Pending);

    // Chuyên cần của một nhóm (môn, coach hoặc một học viên trong một lớp).
    // Rate = (Present + Late) / (Present + Late + Absent); vắng có phép (Excused) không tính vào mẫu số. Null khi chưa có lượt nào.
    public record AttendanceRowDto(string Name, string? Detail, int Present, int Late, int Absent, int Excused)
    {
        public string Display => Detail is null ? Name : $"{Name} ({Detail})";
        public string RateText => Rate is double r ? $"{r * 100:0}%" : "–";
        public int Counted => Present + Late + Absent;
        public double? Rate => Counted == 0 ? null : (double)(Present + Late) / Counted;
    }

    public record AttendanceReportDto(
        AttendanceRowDto Total, IReadOnlyList<AttendanceRowDto> BySport, IReadOnlyList<AttendanceRowDto> ByCoach,
        IReadOnlyList<AttendanceRowDto> AtRisk, double AtRiskThreshold);
}
