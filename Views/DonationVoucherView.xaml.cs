using System.Windows.Controls;
using XpertPharm5Donation.Models;
using XpertPharm5Donation.ViewModels;

namespace XpertPharm5Donation.Views
{
    public partial class DonationVoucherView : UserControl
    {
        public DonationVoucherView(DonationVoucherViewModel viewModel, Data.AppDbContext db)
        {
            InitializeComponent();
            DataContext = viewModel;

            viewModel.RequestDrugEdit += (drug) =>
            {
                var dialogVm = new DrugEditViewModel(db, drug);
                var win = new DrugEditWindow(dialogVm)
                {
                    Owner = System.Windows.Window.GetWindow(this)
                };
                if (win.ShowDialog() == true && dialogVm.SavedDrug != null)
                {
                    // If a drug was added/edited, we can auto-select it if it's new
                    if (drug == null && dialogVm.SavedDrug.Id > 0)
                    {
                        viewModel.SelectDrugSuggestionCommand.Execute(dialogVm.SavedDrug);
                    }
                    else if (drug != null)
                    {
                        // Update UI fields if the current drug was edited
                        viewModel.SelectDrugSuggestionCommand.Execute(dialogVm.SavedDrug);
                    }
                }
            };
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
