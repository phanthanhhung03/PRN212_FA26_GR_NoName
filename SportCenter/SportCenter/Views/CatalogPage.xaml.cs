using SportCenter.Dtos;
using SportCenter.Services;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace SportCenter.Views
{
    // UC11: quản lý môn (cột trái) và các khóa của môn đang chọn (cột phải).
    public partial class CatalogPage : UserControl
    {
        private readonly CatalogService _service = new CatalogService();
        private SportManageDto? _sport;                 // môn đang chọn
        private int _coursesVersion;                    // bỏ kết quả tải khóa cũ khi người dùng đã chọn môn khác
        private bool _busy;                             // đang ghi: chặn bấm chồng

        public CatalogPage()
        {
            InitializeComponent();
            Loaded += async (_, _) => await LoadSportsAsync(null);
        }

        // ---------- Tải dữ liệu ----------

        private async Task LoadSportsAsync(int? selectSportId)
        {
            List<SportManageDto> sports;
            try
            {
                sports = await _service.GetSportsAsync();
            }
            catch (Exception ex)
            {
                ShowError(ex);
                return;
            }

            int? keep = selectSportId ?? _sport?.SportId;
            SportList.ItemsSource = sports;
            SportList.SelectedItem = sports.FirstOrDefault(s => s.SportId == keep) ?? sports.FirstOrDefault();

            bool any = sports.Count > 0;
            EmptyText.Visibility = any ? Visibility.Collapsed : Visibility.Visible;
            DetailPanel.Visibility = any ? Visibility.Visible : Visibility.Collapsed;
            if (!any) _sport = null;
            else await ShowSportAsync();                              // SportManageDto là record: chọn lại môn "bằng nhau" thì SelectionChanged không bắn, nên tải lại thẳng
        }

        private async void SportList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (SportList.SelectedItem is SportManageDto) await ShowSportAsync();
        }

        // Hiện thông tin môn đang chọn và tải các khóa của nó.
        private async Task ShowSportAsync()
        {
            if (SportList.SelectedItem is not SportManageDto sport) return;
            _sport = sport;
            HideError();

            SportTitle.Text = sport.SportName;
            SportPillText.Text = sport.IsActive ? "Hoạt động" : "Đã ngừng";
            SportPill.DataContext = sport;
            SportPillText.DataContext = sport;
            SportDescription.Text = sport.Description ?? "";
            SportDescription.Visibility = string.IsNullOrEmpty(sport.Description) ? Visibility.Collapsed : Visibility.Visible;
            ToggleSportButton.Content = sport.IsActive ? "Ngừng môn" : "Kích hoạt lại";
            ToggleSportButton.Style = (Style)FindResource(sport.IsActive ? "DangerButton" : "SecondaryButton");
            AddCourseButton.IsEnabled = sport.IsActive;

            CourseGrid.ItemsSource = null;                            // không để lộ khóa của môn trước trong lúc tải
            NoCourseText.Visibility = Visibility.Collapsed;
            int version = ++_coursesVersion;
            List<CourseManageDto> courses;
            try
            {
                courses = await _service.GetCoursesAsync(sport.SportId);
            }
            catch (Exception ex)
            {
                if (version == _coursesVersion) ShowError(ex);
                return;
            }
            if (version != _coursesVersion) return;
            CourseGrid.ItemsSource = courses;
            NoCourseText.Visibility = courses.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        }

        // ---------- Môn ----------

        private async void AddSport_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new SportDialog(null) { Owner = Window.GetWindow(this) };
            if (dlg.ShowDialog() != true) return;
            await LoadSportsAsync(dlg.SavedSportId);
            Notice.Show("Đã thêm môn.");
        }

        private async void EditSport_Click(object sender, RoutedEventArgs e)
        {
            if (_sport is null) return;
            var dlg = new SportDialog(_sport) { Owner = Window.GetWindow(this) };
            if (dlg.ShowDialog() != true) return;
            await LoadSportsAsync(_sport.SportId);
            Notice.Show("Đã lưu môn.");
        }

        private async void ToggleSport_Click(object sender, RoutedEventArgs e)
        {
            if (_sport is null || _busy) return;
            SportManageDto sport = _sport;
            bool activate = !sport.IsActive;

            if (!activate)
            {
                string message = $"Môn “{sport.SportName}” sẽ không mở được lớp mới và các khóa của môn không còn dùng để mở lớp.";
                if (sport.ActiveClassCount > 0)
                {
                    message += $"\n\nĐang có {sport.ActiveClassCount} lớp tuyển sinh hoặc đang học. Các lớp này vẫn chạy bình thường, không bị ảnh hưởng.";
                }
                if (!ConfirmDialog.Ask(Window.GetWindow(this), "Ngừng môn", message, "Ngừng môn")) return;
            }

            await RunAsync(() => _service.SetSportActiveAsync(sport.SportId, activate),
                           () => LoadSportsAsync(sport.SportId),
                           activate ? "Đã kích hoạt lại môn." : "Đã ngừng môn.");
        }

        // ---------- Khóa ----------

        private async void AddCourse_Click(object sender, RoutedEventArgs e)
        {
            if (_sport is null) return;
            var dlg = new CourseDialog(_sport, null) { Owner = Window.GetWindow(this) };
            if (dlg.ShowDialog() != true) return;
            await LoadSportsAsync(_sport.SportId);                    // số khóa của môn đổi: tải lại cả hai bên
            Notice.Show("Đã thêm khóa.");
        }

        private async void EditCourse_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as FrameworkElement)?.DataContext is CourseManageDto course) await EditCourseAsync(course);
        }

        private async void CourseGrid_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            // Chỉ khi nhấp đúp vào một dòng dữ liệu (không phải tiêu đề, không phải nút)
            if (e.OriginalSource is DependencyObject src && FindParent<Button>(src) is null
                && FindParent<DataGridRow>(src) is { Item: CourseManageDto course })
            {
                await EditCourseAsync(course);
            }
        }

        private async Task EditCourseAsync(CourseManageDto course)
        {
            if (_sport is null) return;
            var dlg = new CourseDialog(_sport, course) { Owner = Window.GetWindow(this) };
            if (dlg.ShowDialog() != true) return;
            await LoadSportsAsync(_sport.SportId);
            Notice.Show("Đã lưu khóa.");
        }

        private async void ToggleCourse_Click(object sender, RoutedEventArgs e)
        {
            if (_sport is null || _busy || (sender as FrameworkElement)?.DataContext is not CourseManageDto course) return;
            bool activate = !course.IsActive;

            if (!activate)
            {
                string message = $"Khóa “{course.CourseName}” sẽ không dùng để mở lớp mới được nữa.";
                if (course.ActiveClassCount > 0)
                {
                    message += $"\n\nĐang có {course.ActiveClassCount} lớp tuyển sinh hoặc đang học. Các lớp này vẫn chạy bình thường, không bị ảnh hưởng.";
                }
                if (!ConfirmDialog.Ask(Window.GetWindow(this), "Ngừng khóa", message, "Ngừng khóa")) return;
            }

            int sportId = _sport.SportId;
            await RunAsync(() => _service.SetCourseActiveAsync(course.CourseId, activate),
                           () => LoadSportsAsync(sportId),
                           activate ? "Đã kích hoạt lại khóa." : "Đã ngừng khóa.");
        }

        // ---------- Tiện ích ----------

        // Ghi rồi tải lại; lỗi hiện ở thanh lỗi. Dữ liệu luôn được tải lại để phản ánh thay đổi của người khác.
        private async Task RunAsync(Func<Task> write, Func<Task> reload, string successMessage)
        {
            _busy = true;
            try
            {
                HideError();
                await write();
                await reload();
                Notice.Show(successMessage);
            }
            catch (Exception ex)
            {
                await reload();                                       // tải lại trước: ShowSportAsync xóa thanh lỗi
                ShowError(ex);
            }
            finally
            {
                _busy = false;
            }
        }

        private void ShowError(Exception ex)
        {
            ErrorText.Text = ex switch
            {
                InvalidOperationException or UnauthorizedAccessException => ex.Message,
                System.Data.Common.DbException => "Không kết nối được máy chủ. Vui lòng thử lại.",
                _ => "Đã xảy ra lỗi không mong muốn. Vui lòng thử lại."
            };
            ErrorBanner.Visibility = Visibility.Visible;
        }

        private void HideError() => ErrorBanner.Visibility = Visibility.Collapsed;

        private static T? FindParent<T>(DependencyObject child) where T : DependencyObject
        {
            for (DependencyObject? d = child; d is not null; d = System.Windows.Media.VisualTreeHelper.GetParent(d) ?? (d as FrameworkContentElement)?.Parent)
            {
                if (d is T t) return t;
            }
            return null;
        }
    }

    // true -> "Hoạt động", false -> "Đã ngừng"
    public class ActiveTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
            value is true ? "Hoạt động" : "Đã ngừng";

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
            throw new NotSupportedException();
    }

    // IsActive true -> nút "Ngừng", false -> nút "Kích hoạt"
    public class ToggleTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
            value is true ? "Ngừng" : "Kích hoạt";

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
            throw new NotSupportedException();
    }
}
