using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using SportCenter.Dtos;
using SportCenter.Services;

namespace SportCenter.Views;

// UC04 & UC05: Học viên xem các lớp đang tuyển sinh và tự đăng ký lớp học.
public partial class MemberClassPage : UserControl
{
    private readonly MemberCatalogService _catalogService = new();
    private readonly EnrollmentService _enrollmentService = new();
    private List<MemberSportDto> _sports = new();
    private List<MemberClassDto> _rawClasses = new();
    private bool _loading;

    public MemberClassPage()
    {
        InitializeComponent();
        Loaded += async (_, _) => await InitAsync();
    }

    private async Task InitAsync()
    {
        _loading = true;
        try
        {
            _sports = await _catalogService.GetCatalogAsync();

            var sportItems = new List<MemberSportDto> { new(0, "Tất cả các môn", null, new()) };
            sportItems.AddRange(_sports);
            SportFilter.ItemsSource = sportItems;
            SportFilter.SelectedIndex = 0;

            FillCourseFilter(0);
        }
        catch (Exception ex)
        {
            Notice.Show("Lỗi tải danh mục: " + ex.Message);
            return;
        }
        finally
        {
            _loading = false;
        }

        await LoadClassesAsync();
    }

    private void FillCourseFilter(int sportId)
    {
        var courses = _sports
            .Where(s => sportId == 0 || s.SportId == sportId)
            .SelectMany(s => s.Courses)
            .ToList();
        courses.Insert(0, new MemberCourseDto(0, "Tất cả các khóa", null, 0, 0));
        CourseFilter.ItemsSource = courses;
        CourseFilter.SelectedIndex = 0;
    }

    private async Task LoadClassesAsync()
    {
        int? sportId = SportFilter.SelectedItem is MemberSportDto s && s.SportId != 0 ? s.SportId : null;
        int? courseId = CourseFilter.SelectedItem is MemberCourseDto c && c.CourseId != 0 ? c.CourseId : null;

        try
        {
            _rawClasses = await _catalogService.GetOpenClassesAsync(sportId, courseId);
            ApplyFilter();
        }
        catch (Exception ex)
        {
            Notice.Show("Không tải được danh sách lớp: " + ex.Message);
        }
    }

    private void ApplyFilter()
    {
        if (_rawClasses == null) return;

        var filtered = _rawClasses.AsEnumerable();

        // 1. Lọc theo từ khóa tìm kiếm
        string keyword = SearchBox?.Text?.Trim() ?? "";
        if (!string.IsNullOrEmpty(keyword))
        {
            filtered = filtered.Where(c =>
                (c.ClassName != null && c.ClassName.Contains(keyword, StringComparison.OrdinalIgnoreCase)) ||
                (c.SportName != null && c.SportName.Contains(keyword, StringComparison.OrdinalIgnoreCase)) ||
                (c.CourseName != null && c.CourseName.Contains(keyword, StringComparison.OrdinalIgnoreCase)) ||
                (c.Level != null && c.Level.Contains(keyword, StringComparison.OrdinalIgnoreCase)) ||
                (c.CoachName != null && c.CoachName.Contains(keyword, StringComparison.OrdinalIgnoreCase)) ||
                (c.RoomName != null && c.RoomName.Contains(keyword, StringComparison.OrdinalIgnoreCase)) ||
                (c.ScheduleText != null && c.ScheduleText.Contains(keyword, StringComparison.OrdinalIgnoreCase)));
        }

        // 2. Lọc theo trạng thái chỗ / đăng ký
        if (AvailabilityFilter != null)
        {
            filtered = AvailabilityFilter.SelectedIndex switch
            {
                1 => filtered.Where(c => c.SeatsLeft > 0 && !c.IsEnrolled), // Chỉ lớp còn chỗ (chưa đăng ký)
                2 => filtered.Where(c => c.SeatsLeft == 0),                  // Lớp đã hết chỗ (SYS01)
                3 => filtered.Where(c => c.IsEnrolled),                      // Lớp đã đăng ký
                _ => filtered                                                // Tất cả
            };
        }

        var resultList = filtered.ToList();
        ClassGrid.ItemsSource = resultList;

        if (ResultCountText != null)
        {
            ResultCountText.Text = $"Tìm thấy {resultList.Count} lớp";
        }
    }

    private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        string text = SearchBox.Text;
        if (SearchPlaceholder != null)
        {
            SearchPlaceholder.Visibility = string.IsNullOrEmpty(text) ? Visibility.Visible : Visibility.Collapsed;
        }
        if (ClearSearchBtn != null)
        {
            ClearSearchBtn.Visibility = string.IsNullOrEmpty(text) ? Visibility.Collapsed : Visibility.Visible;
        }

        ApplyFilter();
    }

    private void ClearSearch_Click(object sender, RoutedEventArgs e)
    {
        SearchBox.Text = string.Empty;
        SearchBox.Focus();
    }

    private void AvailabilityFilter_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (_loading) return;
        ApplyFilter();
    }

    private async void SportFilter_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (_loading || SportFilter.SelectedItem is not MemberSportDto sport) return;

        _loading = true;
        FillCourseFilter(sport.SportId);
        _loading = false;

        await LoadClassesAsync();
    }

    private async void CourseFilter_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (_loading) return;
        await LoadClassesAsync();
    }

    private async void Reload_Click(object sender, RoutedEventArgs e)
    {
        await LoadClassesAsync();
        Notice.Show("Đã làm mới danh sách lớp.");
    }

    private async void ResetFilters_Click(object sender, RoutedEventArgs e)
    {
        _loading = true;
        SearchBox.Text = string.Empty;
        if (SportFilter.Items.Count > 0) SportFilter.SelectedIndex = 0;
        FillCourseFilter(0);
        if (AvailabilityFilter != null) AvailabilityFilter.SelectedIndex = 0;
        _loading = false;

        await LoadClassesAsync();
        Notice.Show("Đã xóa bộ lọc và làm mới danh sách.");
    }

    private async void EnrollButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { DataContext: MemberClassDto item }) return;

        var confirm = MessageBox.Show(
            $"Bạn có chắc chắn muốn đăng ký tham gia lớp:\n\n" +
            $"• Lớp: {item.ClassName} ({item.SportName})\n" +
            $"• Khóa: {item.CourseName}\n" +
            $"• Khai giảng: {item.StartDate:dd/MM/yyyy}\n" +
            $"• Giờ học: {item.ScheduleText}\n" +
            $"• Học phí: {item.TuitionFee:N0} ₫\n\n" +
            $"Sau khi xác nhận, thông tin lớp học sẽ xuất hiện trong lịch học của bạn.",
            "Xác nhận đăng ký lớp học",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (confirm != MessageBoxResult.Yes) return;

        try
        {
            await _enrollmentService.EnrollSelfAsync(item.ClassId);
            Notice.Show($"Đăng ký thành công lớp {item.ClassName}!");
            await LoadClassesAsync();
        }
        catch (Exception ex)
        {
            Notice.Show("Lỗi đăng ký: " + ex.Message);
        }
    }
}
