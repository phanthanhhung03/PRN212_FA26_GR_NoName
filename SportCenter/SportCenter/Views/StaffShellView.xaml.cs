using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using SportCenter.Entities;
using SportCenter.Models;

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
        new NavItem("Lớp học",            new[] { Manager }, () => new PlaceholderPage("Lớp học", "UC12 · UC13")),
        new NavItem("Môn và khóa học",    new[] { Manager }, () => new PlaceholderPage("Môn và khóa học", "UC11")),
        new NavItem("Tài khoản",          new[] { Manager }, () => new PlaceholderPage("Tài khoản", "UC14")),
        new NavItem("Báo cáo",            new[] { Manager }, () => new PlaceholderPage("Báo cáo", "UC15")),
        new NavItem("Ghi danh hộ",        new[] { Staff },   () => new PlaceholderPage("Ghi danh hộ", "UC10")),
        new NavItem("Lịch dạy",           new[] { Coach },   () => new PlaceholderPage("Lịch dạy và học viên", "UC08")),
        new NavItem("Điểm danh",          new[] { Coach },   () => new PlaceholderPage("Điểm danh", "UC09")),
        new NavItem("Hồ sơ cá nhân",      new[] { Manager, Staff, Coach }, () => new PlaceholderPage("Hồ sơ cá nhân", "UC02")),
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
        page.Opacity = 0;
        PageHost.Content = page;
        page.BeginAnimation(OpacityProperty, new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(200)));
    }

    private void LogoutButton_Click(object sender, RoutedEventArgs e) => LogoutRequested?.Invoke(this, EventArgs.Empty);
}
