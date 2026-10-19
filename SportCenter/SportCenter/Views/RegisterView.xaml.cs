using System;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace SportCenter.Views;

public partial class RegisterView : UserControl
{
    private const int MinAge = 5;

    private static readonly Regex EmailRegex = new(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled);
    private static readonly Regex PhoneRegex = new(@"^0\d{9}$", RegexOptions.Compiled);

    private bool _syncingPassword;

    // Quay lại form đăng nhập Member (bấm Quay lại / Đăng nhập, hoặc sau khi đăng ký xong).
    public event EventHandler? BackRequested;

    public RegisterView()
    {
        InitializeComponent();
        BirthDatePicker.DisplayDateEnd = DateTime.Today;
        Loaded += (_, _) => FullNameBox.Focus();
    }

    private void BackButton_Click(object sender, RoutedEventArgs e) => BackRequested?.Invoke(this, EventArgs.Empty);

    // UI mẫu: kiểm tra hợp lệ rồi báo thành công, chưa lưu dữ liệu.
    private void RegisterButton_Click(object sender, RoutedEventArgs e)
    {
        // Gọi hết (không dùng &&) để hiện lỗi của mọi ô cùng lúc.
        bool valid = true;
        valid &= ValidateFullName();
        valid &= ValidatePhone();
        valid &= ValidateBirthDate();
        valid &= ValidateGender();
        valid &= ValidateEmail();
        valid &= ValidateUsername();
        valid &= ValidatePassword();
        valid &= ValidateConfirm();

        if (!valid) return;

        SuccessBanner.Visibility = Visibility.Visible;
        var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1.2) };
        timer.Tick += (_, _) =>
        {
            timer.Stop();
            BackRequested?.Invoke(this, EventArgs.Empty);
        };
        timer.Start();
    }

    // ---- Validation: mỗi hàm trả true nếu hợp lệ và cập nhật lỗi dưới ô ----

    private bool ValidateFullName()
    {
        string? error = string.IsNullOrWhiteSpace(FullNameBox.Text) ? "Vui lòng nhập họ và tên." : null;
        return SetError(FullNameError, FullNameBox, error);
    }

    private bool ValidatePhone()
    {
        string phone = PhoneBox.Text.Trim();
        string? error = phone.Length == 0 ? "Vui lòng nhập số điện thoại."
            : !PhoneRegex.IsMatch(phone) ? "Số điện thoại gồm 10 số và bắt đầu bằng 0."
            : null;
        return SetError(PhoneError, PhoneBox, error);
    }

    private bool ValidateBirthDate()
    {
        DateTime? birth = BirthDatePicker.SelectedDate;
        string? error = birth is null ? "Vui lòng chọn ngày sinh."
            : birth.Value.Date > DateTime.Today ? "Ngày sinh không được ở tương lai."
            : GetAge(birth.Value.Date) < MinAge ? $"Học viên phải từ {MinAge} tuổi trở lên."
            : null;
        return SetError(BirthDateError, null, error);
    }

    private bool ValidateGender()
    {
        bool chosen = MaleRadio.IsChecked == true || FemaleRadio.IsChecked == true || OtherRadio.IsChecked == true;
        return SetError(GenderError, null, chosen ? null : "Vui lòng chọn giới tính.");
    }

    private bool ValidateEmail()
    {
        string email = EmailBox.Text.Trim();
        string? error = email.Length == 0 ? "Vui lòng nhập email."
            : !EmailRegex.IsMatch(email) ? "Email không đúng định dạng."
            : null;
        return SetError(EmailError, EmailBox, error);
    }

    private bool ValidateUsername()
    {
        string username = UsernameBox.Text.Trim();
        string? error = username.Length == 0 ? "Vui lòng nhập tên đăng nhập."
            : username.Length < 4 ? "Tên đăng nhập tối thiểu 4 ký tự."
            : null;
        return SetError(UsernameError, UsernameBox, error);
    }

    private bool ValidatePassword()
    {
        string password = PasswordBox.Password;
        string? error = password.Length == 0 ? "Vui lòng nhập mật khẩu."
            : password.Length < 6 ? "Mật khẩu tối thiểu 6 ký tự."
            : null;
        Control box = PasswordBox.Visibility == Visibility.Visible ? PasswordBox : PasswordVisibleBox;
        return SetError(PasswordError, box, error);
    }

    private bool ValidateConfirm()
    {
        string? error = ConfirmPasswordBox.Password.Length == 0 ? "Vui lòng nhập lại mật khẩu."
            : ConfirmPasswordBox.Password != PasswordBox.Password ? "Mật khẩu nhập lại không khớp."
            : null;
        return SetError(ConfirmError, ConfirmPasswordBox, error);
    }

    private static int GetAge(DateTime birth)
    {
        DateTime today = DateTime.Today;
        int age = today.Year - birth.Year;
        if (birth > today.AddYears(-age)) age--;
        return age;
    }

    // Hiện/ẩn lỗi dưới ô và đổi màu viền ô nhập (nếu có).
    private static bool SetError(TextBlock errorLabel, Control? input, string? message)
    {
        errorLabel.Text = message ?? string.Empty;
        errorLabel.Visibility = message is null ? Visibility.Collapsed : Visibility.Visible;

        if (input is not null)
        {
            input.BorderBrush = (Brush)Application.Current.FindResource(message is null ? "BorderBrush" : "ErrorBrush");
        }

        return message is null;
    }

    // ---- Handlers: kiểm tra khi rời ô ----

    private void FullName_LostFocus(object sender, RoutedEventArgs e) => ValidateFullName();
    private void Phone_LostFocus(object sender, RoutedEventArgs e) => ValidatePhone();
    private void Email_LostFocus(object sender, RoutedEventArgs e) => ValidateEmail();
    private void Username_LostFocus(object sender, RoutedEventArgs e) => ValidateUsername();
    private void Confirm_LostFocus(object sender, RoutedEventArgs e) => ValidateConfirm();

    private void Password_LostFocus(object sender, RoutedEventArgs e)
    {
        ValidatePassword();
        if (ConfirmPasswordBox.Password.Length > 0) ValidateConfirm();
    }

    private void BirthDate_Changed(object sender, SelectionChangedEventArgs e) => ValidateBirthDate();

    private void Gender_Checked(object sender, RoutedEventArgs e) => ValidateGender();

    // ---- Mật khẩu hiện/ẩn: giữ PasswordBox và TextBox đồng bộ ----

    private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
    {
        if (_syncingPassword) return;
        _syncingPassword = true;
        PasswordVisibleBox.Text = PasswordBox.Password;
        _syncingPassword = false;
    }

    private void PasswordVisibleBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_syncingPassword) return;
        _syncingPassword = true;
        PasswordBox.Password = PasswordVisibleBox.Text;
        _syncingPassword = false;
    }

    private void TogglePasswordButton_Click(object sender, RoutedEventArgs e)
    {
        bool show = PasswordVisibleBox.Visibility != Visibility.Visible;
        PasswordVisibleBox.Visibility = show ? Visibility.Visible : Visibility.Collapsed;
        PasswordBox.Visibility = show ? Visibility.Collapsed : Visibility.Visible;
        TogglePasswordButton.Content = show ? "Ẩn" : "Hiện";

        if (show)
        {
            PasswordVisibleBox.Focus();
            PasswordVisibleBox.CaretIndex = PasswordVisibleBox.Text.Length;
        }
        else
        {
            PasswordBox.Focus();
        }
    }
}
