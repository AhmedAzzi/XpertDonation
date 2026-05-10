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
        public void RefreshBarcode() { /* Not needed for TSPL */ }

        // Properties
        private string _printerName = "Xprinter XP-233B (Copie 1)";
        public string PrinterName { get => _printerName; set { _printerName = value; OnPropertyChanged(); } }

        public string PharmacyName { get; set; } = "PHARMACIE ARAB";
        public string BarcodeNumber { get; set; } = "";
        public string ProductName { get; set; } = "";
        public string Price { get; set; } = "";
        public string ExpiryDate { get; set; } = "";
        public string LotNumber { get; set; } = "";
        public bool IsFree { get; set; } = false;

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
