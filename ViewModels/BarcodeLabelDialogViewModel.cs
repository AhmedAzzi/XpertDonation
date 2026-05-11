using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using System.Windows;
using XDonation.Helpers;
using System.Linq;

namespace XDonation.ViewModels
{
    public class BarcodeLabelDialogViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string name = "") 
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        // Required by View code-behind
        public event Action? PrintRequested;
        public void RefreshBarcode() 
        { 
            if (string.IsNullOrWhiteSpace(BarcodeNumber))
            {
                BarcodeImage = null;
                return;
            }
            BarcodeImage = BarcodeService.GenerateBarcodeImage(BarcodeNumber);
        }

        // Properties
        private string _printerName = "Xprinter XP-233B (Copie 1)";
        public string PrinterName { get => _printerName; set { _printerName = value; OnPropertyChanged(); } }

        private string _pharmacyName = "PHARMACIE ARAB";
        public string PharmacyName { get => _pharmacyName; set { _pharmacyName = value; OnPropertyChanged(); } }

        private string _barcodeNumber = "";
        public string BarcodeNumber { get => _barcodeNumber; set { _barcodeNumber = value; OnPropertyChanged(); RefreshBarcode(); } }

        private string _productName = "";
        public string ProductName { get => _productName; set { _productName = value; OnPropertyChanged(); } }

        private string _price = "";
        public string Price { get => _price; set { _price = value; OnPropertyChanged(); } }

        private string _expiryDate = "";
        public string ExpiryDate { get => _expiryDate; set { _expiryDate = value; OnPropertyChanged(); } }

        private string _lotNumber = "";
        public string LotNumber { get => _lotNumber; set { _lotNumber = value; OnPropertyChanged(); } }

        private bool _isFree = false;
        public bool IsFree { get => _isFree; set { _isFree = value; OnPropertyChanged(); } }

        // UI Toggles
        private bool _showPharmacyName = true;
        public bool ShowPharmacyName { get => _showPharmacyName; set { _showPharmacyName = value; OnPropertyChanged(); } }

        private bool _showBarcodeImage = true;
        public bool ShowBarcodeImage { get => _showBarcodeImage; set { _showBarcodeImage = value; OnPropertyChanged(); } }

        private bool _showBarcodeNumber = true;
        public bool ShowBarcodeNumber { get => _showBarcodeNumber; set { _showBarcodeNumber = value; OnPropertyChanged(); } }

        private bool _showProductName = true;
        public bool ShowProductName { get => _showProductName; set { _showProductName = value; OnPropertyChanged(); } }

        private bool _showPrice = true;
        public bool ShowPrice { get => _showPrice; set { _showPrice = value; OnPropertyChanged(); } }

        private bool _showExpiry = true;
        public bool ShowExpiry { get => _showExpiry; set { _showExpiry = value; OnPropertyChanged(); OnPropertyChanged(nameof(ShowMeta)); } }

        private bool _showLot = true;
        public bool ShowLot { get => _showLot; set { _showLot = value; OnPropertyChanged(); OnPropertyChanged(nameof(ShowMeta)); } }

        public bool ShowMeta => ShowExpiry || ShowLot;

        private System.Windows.Media.Imaging.BitmapSource? _barcodeImage;
        public System.Windows.Media.Imaging.BitmapSource? BarcodeImage { get => _barcodeImage; private set { _barcodeImage = value; OnPropertyChanged(); } }

        private string _statusMessage = "Ready";
        public string StatusMessage { get => _statusMessage; set { _statusMessage = value; OnPropertyChanged(); } }

        private List<string> _availablePrinters = new();
        public List<string> AvailablePrinters { get => _availablePrinters; set { _availablePrinters = value; OnPropertyChanged(); } }

        public ICommand PrintCommand { get; }
        public ICommand CloseCommand { get; }

        public BarcodeLabelDialogViewModel()
        {
            PrintCommand = new RelayCommand(Print);
            CloseCommand = new RelayCommand(Close);
            
            try 
            { 
                var names = RawPrinterHelper.GetPrinterNames();
                AvailablePrinters = names.ToList();
                
                if (AvailablePrinters.Count > 0)
                {
                    // Try to find a printer that looks like a thermal label printer
                    var defaultPrinter = AvailablePrinters.FirstOrDefault(p => 
                        p.Contains("Xprinter", StringComparison.OrdinalIgnoreCase) || 
                        p.Contains("Label", StringComparison.OrdinalIgnoreCase) ||
                        p.Contains("Thermal", StringComparison.OrdinalIgnoreCase));

                    if (defaultPrinter != null)
                    {
                        PrinterName = defaultPrinter;
                    }
                    else if (!AvailablePrinters.Contains(PrinterName))
                    {
                        // If current default is not in the list, pick the first available
                        PrinterName = AvailablePrinters[0];
                    }
                }
            } 
            catch { }
        }

        private void Print(object? obj)
        {
            if (string.IsNullOrWhiteSpace(BarcodeNumber)) {
                StatusMessage = "✗ Barcode required";
                return;
            }

            try
            {
                StatusMessage = "Printing...";
                var printer = new ThermalLabelPrinter(PrinterName);
                printer.PrintLabel(
                    product: ProductName,
                    barcode: BarcodeNumber,
                    lot: LotNumber,
                    exp: ExpiryDate,
                    header: PharmacyName,
                    isFree: IsFree,
                    price: Price
                );
                StatusMessage = "✓ Success!";
                PrintRequested?.Invoke();
            }
            catch (Exception ex)
            {
                StatusMessage = "✗ Error";
                MessageBox.Show($"Print failed: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Close(object? obj)
        {
            foreach (Window window in Application.Current.Windows)
            {
                if (window.DataContext == this) { window.Close(); break; }
            }
        }
    }

    public class RelayCommand : ICommand
    {
        private readonly Action<object?> _execute;
        private readonly Func<object?, bool>? _canExecute;
        public RelayCommand(Action<object?> execute, Func<object?, bool>? canExecute = null)
        {
            _execute = execute;
            _canExecute = canExecute;
        }
        public bool CanExecute(object? parameter) => _canExecute == null || _canExecute(parameter);
        public void Execute(object? parameter) => _execute(parameter);
        public event EventHandler? CanExecuteChanged { add { } remove { } }
    }
}
