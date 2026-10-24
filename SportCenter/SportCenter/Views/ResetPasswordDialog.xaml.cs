using SportCenter.Dtos;
using SportCenter.Services;
using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;

namespace SportCenter.Views
{
    // Hộp thoại Manager đặt mật khẩu mới cho một tài khoản khác. DialogResult = true khi đã lưu.
    public partial class ResetPasswordDialog : Window
    {
        private readonly AccountService _service = new AccountService();
        private readonly AccountDto _account;
        private bool _saving;

        public ResetPasswordDialog(AccountDto account)
        {
            InitializeComponent();
            _account = account;
            InfoText.Text = $"{account.FullName} · {account.Email}";
            Loaded += (_, _) => PasswordBox.Focus();
        }

        private void Input_Changed(object sender, TextChangedEventArgs e)
        {
            if (!IsLoaded) return;
            PasswordError.Visibility = Visibility.Collapsed;
            ErrorBanner.Visibility = Visibility.Collapsed;
        }

        private async void Save_Click(object sender, RoutedEventArgs e)
        {
            if (_saving) return;
            if (PasswordBox.Text.Length < ProfileService.MinPasswordLength)
            {
                PasswordError.Text = $"Mật khẩu tối thiểu {ProfileService.MinPasswordLength} ký tự.";
                PasswordError.Visibility = Visibility.Visible;
                return;
            }

            SetSaving(true);
            try
            {
                await _service.ResetPasswordAsync(_account.UserId, PasswordBox.Text);
                _saving = false;                                     // phải bỏ cờ trước khi đóng (OnClosing chặn đóng khi đang lưu)
                DialogResult = true;
            }
            catch (Exception ex)
            {
                ErrorText.Text = ex switch
                {
                    InvalidOperationException or UnauthorizedAccessException => ex.Message,
                    System.Data.Common.DbException => "Không kết nối được máy chủ. Vui lòng thử lại.",
                    _ => "Đã xảy ra lỗi không mong muốn. Vui lòng thử lại."
                };
                ErrorBanner.Visibility = Visibility.Visible;
                SetSaving(false);
            }
        }

        private void SetSaving(bool saving)
        {
            _saving = saving;
            PasswordBox.IsEnabled = !saving;
            CancelButton.IsEnabled = SaveButton.IsEnabled = !saving;
            SaveButton.Content = saving ? "Đang lưu…" : "Đặt lại";
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            if (_saving) e.Cancel = true;
            base.OnClosing(e);
        }
    }
}
