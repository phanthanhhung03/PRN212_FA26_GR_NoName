using System.Windows;
using System.Windows.Media;

namespace SportCenter.Views
{
    public enum DialogKind { Info, Success, Pending, Error, Question }

    // Hộp thoại thông báo thay MessageBox. Cùng phong cách với ConfirmDialog.
    //   MessageDialog.Show(owner, DialogKind.Error, "Không thể ghi danh", "Bạn đã đăng ký lớp này.");
    //   bool ok = MessageDialog.Ask(owner, DialogKind.Pending, "Xác nhận ghi danh", "...", "Ghi danh", "Để sau", details);
    public partial class MessageDialog : Window
    {
        public record DetailRow(string Label, string Value);

        private MessageDialog(DialogKind kind, string title, string message,
                              string okText, string? cancelText, IEnumerable<DetailRow>? details)
        {
            InitializeComponent();
            Title = title;
            TitleText.Text = title;
            MessageText.Text = message;
            OkButton.Content = okText;

            if (cancelText is null) CancelButton.Visibility = Visibility.Collapsed;
            else CancelButton.Content = cancelText;

            var rows = details?.ToList();
            if (rows is { Count: > 0 })
            {
                DetailList.ItemsSource = rows;
                DetailCard.Visibility = Visibility.Visible;
            }

            ApplyKind(kind);
        }

        // Thông báo một nút (Đóng/OK).
        public static void Show(Window? owner, DialogKind kind, string title, string message,
                                string okText = "Đóng", IEnumerable<DetailRow>? details = null) =>
            new MessageDialog(kind, title, message, okText, null, details) { Owner = owner }.ShowDialog();

        // Hỏi xác nhận: true nếu bấm nút chính.
        public static bool Ask(Window? owner, DialogKind kind, string title, string message,
                               string okText, string cancelText = "Hủy", IEnumerable<DetailRow>? details = null) =>
            new MessageDialog(kind, title, message, okText, cancelText, details) { Owner = owner }.ShowDialog() == true;

        private void Ok_Click(object sender, RoutedEventArgs e) => DialogResult = true;

        // Màu nền badge, màu nét icon, hình icon (vẽ bằng Path 24x24) theo loại thông báo.
        private void ApplyKind(DialogKind kind)
        {
            (string back, string front, string icon) = kind switch
            {
                DialogKind.Error    => ("ErrorBackgroundBrush", "ErrorBrush",       "M12,5 L12,14 M12,18.5 L12,18.6"),
                DialogKind.Success  => ("PrimaryLightBrush",    "PrimaryBrush",     "M5,12.5 L10,17.5 L19,7"),
                DialogKind.Pending  => ("PendingBackgroundBrush", "AccentBrush",    "M12,6 L12,12 L16,14.5"),
                DialogKind.Question => ("PrimaryLightBrush",    "PrimaryBrush",     "M9,9.5 A3,3 0 1 1 13,12.3 C12.4,12.7 12,13.2 12,14 M12,18.5 L12,18.6"),
                _                   => ("PrimaryLightBrush",    "PrimaryDarkBrush", "M12,10.5 L12,18 M12,6.5 L12,6.6"),
            };

            Badge.Background = Res(back);
            Icon.Stroke = Res(front);
            Icon.Data = Geometry.Parse(icon);
            TitleText.Foreground = kind == DialogKind.Error ? Res("ErrorBrush") : Res("PrimaryDarkBrush");

            // Lỗi: nút chính màu đỏ để nổi bật; còn lại giữ xanh của PrimaryButton.
            if (kind == DialogKind.Error) OkButton.Style = (Style)FindResource("DangerButton");
        }

        private Brush Res(string key) => (Brush)FindResource(key);
    }
}
