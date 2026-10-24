using SportCenter.Dtos;
using SportCenter.Services;
using System;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace SportCenter.Views
{
    // Hộp thoại thêm / sửa một khóa của một môn. DialogResult = true khi đã lưu.
    public partial class CourseDialog : Window
    {
        private readonly CatalogService _service = new CatalogService();
        private readonly SportManageDto _sport;
        private readonly CourseManageDto? _existing;
        private bool _saving;
        private decimal? _originalFee;                  // học phí gốc (có thể có phần lẻ): giữ nguyên nếu người dùng không sửa ô học phí

        public CourseDialog(SportManageDto sport, CourseManageDto? existing)
        {
            InitializeComponent();
            _sport = sport;
            _existing = existing;
            LevelBox.ItemsSource = CatalogService.Levels;

            SportText.Text = $"Môn: {sport.SportName}";
            if (existing is not null)
            {
                Title = "Sửa khóa";
                TitleText.Text = "Sửa khóa";
                NameBox.Text = existing.CourseName;
                LevelBox.SelectedItem = CatalogService.Levels.FirstOrDefault(l => l == existing.Level);
                SessionsBox.Text = existing.TotalSessions.ToString();
                _originalFee = existing.TuitionFee;
                FeeBox.Text = ((long)existing.TuitionFee).ToString();
                DescriptionBox.Text = existing.Description ?? "";
                if (existing.ClassCount > 0)                         // đã có lớp: khóa số buổi
                {
                    SessionsBox.IsEnabled = false;
                    SessionsNote.Visibility = Visibility.Visible;
                }
            }
            Loaded += (_, _) => { NameBox.Focus(); NameBox.SelectAll(); };
        }

        private static void ShowField(TextBlock label, string message)
        {
            label.Text = message;
            label.Visibility = Visibility.Visible;
        }

        private void HideFieldErrors()
        {
            NameError.Visibility = LevelError.Visibility = SessionsError.Visibility = FeeError.Visibility = Visibility.Collapsed;
            ErrorBanner.Visibility = Visibility.Collapsed;
        }

        private void Input_Changed(object sender, TextChangedEventArgs e)
        {
            if (IsLoaded) HideFieldErrors();
        }

        private void Level_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (IsLoaded) HideFieldErrors();
        }

        // Chỉ cho gõ chữ số vào ô số buổi và học phí.
        private void Digits_PreviewTextInput(object sender, System.Windows.Input.TextCompositionEventArgs e) =>
            e.Handled = !e.Text.All(char.IsAsciiDigit);

        private void Digits_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Space) e.Handled = true;   // phím cách không đi qua PreviewTextInput
        }

        private void Digits_Pasting(object sender, DataObjectPastingEventArgs e)
        {
            if (e.DataObject.GetData(typeof(string)) is not string text || !text.All(char.IsAsciiDigit)) e.CancelCommand();
        }

        private async void Save_Click(object sender, RoutedEventArgs e)
        {
            if (_saving) return;
            HideFieldErrors();

            string name = NameBox.Text.Trim();
            string? level = LevelBox.SelectedItem as string;
            bool ok = true;
            if (name.Length == 0) { ShowField(NameError, "Vui lòng nhập tên khóa."); ok = false; }
            if (level is null) { ShowField(LevelError, "Vui lòng chọn level."); ok = false; }

            int sessions = 0;
            if (!int.TryParse(SessionsBox.Text, out sessions) || sessions < 1 || sessions > CatalogService.MaxSessions)
            {
                ShowField(SessionsError, $"Từ 1 đến {CatalogService.MaxSessions}.");
                ok = false;
            }
            decimal fee = 0;
            if (_originalFee is decimal original && FeeBox.Text == ((long)original).ToString())
            {
                fee = original;                                      // không sửa học phí: giữ nguyên giá trị gốc
            }
            else if (!decimal.TryParse(FeeBox.Text, out fee) || fee > CatalogService.MaxTuitionFee)
            {
                ShowField(FeeError, $"Từ 0 đến {CatalogService.MaxTuitionFee:N0}.");
                ok = false;
            }
            if (!ok) return;

            SetSaving(true);
            try
            {
                string? description = DescriptionBox.Text;
                if (_existing is null)
                    await _service.CreateCourseAsync(_sport.SportId, name, level, sessions, fee, description);
                else
                    await _service.UpdateCourseAsync(_existing.CourseId, name, level, sessions, fee, description);
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
            NameBox.IsEnabled = LevelBox.IsEnabled = FeeBox.IsEnabled = DescriptionBox.IsEnabled = !saving;
            SessionsBox.IsEnabled = !saving && (_existing is null || _existing.ClassCount == 0);
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
