using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using XDonation.ViewModels;
using XDonation.Data;
using XDonation.Models;
using System.Linq;

namespace XDonation.Views
{
    public partial class ProductsView : UserControl
    {
        private readonly AppDbContext _db;

        public ProductsView(ManageDonationsViewModel vm, AppDbContext db)
        {
            InitializeComponent();
            DataContext = vm;
            _db = db;

            vm.RequestDrugEdit += (drug) =>
            {
                var dialogVm = new DrugEditViewModel(_db, drug);
                var win = new DrugEditWindow(dialogVm)
                {
                    Owner = Window.GetWindow(this)
                };
                win.ShowDialog();
                _ = vm.LoadCommand.ExecuteAsync(null);
            };

            Loaded += async (_, _) => await vm.LoadCommand.ExecuteAsync(null);
        }

        private void SearchBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                if (DataContext is ManageDonationsViewModel vm)
                {
                    _ = vm.SearchCommand.ExecuteAsync(null);
                }
                e.Handled = true;
            }
        }

        private void DataGridRow_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (sender is DataGridRow row && row.Item is Drug drug)
            {
                if (DataContext is ManageDonationsViewModel vm)
                {
                    vm.SelectedDrug = drug;
                    vm.ShowEditDrugFormCommand.Execute(null);
                }
            }
        }
    }
}
