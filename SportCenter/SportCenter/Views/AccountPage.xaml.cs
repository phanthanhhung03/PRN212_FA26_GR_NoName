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
    // UC14: danh sách tài khoản + tạo / sửa / khóa / đặt lại mật khẩu (Manager).
    public partial class AccountPage : UserControl
    {
        private const string AllRoles = "Tất cả vai trò";
        private const string AllStatuses = "Mọi trạng thái";
        private const string ActiveStatus = "Hoạt động";
        private const string LockedStatus = "Đã khóa";

        private readonly AccountService _service = new AccountService();
        private List<AccountDto> _all = new();
        private bool _busy;                             // đang ghi: chặn bấm chồng
        private int _loadVersion;                       // bỏ kết quả tải cũ về muộn
        private bool _ready;                            // đã gán bộ lọc: bỏ qua SelectionChanged lúc khởi tạo

        public AccountPage()
        {
            InitializeComponent();
            RoleFilter.ItemsSource = new[] { AllRoles, "Manager", "Staff", "Coach", "Member" };
            StatusFilter.ItemsSource = new[] { AllStatuses, ActiveStatus, LockedStatus };
            RoleFilter.SelectedIndex = 0;
            StatusFilter.SelectedIndex = 0;
            _ready = true;
            Loaded += async (_, _) => await LoadAsync();
        }

        private async Task LoadAsync()
        {
            int version = ++_loadVersion;
            List<AccountDto> accounts;
            try
            {
                accounts = await _service.GetAccountsAsync();
            }
            catch (Exception ex)
            {
                if (version == _loadVersion) ShowError(ex);
                return;
            }
            if (version != _loadVersion) return;
            _all = accounts;
            ApplyFilter();
        }

        private void Filter_Changed(object sender, SelectionChangedEventArgs e) => ApplyFilter();

        private void Filter_TextChanged(object sender, TextChangedEventArgs e) => ApplyFilter();

        private void ApplyFilter()
        {
            if (!_ready) return;
            string role = (RoleFilter.SelectedItem as string) ?? AllRoles;
            string status = (StatusFilter.SelectedItem as string) ?? AllStatuses;
            string text = SearchBox.Text.Trim();

            IEnumerable<AccountDto> rows = _all;
            if (role != AllRoles) rows = rows.Where(a => a.RoleName == role);
            if (status == ActiveStatus) rows = rows.Where(a => a.IsActive);
            else if (status == LockedStatus) rows = rows.Where(a => !a.IsActive);
            if (text.Length > 0)
            {
                rows = rows.Where(a => Contains(a.FullName, text) || Contains(a.Email, text) || Contains(a.Phone, text));
            }

            var list = rows.ToList();
            AccountGrid.ItemsSource = list;
            EmptyText.Visibility = list.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        }

        private static bool Contains(string? source, string text) =>
            source is not null && source.Contains(text, StringComparison.CurrentCultureIgnoreCase);

        // ---------- Thao tác ----------

        private async void New_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new AccountDialog(null) { Owner = Window.GetWindow(this) };
            if (dlg.ShowDialog() != true) return;
            HideError();
            await LoadAsync();
            Notice.Show("Đã tạo tài khoản.");
        }

        private async void Edit_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as FrameworkElement)?.DataContext is not AccountDto account) return;
            var dlg = new AccountDialog(account) { Owner = Window.GetWindow(this) };
            if (dlg.ShowDialog() != true) return;
            HideError();
            await LoadAsync();
            Notice.Show("Đã lưu tài khoản.");
        }

        private async void ToggleLock_Click(object sender, RoutedEventArgs e)
        {
            if (_busy || (sender as FrameworkElement)?.DataContext is not AccountDto account) return;
            bool lockIt = account.IsActive;

            if (lockIt)
            {
                string message = $"Tài khoản “{account.FullName}” sẽ không đăng nhập được cho tới khi mở khóa.";
                if (account.HasWork)
                {
                    message += $"\n\nNgười này đang phụ trách {account.ActiveClassCount} lớp chưa kết thúc và có {account.UpcomingSessionCount} buổi sắp tới. " +
                               "Các buổi vẫn giữ nguyên người dạy, bạn nên đổi coach cho các buổi đó.";
                }
                if (!ConfirmDialog.Ask(Window.GetWindow(this), "Khóa tài khoản", message, "Khóa tài khoản")) return;
            }

            _busy = true;
            try
            {
                HideError();
                await _service.SetLockedAsync(account.UserId, lockIt);
                await LoadAsync();
                Notice.Show(lockIt ? "Đã khóa tài khoản." : "Đã mở khóa tài khoản.");
            }
            catch (Exception ex)
            {
                await LoadAsync();                                    // dữ liệu có thể đã đổi bởi người khác
                ShowError(ex);
            }
            finally
            {
                _busy = false;
            }
        }

        private void ResetPassword_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as FrameworkElement)?.DataContext is not AccountDto account) return;
            var dlg = new ResetPasswordDialog(account) { Owner = Window.GetWindow(this) };
            if (dlg.ShowDialog() == true) Notice.Show("Đã đặt lại mật khẩu.");
        }

        // ---------- Tiện ích ----------

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
    }

    // IsActive true -> "Hoạt động", false -> "Đã khóa"
    public class LockedTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
            value is true ? "Hoạt động" : "Đã khóa";

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
            throw new NotSupportedException();
    }

    // IsActive true -> nút "Khóa", false -> nút "Mở khóa"
    public class LockToggleTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
            value is true ? "Khóa" : "Mở khóa";

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
            throw new NotSupportedException();
    }
}
