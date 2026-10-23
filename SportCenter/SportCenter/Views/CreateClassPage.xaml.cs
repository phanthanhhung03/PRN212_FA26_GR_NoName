using SportCenter.Dtos;
using SportCenter.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;

namespace SportCenter.Views
{
    // Form "Mở lớp mới" (UC12, UC13).
    // Luồng: môn → khóa → phòng → coach → ngày khai giảng → khung giờ.
    // Khi đã đủ thông tin, các khung giờ mà phòng hoặc coach đang bận bị xám và không chọn được;
    // trạng thái này được tính lại (sp_busy_slots) sau mỗi lần đổi phòng, coach, ngày, khóa hoặc khung giờ.
    public partial class CreateClassPage : UserControl
    {
        private readonly ClassService _service = new ClassService();
        private List<TimeSlotDto> _slots = new();

        private bool _ready;          // tránh xử lý sự kiện khi InitializeComponent còn đang dựng control
        private bool _suspend;        // đang gán nhiều ô cùng lúc: bỏ qua sự kiện, làm mới một lần ở cuối
        private int _version;         // mỗi lần làm mới tăng một bậc; kết quả của lần cũ bị bỏ
        private bool _gridFresh;       // lưới khung giờ đã khớp với phòng/coach/ngày hiện tại (mới tính xong, không lỗi)
        private bool _creating;         // đang gọi tạo lớp: chặn bấm hai lần
        private int _sportToken;      // mỗi lần đổi môn tăng một bậc; kết quả tải khóa/phòng/coach của lần cũ bị bỏ

        // Nối trang do bên ngoài (StaffShellView) đăng ký.
        public event EventHandler? BackRequested;
        public event EventHandler? Created;

        public CreateClassPage()
        {
            InitializeComponent();
            _ready = true;
            StartDatePicker.DisplayDateStart = DateTime.Today;      // không chọn ngày quá khứ
            Loaded += async (_, _) => await InitAsync();
        }

        private async Task InitAsync()
        {
            try
            {
                SportBox.ItemsSource = await _service.GetSportsAsync();
                _slots = await _service.GetTimeSlotsAsync();
                BuildSlotGrid();
                await RefreshAvailabilityAsync();
            }
            catch (Exception ex)
            {
                ShowError(ex);
            }
        }

        // ---------- Lưới khung giờ: hàng = giờ bắt đầu, cột = Thứ 2 đến Thứ 7 ----------

        private void BuildSlotGrid()
        {
            SlotGrid.Children.Clear();
            SlotGrid.RowDefinitions.Clear();
            SlotGrid.ColumnDefinitions.Clear();

            List<TimeOnly> starts = _slots.Select(s => s.StartTime).Distinct().OrderBy(t => t).ToList();
            List<byte> days = _slots.Select(s => s.DayOfWeek).Distinct().OrderBy(d => d).ToList();

            SlotGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            foreach (byte _ in days)
                SlotGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            SlotGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            foreach (TimeOnly _ in starts)
                SlotGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            // Hàng tiêu đề: Thứ 2 ... Thứ 7
            for (int c = 0; c < days.Count; c++)
                AddCell(new TextBlock
                {
                    Text = $"Thứ {days[c]}",
                    FontWeight = FontWeights.SemiBold,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(0, 0, 6, 6)
                }, 0, c + 1);

            for (int r = 0; r < starts.Count; r++)
            {
                TimeSlotDto? first = _slots.FirstOrDefault(s => s.StartTime == starts[r]);
                AddCell(new TextBlock
                {
                    Text = first is null ? "" : $"{first.StartTime:HH:mm} – {first.EndTime:HH:mm}",
                    FontSize = 12,
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(0, 0, 12, 6)
                }, r + 1, 0);

                for (int c = 0; c < days.Count; c++)
                {
                    TimeSlotDto? slot = _slots.FirstOrDefault(s => s.DayOfWeek == days[c] && s.StartTime == starts[r]);
                    if (slot is null) continue;

                    var toggle = new ToggleButton
                    {
                        Content = $"{slot.StartTime:HH:mm}",
                        Tag = slot.TimeSlotId,
                        Style = (Style)FindResource("SlotToggle"),
                        IsEnabled = false                           // chưa đủ thông tin nên chưa chọn được
                    };
                    ToolTipService.SetShowOnDisabled(toggle, true);
                    System.Windows.Automation.AutomationProperties.SetName(toggle, $"Thứ {days[c]} {slot.StartTime:HH:mm} đến {slot.EndTime:HH:mm}");
                    toggle.Click += async (_, _) => await RefreshAvailabilityAsync();
                    AddCell(toggle, r + 1, c + 1);
                }
            }
        }

        private void AddCell(UIElement element, int row, int column)
        {
            Grid.SetRow(element, row);
            Grid.SetColumn(element, column);
            SlotGrid.Children.Add(element);
        }

        private IEnumerable<ToggleButton> SlotToggles() => SlotGrid.Children.OfType<ToggleButton>();

        private List<int> SelectedSlotIds() => SlotToggles()
            .Where(b => b.IsChecked == true)
            .Select(b => (int)b.Tag)
            .ToList();

        // ---------- Lấy thông tin đã chọn (null nếu chưa đủ) ----------

        private sealed record Selection(int SportId, CourseOptionDto Course, RoomOptionDto Room, CoachOptionDto Coach, DateOnly StartDate);

        private Selection? GetSelection()
        {
            if (SportBox.SelectedValue is not int sportId) return null;
            if (CourseBox.SelectedItem is not CourseOptionDto course) return null;
            if (RoomBox.SelectedItem is not RoomOptionDto room) return null;
            if (CoachBox.SelectedItem is not CoachOptionDto coach) return null;
            if (StartDatePicker.SelectedDate is not DateTime start || start.Date < DateTime.Today) return null;
            return new Selection(sportId, course, room, coach, DateOnly.FromDateTime(start));
        }

        // ---------- Thay đổi ô nhập ----------

        private async void Sport_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (!_ready || _suspend) return;
            int token = ++_sportToken;                               // chỉ kết quả của lần đổi môn mới nhất được dùng

            SetSuspended(() =>
            {
                CourseBox.ItemsSource = null;
                RoomBox.ItemsSource = null;
                CoachBox.ItemsSource = null;
                CourseBox.IsEnabled = RoomBox.IsEnabled = CoachBox.IsEnabled = false;
                CourseInfoText.Text = RoomInfoText.Text = CoachInfoText.Text = "";
                CapacityBox.Text = "";
                CapacityHint.Text = "";
            });
            ResetGrid(clearChecks: true);                            // lưới của môn cũ không còn hiệu lực

            if (SportBox.SelectedValue is int sportId)
            {
                try
                {
                    var courses = await _service.GetCoursesAsync(sportId);
                    var rooms = await _service.GetRoomsBySportAsync(sportId);
                    var coaches = await _service.GetCoachesBySportAsync(sportId);
                    if (token != _sportToken) return;                // đã đổi môn lần nữa: bỏ kết quả cũ

                    SetSuspended(() =>
                    {
                        CourseBox.ItemsSource = courses;
                        RoomBox.ItemsSource = rooms;
                        CoachBox.ItemsSource = coaches;
                        CourseBox.IsEnabled = RoomBox.IsEnabled = CoachBox.IsEnabled = true;
                        CourseInfoText.Text = courses.Count == 0 ? "Môn này chưa có khóa học hoạt động." : "";
                        RoomInfoText.Text = rooms.Count == 0 ? "Môn này chưa có phòng hoạt động." : "";
                        CoachInfoText.Text = coaches.Count == 0 ? "Môn này chưa có coach hoạt động." : "";
                    });
                }
                catch (Exception ex)
                {
                    if (token == _sportToken) ShowError(ex);
                    return;
                }
            }

            await RefreshAvailabilityAsync();
        }

        // Chạy một đoạn đồng bộ gán nhiều ô, bỏ qua các sự kiện SelectionChanged do chính việc gán gây ra.
        private void SetSuspended(Action action)
        {
            _suspend = true;
            try { action(); }
            finally { _suspend = false; }
        }

        private async void Course_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (!_ready || _suspend) return;
            CourseInfoText.Text = "";                                // Level và số buổi đã hiện ngay trong ô chọn
            await RefreshAvailabilityAsync();
        }

        private async void Room_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (!_ready || _suspend) return;

            if (RoomBox.SelectedItem is RoomOptionDto room)
            {
                CapacityBox.Text = room.Capacity.ToString();        // tự điền theo sức chứa, sửa thấp hơn được
                RoomInfoText.Text = $"Sức chứa {room.Capacity} chỗ";
            }
            else
            {
                CapacityBox.Text = "";
                RoomInfoText.Text = "";
            }
            await RefreshAvailabilityAsync();
        }

        private async void Coach_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (!_ready || _suspend) return;
            await RefreshAvailabilityAsync();
        }

        private async void StartDate_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (!_ready || _suspend) return;
            StartDatePicker.DisplayDateStart = DateTime.Today;       // gõ ngày quá khứ không làm lịch cho chọn quá khứ
            await RefreshAvailabilityAsync();
        }

        private void ClassName_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_ready) UpdateCreateEnabled();
        }

        private void Capacity_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !e.Text.All(char.IsDigit);                   // sĩ số chỉ nhận chữ số
        }

        private void Capacity_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!_ready) return;
            UpdateCapacityHint();
            UpdateCreateEnabled();
        }

        // Sĩ số hợp lệ: số nguyên từ 1 đến sức chứa của phòng đã chọn.
        private bool TryGetCapacity(out int capacity)
        {
            capacity = 0;
            return RoomBox.SelectedItem is RoomOptionDto room
                && int.TryParse(CapacityBox.Text, out capacity)
                && capacity >= 1 && capacity <= room.Capacity;
        }

        private void UpdateCapacityHint()
        {
            if (RoomBox.SelectedItem is not RoomOptionDto room)
            {
                CapacityHint.Text = "";
                return;
            }
            if (string.IsNullOrWhiteSpace(CapacityBox.Text))
            {
                CapacityHint.Text = $"Nhập từ 1 đến {room.Capacity}";
                CapacityHint.Foreground = (Brush)FindResource("MutedTextBrush");
                return;
            }

            bool ok = TryGetCapacity(out _);
            CapacityHint.Text = ok ? $"Tối đa {room.Capacity}" : $"Phải từ 1 đến {room.Capacity}";
            CapacityHint.Foreground = (Brush)FindResource(ok ? "MutedTextBrush" : "ErrorBrush");
        }

        // ---------- Tính lại khung giờ bận ----------

        // Làm mới trạng thái lưới và dòng tóm tắt. Gọi lại nhiều lần liên tiếp thì chỉ lần cuối có hiệu lực.
        // Trong lúc chưa tính xong (hoặc tính lỗi) nút Tạo lớp tắt, vì lưới có thể đang cũ.
        private async Task RefreshAvailabilityAsync()
        {
            int version = ++_version;
            HideError();
            _gridFresh = false;
            UpdateCreateEnabled();

            Selection? sel = GetSelection();
            if (sel is null)
            {
                ResetGrid(clearChecks: false);                       // giữ các khung đã chọn, chỉ khóa lưới
                if (StartDatePicker.SelectedDate is DateTime picked && picked.Date < DateTime.Today)
                {
                    SlotHint.Text = "Ngày khai giảng không được ở quá khứ.";
                }
                return;
            }

            try
            {
                bool removed = false;
                bool stable = false;

                // Bỏ chọn một ô vì bận làm lịch đổi, nên tính lại cho đến khi không còn ô nào bị bỏ chọn.
                for (int pass = 0; pass <= _slots.Count && !stable; pass++)
                {
                    List<BusySlotDto> busy = await _service.GetBusySlotsAsync(
                        sel.Room.RoomId, sel.Coach.UserId, sel.StartDate, SelectedSlotIds(), sel.Course.TotalSessions);
                    if (version != _version) return;                 // có lần làm mới mới hơn

                    bool uncheckedAny = ApplyBusy(busy);
                    removed |= uncheckedAny;
                    stable = !uncheckedAny;
                }

                SlotHint.Text = removed
                    ? "Một số khung đã chọn bị bỏ vì phòng hoặc coach bận. Ô xám là khung không chọn được."
                    : "Ô xám là khung mà phòng hoặc coach đã có lớp, không chọn được.";
                _gridFresh = stable;

                await UpdateSummaryAsync(sel, version);
            }
            catch (Exception ex)
            {
                if (version != _version) return;
                // Không rõ khung nào bận: khóa lưới (giữ các khung đã chọn) và báo lỗi, thay vì để lưới cũ.
                _gridFresh = false;
                DisableToggles(clearChecks: false);
                SlotHint.Text = "Không tải được khung giờ. Hãy đổi lại một ô thông tin hoặc thử lại.";
                SetSummary("Chưa tính được lịch.", muted: true);
                ShowError(ex);
            }
            finally
            {
                if (version == _version) UpdateCreateEnabled();
            }
        }

        // Khóa lưới khi chưa đủ thông tin; hủy mọi lần làm mới đang chờ.
        private void ResetGrid(bool clearChecks)
        {
            _version++;
            _gridFresh = false;
            DisableToggles(clearChecks);
            SlotHint.Text = "Chọn môn, khóa, phòng, coach và ngày khai giảng để xem khung giờ còn trống.";
            SetSummary("Chọn ít nhất một khung giờ.", muted: true);
            UpdateCreateEnabled();
        }

        private void DisableToggles(bool clearChecks)
        {
            foreach (ToggleButton toggle in SlotToggles())
            {
                if (clearChecks) toggle.IsChecked = false;
                toggle.IsEnabled = false;
                toggle.ToolTip = null;
            }
        }

        // Cập nhật từng ô theo danh sách bận. Trả về true nếu có ô đang chọn bị bỏ chọn.
        private bool ApplyBusy(List<BusySlotDto> busy)
        {
            Dictionary<int, string> reasons = busy.ToDictionary(b => b.TimeSlotId, b => b.Reason);
            bool uncheckedAny = false;

            foreach (ToggleButton toggle in SlotToggles())
            {
                int slotId = (int)toggle.Tag;
                if (reasons.TryGetValue(slotId, out string? reason))
                {
                    if (toggle.IsChecked == true)
                    {
                        toggle.IsChecked = false;
                        uncheckedAny = true;
                    }
                    toggle.IsEnabled = false;
                    toggle.ToolTip = reason switch
                    {
                        "Room" => "Phòng đã có lớp vào khung giờ này",
                        "Coach" => "Coach đã có lớp vào khung giờ này",
                        _ => "Phòng và coach đều đã bận vào khung giờ này"
                    };
                }
                else
                {
                    toggle.IsEnabled = true;
                    toggle.ToolTip = null;
                }
            }
            return uncheckedAny;
        }

        private async Task UpdateSummaryAsync(Selection sel, int version)
        {
            List<int> selected = SelectedSlotIds();
            if (selected.Count == 0)
            {
                SetSummary("Chọn ít nhất một khung giờ.", muted: true);
                return;
            }

            SetSummary("Đang tính lịch…", muted: true);
            List<PlannedDateDto> dates = await _service.PreviewDatesAsync(sel.StartDate, selected, sel.Course.TotalSessions);
            if (version != _version) return;

            if (dates.Count == 0)
            {
                SetSummary("Không sinh được lịch học với các khung giờ đã chọn.", muted: true);
                return;
            }

            DateOnly first = dates[0].SessionDate, last = dates[^1].SessionDate;
            int weeks = (last.DayNumber - first.DayNumber) / 7 + 1;
            SetSummary($"{dates.Count} buổi · {selected.Count} khung mỗi tuần · khoảng {weeks} tuần · " +
                       $"từ {first:dd/MM/yyyy} đến {last:dd/MM/yyyy}", muted: false);
        }

        private void SetSummary(string text, bool muted)
        {
            PreviewText.Text = text;
            PreviewText.Foreground = (Brush)FindResource(muted ? "MutedTextBrush" : "PrimaryDarkBrush");
        }

        // Nút Tạo lớp chỉ bật khi đủ thông tin hợp lệ và có ít nhất một khung giờ.
        private void UpdateCreateEnabled()
        {
            CreateButton.IsEnabled = _gridFresh && !_creating
                && GetSelection() is not null
                && !string.IsNullOrWhiteSpace(ClassNameBox.Text)
                && TryGetCapacity(out _)
                && SelectedSlotIds().Count > 0;
        }

        // ---------- Tạo lớp ----------

        private async void Create_Click(object sender, RoutedEventArgs e)
        {
            if (_creating) return;

            Selection? sel = GetSelection();
            if (!_gridFresh || sel is null || !TryGetCapacity(out int capacity) || string.IsNullOrWhiteSpace(ClassNameBox.Text))
            {
                ShowError("Vui lòng điền đủ và đúng thông tin lớp.");
                return;
            }

            List<int> slotIds = SelectedSlotIds();
            if (slotIds.Count == 0)
            {
                ShowError("Vui lòng chọn ít nhất một khung giờ.");
                return;
            }

            var request = new CreateClassRequest(
                sel.Course.CourseId,
                ClassNameBox.Text.Trim(),
                sel.StartDate,
                capacity,
                sel.Coach.UserId,
                sel.Room.RoomId,
                slotIds);

            SetSaving(true);
            try
            {
                await _service.CreateClassAsync(request);
                Created?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                SetSaving(false);
                // Lớp có thể vừa bị người khác đặt trùng: tính lại khung bận rồi hiện lỗi.
                await RefreshAvailabilityAsync();
                ShowError(ex);
            }
        }

        // Trong lúc lưu: khóa toàn bộ form để không sửa hay bấm lần hai.
        private void SetSaving(bool saving)
        {
            _creating = saving;
            FormScroll.IsEnabled = !saving;
            BackButton.IsEnabled = !saving;                          // rời trang giữa chừng sẽ mất kết quả lưu
            CreateButton.Content = saving ? "Đang lưu…" : "Tạo lớp";
            UpdateCreateEnabled();
        }

        private void Back_Click(object sender, RoutedEventArgs e) => BackRequested?.Invoke(this, EventArgs.Empty);

        // ---------- Thông báo lỗi ----------

        private void ShowError(Exception ex)
        {
            // InvalidOperationException và UnauthorizedAccessException mang thông báo tiếng Việt từ Service.
            ShowError(ex switch
            {
                InvalidOperationException or UnauthorizedAccessException => ex.Message,
                System.Data.Common.DbException => "Không kết nối được máy chủ. Vui lòng thử lại.",
                _ => "Đã xảy ra lỗi không mong muốn. Vui lòng thử lại."
            });
        }

        private void ShowError(string message)
        {
            ErrorText.Text = message;
            ErrorBanner.Visibility = Visibility.Visible;
        }

        private void HideError() => ErrorBanner.Visibility = Visibility.Collapsed;
    }
}
