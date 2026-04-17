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
                    _suppressSelectionChanged = true;
                    await _vm.SelectFromSearchCommand.ExecuteAsync(selected);
                    _suppressSelectionChanged = false;
                    ProductCombo.SelectedIndex = -1;

                    if (_vm.AddToCartCommand.CanExecute(null))
                        _vm.AddToCartCommand.Execute(null);
                }
                else
                {
                    if (_vm.AddToCartCommand.CanExecute(null))
                        _vm.AddToCartCommand.Execute(null);
                }
                return;
            }
            if (e.Key is Key.Down or Key.Up or Key.Left or Key.Right or Key.Tab) return;
            await _vm.FilterSuggestionsCommand.ExecuteAsync(null);
            ProductCombo.IsDropDownOpen = true;
        }

        private async void ProductCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_suppressSelectionChanged) return;
            if (ProductCombo.SelectedItem is Models.Drug selected)
            {
                _suppressSelectionChanged = true;
                await _vm.SelectFromSearchCommand.ExecuteAsync(selected);
                ProductCombo.SelectedIndex = -1;
                _suppressSelectionChanged = false;
            }
        }

        private void BtnIncQty_Click(object sender, RoutedEventArgs e)
        {
            _vm.Quantity++;
        }

        private void BtnDecQty_Click(object sender, RoutedEventArgs e)
        {
            if (_vm.Quantity > 1) _vm.Quantity--;
        }

        private void QuantityBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !MyRegex().IsMatch(e.Text);
        }

        [GeneratedRegex(@"^\d+$")]
        private static partial Regex MyRegex();
    }
}
