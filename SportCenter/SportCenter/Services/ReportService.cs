using Microsoft.EntityFrameworkCore;
using SportCenter.Dtos;
using SportCenter.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SportCenter.Services
{
    // UC15: báo cáo cho Manager (tổng quan, doanh thu, chuyên cần). Chỉ đọc dữ liệu, không ghi gì.
    public class ReportService
    {
        public const double AtRiskThreshold = 0.7;     // học viên dưới ngưỡng này được liệt kê cần chú ý
        public const int TrendMonths = 6;

        // Các tháng để chọn: tháng hiện tại và 11 tháng trước đó.
        public static List<MonthOption> RecentMonths()
        {
            var now = DateTime.Today;
            return Enumerable.Range(0, 12)
                .Select(i => now.AddMonths(-i))
                .Select(d => new MonthOption(d.Year, d.Month))
                .ToList();
        }

        public async Task<OverviewDto> GetOverviewAsync(int year, int month)
        {
            RequireManager();
            (DateTime from, DateTime to) = MonthRange(year, month);
            var fromDate = DateOnly.FromDateTime(from);
            var toDate = DateOnly.FromDateTime(to);
            using var db = new SportCenterDbContext();

            var running = db.Classes.Where(k => k.Status == "Enrolling" || k.Status == "Ongoing");
            int runningClasses = await running.CountAsync();
            int capacity = await running.SumAsync(k => (int?)k.MaxCapacity) ?? 0;
            int enrolled = await db.ClassMembers.CountAsync(m => m.Status == "Active"
                && (m.Class.Status == "Enrolling" || m.Class.Status == "Ongoing"));
            int activeMembers = await db.ClassMembers
                .Where(m => m.Status == "Active" && (m.Class.Status == "Enrolling" || m.Class.Status == "Ongoing"))
                .Select(m => m.MemberId).Distinct().CountAsync();

            decimal revenue = await db.Payments
                .Where(p => p.Status == "Paid" && p.PaidAt >= from && p.PaidAt < to)
                .SumAsync(p => (decimal?)p.Amount) ?? 0m;
            int pending = await db.Payments.CountAsync(p => p.Status == "Pending" && p.Enrollment.Status != "Dropped");
            int cancelled = await db.Sessions.CountAsync(s => s.Status == "Cancelled"
                && s.SessionDate >= fromDate && s.SessionDate < toDate);

            return new OverviewDto(activeMembers, runningClasses,
                                   capacity == 0 ? null : (double)enrolled / capacity,
                                   revenue, pending, cancelled);
        }

        public async Task<RevenueReportDto> GetRevenueAsync(int year, int month)
        {
            RequireManager();
            (DateTime from, DateTime to) = MonthRange(year, month);
            using var db = new SportCenterDbContext();

            var paid = db.Payments.AsNoTracking()
                .Where(p => p.Status == "Paid" && p.PaidAt >= from && p.PaidAt < to);

            var bySport = await paid
                .GroupBy(p => new { p.Enrollment.Class.Course.SportId, p.Enrollment.Class.Course.Sport.SportName })
                .Select(g => new RevenueRowDto(g.Key.SportName, g.Count(), g.Sum(p => p.Amount)))
                .ToListAsync();
            var byCourse = await paid
                .GroupBy(p => new { p.Enrollment.Class.CourseId, p.Enrollment.Class.Course.Sport.SportName, p.Enrollment.Class.Course.CourseName })
                .Select(g => new RevenueRowDto(g.Key.SportName + " · " + g.Key.CourseName, g.Count(), g.Sum(p => p.Amount)))
                .ToListAsync();

            // 6 tháng gần nhất kết thúc ở tháng đã chọn (tháng không có doanh thu vẫn hiện 0)
            DateTime trendFrom = from.AddMonths(-(TrendMonths - 1));
            var payments = await db.Payments.AsNoTracking()
                .Where(p => p.Status == "Paid" && p.PaidAt >= trendFrom && p.PaidAt < to)
                .Select(p => new { p.PaidAt, p.Amount })
                .ToListAsync();
            var trend = Enumerable.Range(0, TrendMonths)
                .Select(i => trendFrom.AddMonths(i))
                .Select(d => new RevenueMonthDto(d.Year, d.Month,
                    payments.Where(p => p.PaidAt!.Value.Year == d.Year && p.PaidAt.Value.Month == d.Month).Sum(p => p.Amount)))
                .ToList();

            var now = DateTime.Now;
            var pendingRows = await db.Payments.AsNoTracking()
                .Where(p => p.Status == "Pending" && p.Enrollment.Status != "Dropped")
                .OrderBy(p => p.CreatedAt)
                .Select(p => new { p.PaymentId, Member = p.Enrollment.Member.FullName, Class = p.Enrollment.Class.ClassName, p.Amount, p.CreatedAt })
                .ToListAsync();
            var pending = pendingRows
                .Select(p => new PendingPaymentDto(p.PaymentId, p.Member, p.Class, p.Amount, p.CreatedAt,
                                                   Math.Max(0, (int)(now - p.CreatedAt).TotalDays)))
                .ToList();

            return new RevenueReportDto(bySport.Sum(r => r.Amount),
                bySport.OrderByDescending(r => r.Amount).ToList(),
                byCourse.OrderByDescending(r => r.Amount).ToList(),
                trend, pending);
        }

        // Chuyên cần của các buổi đã hoàn thành (Completed) trong tháng.
        public async Task<AttendanceReportDto> GetAttendanceAsync(int year, int month)
        {
            RequireManager();
            (DateTime from, DateTime to) = MonthRange(year, month);
            var fromDate = DateOnly.FromDateTime(from);
            var toDate = DateOnly.FromDateTime(to);
            using var db = new SportCenterDbContext();

            var rows = await db.Attendances.AsNoTracking()
                .Where(a => a.Session.Status == "Completed" && a.Session.SessionDate >= fromDate && a.Session.SessionDate < toDate)
                .Select(a => new
                {
                    a.Status, a.MemberId,
                    CoachId = a.Session.CoachId,
                    SportId = a.Session.Class.Course.SportId,
                    Enrolled = a.Session.Class.ClassMembers.Any(m => m.MemberId == a.MemberId && m.Status == "Active"),
                    Member = a.Member.FullName,
                    Class = a.Session.Class.ClassName,
                    ClassId = a.Session.ClassId,
                    Sport = a.Session.Class.Course.Sport.SportName,
                    Coach = a.Session.Coach.FullName
                })
                .ToListAsync();

            AttendanceRowDto Make(string name, string? detail, IEnumerable<string> statuses)
            {
                var list = statuses.ToList();
                return new AttendanceRowDto(name, detail,
                    list.Count(s => s == "Present"), list.Count(s => s == "Late"),
                    list.Count(s => s == "Absent"), list.Count(s => s == "Excused"));
            }

            var bySport = rows.GroupBy(r => new { r.SportId, r.Sport })
                .Select(g => Make(g.Key.Sport, null, g.Select(r => r.Status)))
                .OrderBy(r => r.Rate ?? 2).ThenBy(r => r.Name).ToList();
            var byCoach = rows.GroupBy(r => new { r.CoachId, r.Coach })
                .Select(g => Make(g.Key.Coach, null, g.Select(r => r.Status)))
                .OrderBy(r => r.Rate ?? 2).ThenBy(r => r.Name).ToList();
            // Học viên đã rời lớp (Dropped) không còn cần chú ý
            var atRisk = rows.Where(r => r.Enrolled).GroupBy(r => new { r.MemberId, r.ClassId })
                .Select(g => Make(g.First().Member, g.First().Class, g.Select(r => r.Status)))
                .Where(r => r.Rate is double rate && rate < AtRiskThreshold)
                .OrderBy(r => r.Rate).ThenByDescending(r => r.Absent).ToList();

            return new AttendanceReportDto(Make("Toàn trung tâm", null, rows.Select(r => r.Status)),
                                           bySport, byCoach, atRisk, AtRiskThreshold);
        }

        // Khoảng [đầu tháng, đầu tháng sau).
        private static (DateTime From, DateTime To) MonthRange(int year, int month)
        {
            if (year < 2000 || year > 2100 || month < 1 || month > 12) throw new InvalidOperationException("Tháng báo cáo không hợp lệ.");
            var from = new DateTime(year, month, 1);
            return (from, from.AddMonths(1));
        }

        private static void RequireManager()
        {
            if (CurrentUser.Value?.Role.RoleName != "Manager")
            {
                throw new UnauthorizedAccessException("Chỉ Manager được xem báo cáo.");
            }
        }
    }
}
