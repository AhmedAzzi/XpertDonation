using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using XDonation.Models;
using XDonation.ViewModels;

namespace XDonation.Views
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
                    viewModel.SelectDrugSuggestionCommand.Execute(dialogVm.SavedDrug);
                }
            };
        }

        private void SuggestionsList_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter || e.Key == Key.Return)
            {
                if (SuggestionsList.SelectedItem is Drug selected && DataContext is DonationVoucherViewModel vm)
                {
                    vm.SelectDrugSuggestionCommand.Execute(selected);
                    SuggestionsPopup.IsOpen = false;
                }
                e.Handled = true;
            }
        }

        private void SuggestionsList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (SuggestionsList.SelectedItem is Drug selected && DataContext is DonationVoucherViewModel vm)
            {
                vm.SelectDrugSuggestionCommand.Execute(selected);
                SuggestionsPopup.IsOpen = false;
            }
        }

        private void DrugTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Down)
            {
                if (!SuggestionsPopup.IsOpen)
                {
                    SuggestionsPopup.IsOpen = true;
                    SuggestionsList.SelectedIndex = 0;
                }
                else if (SuggestionsList.HasItems)
                {
                    int next = SuggestionsList.SelectedIndex + 1;
                    if (next < SuggestionsList.Items.Count)
                    {
                        SuggestionsList.SelectedIndex = next;
                        SuggestionsList.ScrollIntoView(SuggestionsList.SelectedItem);
                    }
                }
                e.Handled = true;
            }
            else if (e.Key == Key.Up)
            {
                if (!SuggestionsPopup.IsOpen && SuggestionsList.HasItems)
                {
                    SuggestionsPopup.IsOpen = true;
                    SuggestionsList.SelectedIndex = SuggestionsList.Items.Count - 1;
                }
                else
                {
                    int prev = SuggestionsList.SelectedIndex - 1;
                    if (prev >= 0)
                    {
                        SuggestionsList.SelectedIndex = prev;
                        SuggestionsList.ScrollIntoView(SuggestionsList.SelectedItem);
                    }
                }
                e.Handled = true;
            }
            else if (e.Key == Key.Enter || e.Key == Key.Return)
            {
                if (SuggestionsPopup.IsOpen && SuggestionsList.SelectedItem is Drug selected)
                {
                    if (DataContext is DonationVoucherViewModel vm)
                    {
                        vm.SelectDrugSuggestionCommand.Execute(selected);
                    }
                    SuggestionsPopup.IsOpen = false;
                    e.Handled = true;
                }
            }
            else if (e.Key == Key.Escape)
            {
                SuggestionsPopup.IsOpen = false;
                e.Handled = true;
            }
        }

        private void DrugTextBox_LostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            // Keep popup open if user clicks on the suggestions list
            if (e.NewFocus is not ListBoxItem && e.NewFocus is not ScrollViewer && e.NewFocus is not ListBox)
            {
                if (DataContext is DonationVoucherViewModel vm)
                {
                    vm.IsSuggestionOpen = false;
                }
            }
        }
    }
}
