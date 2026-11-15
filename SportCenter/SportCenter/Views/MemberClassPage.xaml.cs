using System.Windows;
using System.Windows.Controls;
using SportCenter.Dtos;
using SportCenter.Services;

namespace SportCenter.Views;

// Học viên xem các lớp đang tuyển sinh, lọc theo môn và khóa.
public partial class MemberClassPage : UserControl
{
    private readonly MemberCatalogService _service = new();
    private List<MemberSportDto> _sports = new();
    private bool _loading;           // đang đổ dữ liệu vào ô lọc: bỏ qua SelectionChanged

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
            _sports = await _service.GetCatalogAsync();

            // Phần tử đầu là "Tất cả": Id = 0 nghĩa là không lọc.
            var sportItems = new List<MemberSportDto> { new(0, "Tất cả các môn", null, new()) };
            sportItems.AddRange(_sports);
            SportFilter.ItemsSource = sportItems;
            SportFilter.SelectedIndex = 0;

            FillCourseFilter(0);
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

    // Ô khóa chỉ liệt kê khóa của môn đang chọn (môn 0 = tất cả các khóa).
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
            ClassGrid.ItemsSource = await _service.GetOpenClassesAsync(sportId, courseId);
        }
        catch (Exception ex)
        {
            ShowError(ex);
        }
    }

    private async void SportFilter_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (_loading || SportFilter.SelectedItem is not MemberSportDto sport) return;

        _loading = true;                       // đổi ô khóa sẽ bắn sự kiện: chặn lại
        FillCourseFilter(sport.SportId);
        _loading = false;

        await LoadClassesAsync();
    }

    private async void CourseFilter_Changed(object sender, SelectionChangedEventArgs e)
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

    private static void ShowEnrollError(Exception ex)
    {
        string message = ex switch
        {
            // Lỗi nghiệp vụ từ EnrollmentService/PaymentService (đã có tiếng Việt):
            // "đã đăng ký lớp", "lớp hết chỗ", "lớp không mở"...
            InvalidOperationException => ex.Message,
            UnauthorizedAccessException => ex.Message,
            _ => "Không ghi danh được. Vui lòng thử lại."
        };
        MessageBox.Show(message, "Ghi danh", MessageBoxButton.OK, MessageBoxImage.Warning);
    }

    private readonly EnrollmentService _enrollment = new();
    private readonly PaymentService _payment = new();
    private bool _busy;

    private async void Enroll_Click(object sender, RoutedEventArgs e)
    {
        if (_busy || sender is not FrameworkElement { Tag: int classId }) return;

        var ok = MessageBox.Show("Đăng ký lớp này? Học phí sẽ ở trạng thái chờ thanh toán.",
            "Xác nhận ghi danh", MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (ok != MessageBoxResult.Yes) return;

        _busy = true;
        try
        {
            int enrollmentId = await _enrollment.EnrollCurrentMemberAsync(classId);
            var payment = await _payment.CreatePendingPaymentAsync(enrollmentId);

            bool? paid = new PaymentWindow(payment) { Owner = Window.GetWindow(this) }.ShowDialog();

            if (paid != true)
            {
                // đóng X / bấm Fail / không trả => nhả chỗ
                try { await _payment.ConfirmMockPaymentAsync(payment.PaymentId, false); }
                catch { /* đã Cancelled rồi (vd bấm Fail) thì SP ném 5100x, bỏ qua */ }
            }
            await LoadClassesAsync();
        }
        catch (Exception ex)
        {
            ShowEnrollError(ex);
            await LoadClassesAsync();
        }
        finally { _busy = false; }
    }
}
