using System;
using System.Windows;
using System.Windows.Controls;

namespace SportCenter.Views;

public partial class RoleSelectView : UserControl
{
    public event EventHandler? StaffSelected;
    public event EventHandler? MemberSelected;

    public RoleSelectView()
    {
        InitializeComponent();
    }

    private void StaffButton_Click(object sender, RoutedEventArgs e) => StaffSelected?.Invoke(this, EventArgs.Empty);
    private void MemberButton_Click(object sender, RoutedEventArgs e) => MemberSelected?.Invoke(this, EventArgs.Empty);


}
