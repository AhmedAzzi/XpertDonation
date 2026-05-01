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
        private bool _suppressSelectionChanged;

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

        private void BarcodeBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
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
                e.Handled = true;
                BarcodeBox.Focus();
            }
        }

        private async void ProductCombo_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter || e.Key == Key.Return)
            {
                if (ProductCombo.SelectedItem is Models.Drug selected)
                {
                    // User pressed Enter while a product is selected in the dropdown
                    _suppressSelectionChanged = true;
                    await _vm.SelectFromSearchCommand.ExecuteAsync(selected);
                    
                    if (_vm.AddToCartCommand.CanExecute(null))
                        _vm.AddToCartCommand.Execute(null);
                        
                    ProductCombo.SelectedIndex = -1;
                    _vm.SearchText = string.Empty;
                    _suppressSelectionChanged = false;
                }
                else if (_vm.SelectedDrug != null && string.IsNullOrWhiteSpace(ProductCombo.Text))
                {
                    // If they just press Enter in an empty combo box, add the currently selected drug
                    if (_vm.AddToCartCommand.CanExecute(null))
                        _vm.AddToCartCommand.Execute(null);
                }
                else
                {
                    // No item selected and text is not empty
                    _vm.StatusMessage = "Veuillez sélectionner un produit dans la liste.";
                    _vm.IsStatusError = true;
                }
                e.Handled = true;
                return;
            }
            if (e.Key is Key.Down or Key.Up or Key.Left or Key.Right or Key.Tab) return;
            await _vm.FilterSuggestionsCommand.ExecuteAsync(null);
            
            var textBox = ProductCombo.Template.FindName("PART_EditableTextBox", ProductCombo) as TextBox;
            int caret = textBox?.CaretIndex ?? 0;
            
            ProductCombo.IsDropDownOpen = true;
            
            if (textBox != null)
            {
                textBox.CaretIndex = caret;
            }
        }
        
        private void ProductCombo_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Down)
            {
                if (!ProductCombo.IsDropDownOpen)
                {
                    ProductCombo.IsDropDownOpen = true;
                    e.Handled = true;
                }
                else if (ProductCombo.SelectedIndex == -1 && ProductCombo.HasItems)
                {
                    // If dropdown is open but no item is highlighted, select the first one
                    ProductCombo.SelectedIndex = 0;
                    e.Handled = true;
                }
            }
        }

        private async void ProductCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_suppressSelectionChanged) return;
            if (ProductCombo.SelectedItem is Models.Drug selected)
            {
                // Load the drug info into the panel
                await _vm.SelectFromSearchCommand.ExecuteAsync(selected);
            }
            else
            {
                // Clear selection if nothing is selected in the combo
                _vm.SelectedDrug = null;
                _vm.SelectedStockBatch = null;
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
            if (sender is ListView listView && listView.SelectedItem is CartItem selectedItem)
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
