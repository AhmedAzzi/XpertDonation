using System;
using System.Windows;
using System.Windows.Controls;
using XpertPharm5Donation.ViewModels;

namespace XpertPharm5Donation.Views
{
    public partial class StockLotsView : UserControl
    {
        public StockLotsView(StockLotsViewModel vm)
        {
            InitializeComponent();
            DataContext = vm;
            Loaded += async (_, _) => await vm.LoadCommand.ExecuteAsync(null);
        }

        private void FilterRadio_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is StockLotsViewModel vm && sender is RadioButton rb && rb.Tag is string tag)
            {
                if (Enum.TryParse<StockFilterType>(tag, out var filterType))
                {
                    vm.SelectedFilter = filterType;
                }
            }
        }

        private void OnFilterTextChanged(object sender, TextChangedEventArgs e)
        {
            if (DataContext is StockLotsViewModel vm)
            {
                vm.FilterByText();
            }
        }
    }
}
