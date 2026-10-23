using SportCenter.Entities;
using SportCenter.Services;
using System;
using System.Windows;
using System.Windows.Controls;

namespace SportCenter.Views;

public partial class LoginFormView : UserControl
{
    private bool _syncingPassword;

    public event EventHandler? BackRequested;
    public event EventHandler? RegisterRequested;

    private readonly bool _isMember;
    public event EventHandler<User>? LoginSucceeded;
    public LoginFormView(bool isMember)
    {
        InitializeComponent();
        _isMember = isMember;

        if (isMember)
        {
            TitleText.Text = "Đăng nhập Học viên";
            SubtitleText.Text = "Chào mừng bạn quay lại!";
            RegisterPanel.Visibility = Visibility.Visible;
        }
        else
        {
            TitleText.Text = "Đăng nhập Nhân viên";
            SubtitleText.Text = "Dành cho Staff, Coach và Manager";
        }

        Loaded += (_, _) => UserEmailBox.Focus();
    }

    private void BackButton_Click(object sender, RoutedEventArgs e) => BackRequested?.Invoke(this, EventArgs.Empty);

    // UI mẫu: chưa xác thực thật, chỉ hiện lỗi khi để trống.
    private async void LoginButton_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(UserEmailBox.Text) || PasswordBox.Password.Length == 0)
        {
            ShowError("Vui lòng nhập email và mật khẩu.");
            return;
        }

        LoginButton.IsEnabled = true;
        try
        {
            User? user = await new AuthService()
                .LoginAsync(UserEmailBox.Text, PasswordBox.Password, _isMember);

            if (user is null)
            {
                ShowError("Email hoặc mật khẩu không đúng.");
                return;
            }

            CurrentUser.Value = user;
            LoginSucceeded?.Invoke(this, user);
        }
        catch
        {
            ShowError("Không kết nối được máy chủ. Vui lòng thử lại.");
        }
        finally
        {
            ShowError("Không kết nối được máy chủ. Vui lòng thử lại.");
        }

    }

    private void ForgotPassword_Click(object sender, RoutedEventArgs e)
    {
        // Chưa có màn Quên mật khẩu (UC17).
    }

    private void Register_Click(object sender, RoutedEventArgs e) => RegisterRequested?.Invoke(this, EventArgs.Empty);

    private void Input_Changed(object sender, TextChangedEventArgs e) => HideError();

    private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
    {
        if (_syncingPassword) return;
        _syncingPassword = true;
        PasswordVisibleBox.Text = PasswordBox.Password;
        _syncingPassword = false;
        HideError();
    }

    private void PasswordVisibleBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_syncingPassword) return;
        _syncingPassword = true;
        PasswordBox.Password = PasswordVisibleBox.Text;
        _syncingPassword = false;
        HideError();
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

    private void ShowError(string message)
    {
        ErrorText.Text = message;
        ErrorBanner.Visibility = Visibility.Visible;
    }

    private void HideError() => ErrorBanner.Visibility = Visibility.Collapsed;
}
