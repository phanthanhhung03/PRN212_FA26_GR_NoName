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
    // UC08: Lịch dạy và học viên (dành cho Coach)
    public partial class CoachSchedulePage : UserControl
    {
        private readonly CoachScheduleService _service = new();
        private bool _ready;
        private List<CoachSessionDto> _allSessions = new();
        private CoachSessionDto? _selectedSession;

        public event EventHandler<int>? AttendanceRequested;

        public CoachSchedulePage()
        {
            InitializeComponent();

            DateRangeFilter.ItemsSource = new[] { "Tất cả thời gian", "Hôm nay", "Tuần này", "Tháng này" };
            DateRangeFilter.SelectedIndex = 0;

            StatusFilter.ItemsSource = new[] { "Mọi trạng thái", "Scheduled", "Completed", "Cancelled" };
            StatusFilter.SelectedIndex = 0;

            Loaded += async (_, _) => await InitAsync();
        }

        private async Task InitAsync()
        {
            try
            {
                var classes = await _service.GetMyClassesAsync();
                var classOptions = new List<CoachClassOptionDto>
                {
                    new(0, "Tất cả các lớp", "")
                };
                classOptions.AddRange(classes);
                ClassFilter.ItemsSource = classOptions;
                ClassFilter.SelectedIndex = 0;

                _ready = true;
                await LoadSessionsAsync();
            }
            catch (Exception ex)
            {
                Notice.Show("Lỗi tải dữ liệu: " + ex.Message);
            }
        }

        private async Task LoadSessionsAsync()
        {
            if (!_ready) return;

            try
            {
                int? classId = null;
                if (ClassFilter.SelectedValue is int cId && cId > 0)
                {
                    classId = cId;
                }

                DateOnly? fromDate = null;
                DateOnly? toDate = null;
                var today = DateOnly.FromDateTime(DateTime.Today);

                switch (DateRangeFilter.SelectedIndex)
                {
                    case 1: // Hôm nay
                        fromDate = today;
                        toDate = today;
                        break;
                    case 2: // Tuần này
                        int diff = (7 + (int)DateTime.Today.DayOfWeek - (int)DayOfWeek.Monday) % 7;
                        fromDate = today.AddDays(-diff);
                        toDate = fromDate.Value.AddDays(6);
                        break;
                    case 3: // Tháng này
                        fromDate = new DateOnly(today.Year, today.Month, 1);
                        toDate = fromDate.Value.AddMonths(1).AddDays(-1);
                        break;
                }

                string? status = null;
                if (StatusFilter.SelectedIndex > 0)
                {
                    status = StatusFilter.SelectedItem?.ToString();
                }

                _allSessions = await _service.GetMySessionsAsync(classId, fromDate, toDate, status);
                SessionsGrid.ItemsSource = _allSessions;

                ClearSelection();
            }
            catch (Exception ex)
            {
                Notice.Show("Không thể tải danh sách buổi dạy: " + ex.Message);
            }
        }

        private void ClearSelection()
        {
            _selectedSession = null;
            SelectedClassText.Text = "Chọn một buổi học bên trái để xem";
            DetailBox.Visibility = Visibility.Collapsed;
            MembersGrid.ItemsSource = null;
            RosterTitleText.Text = "Danh sách học viên";
            GoToAttendanceButton.IsEnabled = false;
        }

        private async void SessionsGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (SessionsGrid.SelectedItem is not CoachSessionDto session)
            {
                ClearSelection();
                return;
            }

            _selectedSession = session;
            SelectedClassText.Text = $"{session.ClassName} ({session.SportName})";
            SessionTimeInfo.Text = $"{session.DayOfWeekDisplay}, {session.SessionDate:dd/MM/yyyy} · {session.TimeDisplay}";
            SessionRoomInfo.Text = $"Phòng: {session.RoomName} · Buổi số {session.ProgressDisplay}";
            SessionStatusInfo.Text = $"Trạng thái: {session.Status} (Có mặt: {session.PresentCount}/{session.MemberCount})";
            DetailBox.Visibility = Visibility.Visible;
            GoToAttendanceButton.IsEnabled = session.Status != "Cancelled";

            try
            {
                var members = await _service.GetSessionMembersAsync(session.SessionId);
                MembersGrid.ItemsSource = members;
                RosterTitleText.Text = $"Danh sách học viên ({members.Count})";
            }
            catch (Exception ex)
            {
                Notice.Show("Lỗi tải học viên: " + ex.Message);
            }
        }

        private async void Filter_Changed(object sender, SelectionChangedEventArgs e)
        {
            await LoadSessionsAsync();
        }

        private async void Reload_Click(object sender, RoutedEventArgs e)
        {
            await LoadSessionsAsync();
            Notice.Show("Đã làm mới dữ liệu.");
        }

        private void QuickAttendance_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button { DataContext: CoachSessionDto session })
            {
                if (session.Status == "Cancelled")
                {
                    Notice.Show("Buổi học đã bị hủy, không thể điểm danh.");
                    return;
                }
                AttendanceRequested?.Invoke(this, session.SessionId);
            }
        }

        private void GoToAttendance_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedSession is not null)
            {
                if (_selectedSession.Status == "Cancelled")
                {
                    Notice.Show("Buổi học đã bị hủy, không thể điểm danh.");
                    return;
                }
                AttendanceRequested?.Invoke(this, _selectedSession.SessionId);
            }
        }
    }
}
