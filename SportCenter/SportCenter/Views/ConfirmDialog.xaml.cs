using System.Windows;

namespace SportCenter.Views
{
    // Hộp thoại xác nhận cho thao tác khó hoàn tác (vd. hủy buổi). Dùng: ConfirmDialog.Ask(owner, tiêu đề, nội dung, "Hủy buổi").
    public partial class ConfirmDialog : Window
    {
        private ConfirmDialog(string title, string message, string confirmText)
        {
            InitializeComponent();
            Title = title;
            TitleText.Text = title;
            MessageText.Text = message;
            ConfirmButton.Content = confirmText;
        }

        // Trả về true nếu người dùng bấm nút xác nhận.
        public static bool Ask(Window? owner, string title, string message, string confirmText) =>
            new ConfirmDialog(title, message, confirmText) { Owner = owner }.ShowDialog() == true;

        private void Confirm_Click(object sender, RoutedEventArgs e) => DialogResult = true;
    }
}
