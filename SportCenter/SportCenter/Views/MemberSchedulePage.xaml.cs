using System.Windows;
using System.Windows.Controls;
using SportCenter.Services;

namespace SportCenter.Views;

// Học viên xem lịch học và kết quả điểm danh của mình.
public partial class MemberSchedulePage : UserControl
{
    private readonly MemberScheduleService _service = new();

    public MemberSchedulePage()
    {
        InitializeComponent();
        Loaded += async (_, _) => await LoadAsync();
    }

    private async Task LoadAsync()
    {
        try
        {
            var sessions = await _service.GetMySessionsAsync();
            SessionGrid.ItemsSource = sessions;
            EmptyText.Visibility = sessions.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        }
        catch (Exception ex)
        {
            string message = ex is UnauthorizedAccessException
                ? ex.Message
                : "Không tải được lịch học. Vui lòng thử lại.";
            MessageBox.Show(message, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }
}
