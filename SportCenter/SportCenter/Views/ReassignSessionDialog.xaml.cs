using SportCenter.Dtos;
using SportCenter.Services;
using System;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace SportCenter.Views
{
    // Hộp thoại đổi coach và/hoặc phòng của một buổi chưa bắt đầu. DialogResult = true khi đã lưu.
    public partial class ReassignSessionDialog : Window
    {
        private readonly ClassService _service = new ClassService();
        private readonly int _sessionId;
        private ReassignOptionsDto? _options;
        private bool _suspend;          // đang gán các ô: bỏ qua SelectionChanged
        private bool _saving;

        public ReassignSessionDialog(int sessionId)
        {
            InitializeComponent();
            _sessionId = sessionId;
            Loaded += async (_, _) => await LoadAsync();
        }

        private async Task LoadAsync()
        {
            try
            {
                _options = await _service.GetReassignOptionsAsync(_sessionId);
            }
            catch (Exception ex)
            {
                _options = null;                                     // không có danh sách hợp lệ: khóa form
                CoachBox.IsEnabled = RoomBox.IsEnabled = false;
                UpdateSaveEnabled();
                ShowError(ex);                                       // vd. buổi đã bắt đầu: không cho sửa
                return;
            }

            _suspend = true;
            try
            {
                InfoText.Text = $"Buổi {_options.SessionNo} · Thứ {_options.DayOfWeek}, {_options.SessionDate:dd/MM/yyyy} · " +
                                $"{_options.StartTime:HH:mm} – {_options.EndTime:HH:mm}";
                CoachBox.ItemsSource = _options.Coaches;
                RoomBox.ItemsSource = _options.Rooms;
                CoachBox.SelectedItem = _options.Coaches.FirstOrDefault(c => c.IsCurrent);
                RoomBox.SelectedItem = _options.Rooms.FirstOrDefault(r => r.IsCurrent);
                CoachBox.IsEnabled = RoomBox.IsEnabled = true;
            }
            finally
            {
                _suspend = false;
            }
            UpdateSaveEnabled();
        }

        // Chỉ bật Lưu khi có ít nhất một mục khác hiện tại.
        private void UpdateSaveEnabled()
        {
            SaveButton.IsEnabled = !_saving && _options is not null && (ChosenCoachId() is not null || ChosenRoomId() is not null);
        }

        // Id mới của coach / phòng nếu khác hiện tại, ngược lại null (giữ nguyên).
        private int? ChosenCoachId() =>
            CoachBox.SelectedItem is CoachChoiceDto { IsCurrent: false, IsSelectable: true } c ? c.UserId : null;

        private int? ChosenRoomId() =>
            RoomBox.SelectedItem is RoomChoiceDto { IsCurrent: false, IsSelectable: true } r ? r.RoomId : null;

        private void Choice_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (_suspend) return;
            HideError();
            UpdateSaveEnabled();
        }

        private async void Save_Click(object sender, RoutedEventArgs e)
        {
            if (_saving) return;
            int? coachId = ChosenCoachId();
            int? roomId = ChosenRoomId();
            if (coachId is null && roomId is null) return;

            SetSaving(true);
            try
            {
                await _service.ReassignSessionAsync(_sessionId, coachId, roomId);
                _saving = false;                                     // đã lưu xong: cho phép đóng (OnClosing chặn đóng khi _saving)
                DialogResult = true;
            }
            catch (Exception ex)
            {
                ShowError(ex);
                await LoadAsync();                                   // coach/phòng có thể vừa bị người khác đặt: tải lại danh sách
                SetSaving(false);                                    // chỉ mở lại form sau khi đã tải xong
            }
        }

        private void SetSaving(bool saving)
        {
            _saving = saving;
            CoachBox.IsEnabled = RoomBox.IsEnabled = !saving && _options is not null;
            CancelButton.IsEnabled = !saving;
            SaveButton.Content = saving ? "Đang lưu…" : "Lưu";
            UpdateSaveEnabled();
        }

        private void ShowError(Exception ex)
        {
            ErrorText.Text = ex switch
            {
                InvalidOperationException or UnauthorizedAccessException => ex.Message,
                System.Data.Common.DbException => "Không kết nối được máy chủ. Vui lòng thử lại.",
                _ => "Đã xảy ra lỗi không mong muốn. Vui lòng thử lại."
            };
            ErrorBanner.Visibility = Visibility.Visible;
        }

        private void HideError() => ErrorBanner.Visibility = Visibility.Collapsed;

        // Không cho đóng cửa sổ (nút X, Alt+F4) khi đang lưu, để kết quả lưu không bị mất.
        protected override void OnClosing(CancelEventArgs e)
        {
            if (_saving) e.Cancel = true;
            base.OnClosing(e);
        }
    }
}
