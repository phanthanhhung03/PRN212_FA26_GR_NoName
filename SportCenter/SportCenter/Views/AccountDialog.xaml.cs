using SportCenter.Dtos;
using SportCenter.Services;
using System;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace SportCenter.Views
{
    // Hộp thoại tạo (existing = null) hoặc sửa một tài khoản Staff / Coach. DialogResult = true khi đã lưu.
    public partial class AccountDialog : Window
    {
        private readonly AccountService _service = new AccountService();
        private readonly AccountDto? _existing;
        private bool _saving;
        private bool _loaded;                           // đã tải xong danh sách môn

        public AccountDialog(AccountDto? existing)
        {
            InitializeComponent();
            _existing = existing;
            RoleBox.ItemsSource = AccountService.CreatableRoles;

            if (existing is null)
            {
                RoleBox.SelectedIndex = 0;
            }
            else
            {
                Title = "Sửa tài khoản";
                TitleText.Text = "Sửa tài khoản";
                NameBox.Text = existing.FullName;
                EmailBox.Text = existing.Email;
                EmailBox.IsEnabled = false;                          // email đổi ở Hồ sơ cá nhân của chính người đó
                PhoneBox.Text = existing.Phone ?? "";
                RoleBox.SelectedItem = existing.RoleName;
                PasswordPanel.Visibility = Visibility.Collapsed;
                RoleBox.IsEnabled = false;                           // role Staff / Coach không đổi sau khi tạo
                if (existing.HasWork) RoleNote.Visibility = Visibility.Visible;     // coach đang có lớp / buổi: khóa cả môn
            }
            Loaded += async (_, _) => await LoadSportsAsync();
        }

        private async Task LoadSportsAsync()
        {
            try
            {
                var sports = await _service.GetSportsAsync();
                // Môn hiện tại của Coach có thể đã ngừng: vẫn hiện để sửa được tên / SĐT mà không bị ép đổi môn.
                if (_existing is { SportId: int current } && sports.All(s => s.SportId != current))
                {
                    sports.Add(new SportOptionDto(current, $"{_existing.SportName} (ngừng)"));
                }
                SportBox.ItemsSource = sports;
            }
            catch (Exception ex)
            {
                ShowBanner(ex);
                return;
            }
            if (_existing?.SportId is int sportId) SportBox.SelectedValue = sportId;
            if (_existing is { HasWork: true }) SportBox.IsEnabled = false;
            _loaded = true;
            NameBox.Focus();
        }

        private bool IsCoach => RoleBox.SelectedItem as string == "Coach";

        private void Role_Changed(object sender, SelectionChangedEventArgs e)
        {
            SportPanel.Visibility = IsCoach || RoleNote.Visibility == Visibility.Visible ? Visibility.Visible : Visibility.Collapsed;
            if (IsLoaded) HideErrors();
        }

        private void Sport_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (IsLoaded) HideErrors();
        }

        private void Input_Changed(object sender, TextChangedEventArgs e)
        {
            if (IsLoaded) HideErrors();
        }

        private static void ShowField(TextBlock label, string message)
        {
            label.Text = message;
            label.Visibility = Visibility.Visible;
        }

        private void HideErrors()
        {
            NameError.Visibility = EmailError.Visibility = PhoneError.Visibility =
                SportError.Visibility = PasswordError.Visibility = Visibility.Collapsed;
            ErrorBanner.Visibility = Visibility.Collapsed;
        }

        // Số điện thoại chỉ gồm chữ số (phím cách không đi qua PreviewTextInput nên chặn riêng).
        private void Phone_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Space) e.Handled = true;
        }

        private void Phone_PreviewTextInput(object sender, System.Windows.Input.TextCompositionEventArgs e) =>
            e.Handled = !e.Text.All(char.IsAsciiDigit);

        // Dán số có dấu cách / gạch (vd. "0912 345 678"): bỏ ký tự không phải chữ số rồi mới dán.
        private void Phone_Pasting(object sender, DataObjectPastingEventArgs e)
        {
            if (e.DataObject.GetData(typeof(string)) is not string text) { e.CancelCommand(); return; }
            string digits = new string(text.Where(char.IsAsciiDigit).ToArray());
            if (digits.Length == 0) { e.CancelCommand(); return; }
            e.DataObject = new DataObject(DataFormats.Text, digits);
        }

        private async void Save_Click(object sender, RoutedEventArgs e)
        {
            if (_saving) return;
            HideErrors();

            string name = NameBox.Text.Trim();
            string email = EmailBox.Text.Trim();
            string phone = PhoneBox.Text.Trim();
            string role = (RoleBox.SelectedItem as string) ?? "";
            int? sportId = IsCoach ? SportBox.SelectedValue as int? : null;
            string password = PasswordBox.Text;

            bool ok = true;
            if (name.Length == 0) { ShowField(NameError, "Vui lòng nhập họ và tên."); ok = false; }
            if (_existing is null)
            {
                if (email.Length == 0) { ShowField(EmailError, "Vui lòng nhập email."); ok = false; }
                else if (!ProfileService.IsValidEmail(email)) { ShowField(EmailError, "Email không đúng định dạng."); ok = false; }
                if (password.Length < ProfileService.MinPasswordLength)
                {
                    ShowField(PasswordError, $"Mật khẩu tối thiểu {ProfileService.MinPasswordLength} ký tự.");
                    ok = false;
                }
            }
            if (phone.Length > 0 && !ProfileService.IsValidPhone(phone)) { ShowField(PhoneError, "Gồm 10 số và bắt đầu bằng 0."); ok = false; }
            if (IsCoach && sportId is null)
            {
                ShowField(SportError, _loaded ? "Vui lòng chọn môn." : "Chưa tải được danh sách môn.");
                ok = false;
            }
            if (!ok) return;

            SetSaving(true);
            try
            {
                if (_existing is null) await _service.CreateAsync(name, email, phone, role, sportId, password);
                else await _service.UpdateAsync(_existing.UserId, name, phone, role, sportId);
                _saving = false;                                     // phải bỏ cờ trước khi đóng (OnClosing chặn đóng khi đang lưu)
                DialogResult = true;
            }
            catch (Exception ex)
            {
                ShowBanner(ex);
                SetSaving(false);
            }
        }

        private void ShowBanner(Exception ex)
        {
            ErrorText.Text = ex switch
            {
                InvalidOperationException or UnauthorizedAccessException => ex.Message,
                System.Data.Common.DbException => "Không kết nối được máy chủ. Vui lòng thử lại.",
                _ => "Đã xảy ra lỗi không mong muốn. Vui lòng thử lại."
            };
            ErrorBanner.Visibility = Visibility.Visible;
        }

        private void SetSaving(bool saving)
        {
            _saving = saving;
            bool locked = _existing is { HasWork: true };
            NameBox.IsEnabled = PhoneBox.IsEnabled = PasswordBox.IsEnabled = !saving;
            EmailBox.IsEnabled = !saving && _existing is null;
            RoleBox.IsEnabled = !saving && _existing is null;
            SportBox.IsEnabled = !saving && !locked;
            CancelButton.IsEnabled = SaveButton.IsEnabled = !saving;
            SaveButton.Content = saving ? "Đang lưu…" : "Lưu";
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            if (_saving) e.Cancel = true;
            base.OnClosing(e);
        }
    }
}
