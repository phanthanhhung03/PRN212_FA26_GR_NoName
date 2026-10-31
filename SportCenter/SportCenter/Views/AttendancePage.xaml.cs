using SportCenter.Dtos;
using SportCenter.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace SportCenter.Views
{
    // UC09: Điểm danh học viên (dành cho Coach)
    public partial class AttendancePage : UserControl
    {
        private readonly CoachScheduleService _service = new();
        private int? _targetSessionId;
        private bool _ready;
        private ObservableCollection<SessionMemberAttendanceDto> _members = new();

        public event EventHandler? BackRequested;

        public class SessionSelectOption
        {
            public int SessionId { get; set; }
            public string DisplayInfo { get; set; } = string.Empty;
            public CoachSessionDto Dto { get; set; } = null!;
        }

        public AttendancePage(int? initialSessionId = null)
        {
            InitializeComponent();
            _targetSessionId = initialSessionId;

            if (_targetSessionId.HasValue)
            {
                BackButton.Visibility = Visibility.Visible;
            }

            Loaded += async (_, _) => await InitAsync();
        }

        public void SetTargetSession(int sessionId)
        {
            _targetSessionId = sessionId;
            BackButton.Visibility = Visibility.Visible;
            if (_ready)
            {
                SessionSelector.SelectedValue = sessionId;
            }
        }

        private async Task InitAsync()
        {
            try
            {
                var sessions = await _service.GetMySessionsAsync();
                var options = sessions.Select(s => new SessionSelectOption
                {
                    SessionId = s.SessionId,
                    DisplayInfo = $"{s.SessionDate:dd/MM/yyyy} ({s.DayOfWeekDisplay}) {s.TimeDisplay} · {s.ClassName} (B.{s.ProgressDisplay}) - {s.Status}",
                    Dto = s
                }).ToList();

                SessionSelector.ItemsSource = options;

                if (options.Count == 0)
                {
                    Notice.Show("Bạn chưa có buổi dạy nào trong hệ thống.");
                    return;
                }

                _ready = true;

                if (_targetSessionId.HasValue && options.Any(o => o.SessionId == _targetSessionId.Value))
                {
                    SessionSelector.SelectedValue = _targetSessionId.Value;
                }
                else
                {
                    // Chọn buổi Scheduled gần nhất hoặc buổi đầu tiên
                    var upcoming = options.FirstOrDefault(o => o.Dto.Status == "Scheduled") ?? options[0];
                    SessionSelector.SelectedValue = upcoming.SessionId;
                }
            }
            catch (Exception ex)
            {
                Notice.Show("Lỗi tải danh sách buổi dạy: " + ex.Message);
            }
        }

        private async void SessionSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!_ready || SessionSelector.SelectedItem is not SessionSelectOption option) return;

            await LoadSessionAttendanceAsync(option.SessionId, option.Dto);
        }

        private async Task LoadSessionAttendanceAsync(int sessionId, CoachSessionDto? sessionDto = null)
        {
            try
            {
                if (sessionDto is null)
                {
                    var all = await _service.GetMySessionsAsync();
                    sessionDto = all.FirstOrDefault(s => s.SessionId == sessionId);
                }

                if (sessionDto is not null)
                {
                    ClassTitleText.Text = $"{sessionDto.ClassName} · {sessionDto.SportName}";
                    SessionTimeText.Text = $"Thời gian: {sessionDto.DayOfWeekDisplay}, {sessionDto.SessionDate:dd/MM/yyyy} ({sessionDto.TimeDisplay}) - Buổi {sessionDto.ProgressDisplay}";
                    SessionRoomText.Text = $"Phòng: {sessionDto.RoomName}";
                    SessionStatusText.Text = $"Trạng thái: {sessionDto.Status}";

                    MarkCompletedCheckBox.IsChecked = sessionDto.Status == "Completed";
                    MarkCompletedCheckBox.IsEnabled = sessionDto.Status == "Scheduled";
                }

                var list = await _service.GetSessionMembersAsync(sessionId);
                _members = new ObservableCollection<SessionMemberAttendanceDto>(list);
                AttendanceGrid.ItemsSource = _members;

                SummaryCountText.Text = $"Tổng số: {_members.Count} học viên (Có mặt: {_members.Count(m => m.Status == "Present" || m.Status == "Late")})";

                if (sessionDto?.Status == "Cancelled")
                {
                    Notice.Show("Buổi học này đã bị hủy, không thể điểm danh.");
                    SaveButton.IsEnabled = false;
                }
                else
                {
                    SaveButton.IsEnabled = true;
                }
            }
            catch (Exception ex)
            {
                Notice.Show("Lỗi tải thông tin học viên: " + ex.Message);
            }
        }

        private void MarkAllPresent_Click(object sender, RoutedEventArgs e)
        {
            foreach (var m in _members)
            {
                m.Status = "Present";
            }
            AttendanceGrid.Items.Refresh();
            UpdateSummary();
        }

        private void MarkAllAbsent_Click(object sender, RoutedEventArgs e)
        {
            foreach (var m in _members)
            {
                m.Status = "Absent";
            }
            AttendanceGrid.Items.Refresh();
            UpdateSummary();
        }

        private void UpdateSummary()
        {
            SummaryCountText.Text = $"Tổng số: {_members.Count} học viên (Có mặt: {_members.Count(m => m.Status == "Present" || m.Status == "Late")})";
        }

        private async void SaveAttendance_Click(object sender, RoutedEventArgs e)
        {
            if (SessionSelector.SelectedValue is not int sessionId) return;

            try
            {
                SaveButton.IsEnabled = false;

                var updates = _members.Select(m => new AttendanceItemUpdateDto(
                    m.MemberId,
                    m.Status,
                    m.Remarks
                )).ToList();

                bool markCompleted = MarkCompletedCheckBox.IsChecked == true;
                await _service.SaveAttendanceAsync(sessionId, updates, markCompleted);

                Notice.Show("Đã lưu điểm danh thành công!");

                // Tải lại để cập nhật ngày giờ và trạng thái
                await LoadSessionAttendanceAsync(sessionId);
            }
            catch (Exception ex)
            {
                Notice.Show("Lỗi lưu điểm danh: " + ex.Message);
            }
            finally
            {
                SaveButton.IsEnabled = true;
            }
        }

        private async void Reload_Click(object sender, RoutedEventArgs e)
        {
            if (SessionSelector.SelectedValue is int sessionId)
            {
                await LoadSessionAttendanceAsync(sessionId);
                Notice.Show("Đã làm mới dữ liệu.");
            }
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            BackRequested?.Invoke(this, EventArgs.Empty);
        }
    }
}
