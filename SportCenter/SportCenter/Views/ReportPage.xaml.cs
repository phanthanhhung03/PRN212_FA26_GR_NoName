using Microsoft.Win32;
using SportCenter.Dtos;
using SportCenter.Services;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace SportCenter.Views
{
    // UC15: báo cáo cho Manager (tổng quan, doanh thu, chuyên cần) theo tháng, xuất PDF từng tab.
    public partial class ReportPage : UserControl
    {
        private static readonly System.Globalization.CultureInfo Vi = new("vi-VN");

        private readonly ReportService _service = new ReportService();
        private Snapshot? _snap;                        // dữ liệu đang hiện (cùng một tháng); null khi đang tải hoặc tải lỗi
        private int _loadVersion;                       // bỏ kết quả tải cũ về muộn khi đổi tháng liên tục
        private bool _exporting;

        private record Stat(string Value, string Label);
        private record Snapshot(MonthOption Month, OverviewDto Overview, RevenueReportDto Revenue, AttendanceReportDto Attendance);

        public ReportPage()
        {
            InitializeComponent();
            MonthBox.ItemsSource = ReportService.RecentMonths();
            Loaded += (_, _) => { if (MonthBox.SelectedIndex < 0) MonthBox.SelectedIndex = 0; };     // SelectionChanged tải dữ liệu
        }

        private async void MonthBox_SelectionChanged(object sender, SelectionChangedEventArgs e) => await LoadAsync();

        private void Tabs_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.OriginalSource == Tabs) UpdateExportEnabled();
        }

        private async Task LoadAsync()
        {
            if (MonthBox.SelectedItem is not MonthOption month) return;
            int version = ++_loadVersion;
            _snap = null;                               // đang tải: chưa xuất được
            UpdateExportEnabled();
            HideError();

            try
            {
                var overview = await _service.GetOverviewAsync(month.Year, month.Month);
                var revenue = await _service.GetRevenueAsync(month.Year, month.Month);
                var attendance = await _service.GetAttendanceAsync(month.Year, month.Month);
                if (version != _loadVersion) return;

                ShowOverview(overview, month);
                ShowRevenue(revenue);
                ShowAttendance(attendance);
                _snap = new Snapshot(month, overview, revenue, attendance);
            }
            catch (Exception ex)
            {
                if (version == _loadVersion) ShowError(ex);
                return;
            }
            UpdateExportEnabled();
        }

        // ---------- Hiển thị ----------

        private void ShowOverview(OverviewDto o, MonthOption month)
        {
            OverviewCards.ItemsSource = new[]
            {
                new Stat(o.ActiveMembers.ToString(), "Học viên đang học (hiện tại)"),
                new Stat(o.RunningClasses.ToString(), "Lớp đang chạy (hiện tại)"),
                new Stat(ReportPdfExporter.Percent(o.FillRate), "Lấp đầy chung của các lớp đang chạy (hiện tại)"),
                new Stat(ReportPdfExporter.Money(o.Revenue), $"Doanh thu tháng {month.Label}"),
                new Stat(o.PendingPaymentCount.ToString(), "Khoản đang chờ thanh toán (hiện tại)"),
                new Stat(o.CancelledSessions.ToString(), $"Buổi bị hủy trong tháng {month.Label}"),
            };
        }

        private void ShowRevenue(RevenueReportDto r)
        {
            RevenueTotal.Text = $"Tổng doanh thu của tháng: {ReportPdfExporter.Money(r.Total)}";
            RevenueSportGrid.ItemsSource = r.BySport;
            RevenueCourseGrid.ItemsSource = r.ByCourse;
            PendingGrid.ItemsSource = r.Pending;
            PendingEmpty.Visibility = r.Pending.Count == 0 ? Visibility.Visible : Visibility.Collapsed;

            // Biểu đồ thanh ngang đơn giản cho 6 tháng gần nhất
            TrendPanel.Children.Clear();
            decimal max = r.Trend.Max(t => t.Amount);
            foreach (var t in r.Trend)
            {
                double width = max == 0 ? 0 : (double)(t.Amount / max) * 320;
                var row = new DockPanel { Margin = new Thickness(0, 0, 0, 6) };
                row.Children.Add(new TextBlock { Text = t.Label, Width = 70, VerticalAlignment = VerticalAlignment.Center });
                var amount = new TextBlock
                {
                    Text = ReportPdfExporter.Money(t.Amount), Width = 140, TextAlignment = TextAlignment.Right,
                    VerticalAlignment = VerticalAlignment.Center
                };
                DockPanel.SetDock(amount, Dock.Right);
                row.Children.Add(amount);
                row.Children.Add(new Border
                {
                    Height = 16, Width = Math.Max(width, t.Amount > 0 ? 3 : 0), HorizontalAlignment = HorizontalAlignment.Left,
                    CornerRadius = new CornerRadius(4), Background = (Brush)FindResource("PrimaryBrush")
                });
                TrendPanel.Children.Add(row);
            }
        }

        private void ShowAttendance(AttendanceReportDto a)
        {
            AttendanceTotal.Text = a.Total.Counted == 0
                ? "Toàn trung tâm: chưa có dữ liệu điểm danh của các buổi đã hoàn thành trong tháng này."
                : $"Toàn trung tâm: {a.Total.RateText} chuyên cần ({a.Total.Counted} lượt điểm danh, {a.Total.Late} lượt muộn).";
            AttSportGrid.ItemsSource = a.BySport;
            AttCoachGrid.ItemsSource = a.ByCoach;
            AtRiskTitle.Text = $"Học viên cần chú ý (chuyên cần dưới {a.AtRiskThreshold * 100:0}%)";
            AtRiskGrid.ItemsSource = a.AtRisk;
            AtRiskEmpty.Visibility = a.AtRisk.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        }

        // Cuộn chuột trên bảng thì chuyển cho ScrollViewer của tab (các bảng không tự cuộn).
        private void Grid_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            e.Handled = true;
            var scroll = (Tabs.SelectedIndex == 1 ? RevenueScroll : AttendanceScroll);
            scroll.ScrollToVerticalOffset(scroll.VerticalOffset - e.Delta / 3.0);
        }

        // ---------- Xuất PDF ----------

        private void UpdateExportEnabled()
        {
            ExportButton.IsEnabled = !_exporting && _snap is not null;
        }

        private async void Export_Click(object sender, RoutedEventArgs e)
        {
            if (_exporting || _snap is not { } snap) return;       // snap: một bộ dữ liệu cùng tháng, chụp trước khi mở hộp thoại
            var (month, overview, revenue, attendance) = snap;

            (string key, string name) = Tabs.SelectedIndex switch
            {
                0 => ("TongQuan", "tổng quan"),
                1 => ("DoanhThu", "doanh thu"),
                _ => ("ChuyenCan", "chuyên cần")
            };
            var dialog = new SaveFileDialog
            {
                Title = $"Xuất báo cáo {name}",
                Filter = "PDF (*.pdf)|*.pdf",
                FileName = $"BaoCao_{key}_{month.Year}-{month.Month:00}.pdf",
                AddExtension = true,
                OverwritePrompt = true
            };
            if (dialog.ShowDialog() != true) return;

            int tab = Tabs.SelectedIndex;
            string path = dialog.FileName;
            if (!path.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase)) path += ".pdf";

            _exporting = true;
            ExportButton.IsEnabled = false;
            ExportButton.Content = "Đang xuất…";
            HideError();
            try
            {
                await Task.Run(() =>
                {
                    if (tab == 0) ReportPdfExporter.ExportOverview(path, month, overview);
                    else if (tab == 1) ReportPdfExporter.ExportRevenue(path, month, revenue);
                    else ReportPdfExporter.ExportAttendance(path, month, attendance);
                });
                Notice.Show("Đã xuất PDF.");
            }
            catch (System.IO.IOException)
            {
                ShowMessage("Không ghi được file. Hãy đóng file nếu đang mở hoặc chọn nơi lưu khác.");
            }
            catch (UnauthorizedAccessException)
            {
                ShowMessage("Không có quyền ghi vào nơi đã chọn. Hãy chọn thư mục khác.");
            }
            catch (Exception)
            {
                ShowMessage("Không xuất được PDF. Vui lòng thử lại.");
            }
            finally
            {
                _exporting = false;
                ExportButton.Content = "Xuất PDF";
                UpdateExportEnabled();
            }
        }

        // ---------- Tiện ích ----------

        private void ShowError(Exception ex) => ShowMessage(ex switch
        {
            InvalidOperationException or UnauthorizedAccessException => ex.Message,
            System.Data.Common.DbException => "Không kết nối được máy chủ. Vui lòng thử lại.",
            _ => "Đã xảy ra lỗi không mong muốn. Vui lòng thử lại."
        });

        private void ShowMessage(string message)
        {
            ErrorText.Text = message;
            ErrorBanner.Visibility = Visibility.Visible;
        }

        private void HideError() => ErrorBanner.Visibility = Visibility.Collapsed;
    }
}
