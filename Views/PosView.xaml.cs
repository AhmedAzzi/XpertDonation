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

        private async void ProductBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter || e.Key == Key.Return)
            {
                if (!string.IsNullOrWhiteSpace(ProductBox.Text))
                {
                    await _vm.FilterSuggestionsCommand.ExecuteAsync(null);
                    var match = _vm.DrugSuggestions.FirstOrDefault();
                    if (match != null)
                    {
                        await _vm.SelectFromSearchCommand.ExecuteAsync(match);
                        if (_vm.AddToCartCommand.CanExecute(null))
                            _vm.AddToCartCommand.Execute(null);
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
                e.Handled = true;
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
