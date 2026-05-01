using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using ZXing;
using System.IO;
using System.Drawing;
using System.Windows;

namespace XpertPharm5Donation.ViewModels
{
    public class BarcodeLabelDialogViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        // Fields to show/hide
        public bool ShowPharmacyName { get => _showPharmacyName; set { _showPharmacyName = value; OnPropertyChanged(); } }
        public bool ShowBarcodeImage { get => _showBarcodeImage; set { _showBarcodeImage = value; OnPropertyChanged(); } }
        public bool ShowBarcodeNumber { get => _showBarcodeNumber; set { _showBarcodeNumber = value; OnPropertyChanged(); } }
        public bool ShowProductName { get => _showProductName; set { _showProductName = value; OnPropertyChanged(); } }
        public bool ShowPrice { get => _showPrice; set { _showPrice = value; OnPropertyChanged(); } }
        public bool ShowExpiry { get => _showExpiry; set { _showExpiry = value; OnPropertyChanged(); } }
        public bool ShowLot { get => _showLot; set { _showLot = value; OnPropertyChanged(); } }
        public double FontSize { get => _fontSize; set { _fontSize = value; OnPropertyChanged(); } }

        // Data
        public string PharmacyName { get; set; }
        public string BarcodeNumber { get; set; }
        public string ProductName { get; set; }
        public string Price { get; set; }
        public string ExpiryDate { get; set; }
        public string LotNumber { get; set; }

        public BitmapImage BarcodeImage => GenerateBarcode(BarcodeNumber);

        // Commands
        public ICommand PrintCommand { get; }
        public ICommand CloseCommand { get; }

        // Event to notify dialog to print
        public event Action? PrintRequested;

        // Backing fields
        private bool _showPharmacyName = true, _showBarcodeImage = true, _showBarcodeNumber = true, _showProductName = true, _showPrice = true, _showExpiry = true, _showLot = true;
        private double _fontSize = 10;


        public BarcodeLabelDialogViewModel()
        {
            PrintCommand = new RelayCommand(Print);
            CloseCommand = new RelayCommand(Close);
        }

        private BitmapImage GenerateBarcode(string code)
        {
            if (string.IsNullOrEmpty(code)) return null;
            var writer = new BarcodeWriter<System.Drawing.Bitmap>
            {
                Format = BarcodeFormat.CODE_128,
                Options = new ZXing.Common.EncodingOptions { Height = 40, Width = 130, Margin = 0 }
            };
            using (var bitmap = writer.Write(code))
            using (var memory = new MemoryStream())
            {
                bitmap.Save(memory, System.Drawing.Imaging.ImageFormat.Png);
                memory.Position = 0;
                var image = new BitmapImage();
                image.BeginInit();
                image.StreamSource = memory;
                image.CacheOption = BitmapCacheOption.OnLoad;
                image.EndInit();
                return image;
            }
        }

        private void Print(object obj)
        {
            PrintRequested?.Invoke();
        }

        private void Close(object obj)
        {
            Application.Current.Windows[Application.Current.Windows.Count - 1]?.Close();
        }
    }

    // Simple RelayCommand implementation
    public class RelayCommand : ICommand
    {
        private readonly Action<object> _execute;
        private readonly Func<object, bool> _canExecute;
        public RelayCommand(Action<object> execute, Func<object, bool> canExecute = null)
        {
            _execute = execute;
            _canExecute = canExecute;
        }
        public bool CanExecute(object parameter) => _canExecute == null || _canExecute(parameter);
        public void Execute(object parameter) => _execute(parameter);
        public event EventHandler CanExecuteChanged { add { } remove { } }
    }
}
