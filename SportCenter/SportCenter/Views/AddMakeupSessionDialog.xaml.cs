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
    // Hộp thoại xếp thủ công một buổi bù cho lớp đang thiếu buổi: chọn ngày → khung giờ (đúng thứ của ngày) → coach, phòng rảnh.
    // DialogResult = true khi đã lưu; CreatedSessionNo là số buổi vừa tạo.
    public partial class AddMakeupSessionDialog : Window
    {
        // Một khung giờ để chọn; không chọn được nếu đã qua giờ bắt đầu.
        private sealed record SlotChoice(int TimeSlotId, string Text, bool IsSelectable);

        private readonly ClassService _service = new ClassService();
        private readonly int _classId;
        private MakeupContextDto? _context;
        private System.Collections.Generic.List<TimeSlotDto> _allSlots = new();
        private bool _suspend;          // đang gán các ô: bỏ qua sự kiện
        private bool _saving;
        private int _choicesToken;      // mỗi lần tải coach/phòng tăng một bậc; kết quả của lần cũ bị bỏ

        public int CreatedSessionNo { get; private set; }

        public AddMakeupSessionDialog(int classId)
        {
            InitializeComponent();
            _classId = classId;
            Loaded += async (_, _) => await LoadAsync();
        }

        private async Task LoadAsync()
        {
            try
            {
                _context = await _service.GetMakeupContextAsync(_classId);
                _allSlots = await _service.GetTimeSlotsAsync();
            }
            catch (Exception ex)
            {
                ShowError(ex);                                       // vd. lớp đã đủ buổi
                return;
            }

            InfoText.Text = $"Lớp {_context.ClassName} · còn thiếu {_context.MissingSessions} buổi · " +
                            $"buổi bù sẽ là buổi số {_context.NextSessionNo}";
            DatePick.DisplayDateStart = MinDate().ToDateTime(TimeOnly.MinValue);
            DatePick.IsEnabled = true;
            SlotHint.Text = "Chọn ngày để xem các khung giờ.";
        }

        // Ngày sớm nhất có thể xếp buổi bù: không trước hôm nay và không trước ngày khai giảng của lớp.
        private DateOnly MinDate()
        {
            DateOnly today = DateOnly.FromDateTime(DateTime.Today);
            return _context is not null && _context.StartDate > today ? _context.StartDate : today;
        }

        // ---------- Chọn ngày → khung giờ ----------

        private void Date_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (_context is null || _suspend) return;
            HideError();
            _choicesToken++;                                         // hủy lần tải coach/phòng đang chờ
            ClearChoices();

            DatePick.DisplayDateStart = MinDate().ToDateTime(TimeOnly.MinValue);   // gõ ngày sớm vào ô không làm lịch cho chọn sớm
            if (DatePick.SelectedDate is not DateTime picked)
            {
                SlotBox.ItemsSource = null;
                SlotBox.IsEnabled = false;
                return;
            }

            DateOnly date = DateOnly.FromDateTime(picked);
            if (date < MinDate())
            {
                SlotBox.ItemsSource = null;
                SlotBox.IsEnabled = false;
                SlotHint.Text = $"Ngày xếp buổi bù phải từ {MinDate():dd/MM/yyyy} trở đi (không sớm hơn hôm nay hoặc ngày khai giảng).";
                UpdateSaveEnabled();
                return;
            }
            byte day = SessionRules.DbDayOfWeek(date);
            var slots = _allSlots
                .Where(s => s.DayOfWeek == day)
                .OrderBy(s => s.StartTime)
                .Select(s => new SlotChoice(s.TimeSlotId, $"{s.StartTime:HH:mm} – {s.EndTime:HH:mm}",
                                            !SessionRules.HasStarted(date, s.StartTime)))
                .ToList();

            _suspend = true;
            try { SlotBox.ItemsSource = slots; }
            finally { _suspend = false; }
            SlotBox.IsEnabled = slots.Count > 0;
            SlotHint.Text = slots.Count == 0
                ? "Ngày này không có khung giờ nào (trung tâm không dạy Chủ nhật)."
                : slots.All(s => !s.IsSelectable) ? "Các khung giờ của ngày này đã qua, hãy chọn ngày khác." : "";
            UpdateSaveEnabled();
        }

        // ---------- Chọn khung giờ → tải coach, phòng rảnh ----------

        private async void Slot_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (_context is null || _suspend) return;
            HideError();
            ClearChoices();
            if (SlotBox.SelectedItem is not SlotChoice { IsSelectable: true } slot || DatePick.SelectedDate is not DateTime picked)
            {
                UpdateSaveEnabled();
                return;
            }
            await LoadChoicesAsync(DateOnly.FromDateTime(picked), slot.TimeSlotId);
        }

        private async Task LoadChoicesAsync(DateOnly date, int timeSlotId, int? keepCoachId = null, int? keepRoomId = null)
        {
            int token = ++_choicesToken;
            try
            {
                MakeupChoicesDto choices = await _service.GetMakeupChoicesAsync(_classId, date, timeSlotId);
                if (token != _choicesToken) return;                  // đã đổi ngày/khung trong lúc chờ

                _suspend = true;
                try
                {
                    CoachBox.ItemsSource = choices.Coaches;
                    RoomBox.ItemsSource = choices.Rooms;
                    // Chọn lại lựa chọn trước đó nếu còn chọn được; ngược lại chọn sẵn coach và phòng gốc của lớp nếu đang rảnh.
                    int? wantCoach = keepCoachId ?? _context!.DefaultCoachId;
                    int wantRoom = keepRoomId ?? _context!.DefaultRoomId;
                    CoachBox.SelectedItem = choices.Coaches.FirstOrDefault(c => c.UserId == wantCoach && c.IsSelectable);
                    RoomBox.SelectedItem = choices.Rooms.FirstOrDefault(r => r.RoomId == wantRoom && r.IsSelectable);
                    CoachBox.IsEnabled = RoomBox.IsEnabled = true;
                }
                finally
                {
                    _suspend = false;
                }
            }
            catch (Exception ex)
            {
                if (token == _choicesToken) ShowError(ex);
            }
            UpdateSaveEnabled();
        }

        private void ClearChoices()
        {
            _suspend = true;
            try
            {
                CoachBox.ItemsSource = null;
                RoomBox.ItemsSource = null;
                CoachBox.IsEnabled = RoomBox.IsEnabled = false;
            }
            finally
            {
                _suspend = false;
            }
            UpdateSaveEnabled();
        }

        private void Choice_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (_suspend) return;
            HideError();
            UpdateSaveEnabled();
        }

        // Chỉ bật khi đã chọn đủ ngày, khung giờ, coach và phòng đều chọn được.
        private void UpdateSaveEnabled()
        {
            SaveButton.IsEnabled = !_saving
                && DatePick.SelectedDate is not null
                && SlotBox.SelectedItem is SlotChoice { IsSelectable: true }
                && CoachBox.SelectedItem is CoachChoiceDto { IsSelectable: true }
                && RoomBox.SelectedItem is RoomChoiceDto { IsSelectable: true };
        }

        // ---------- Lưu ----------

        private async void Save_Click(object sender, RoutedEventArgs e)
        {
            if (_saving) return;
            if (DatePick.SelectedDate is not DateTime picked
                || SlotBox.SelectedItem is not SlotChoice { IsSelectable: true } slot
                || CoachBox.SelectedItem is not CoachChoiceDto { IsSelectable: true } coach
                || RoomBox.SelectedItem is not RoomChoiceDto { IsSelectable: true } room)
            {
                return;
            }

            DateOnly date = DateOnly.FromDateTime(picked);
            SetSaving(true);
            try
            {
                CreatedSessionNo = await _service.AddMakeupSessionAsync(_classId, date, slot.TimeSlotId, coach.UserId, room.RoomId);
                _saving = false;                                     // đã lưu xong: cho phép đóng (OnClosing chặn đóng khi _saving)
                DialogResult = true;
            }
            catch (Exception ex)
            {
                ShowError(ex);
                await LoadChoicesAsync(date, slot.TimeSlotId, coach.UserId, room.RoomId);   // coach/phòng có thể vừa bị người khác đặt: tải lại, giữ lựa chọn
                await RefreshContextAsync();                                                 // lớp có thể vừa đủ buổi: khóa form nếu vậy
                SetSaving(false);
            }
        }

        // Tải lại thông tin lớp sau một lần lưu lỗi; nếu lớp không còn xếp bù được (vd. đã đủ buổi) thì hiện lý do và khóa form.
        private async Task RefreshContextAsync()
        {
            try
            {
                _context = await _service.GetMakeupContextAsync(_classId);
                InfoText.Text = $"Lớp {_context.ClassName} · còn thiếu {_context.MissingSessions} buổi · " +
                                $"buổi bù sẽ là buổi số {_context.NextSessionNo}";
            }
            catch (Exception ex)
            {
                _context = null;
                ShowError(ex);
                DatePick.IsEnabled = SlotBox.IsEnabled = CoachBox.IsEnabled = RoomBox.IsEnabled = false;
            }
        }

        private void SetSaving(bool saving)
        {
            _saving = saving;
            DatePick.IsEnabled = SlotBox.IsEnabled = CoachBox.IsEnabled = RoomBox.IsEnabled = !saving && _context is not null;
            CancelButton.IsEnabled = !saving;
            SaveButton.Content = saving ? "Đang lưu…" : "Xếp buổi bù";
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
