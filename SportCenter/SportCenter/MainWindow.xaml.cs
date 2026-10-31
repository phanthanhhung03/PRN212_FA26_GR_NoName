using SportCenter.Entities;
using SportCenter.Services;
using SportCenter.Views;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;

namespace SportCenter;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        ShowRoleSelect();
    }

    private void ShowRoleSelect()
    {
        var view = new RoleSelectView();
        view.StaffSelected += (_, _) => ShowLogin(isMember: false);
        view.MemberSelected += (_, _) => ShowLogin(isMember: true);
        Navigate(view);
    }

    private void ShowLogin(bool isMember)
    {
        var view = new LoginFormView(isMember);
        view.BackRequested += (_, _) => ShowRoleSelect();
        view.RegisterRequested += (_, _) => ShowRegister();
        view.LoginSucceeded += (_, user) => ShowHome(user);
        Navigate(view);
    }

    private void ShowHome(User user)
    {
        if (user.Role.RoleName != "Member")
        {
            var shell = new StaffShellView(user);
            shell.LogoutRequested += (_, _) =>
            {
                CurrentUser.Value = null;
                ShowRoleSelect();
            };
            Navigate(shell);
            return;
        }

        var memberShell = new MemberShellView(user);
        memberShell.LogoutRequested += (_, _) =>
        {
            CurrentUser.Value = null;
            ShowRoleSelect();
        };
        Navigate(memberShell);
    }

    private void ShowRegister()
    {
        var view = new RegisterView();
        view.BackRequested += (_, _) => ShowLogin(isMember: true);
        Navigate(view);
    }

    private void Navigate(UserControl view)
    {
        view.Opacity = 0;
        MainContent.Content = view;
        view.BeginAnimation(OpacityProperty, new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(250)));
    }
}
