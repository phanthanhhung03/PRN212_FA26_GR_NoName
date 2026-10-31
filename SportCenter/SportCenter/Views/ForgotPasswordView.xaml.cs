using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using SportCenter.Services;

namespace SportCenter.Views;

// Quên mật khẩu: đặt mật khẩu mới sau khi xác minh bằng email + số điện thoại.
public partial class ForgotPasswordView : UserControl
{
    private readonly AuthService _service = new();

    // Quay lại form đăng nhập (bấm Quay lại, hoặc sau khi đặt lại xong).
    public event EventHandler? BackRequested;

    public ForgotPasswordView()
    {
        InitializeComponent();
        Loaded += (_, _) => EmailBox.Focus();
    }

    private void BackButton_Click(object sender, RoutedEventArgs e) => BackRequested?.Invoke(this, EventArgs.Empty);

    private async void ResetButton_Click(object sender, RoutedEventArgs e)
    {
        ErrorBanner.Visibility = Visibility.Collapsed;

        if (NewPasswordBox.Password != ConfirmBox.Password)
        {
            ShowError("Mật khẩu nhập lại không khớp.");
            return;
        }

        ResetButton.IsEnabled = false;                // chặn bấm hai lần trong lúc đang lưu
        try
        {
            await _service.ResetPasswordAsync(EmailBox.Text, PhoneBox.Text, NewPasswordBox.Password);
        }
        catch (InvalidOperationException ex)
        {
            ShowError(ex.Message);
            return;
        }
        catch
        {
            ShowError("Không kết nối được máy chủ. Vui lòng thử lại.");
            return;
        }
        finally
        {
            ResetButton.IsEnabled = true;
        }

        SuccessBanner.Visibility = Visibility.Visible;
        var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1.2) };
        timer.Tick += (_, _) =>
        {
            timer.Stop();
            BackRequested?.Invoke(this, EventArgs.Empty);
        };
        timer.Start();
    }

    private void ShowError(string message)
    {
        ErrorText.Text = message;
        ErrorBanner.Visibility = Visibility.Visible;
    }
}
