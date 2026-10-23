using SportCenter.Dtos;
using SportCenter.Services;
using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;

namespace SportCenter.Views
{
    // Hộp thoại thêm / sửa một môn. DialogResult = true khi đã lưu; SavedSportId là id môn vừa lưu.
    public partial class SportDialog : Window
    {
        private readonly CatalogService _service = new CatalogService();
        private readonly SportManageDto? _existing;
        private bool _saving;

        public int SavedSportId { get; private set; }

        public SportDialog(SportManageDto? existing)
        {
            InitializeComponent();
            _existing = existing;
            if (existing is not null)
            {
                Title = "Sửa môn";
                TitleText.Text = "Sửa môn";
                NameBox.Text = existing.SportName;
                DescriptionBox.Text = existing.Description ?? "";
            }
            Loaded += (_, _) => { NameBox.Focus(); NameBox.SelectAll(); };
        }

        private void Input_Changed(object sender, TextChangedEventArgs e)
        {
            if (!IsLoaded) return;
            NameError.Visibility = Visibility.Collapsed;
            ErrorBanner.Visibility = Visibility.Collapsed;
        }

        private async void Save_Click(object sender, RoutedEventArgs e)
        {
            if (_saving) return;
            string name = NameBox.Text.Trim();
            if (name.Length == 0)
            {
                NameError.Text = "Vui lòng nhập tên môn.";
                NameError.Visibility = Visibility.Visible;
                NameBox.Focus();
                return;
            }

            SetSaving(true);
            try
            {
                string? description = DescriptionBox.Text;
                if (_existing is null) SavedSportId = await _service.CreateSportAsync(name, description);
                else
                {
                    await _service.UpdateSportAsync(_existing.SportId, name, description);
                    SavedSportId = _existing.SportId;
                }
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
            NameBox.IsEnabled = DescriptionBox.IsEnabled = !saving;
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
