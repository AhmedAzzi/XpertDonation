using System.Windows.Controls;
using XpertPharm5Donation.Models;
using XpertPharm5Donation.ViewModels;

namespace XpertPharm5Donation.Views
{
    public partial class DonationVoucherView : UserControl
    {
        public DonationVoucherView(DonationVoucherViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
        }

        private void ListBox_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (sender is ListBox listBox && listBox.SelectedItem is Drug drug)
            {
                if (DataContext is DonationVoucherViewModel vm)
                {
                    vm.SelectDrugSuggestionCommand.Execute(drug);
                }
            }
        }
    }
}
