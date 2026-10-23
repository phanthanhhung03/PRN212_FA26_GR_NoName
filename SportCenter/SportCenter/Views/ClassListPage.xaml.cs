using Microsoft.EntityFrameworkCore;
using SportCenter.Entities;
using SportCenter.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace SportCenter.Views
{
    public partial class ClassListPage : UserControl
    {
        private readonly ClassService _service = new ClassService();
        private bool _loading;

        public ClassListPage()
        {
            InitializeComponent();
            Loaded += async (_, _) => await InitAsync();
        }

        private async Task InitAsync()
        {
            _loading = true;
            try
            {
                var sports = await _service.GetSportsAsync();
                sports.Insert(0, new Dtos.SportOptionDto(0, "Tất cả các môn"));
                SportFilter.ItemsSource = sports;
                SportFilter.SelectedIndex = 0;


                var statuses = await _service.GetClassStatusesAsync();
                statuses.Insert(0, "Tất cả trạng thái");
                StatusFilter.ItemsSource = statuses;
                StatusFilter.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                ShowError(ex);
                return;
            }
            finally
            {
                _loading = false;
            }

            await LoadClassesAsync();
        }

        private async Task LoadClassesAsync()
        {
            int? sportId = SportFilter.SelectedValue is int id && id != 0 ? id : null;
            string? status = StatusFilter.SelectedIndex > 0 ? StatusFilter.SelectedItem.ToString() : null;

            try
            {
                ClassGrid.ItemsSource = await _service.GetClassAsync(sportId, status);
            }
            catch (Exception ex)
            {
                ShowError(ex);
            }
        }

        private async void Filter_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (_loading) return;
            await LoadClassesAsync();
        }

        private static void ShowError(Exception ex)
        {
            string message = ex is UnauthorizedAccessException
                ? ex.Message
                : "Không tải được danh sách lớp. Vui lòng thử lại.";
            MessageBox.Show(message, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }
}
