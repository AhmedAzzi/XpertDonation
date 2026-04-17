using System;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace XpertPharm5Donation.ViewModels
{
    /// <summary>
    /// ViewModel for SalesCounterView
    /// Handles product selection, pricing, and transaction details
    /// </summary>
    public partial class SalesCounterViewModel : ObservableObject
    {
        [ObservableProperty] private string barcode = string.Empty;
        [ObservableProperty] private decimal price = 0m;
        [ObservableProperty] private string selectedProduct = string.Empty;
        [ObservableProperty] private int quantity = 0;
        [ObservableProperty] private ObservableCollection<CartItemViewModel> products = new();
        [ObservableProperty] private CartItemViewModel? selectedProductItem;

        public SalesCounterViewModel()
        {
            InitializeWithSampleData();
        }

        private void InitializeWithSampleData()
        {
            // Add sample data for testing
            Products.Add(new CartItemViewModel
            {
                ProductName = "FUMACUR 200 MG B/80 COMPRIME 200 MG COMP. B/80",
                UnitPrice = 153.65m,
                Quantity = 1,
                ExpiryDate = new DateTime(2028, 12, 1),
                LotNumber = "1470",
                Barcode = "53511052",
                Total = 153.65m
            });
        }
    }

    public partial class CartItemViewModel : ObservableObject
    {
        [ObservableProperty] private string productName = string.Empty;
        [ObservableProperty] private decimal unitPrice = 0m;
        [ObservableProperty] private int quantity = 0;
        [ObservableProperty] private DateTime expiryDate = DateTime.MinValue;
        [ObservableProperty] private string lotNumber = string.Empty;
        [ObservableProperty] private string barcode = string.Empty;
        [ObservableProperty] private decimal total = 0m;
    }
}
