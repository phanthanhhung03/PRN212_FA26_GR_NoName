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
            ClassGrid.ItemsSource = await _catalogService.GetOpenClassesAsync(sportId, courseId);
        }
        catch (Exception ex)
        {
            Notice.Show("Không tải được danh sách lớp: " + ex.Message);
        }
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
