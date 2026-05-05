using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using XpertPharm5Donation.Helpers;

namespace XpertPharm5Donation.ViewModels
{
    /// <summary>
    /// Updated BarcodeLabelDialogViewModel with TSPL thermal printer support.
    /// 
    /// Integration:
    /// 1. Replace PrintVisual logic with ThermalLabelPrinter
    /// 2. No barcode image generation needed (printer-native)
    /// 3. Direct TSPL commands to hardware
    /// </summary>
    public class BarcodeLabelDialogViewModelTSPL : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string name = "") 
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        // ===== Properties =====
        private string _printerName = "Xprinter";
        public string PrinterName
        {
            get => _printerName;
            set { _printerName = value; OnPropertyChanged(); }
        }

        private string _pharmacyName = "PHARMACIE ARAB";
        public string PharmacyName
        {
            get => _pharmacyName;
            set { _pharmacyName = value; OnPropertyChanged(); }
        }

        private string _barcodeNumber = "";
        public string BarcodeNumber
        {
            get => _barcodeNumber;
            set { _barcodeNumber = value; OnPropertyChanged(); }
        }

        private string _productName = "";
        public string ProductName
        {
            get => _productName;
            set { _productName = value; OnPropertyChanged(); }
        }

        private string _price = "";
        public string Price
        {
            get => _price;
            set { _price = value; OnPropertyChanged(); }
        }

        private string _expiryDate = "";
        public string ExpiryDate
        {
            get => _expiryDate;
            set { _expiryDate = value; OnPropertyChanged(); }
        }

        private string _lotNumber = "";
        public string LotNumber
        {
            get => _lotNumber;
            set { _lotNumber = value; OnPropertyChanged(); }
        }

        private bool _isFree = true;
        public bool IsFree
        {
            get => _isFree;
            set { _isFree = value; OnPropertyChanged(); }
        }

        private string _statusMessage = "Ready";
        public string StatusMessage
        {
            get => _statusMessage;
            set { _statusMessage = value; OnPropertyChanged(); }
        }

        private bool _isPrinting = false;
        public bool IsPrinting
        {
            get => _isPrinting;
            set { _isPrinting = value; OnPropertyChanged(); }
        }

        // ===== Commands =====
        public ICommand PrintCommand { get; }
        public ICommand CloseCommand { get; }
        public ICommand RefreshPrinterListCommand { get; }

        private List<string> _availablePrinters = new();
        public List<string> AvailablePrinters
        {
            get => _availablePrinters;
            set { _availablePrinters = value; OnPropertyChanged(); }
        }

        public event Action? PrintRequested;
        public event Action? CloseRequested;

        // ===== Constructor =====
        public BarcodeLabelDialogViewModelTSPL()
        {
            PrintCommand = new RelayCommand(Print, CanPrint);
            CloseCommand = new RelayCommand(Close);
            RefreshPrinterListCommand = new RelayCommand(RefreshPrinterList);

            RefreshPrinterList(null);
        }

        // ===== Print Logic =====
        private void Print(object? obj)
        {
            if (!ValidateInput())
                return;

            try
            {
                IsPrinting = true;
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

                StatusMessage = "✓ Label printed successfully!";
                PrintRequested?.Invoke();
            }
            catch (Exception ex)
            {
                StatusMessage = $"✗ Error: {ex.Message}";
                System.Windows.MessageBox.Show(
                    $"Print failed:\n\n{ex.Message}",
                    "Thermal Printer Error",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Error
                );
            }
            finally
            {
                IsPrinting = false;
            }
        }

        private void Close(object? obj)
        {
            CloseRequested?.Invoke();
        }

        private void RefreshPrinterList(object? obj)
        {
            try
            {
                AvailablePrinters = new List<string>(RawPrinterHelper.GetPrinterNames());
                StatusMessage = $"Found {AvailablePrinters.Count} printer(s)";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error reading printers: {ex.Message}";
            }
        }

        private bool CanPrint(object? obj)
        {
            return !IsPrinting && !string.IsNullOrWhiteSpace(BarcodeNumber);
        }

        private bool ValidateInput()
        {
            if (string.IsNullOrWhiteSpace(BarcodeNumber))
            {
                StatusMessage = "✗ Barcode is required";
                return false;
            }

            if (string.IsNullOrWhiteSpace(ProductName))
            {
                StatusMessage = "✗ Product name is required";
                return false;
            }

            if (string.IsNullOrWhiteSpace(LotNumber))
            {
                StatusMessage = "✗ Lot number is required";
                return false;
            }

            if (string.IsNullOrWhiteSpace(ExpiryDate))
            {
                StatusMessage = "✗ Expiry date is required";
                return false;
            }

            return true;
        }
    }
}
