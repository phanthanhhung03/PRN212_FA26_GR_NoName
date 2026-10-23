using System;
using System.Windows.Controls;

namespace SportCenter.Models;

// Một mục menu của StaffShellView: hiện cho các role trong Roles, bấm vào thì tạo trang bằng CreatePage.
public record NavItem(string Title, string[] Roles, Func<UserControl> CreatePage);
