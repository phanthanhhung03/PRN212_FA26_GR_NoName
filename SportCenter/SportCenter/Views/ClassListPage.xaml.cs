using Microsoft.EntityFrameworkCore;
using SportCenter.Entities;
using SportCenter.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace SportCenter.Views
{
    public partial class ClassListPage : UserControl
    {
        private readonly ClassService _service = new ClassService();
        private bool _loading;
        public event EventHandler? NewClassRequested;
        public event EventHandler<int>? ClassDetailRequested;        // tham số: class_id của lớp muốn xem

        public ClassListPage()
        {
            InitializeComponent();
            Loaded += async (_, _) => await InitAsync();
        }

        private async Task InitAsync()
        {
            _loading = true;
            try
            {
                var sports = await _service.GetSportsAsync();
                sports.Insert(0, new Dtos.SportOptionDto(0, "Tất cả các môn"));
                SportFilter.ItemsSource = sports;
                SportFilter.SelectedIndex = 0;


                var statuses = await _service.GetClassStatusesAsync();
                statuses.Insert(0, "Tất cả trạng thái");
                StatusFilter.ItemsSource = statuses;
                StatusFilter.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                ShowError(ex);
                return;
            }
            finally
            {
                _loading = false;
            }

            await LoadClassesAsync();
        }

        private async Task LoadClassesAsync()
        {
            int? sportId = SportFilter.SelectedValue is int id && id != 0 ? id : null;
            string? status = StatusFilter.SelectedIndex > 0 ? StatusFilter.SelectedItem.ToString() : null;

            try
            {
                ClassGrid.ItemsSource = await _service.GetClassAsync(sportId, status);
            }
            catch (Exception ex)
            {
                ShowError(ex);
            }
        }

        private async void Filter_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (_loading) return;
            await LoadClassesAsync();
        }

        private static void ShowError(Exception ex)
        {
            string message = ex is UnauthorizedAccessException
                ? ex.Message
                : "Không tải được danh sách lớp. Vui lòng thử lại.";
            MessageBox.Show(message, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        private void NewClassButton_Click(object sender, RoutedEventArgs e) => NewClassRequested?.Invoke(this, EventArgs.Empty);

        // Bấm nút "Xem lịch" ở cuối dòng: lấy lớp của đúng dòng đó.
        private void ViewSchedule_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement { DataContext: Dtos.ClassListItemDto item })
            {
                ClassDetailRequested?.Invoke(this, item.ClassId);
            }
        }

        // Bấm đúp vào một dòng dữ liệu (không phải tiêu đề, thanh cuộn hay chỗ trống) để xem lịch.
        private void ClassGrid_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            DependencyObject? source = e.OriginalSource as DependencyObject;
            // Dừng khi gặp nút (đã có Click riêng) hoặc tiêu đề cột; chỉ đi lên theo cây hiển thị (Run, Hyperlink... không có cha hiển thị).
            while (source is System.Windows.Media.Visual
                   && source is not DataGridRow
                   && source is not System.Windows.Controls.Primitives.DataGridColumnHeader
                   && source is not System.Windows.Controls.Primitives.ButtonBase)
            {
                source = System.Windows.Media.VisualTreeHelper.GetParent(source);
            }

            if (source is DataGridRow { Item: Dtos.ClassListItemDto item })
            {
                ClassDetailRequested?.Invoke(this, item.ClassId);
            }
        }

        // Hiện thông báo ngắn ở đầu trang, tự ẩn sau 5 giây (hoặc bấm ✕).
        private readonly System.Windows.Threading.DispatcherTimer _noticeTimer = new() { Interval = TimeSpan.FromSeconds(5) };

        public void ShowNotice(string message)
        {
            NoticeText.Text = message;
            NoticeBanner.Visibility = Visibility.Visible;

            _noticeTimer.Stop();
            _noticeTimer.Tick -= NoticeTimer_Tick;
            _noticeTimer.Tick += NoticeTimer_Tick;
            _noticeTimer.Start();
        }

        private void NoticeTimer_Tick(object? sender, EventArgs e) => HideNotice();

        private void NoticeClose_Click(object sender, RoutedEventArgs e) => HideNotice();

        private void HideNotice()
        {
            _noticeTimer.Stop();
            NoticeBanner.Visibility = Visibility.Collapsed;
        }
    }
}
