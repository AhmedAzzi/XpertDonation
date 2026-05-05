using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using XpertPharm5Donation.ViewModels;
using XpertPharm5Donation.Data;
using XpertPharm5Donation.Models;
using System.Linq;

namespace XpertPharm5Donation.Views
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
    }
}
