using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace SportCenter.Views
{
    // Thông báo ngắn dùng chung cho các trang (vd. "Đã tạo lớp thành công.").
    public partial class NoticeBar : UserControl
    {
        private readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromSeconds(5) };

        public NoticeBar()
        {
            InitializeComponent();
            _timer.Tick += (_, _) => Hide();
            Unloaded += (_, _) => Hide();                  // gỡ khỏi cây thì ẩn luôn, tránh hiện mãi khi gắn lại
        }

        // Hiện thông báo; gọi lại thì thay nội dung và tính lại 5 giây.
        public void Show(string message)
        {
            MessageText.Text = message;
            Visibility = Visibility.Visible;
            _timer.Stop();
            _timer.Start();
        }

        public void Hide()
        {
            _timer.Stop();
            Visibility = Visibility.Collapsed;
        }

        private void Close_Click(object sender, RoutedEventArgs e) => Hide();
    }
}
