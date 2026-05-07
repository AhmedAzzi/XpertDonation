using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using XpertPharm5Donation.Models;
using XpertPharm5Donation.ViewModels;

namespace XpertPharm5Donation.Views
{
    public partial class PosView : UserControl
    {
        private readonly MainViewModel _vm;

        public PosView(MainViewModel vm)
        {
            InitializeComponent();
            _vm = vm;
            DataContext = vm;
            Loaded += (_, _) => BarcodeBox.Focus();
        }

        // Keep focus on barcode after any click
        protected override void OnPreviewMouseDown(MouseButtonEventArgs e)
        {
            base.OnPreviewMouseDown(e);
        }

        private void BarcodeBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter || e.Key == Key.Return)
            {
                e.Handled = true;
                if (string.IsNullOrWhiteSpace(BarcodeBox.Text))
                {
                    if (_vm.AddToCartCommand.CanExecute(null))
                    {
                        _vm.AddToCartCommand.Execute(null);
                    }
                }
                else
                {
                    _ = _vm.ScanBarcodeCommand.ExecuteAsync(null);
                }
                BarcodeBox.Focus();
                BarcodeBox.SelectAll();
            }
        }

        private void ProductBox_PreviewKeyDown(object sender, KeyEventArgs e)
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
                    _ = _vm.SelectFromSearchCommand.ExecuteAsync(selected);
                    SuggestionsPopup.IsOpen = false;
                    e.Handled = true;
                    return;
                }

                if (!string.IsNullOrWhiteSpace(ProductBox.Text))
                {
                    _vm.FilterSuggestionsCommand.Execute(null);
                    var match = _vm.DrugSuggestions.FirstOrDefault();
                    if (match != null)
                    {
                        _ = _vm.SelectFromSearchCommand.ExecuteAsync(match);
                    }
                    else
                    {
                        _vm.StatusMessage = "Aucun produit correspondant trouvé.";
                        _vm.IsStatusError = true;
                    }
                }
                else if (_vm.SelectedDrug != null)
                {
                    if (_vm.AddToCartCommand.CanExecute(null))
                        _vm.AddToCartCommand.Execute(null);
                }
                ProductBox.Text = string.Empty;
                SuggestionsPopup.IsOpen = false;
                e.Handled = true;
                BarcodeBox.Focus();
            }
            else if (e.Key == Key.Escape)
            {
                SuggestionsPopup.IsOpen = false;
                e.Handled = true;
            }
        }

        private void ProductBox_LostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            if (e.NewFocus is not ListBoxItem && e.NewFocus is not ScrollViewer && e.NewFocus is not ListBox)
            {
                _vm.IsSuggestionOpen = false;
            }
        }

        private void SuggestionsList_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter || e.Key == Key.Return)
            {
                if (SuggestionsList.SelectedItem is Drug selected)
                {
                    _ = _vm.SelectFromSearchCommand.ExecuteAsync(selected);
                    SuggestionsPopup.IsOpen = false;
                    BarcodeBox.Focus();
                }
                e.Handled = true;
            }
        }

        private void SuggestionsList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (SuggestionsList.SelectedItem is Drug selected)
            {
                _ = _vm.SelectFromSearchCommand.ExecuteAsync(selected);
                SuggestionsPopup.IsOpen = false;
                BarcodeBox.Focus();
            }
        }

        private void QuantityBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !MyRegex().IsMatch(e.Text);
        }

        private void QuantityBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter || e.Key == Key.Return)
            {
                if (_vm.SelectedCartItem != null)
                {
                    if (_vm.Quantity > _vm.SelectedCartItem.AvailableStock)
                    {
                        _vm.StatusMessage = $"❌ Stock insuffisant pour ce lot. Restant : {_vm.SelectedCartItem.AvailableStock}";
                        _vm.IsStatusError = true;
                        e.Handled = true;
                        return;
                    }
                    if (_vm.Quantity <= 0)
                    {
                        _vm.StatusMessage = "La quantité doit être supérieure à zéro.";
                        _vm.IsStatusError = true;
                        e.Handled = true;
                        return;
                    }

                    _vm.SelectedCartItem.Quantity = _vm.Quantity;
                    _vm.UpdateCartTotals();
                    
                    _vm.StatusMessage = "Quantité modifiée avec succès.";
                    _vm.IsStatusError = false;

                    _vm.SelectedCartItem = null;
                    _vm.Quantity = 1;
                }
                else if (_vm.AddToCartCommand.CanExecute(null))
                {
                    _vm.AddToCartCommand.Execute(null);
                }
                e.Handled = true;
            }
        }

        private void CartList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (sender is DataGrid dataGrid && dataGrid.SelectedItem is CartItem selectedItem)
            {
                // Set the quantity to match the selected row
                _vm.Quantity = selectedItem.Quantity;
                
                // Focus the top quantity box
                QuantityBox.Focus();
                QuantityBox.SelectAll();
            }
        }

        private T? FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
        {
            for (int i = 0; i < System.Windows.Media.VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = System.Windows.Media.VisualTreeHelper.GetChild(parent, i);
                if (child is T t) return t;
                var result = FindVisualChild<T>(child);
                if (result != null) return result;
            }
            return null;
        }

        [GeneratedRegex(@"^\d+$")]
        private static partial Regex MyRegex();
    }
}
