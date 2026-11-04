using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using SportCenter.Dtos;
using SportCenter.Services;

namespace SportCenter.Views;

// UC10: Nhân viên (Staff) ghi danh hộ học viên tại quầy.
public partial class StaffEnrollmentPage : UserControl
{
    private readonly EnrollmentService _service = new();
    private List<MemberSearchOptionDto> _members = new();
    private List<ClassEnrollOptionDto> _classes = new();

    private MemberSearchOptionDto? _selectedMember;
    private ClassEnrollOptionDto? _selectedClass;

    public StaffEnrollmentPage()
    {
        InitializeComponent();
        Loaded += async (_, _) => await InitAsync();
    }

    private async Task InitAsync()
    {
        try
        {
            await LoadClassesAsync();
            await SearchMembersAsync(string.Empty);
            await LoadRecentEnrollmentsAsync();
        }
        catch (Exception ex)
        {
            Notice.Show("Lỗi tải dữ liệu: " + ex.Message);
        }
    }

    private async Task LoadClassesAsync()
    {
        _classes = await _service.GetOpenClassesForStaffAsync();
        ClassSelector.ItemsSource = _classes;
        if (_classes.Count > 0)
        {
            ClassSelector.SelectedIndex = 0;
        }
    }

    private async Task SearchMembersAsync(string keyword)
    {
        _members = await _service.SearchMembersAsync(keyword);
        MemberResultsList.ItemsSource = _members;
    }

    private async Task LoadRecentEnrollmentsAsync()
    {
        var recent = await _service.GetRecentEnrollmentsAsync();
        RecentGrid.ItemsSource = recent;
    }

    private async void MemberSearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        await SearchMembersAsync(MemberSearchBox.Text);
    }

    private void MemberResultsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (MemberResultsList.SelectedItem is not MemberSearchOptionDto member)
        {
            return;
        }

        _selectedMember = member;
        SelectedMemberName.Text = member.FullName;
        SelectedMemberContact.Text = $"Email: {member.Email}   |   SĐT: {member.Phone ?? "Chưa có"}";

        // Ẩn khung tìm kiếm và hiện thẻ học viên được chọn
        SearchPanel.Visibility = Visibility.Collapsed;
        SelectedMemberBox.Visibility = Visibility.Visible;
        InlineErrorBorder.Visibility = Visibility.Collapsed;
    }

    private void ChangeMember_Click(object sender, RoutedEventArgs e)
    {
        _selectedMember = null;
        MemberResultsList.SelectedItem = null;
        SelectedMemberBox.Visibility = Visibility.Collapsed;
        SearchPanel.Visibility = Visibility.Visible;
        MemberSearchBox.Focus();
        InlineErrorBorder.Visibility = Visibility.Collapsed;
    }

    private void ClassSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        InlineErrorBorder.Visibility = Visibility.Collapsed;
        if (ClassSelector.SelectedItem is not ClassEnrollOptionDto c)
        {
            _selectedClass = null;
            SelectedClassBox.Visibility = Visibility.Collapsed;
            return;
        }

        _selectedClass = c;
        SelectedClassTitle.Text = $"Lớp: {c.ClassName} (Môn: {c.SportName} - Khóa: {c.CourseName})";
        SelectedClassSchedule.Text = $"Lịch học: {c.ScheduleText}";
        SelectedClassRoomCoach.Text = $"Phòng: {c.RoomName}   |   HLV: {c.CoachName}   |   Còn trống: {c.SeatsLeft}/{c.MaxCapacity}";
        SelectedClassFee.Text = $"Học phí: {c.TuitionFee:N0} ₫";
        SelectedClassBox.Visibility = Visibility.Visible;
    }

    private async void SubmitEnroll_Click(object sender, RoutedEventArgs e)
    {
        InlineErrorBorder.Visibility = Visibility.Collapsed;

        if (_selectedMember is null)
        {
            Notice.Show("Vui lòng tìm và chọn một học viên.");
            MessageBox.Show("Vui lòng tìm và chọn một học viên trước khi ghi danh.", "Thiếu thông tin", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        if (_selectedClass is null)
        {
            Notice.Show("Vui lòng chọn một lớp học.");
            MessageBox.Show("Vui lòng chọn một lớp học.", "Thiếu thông tin", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        bool isPaid = PaidAtCounterCheck.IsChecked == true;
        string confirmMsg = $"Xác nhận ghi danh học viên '{_selectedMember.FullName}' vào lớp '{_selectedClass.ClassName}'?\n\n" +
                           $"• Học phí: {_selectedClass.TuitionFee:N0} ₫\n" +
                           $"• Thu phí tại quầy: {(isPaid ? "Đã thu (Kích hoạt ngay)" : "Chưa thu (Chờ thanh toán)")}";

        var confirm = MessageBox.Show(confirmMsg, "Xác nhận ghi danh hộ", MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (confirm != MessageBoxResult.Yes) return;

        try
        {
            SubmitEnrollButton.IsEnabled = false;
            await _service.StaffEnrollAsync(_selectedClass.ClassId, _selectedMember.MemberId, isPaid);

            Notice.Show($"Đã ghi danh thành công cho '{_selectedMember.FullName}' vào lớp '{_selectedClass.ClassName}'!");

            // Làm mới lớp học và lịch sử ghi danh
            await LoadClassesAsync();
            await LoadRecentEnrollmentsAsync();
        }
        catch (Exception ex)
        {
            // Hiển thị cảnh báo trực quan đầy đủ
            Notice.Show("Lỗi ghi danh: " + ex.Message);
            InlineErrorText.Text = ex.Message;
            InlineErrorBorder.Visibility = Visibility.Visible;
            MessageBox.Show(ex.Message, "Không thể ghi danh", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        finally
        {
            SubmitEnrollButton.IsEnabled = true;
        }
    }

    private async void RefreshRecent_Click(object sender, RoutedEventArgs e)
    {
        await LoadRecentEnrollmentsAsync();
        Notice.Show("Đã làm mới danh sách ghi danh gần đây.");
    }

    private async void NewMember_Click(object sender, RoutedEventArgs e)
    {
        InlineErrorBorder.Visibility = Visibility.Collapsed;
        var dlg = new NewMemberQuickDialog
        {
            Owner = Window.GetWindow(this)
        };

        if (dlg.ShowDialog() == true)
        {
            MemberSearchBox.Text = dlg.CreatedMemberName;
            await SearchMembersAsync(dlg.CreatedMemberName);

            // Tự động chọn học viên vừa tạo
            var newlyCreated = _members.FirstOrDefault(m => m.MemberId == dlg.CreatedMemberId);
            if (newlyCreated is not null)
            {
                MemberResultsList.SelectedItem = newlyCreated;
            }

            Notice.Show($"Đã tạo tài khoản cho '{dlg.CreatedMemberName}'. Hãy chọn lớp và xác nhận ghi danh.");
        }
    }
}
