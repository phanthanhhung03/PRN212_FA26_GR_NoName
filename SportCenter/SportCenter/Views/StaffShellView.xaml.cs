using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using SportCenter.Dtos;
using SportCenter.Entities;
using SportCenter.Models;
using SportCenter.Services;
using SportCenter.Views;

namespace SportCenter.Views;

// Khung dùng chung cho Manager, Staff và Coach: sidebar lọc theo role, nội dung đổi theo mục được chọn.
public partial class StaffShellView : UserControl
{
    private const string Manager = "Manager";
    private const string Staff = "Staff";
    private const string Coach = "Coach";

    // Thứ tự trong danh sách chính là thứ tự trên sidebar.
    private static readonly List<NavItem> AllItems = new()
    {
        new NavItem("Lớp học",            new[] { Manager }, () => new ClassListPage()),
        new NavItem("Môn và khóa học",    new[] { Manager }, () => new CatalogPage()),
        new NavItem("Tài khoản",          new[] { Manager }, () => new AccountPage()),
        new NavItem("Báo cáo",            new[] { Manager }, () => new PlaceholderPage("Báo cáo", "UC15")),
        new NavItem("Ghi danh hộ",        new[] { Staff },   () => new PlaceholderPage("Ghi danh hộ", "UC10")),
        new NavItem("Lịch dạy",           new[] { Coach },   () => new PlaceholderPage("Lịch dạy và học viên", "UC08")),
        new NavItem("Điểm danh",          new[] { Coach },   () => new PlaceholderPage("Điểm danh", "UC09")),
        new NavItem("Hồ sơ cá nhân",      new[] { Manager, Staff, Coach }, () => new ProfilePage()),
    };

    public event EventHandler? LogoutRequested;

    public StaffShellView(User user)
    {
        InitializeComponent();
        RoleText.Text = $"{user.FullName} · {user.Role.RoleName}";
        BuildMenu(user.Role.RoleName);
    }

    private void BuildMenu(string roleName)
    {
        List<NavItem> items = AllItems.Where(i => i.Roles.Contains(roleName)).ToList();

        foreach (NavItem item in items)
        {
            var button = new RadioButton
            {
                Content = item.Title,
                GroupName = "Nav",
                Style = (Style)FindResource("NavButton"),
                Tag = item
            };
            button.Checked += NavButton_Checked;
            NavMenu.Children.Add(button);
        }

        // Mở sẵn mục đầu tiên của role.
        if (NavMenu.Children.Count > 0)
        {
            ((RadioButton)NavMenu.Children[0]).IsChecked = true;
        }
    }

    private void NavButton_Checked(object sender, RoutedEventArgs e)
    {
        var item = (NavItem)((RadioButton)sender).Tag;
        PageTitleText.Text = item.Title;

        UserControl page = item.CreatePage();
        if (page is ClassListPage list)
        {
            WireClassList(list);
        }
        else if (page is ProfilePage profile)
        {
            profile.ProfileSaved += (_, _) => RefreshUserText();
        }
        ShowPage(page);
    }

    private void RefreshUserText()
    {
        User user = CurrentUser.Value!;
        RoleText.Text = $"{user.FullName} · {user.Role.RoleName}";
    }

    private void LogoutButton_Click(object sender, RoutedEventArgs e) => LogoutRequested?.Invoke(this, EventArgs.Empty);

    private void ShowClassList(string? notice = null)
    {
        PageTitleText.Text = "Lớp học";
        var list = new ClassListPage();
        WireClassList(list);
        ShowPage(list);
        if (notice is not null) list.ShowNotice(notice);
    }

    // Đăng ký các sự kiện của trang danh sách lớp (dùng ở mọi nơi tạo ClassListPage).
    private void WireClassList(ClassListPage list)
    {
        list.NewClassRequested += (_, _) => ShowClassForm();
        list.ClassDetailRequested += (_, classId) => ShowClassDetail(classId);
    }

    private void ShowClassDetail(int classId)
    {
        PageTitleText.Text = "Chi tiết lớp";
        var detail = new ClassDetailPage(classId);
        detail.BackRequested += (_, _) => ShowClassList();
        ShowPage(detail);
    }

    private void ShowClassForm()
    {
        PageTitleText.Text = "Mở lớp mới";
        var form = new CreateClassPage();
        form.BackRequested += (_, _) => ShowClassList();
        form.Created += (_, _) => ShowClassList("Đã tạo lớp thành công.");
        ShowPage(form);
    }

    private void ShowPage(UserControl page)
    {
        page.Opacity = 0;
        PageHost.Content = page;
        page.BeginAnimation(OpacityProperty, new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(200)));
    }
}
