using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using SportCenter.Dtos;
using SportCenter.Services;
namespace SportCenter.Views;
/// <summary>UC10: Staff ghi danh hộ bằng cùng stored procedure với UC05.</summary>
public partial class StaffEnrollmentPage : UserControl
{
    private readonly EnrollmentService _enrollmentService = new();
    private readonly PaymentService _paymentService = new();
    public StaffEnrollmentPage() { InitializeComponent(); Loaded += async (_, _) => await LoadClassesAsync(); }
    private async Task LoadClassesAsync() { try { ClassGrid.ItemsSource = await _enrollmentService.GetEnrollingClassesAsync(); } catch (Exception ex) { ShowError(ex); } }
    private async void Search_Click(object sender, RoutedEventArgs e) => await SearchAsync();
    private async void SearchBox_KeyDown(object sender, KeyEventArgs e) { if (e.Key == Key.Enter) await SearchAsync(); }
    private async Task SearchAsync() { try { MemberGrid.ItemsSource = await _enrollmentService.SearchMembersAsync(SearchBox.Text); } catch (Exception ex) { ShowError(ex); } }
    private async void Enroll_Click(object sender, RoutedEventArgs e)
    {
        if (MemberGrid.SelectedItem is not MemberLookupDto member) { MessageBox.Show("Hãy tìm và chọn một học viên trước.", "Thiếu học viên", MessageBoxButton.OK, MessageBoxImage.Information); return; }
        if (sender is not Button { Tag: int classId }) return;
        try
        {
            int enrollmentId = await _enrollmentService.EnrollMemberByStaffAsync(classId, member.UserId);
            var payment = await _paymentService.CreatePendingPaymentAsync(enrollmentId);
            MessageBox.Show($"Đã ghi danh {member.FullName} thành công. Giao dịch đang Pending.", "Ghi danh thành công", MessageBoxButton.OK, MessageBoxImage.Information);
            var paymentWindow = new PaymentWindow(payment) { Owner = Window.GetWindow(this) };
            paymentWindow.ShowDialog();
            await LoadClassesAsync();
        }
        catch (Exception ex) { ShowError(ex); }
    }
    private static void ShowError(Exception ex) => MessageBox.Show(ex.Message, "Không thể ghi danh", MessageBoxButton.OK, MessageBoxImage.Warning);
}
