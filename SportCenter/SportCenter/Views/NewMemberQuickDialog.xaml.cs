using System;
using System.Windows;
using System.Windows.Controls;
using SportCenter.Services;

namespace SportCenter.Views;

public partial class NewMemberQuickDialog : Window
{
    private readonly EnrollmentService _service = new();
    public int CreatedMemberId { get; private set; }
    public string CreatedMemberName { get; private set; } = string.Empty;

    public NewMemberQuickDialog()
    {
        InitializeComponent();
        BirthDatePicker.SelectedDate = DateTime.Today.AddYears(-18);
    }

    private async void Save_Click(object sender, RoutedEventArgs e)
    {
        string fullName = FullNameBox.Text.Trim();
        string email = EmailBox.Text.Trim();
        string phone = PhoneBox.Text.Trim();

        if (fullName.Length == 0)
        {
            MessageBox.Show("Vui lòng nhập họ và tên.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (!ProfileService.IsValidEmail(email))
        {
            MessageBox.Show("Email không đúng định dạng.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (!ProfileService.IsValidPhone(phone))
        {
            MessageBox.Show("Số điện thoại gồm 10 số và bắt đầu bằng 0.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (!BirthDatePicker.SelectedDate.HasValue)
        {
            MessageBox.Show("Vui lòng chọn ngày sinh.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var birthDate = DateOnly.FromDateTime(BirthDatePicker.SelectedDate.Value);
        string gender = (GenderCombo.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "Male";

        try
        {
            CreatedMemberId = await _service.QuickCreateMemberAsync(fullName, email, phone, birthDate, gender);
            CreatedMemberName = fullName;
            DialogResult = true;
            Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Lỗi tạo học viên", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}

