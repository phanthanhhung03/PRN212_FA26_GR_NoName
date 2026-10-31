using System.Windows;
using System.Windows.Controls;
using SportCenter.Services;

namespace SportCenter.Views;

// Học viên xem các môn và khóa học đang mở.
public partial class MemberCatalogPage : UserControl
{
    private readonly MemberCatalogService _service = new();

    public MemberCatalogPage()
    {
        InitializeComponent();
        Loaded += async (_, _) => await LoadAsync();
    }

    private async Task LoadAsync()
    {
        try
        {
            var sports = await _service.GetCatalogAsync();
            SportList.ItemsSource = sports;
            EmptyText.Visibility = sports.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        }
        catch (Exception ex)
        {
            string message = ex is UnauthorizedAccessException
                ? ex.Message
                : "Không tải được danh sách môn và khóa học. Vui lòng thử lại.";
            MessageBox.Show(message, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }
}