using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ZXing;
using ZXing.Common;
using System.Windows;
using System.Windows.Interop;

namespace XpertPharm5Donation.ViewModels
{
    public class BarcodeLabelDialogViewModel : INotifyPropertyChanged
    {
        [DllImport("gdi32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool DeleteObject(IntPtr hObject);

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string name = "") => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        public bool ShowPharmacyName { get => _showPharmacyName; set { _showPharmacyName = value; OnPropertyChanged(); } }
        public bool ShowBarcodeImage { get => _showBarcodeImage; set { _showBarcodeImage = value; OnPropertyChanged(); } }
        public bool ShowBarcodeNumber { get => _showBarcodeNumber; set { _showBarcodeNumber = value; OnPropertyChanged(); } }
        public bool ShowProductName { get => _showProductName; set { _showProductName = value; OnPropertyChanged(); } }
        public bool ShowPrice { get => _showPrice; set { _showPrice = value; OnPropertyChanged(); } }
        public bool ShowExpiry { get => _showExpiry; set { _showExpiry = value; OnPropertyChanged(); } }
        public bool ShowLot { get => _showLot; set { _showLot = value; OnPropertyChanged(); } }
        public double FontSize { get => _fontSize; set { _fontSize = value; OnPropertyChanged(); } }

        public string PharmacyName { get; set; } = string.Empty;
        private string _barcodeNumber = string.Empty;
        public string BarcodeNumber
        {
            get => _barcodeNumber;
            set { _barcodeNumber = value; OnPropertyChanged(); RefreshBarcode(); }
        }
        public string ProductName { get; set; } = string.Empty;
        public string Price { get; set; } = string.Empty;
        public string ExpiryDate { get; set; } = string.Empty;
        public string LotNumber { get; set; } = string.Empty;

        private BitmapSource? _barcodeImage;
        public BitmapSource? BarcodeImage
        {
            get => _barcodeImage;
            private set { _barcodeImage = value; OnPropertyChanged(); }
        }

        public ICommand PrintCommand { get; }
        public ICommand CloseCommand { get; }

        public event Action? PrintRequested;

        private bool _showPharmacyName = true, _showBarcodeImage = true, _showBarcodeNumber = true, _showProductName = true, _showPrice = true, _showExpiry = true, _showLot = true;
        private double _fontSize = 10;


        public BarcodeLabelDialogViewModel()
        {
            PrintCommand = new RelayCommand(Print);
            CloseCommand = new RelayCommand(Close);
        }

        public void RefreshBarcode()
        {
            BarcodeImage = GenerateBarcode(_barcodeNumber);
        }

        private BitmapSource? GenerateBarcode(string? code)
        {
            if (string.IsNullOrEmpty(code)) return null;

            var writer = new ZXing.Windows.Compatibility.BarcodeWriter
            {
                Format = BarcodeFormat.CODE_128,
                Options = new EncodingOptions { Height = 100, Width = 300, Margin = 3, PureBarcode = true }
            };

            using var bitmap = writer.Write(code);
            IntPtr hBitmap = bitmap.GetHbitmap();
            try
            {
                BitmapSource source = Imaging.CreateBitmapSourceFromHBitmap(
                    hBitmap,
                    IntPtr.Zero,
                    Int32Rect.Empty,
                    BitmapSizeOptions.FromEmptyOptions());
                source.Freeze();
                return source;
            }
            finally
            {
                DeleteObject(hBitmap);
            }
        }

        private void Print(object? obj)
        {
            PrintRequested?.Invoke();
        }

        private void Close(object? obj)
        {
            Application.Current.Windows[Application.Current.Windows.Count - 1]?.Close();
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
