using SportCenter.Entities;
using SportCenter.Services;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace SportCenter.Views
{
    // Hồ sơ cá nhân (UC02): sửa họ tên, email, SĐT và đổi mật khẩu cho Manager, Staff, Coach.
    public partial class ProfilePage : UserControl
    {
        private readonly ProfileService _service = new ProfileService();
        private bool _busy;              // đang gọi lưu: chặn bấm hai lần
        private bool _loading;           // đang đổ dữ liệu vào ô: bỏ qua TextChanged

        // Giá trị đã lưu: nút "Lưu thay đổi" chỉ bật khi có ô khác các giá trị này.
        private string _savedName = "", _savedEmail = "", _savedPhone = "";

        // Báo cho khung ngoài (StaffShellView) cập nhật tên hiển thị trên thanh trên.
        public event EventHandler? ProfileSaved;

        public ProfilePage()
        {
            InitializeComponent();
            Loaded += async (_, _) => await LoadAsync();
        }

        private async Task LoadAsync()
        {
            try
            {
                User user = await _service.GetAsync(CurrentUser.Value!.UserId);

                _loading = true;
                try
                {
                    FullNameBox.Text = user.FullName;
                    EmailBox.Text = user.Email;
                    PhoneBox.Text = user.Phone ?? "";
                }
                finally
                {
                    _loading = false;
                }

                RememberSaved();
                ShowHeader(user.FullName, user.Role.RoleName);
                UpdateButtons();
            }
            catch (Exception ex)
            {
                ShowProfileError(ErrorMessage(ex));
            }
        }

        private void RememberSaved()
        {
            _savedName = FullNameBox.Text.Trim();
            _savedEmail = EmailBox.Text.Trim();
            _savedPhone = PhoneBox.Text.Trim();
        }

        private void ShowHeader(string fullName, string roleName)
        {
            HeaderName.Text = fullName;
            HeaderRole.Text = roleName;
            AvatarText.Text = Initials(fullName);
        }

        // Chữ cái đầu của từ đầu và từ cuối trong tên (Nguyễn Hữu Dũng → ND).
        private static string Initials(string fullName)
        {
            string[] parts = fullName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            return parts.Length switch
            {
                0 => "?",
                1 => parts[0][..1].ToUpper(),
                _ => (parts[0][..1] + parts[^1][..1]).ToUpper()
            };
        }

        // ---------- Lưu thông tin ----------

        private async void Save_Click(object sender, RoutedEventArgs e)
        {
            if (_busy) return;
            HideProfileError();

            // Gọi hết (không dùng &&) để hiện lỗi của mọi ô cùng lúc.
            bool valid = true;
            valid &= ValidateFullName();
            valid &= ValidateEmail();
            valid &= ValidatePhone();
            if (!valid) return;

            SetBusy(true);
            try
            {
                await _service.UpdateAsync(CurrentUser.Value!.UserId, FullNameBox.Text, EmailBox.Text, PhoneBox.Text);

                RememberSaved();
                ShowHeader(CurrentUser.Value.FullName, CurrentUser.Value.Role.RoleName);
                Notice.Show("Đã cập nhật hồ sơ.");
                ProfileSaved?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                ShowProfileError(ErrorMessage(ex));
            }
            finally
            {
                SetBusy(false);
            }
        }

        // ---------- Đổi mật khẩu ----------

        private async void ChangePassword_Click(object sender, RoutedEventArgs e)
        {
            if (_busy) return;
            HidePasswordError();

            bool valid = true;
            valid &= ValidateCurrentPassword();
            valid &= ValidateNewPassword();
            valid &= ValidateConfirm();
            if (!valid) return;

            SetBusy(true);
            try
            {
                await _service.ChangePasswordAsync(CurrentUser.Value!.UserId, CurrentPasswordBox.Password, NewPasswordBox.Password);

                CurrentPasswordBox.Clear();
                NewPasswordBox.Clear();
                ConfirmPasswordBox.Clear();
                ClearPasswordErrors();
                Notice.Show("Đã đổi mật khẩu.");
            }
            catch (Exception ex)
            {
                ShowPasswordError(ErrorMessage(ex));
            }
            finally
            {
                SetBusy(false);
            }
        }

        private void SetBusy(bool busy)
        {
            _busy = busy;
            SaveButton.Content = busy ? "Đang lưu…" : "Lưu thay đổi";
            UpdateButtons();
        }

        // "Lưu thay đổi" bật khi có thay đổi; "Đổi mật khẩu" bật khi đã nhập cả ba ô.
        private void UpdateButtons()
        {
            bool changed = FullNameBox.Text.Trim() != _savedName
                        || EmailBox.Text.Trim() != _savedEmail
                        || PhoneBox.Text.Trim() != _savedPhone;
            SaveButton.IsEnabled = !_busy && changed;

            PasswordButton.IsEnabled = !_busy
                && CurrentPasswordBox.Password.Length > 0
                && NewPasswordBox.Password.Length > 0
                && ConfirmPasswordBox.Password.Length > 0;
        }

        // ---------- Kiểm tra hợp lệ: mỗi hàm trả true nếu hợp lệ và cập nhật lỗi dưới ô ----------

        private bool ValidateFullName()
        {
            string? error = string.IsNullOrWhiteSpace(FullNameBox.Text) ? "Vui lòng nhập họ và tên." : null;
            return SetError(FullNameError, FullNameBox, error);
        }

        private bool ValidateEmail()
        {
            string email = EmailBox.Text.Trim();
            string? error = email.Length == 0 ? "Vui lòng nhập email."
                : !ProfileService.IsValidEmail(email) ? "Email không đúng định dạng."
                : null;
            return SetError(EmailError, EmailBox, error);
        }

        // Số điện thoại không bắt buộc; nếu nhập thì phải đúng 10 số.
        private bool ValidatePhone()
        {
            string phone = PhoneBox.Text.Trim();
            string? error = phone.Length > 0 && !ProfileService.IsValidPhone(phone)
                ? "Số điện thoại gồm 10 số và bắt đầu bằng 0."
                : null;
            return SetError(PhoneError, PhoneBox, error);
        }

        private bool ValidateCurrentPassword()
        {
            string? error = CurrentPasswordBox.Password.Length == 0 ? "Vui lòng nhập mật khẩu hiện tại." : null;
            return SetError(CurrentPasswordError, CurrentPasswordBox, error);
        }

        private bool ValidateNewPassword()
        {
            string password = NewPasswordBox.Password;
            string? error = password.Length == 0 ? "Vui lòng nhập mật khẩu mới."
                : password.Length < ProfileService.MinPasswordLength ? $"Mật khẩu tối thiểu {ProfileService.MinPasswordLength} ký tự."
                : password == CurrentPasswordBox.Password ? "Mật khẩu mới phải khác mật khẩu hiện tại."
                : null;
            return SetError(NewPasswordError, NewPasswordBox, error);
        }

        private bool ValidateConfirm()
        {
            string? error = ConfirmPasswordBox.Password.Length == 0 ? "Vui lòng nhập lại mật khẩu mới."
                : ConfirmPasswordBox.Password != NewPasswordBox.Password ? "Mật khẩu nhập lại không khớp."
                : null;
            return SetError(ConfirmError, ConfirmPasswordBox, error);
        }

        private static bool SetError(TextBlock errorLabel, Control input, string? message)
        {
            errorLabel.Text = message ?? string.Empty;
            errorLabel.Visibility = message is null ? Visibility.Collapsed : Visibility.Visible;
            input.BorderBrush = (Brush)Application.Current.FindResource(message is null ? "BorderBrush" : "ErrorBrush");
            return message is null;
        }

        private void ClearPasswordErrors()
        {
            SetError(CurrentPasswordError, CurrentPasswordBox, null);
            SetError(NewPasswordError, NewPasswordBox, null);
            SetError(ConfirmError, ConfirmPasswordBox, null);
        }

        // ---------- Sự kiện ô nhập ----------

        private void Info_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_loading) return;
            HideProfileError();
            UpdateButtons();
        }

        private void Password_Changed(object sender, RoutedEventArgs e)
        {
            HidePasswordError();
            UpdateButtons();
        }

        private void Phone_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !e.Text.All(char.IsDigit);                   // SĐT chỉ nhận chữ số
        }

        private void FullName_LostFocus(object sender, RoutedEventArgs e) => ValidateFullName();
        private void Email_LostFocus(object sender, RoutedEventArgs e) => ValidateEmail();
        private void Phone_LostFocus(object sender, RoutedEventArgs e) => ValidatePhone();
        private void CurrentPassword_LostFocus(object sender, RoutedEventArgs e)
        {
            // Rời ô khi còn trống chỉ để người dùng chuyển ô: báo thiếu khi bấm nút, tránh hiện lỗi sớm.
            if (CurrentPasswordBox.Password.Length > 0) ValidateCurrentPassword();
        }
        private void NewPassword_LostFocus(object sender, RoutedEventArgs e)
        {
            if (NewPasswordBox.Password.Length > 0) ValidateNewPassword();
            if (ConfirmPasswordBox.Password.Length > 0) ValidateConfirm();
        }
        private void Confirm_LostFocus(object sender, RoutedEventArgs e)
        {
            if (ConfirmPasswordBox.Password.Length > 0) ValidateConfirm();
        }

        // ---------- Thông báo lỗi trong thẻ ----------

        private static string ErrorMessage(Exception ex) => ex switch
        {
            InvalidOperationException or UnauthorizedAccessException => ex.Message,   // thông báo tiếng Việt từ Service
            System.Data.Common.DbException => "Không kết nối được máy chủ. Vui lòng thử lại.",
            _ => "Đã xảy ra lỗi không mong muốn. Vui lòng thử lại."
        };

        private void ShowProfileError(string message)
        {
            ProfileErrorText.Text = message;
            ProfileErrorBanner.Visibility = Visibility.Visible;
        }

        private void HideProfileError() => ProfileErrorBanner.Visibility = Visibility.Collapsed;

        private void ShowPasswordError(string message)
        {
            PasswordErrorText.Text = message;
            PasswordErrorBanner.Visibility = Visibility.Visible;
        }

        private void HidePasswordError() => PasswordErrorBanner.Visibility = Visibility.Collapsed;
    }
}
