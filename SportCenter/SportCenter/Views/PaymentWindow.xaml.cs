using System.Windows;
using System.Windows.Media.Imaging;
using System.IO;
using QRCoder;
using SportCenter.Dtos;
using SportCenter.Services;
namespace SportCenter.Views;
public partial class PaymentWindow : Window
{
    private readonly PaymentCheckoutDto _payment;
    private readonly PaymentService _service = new();
    private bool _busy;
    public PaymentWindow(PaymentCheckoutDto payment)
    {
        InitializeComponent(); _payment = payment;
        AmountText.Text = $"{payment.Amount:N0} ₫"; OrderText.Text = $"Mã đơn: {payment.OrderCode}";
        DescriptionText.Text = payment.Description; StatusText.Text = $"Trạng thái: {payment.Status}";
        QrPayloadText.Text = payment.QrCode ?? "Không có dữ liệu QR.";
        if (!string.IsNullOrWhiteSpace(payment.QrCode))
        {
            using var generator = new QRCodeGenerator();
            using var data = generator.CreateQrCode(payment.QrCode, QRCodeGenerator.ECCLevel.Q);
            var png = new PngByteQRCode(data).GetGraphic(8);
            var bitmap = new BitmapImage();
            using var stream = new MemoryStream(png);
            bitmap.BeginInit(); bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.StreamSource = stream; bitmap.EndInit(); bitmap.Freeze();
            QrImage.Source = bitmap;
        }
        SuccessButton.IsEnabled = payment.Status == "Pending";
    }
    private async void Success_Click(object sender, RoutedEventArgs e) => await ConfirmAsync(true);
    private async void Fail_Click(object sender, RoutedEventArgs e) => await ConfirmAsync(false);
    private async Task ConfirmAsync(bool success)
    {
        if (_busy) return; _busy = true; SuccessButton.IsEnabled = false;
        try
        {
            await _service.ConfirmMockPaymentAsync(_payment.PaymentId, success);
            MessageBox.Show(success ? "Thanh toán mô phỏng thành công. Đăng ký đã chuyển sang Active." : "Thanh toán mô phỏng thất bại. Đăng ký đã hủy; bạn có thể ghi danh lại để thử lần nữa.", "Kết quả thanh toán", MessageBoxButton.OK, success ? MessageBoxImage.Information : MessageBoxImage.Warning);
            DialogResult = success;
        }
        catch (Exception ex) { MessageBox.Show(ex.Message, "Không thể xử lý thanh toán", MessageBoxButton.OK, MessageBoxImage.Warning); SuccessButton.IsEnabled = true; }
        finally { _busy = false; }
    }
    private void Close_Click(object sender, RoutedEventArgs e) => Close();
}
