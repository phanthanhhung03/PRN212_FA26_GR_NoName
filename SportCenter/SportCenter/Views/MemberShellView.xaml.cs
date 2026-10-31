using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using SportCenter.Entities;
using SportCenter.Models;

namespace SportCenter.Views;

// Khung dành cho học viên: sidebar bên trái, nội dung đổi theo mục được chọn.
public partial class MemberShellView : UserControl
{
    // Thứ tự trong danh sách là thứ tự trên sidebar. Mục chưa làm tạm dùng PlaceholderPage.
    private static readonly List<NavItem> Items = new()
    {
        new NavItem("Môn và khóa học", new[] { "Member" }, () => new MemberCatalogPage()),
        new NavItem("Lớp tuyển sinh",      new[] { "Member" }, () => new MemberClassPage()),
        new NavItem("Lịch học của tôi",    new[] { "Member" }, () => new MemberSchedulePage()),
        new NavItem("Hồ sơ cá nhân",       new[] { "Member" }, () => new PlaceholderPage("Hồ sơ cá nhân", "")),
    };

    public event EventHandler? LogoutRequested;

    public MemberShellView(User user)
    {
        InitializeComponent();
        RoleText.Text = $"{user.FullName} · {user.Role.RoleName}";
        BuildMenu();
    }

    private void BuildMenu()
    {
        foreach (NavItem item in Items)
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

        ((RadioButton)NavMenu.Children[0]).IsChecked = true;   // mở sẵn mục đầu
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