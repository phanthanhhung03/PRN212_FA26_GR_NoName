using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using SportCenter.Dtos;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace SportCenter.Services
{
    // UC15: xuất từng tab báo cáo ra file PDF (A4). Dùng đúng dữ liệu đang hiện trên màn hình.
    public static class ReportPdfExporter
    {
        private static readonly CultureInfo Vi = new("vi-VN");
        private const string Green = "#166534";
        private const string Muted = "#6B7280";

        static ReportPdfExporter()
        {
            QuestPDF.Settings.License = LicenseType.Community;     // miễn phí cho dự án học tập / quy mô nhỏ
            QuestPDF.Settings.UseSystemFonts = true;               // từ 2026.9 mặc định không dùng font hệ thống; app chỉ chạy trên Windows nên có sẵn Segoe UI (hỗ trợ tiếng Việt)
        }

        public static string Money(decimal amount) => amount.ToString("N0", Vi) + " ₫";
        public static string Percent(double? rate) => rate is double r ? (r * 100).ToString("0", Vi) + "%" : "–";

        // ---------- Tổng quan ----------

        public static void ExportOverview(string path, MonthOption month, OverviewDto o) =>
            OverviewDocument(month, o).GeneratePdf(path);

        public static Document OverviewDocument(MonthOption month, OverviewDto o)
        {
            var rows = new List<string[]>
            {
                new[] { "Học viên đang học (hiện tại)", o.ActiveMembers.ToString() },
                new[] { "Lớp đang chạy (hiện tại)", o.RunningClasses.ToString() },
                new[] { "Tỷ lệ lấp đầy chung (hiện tại)", Percent(o.FillRate) },
                new[] { $"Doanh thu tháng {month.Label}", Money(o.Revenue) },
                new[] { "Khoản đang chờ thanh toán (hiện tại)", o.PendingPaymentCount.ToString() },
                new[] { $"Buổi bị hủy trong tháng {month.Label}", o.CancelledSessions.ToString() },
            };
            return Compose("Báo cáo tổng quan", $"Tháng {month.Label}", col =>
            {
                col.Item().Element(c => Table(c, new[] { "Chỉ số", "Giá trị" }, new[] { 3f, 2f }, rows, rightFrom: 1));
            });
        }

        // ---------- Doanh thu ----------

        public static void ExportRevenue(string path, MonthOption month, RevenueReportDto r) =>
            RevenueDocument(month, r).GeneratePdf(path);

        public static Document RevenueDocument(MonthOption month, RevenueReportDto r)
        {
            return Compose("Báo cáo doanh thu", $"Tháng {month.Label} · Tổng: {Money(r.Total)}", col =>
            {
                Section(col, "Theo môn");
                col.Item().Element(c => Table(c, new[] { "Môn", "Số khoản", "Doanh thu" }, new[] { 3f, 1f, 2f },
                    r.BySport.Select(x => new[] { x.Name, x.PaymentCount.ToString(), Money(x.Amount) }), rightFrom: 1));
                Section(col, "Theo khóa");
                col.Item().Element(c => Table(c, new[] { "Khóa", "Số khoản", "Doanh thu" }, new[] { 3f, 1f, 2f },
                    r.ByCourse.Select(x => new[] { x.Name, x.PaymentCount.ToString(), Money(x.Amount) }), rightFrom: 1));
                Section(col, $"{ReportService.TrendMonths} tháng gần nhất");
                col.Item().Element(c => Table(c, new[] { "Tháng", "Doanh thu" }, new[] { 1f, 2f },
                    r.Trend.Select(x => new[] { x.Label, Money(x.Amount) }), rightFrom: 1));
                Section(col, "Khoản đang chờ thanh toán");
                col.Item().Element(c => Table(c, new[] { "Học viên", "Lớp", "Số tiền", "Đã chờ (ngày)" }, new[] { 2f, 2f, 2f, 1.2f },
                    r.Pending.Select(x => new[] { x.MemberName, x.ClassName, Money(x.Amount), x.DaysWaiting.ToString() }), rightFrom: 2));
            });
        }

        // ---------- Chuyên cần ----------

        public static void ExportAttendance(string path, MonthOption month, AttendanceReportDto a) =>
            AttendanceDocument(month, a).GeneratePdf(path);

        public static Document AttendanceDocument(MonthOption month, AttendanceReportDto a)
        {
            string[] heads = { "", "Có mặt", "Muộn", "Vắng", "Có phép", "Chuyên cần" };
            float[] widths = { 3f, 1f, 1f, 1f, 1f, 1.3f };
            string[] Row(AttendanceRowDto x) =>
                new[] { x.Detail is null ? x.Name : $"{x.Name} ({x.Detail})", x.Present.ToString(), x.Late.ToString(), x.Absent.ToString(), x.Excused.ToString(), Percent(x.Rate) };
            string[] With(string first) { var h = (string[])heads.Clone(); h[0] = first; return h; }

            return Compose("Báo cáo chuyên cần", $"Tháng {month.Label} · Toàn trung tâm: {Percent(a.Total.Rate)} ({a.Total.Counted} lượt)", col =>
            {
                col.Item().Text("Chuyên cần = (Có mặt + Muộn) / (Có mặt + Muộn + Vắng); vắng có phép không tính. Chỉ tính các buổi đã hoàn thành.")
                    .FontSize(9).FontColor(Muted);
                Section(col, "Theo môn");
                col.Item().Element(c => Table(c, With("Môn"), widths, a.BySport.Select(Row), rightFrom: 1));
                Section(col, "Theo coach");
                col.Item().Element(c => Table(c, With("Coach"), widths, a.ByCoach.Select(Row), rightFrom: 1));
                Section(col, $"Học viên cần chú ý (chuyên cần dưới {a.AtRiskThreshold * 100:0}%)");
                col.Item().Element(c => Table(c, With("Học viên (lớp)"), widths, a.AtRisk.Select(Row), rightFrom: 1));
            });
        }

        // ---------- Khung chung ----------

        private static Document Compose(string title, string subtitle, Action<ColumnDescriptor> content)
        {
            return Document.Create(doc => doc.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(36);
                page.DefaultTextStyle(t => t.FontFamily("Segoe UI").FontSize(10));

                page.Header().Column(h =>
                {
                    h.Item().Text("SportCenter").FontSize(10).FontColor(Muted);
                    h.Item().Text(title).FontSize(20).Bold().FontColor(Green);
                    h.Item().Text(subtitle).FontSize(11).FontColor(Muted);
                    h.Item().PaddingTop(6).LineHorizontal(1).LineColor(Green);
                });
                page.Content().PaddingVertical(10).Column(col => { col.Spacing(6); content(col); });
                page.Footer().Row(r =>
                {
                    r.RelativeItem().Text($"Xuất lúc {DateTime.Now:dd/MM/yyyy HH:mm}").FontSize(8).FontColor(Muted);
                    r.RelativeItem().AlignRight().Text(t =>
                    {
                        t.DefaultTextStyle(s => s.FontSize(8).FontColor(Muted));
                        t.Span("Trang ");
                        t.CurrentPageNumber();
                        t.Span(" / ");
                        t.TotalPages();
                    });
                });
            }));
        }

        private static void Section(ColumnDescriptor col, string title) =>
            col.Item().PaddingTop(10).Text(title).FontSize(13).Bold();

        // Bảng có tiêu đề; các cột từ rightFrom trở đi căn phải. Không có dòng nào thì in "Không có dữ liệu".
        private static void Table(IContainer container, string[] headers, float[] widths, IEnumerable<string[]> rows, int rightFrom)
        {
            var list = rows.ToList();
            if (list.Count == 0)
            {
                container.Text("Không có dữ liệu.").FontColor(Muted);
                return;
            }
            container.Table(table =>
            {
                table.ColumnsDefinition(c => { foreach (float w in widths) c.RelativeColumn(w); });
                table.Header(h =>
                {
                    for (int i = 0; i < headers.Length; i++)
                    {
                        var cell = h.Cell().Background("#DCFCE7").Padding(5);
                        (i >= rightFrom ? cell.AlignRight() : cell).Text(headers[i]).Bold().FontColor(Green);
                    }
                });
                foreach (var row in list)
                {
                    for (int i = 0; i < row.Length; i++)
                    {
                        var cell = table.Cell().BorderBottom(0.5f).BorderColor("#E5E7EB").Padding(5);
                        (i >= rightFrom ? cell.AlignRight() : cell).Text(row[i]);
                    }
                }
            });
        }
    }
}
