using SportCenter.Dtos;
using SportCenter.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace SportCenter.Views
{
    // Chi tiết một lớp: thông tin lớp, tiến độ và bảng các buổi học (UC12).
    public partial class ClassDetailPage : UserControl
    {
        private readonly ClassService _service = new ClassService();
        private readonly int _classId;
        private bool _busy;                    // đang xử lý một thao tác (đổi, hủy, xếp buổi bù): chặn bấm chồng

        // Nối trang do bên ngoài (StaffShellView) đăng ký.
        public event EventHandler? BackRequested;

        public ClassDetailPage(int classId)
        {
            InitializeComponent();
            _classId = classId;
            Loaded += async (_, _) => await LoadAsync();
        }

        private async Task LoadAsync()
        {
            ErrorText.Visibility = Visibility.Collapsed;            // Loaded có thể chạy lại khi trang được gắn lại
            try
            {
                ClassDetailDto? detail = await _service.GetClassDetailAsync(_classId);
                if (detail is null)
                {
                    ShowError("Không tìm thấy lớp này.");
                    return;
                }

                List<SessionItemDto> sessions = await _service.GetClassSessionsAsync(_classId);
                ShowDetail(detail, sessions);
            }
            catch (Exception ex)
            {
                ShowError(ex is UnauthorizedAccessException
                    ? ex.Message
                    : "Không tải được thông tin lớp. Vui lòng thử lại.");
            }
        }

        private void ShowDetail(ClassDetailDto detail, List<SessionItemDto> sessions)
        {
            TitleText.Text = detail.ClassName;
            StatusBadgeText.Text = detail.Status;
            StatusBadge.Visibility = Visibility.Visible;

            SportText.Text = string.IsNullOrWhiteSpace(detail.Level) ? detail.SportName : $"{detail.SportName} · {detail.Level}";
            CoachText.Text = detail.CoachName ?? "Chưa phân công";
            RoomText.Text = detail.RoomName;
            DatesText.Text = $"{detail.StartDate:dd/MM/yyyy} → {(detail.EndDate is DateOnly end ? end.ToString("dd/MM/yyyy") : "chưa xác định")}";
            MembersText.Text = $"{detail.ActiveMembers}/{detail.MaxCapacity}";

            int done = sessions.Count(s => s.Status == "Completed");
            ProgressText.Text = $"Đã học {done}/{detail.TotalSessions} buổi";
            ProgressBar.Value = detail.TotalSessions > 0 ? Math.Min(1.0, (double)done / detail.TotalSessions) : 0;
            InfoCard.Visibility = Visibility.Visible;

            // Buổi sắp tới gần nhất: buổi chưa học, chưa hủy, từ hôm nay trở đi.
            int missing = detail.TotalSessions - sessions.Count(s => s.Status != "Cancelled");
            MakeupButton.Content = $"Xếp buổi bù (thiếu {missing})";
            MakeupButton.Visibility = missing > 0 && detail.Status is not ("Cancelled" or "Completed") ? Visibility.Visible : Visibility.Collapsed;

            DateOnly today = DateOnly.FromDateTime(DateTime.Today);
            int? nextId = sessions
                .Where(s => s.Status == "Scheduled" && s.SessionDate >= today)
                .OrderBy(s => s.SessionDate).ThenBy(s => s.StartTime)
                .Select(s => (int?)s.SessionId)
                .FirstOrDefault();

            SessionGrid.ItemsSource = sessions.Select(s => new SessionRow(s, s.SessionId == nextId, today)).ToList();
        }

        private void ShowError(string message)
        {
            ErrorText.Text = message;
            ErrorText.Visibility = Visibility.Visible;
        }

        private void Back_Click(object sender, RoutedEventArgs e) => BackRequested?.Invoke(this, EventArgs.Empty);

        // Bấm "Đổi" ở một buổi: mở hộp thoại đổi coach/phòng, lưu xong thì tải lại bảng.
        private async void Reassign_Click(object sender, RoutedEventArgs e)
        {
            if (_busy || sender is not FrameworkElement { DataContext: SessionRow row }) return;

            _busy = true;
            try
            {
                var dialog = new ReassignSessionDialog(row.SessionId) { Owner = Window.GetWindow(this) };
                if (dialog.ShowDialog() == true)
                {
                    await LoadAsync();
                    Notice.Show($"Đã đổi buổi {row.SessionNo}.");
                }
            }
            finally
            {
                _busy = false;
            }
        }

        // Bấm "Hủy" ở một buổi: xác nhận, hủy, rồi báo kết quả (có buổi bù tự động hay chưa).
        private async void Cancel_Click(object sender, RoutedEventArgs e)
        {
            if (_busy || sender is not FrameworkElement { DataContext: SessionRow row }) return;

            bool confirmed = ConfirmDialog.Ask(Window.GetWindow(this), "Hủy buổi học",
                $"Hủy buổi {row.SessionNo} ({row.DateText}, {row.TimeText})?\n\n" +
                "Hệ thống sẽ thử xếp buổi bù vào cuối lịch với coach và phòng gốc của lớp. " +
                "Nếu không xếp được, bạn có thể xếp buổi bù thủ công sau.",
                "Hủy buổi");
            if (!confirmed) return;

            _busy = true;
            try
            {
                CancelSessionResultDto result = await _service.CancelSessionAsync(row.SessionId);
                await LoadAsync();
                Notice.Show(result.MakeupSessionNo is int no
                    ? $"Đã hủy buổi {row.SessionNo}. Buổi bù: buổi {no}, Thứ {result.MakeupDayOfWeek}, {result.MakeupDate:dd/MM/yyyy}."
                    : $"Đã hủy buổi {row.SessionNo}. Chưa xếp được buổi bù tự động, hãy bấm “Xếp buổi bù”.");
            }
            catch (Exception ex)
            {
                await LoadAsync();                                   // trạng thái buổi có thể đã đổi: tải lại rồi báo lỗi
                ShowError(ex is InvalidOperationException or UnauthorizedAccessException
                    ? ex.Message
                    : "Không hủy được buổi học. Vui lòng thử lại.");
            }
            finally
            {
                _busy = false;
            }
        }

        // Bấm "Xếp buổi bù": mở hộp thoại chọn ngày, khung giờ, coach, phòng.
        private async void Makeup_Click(object sender, RoutedEventArgs e)
        {
            if (_busy) return;

            _busy = true;
            try
            {
                var dialog = new AddMakeupSessionDialog(_classId) { Owner = Window.GetWindow(this) };
                bool saved = dialog.ShowDialog() == true;
                await LoadAsync();                                   // luôn tải lại: lớp có thể đã đủ buổi trong lúc mở hộp thoại
                if (saved)
                {
                    Notice.Show(dialog.CreatedSessionNo > 0 ? $"Đã xếp buổi bù: buổi {dialog.CreatedSessionNo}." : "Đã xếp buổi bù.");
                }
            }
            finally
            {
                _busy = false;
            }
        }

        // Một dòng của bảng: chuyển dữ liệu buổi học thành chữ hiển thị.
        private sealed class SessionRow
        {
            public SessionRow(SessionItemDto s, bool isNext, DateOnly today)
            {
                SessionId = s.SessionId;
                SessionNo = s.SessionNo;
                CanEdit = SessionRules.CanModify(s.Status, s.SessionDate, s.StartTime);
                DateText = $"Thứ {s.DayOfWeek}, {s.SessionDate:dd/MM/yyyy}";
                TimeText = $"{s.StartTime:HH:mm} – {s.EndTime:HH:mm}";
                RoomName = s.RoomName;
                CoachName = s.CoachName;
                Status = s.Status;
                StatusText = s.Status switch
                {
                    "Completed" => "Đã học",
                    "Cancelled" => "Đã hủy",
                    "Scheduled" when s.SessionDate < today => "Chưa cập nhật",   // đã qua ngày học nhưng chưa đánh dấu hoàn thành
                    "Scheduled" => "Sắp học",
                    _ => s.Status
                };
                IsNext = isNext;

                var notes = new List<string>();
                if (s.IsMakeup) notes.Add("Buổi bù");
                if (s.IsRoomChanged || s.IsCoachChanged)
                {
                    string what = s.IsRoomChanged && s.IsCoachChanged ? "phòng, coach" : s.IsRoomChanged ? "phòng" : "coach";
                    notes.Add($"Đổi tạm {what}");
                }
                if (isNext) notes.Add("Buổi tiếp theo");
                NoteText = string.Join(" · ", notes);
            }

            public int SessionId { get; }
            public bool CanEdit { get; }
            public int SessionNo { get; }
            public string DateText { get; }
            public string TimeText { get; }
            public string RoomName { get; }
            public string CoachName { get; }
            public string Status { get; }
            public string StatusText { get; }
            public string NoteText { get; }
            public bool IsNext { get; }
        }
    }
}
