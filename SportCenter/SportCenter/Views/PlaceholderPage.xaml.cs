using System.Windows.Controls;

namespace SportCenter.Views;

// Trang giữ chỗ cho các chức năng chưa làm.
public partial class PlaceholderPage : UserControl
{
    public PlaceholderPage(string title, string useCase)
    {
        InitializeComponent();
        TitleText.Text = title;
        CodeText.Text = useCase;
    }
}
