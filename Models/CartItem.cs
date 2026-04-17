using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace XpertPharm5Donation.Models
{
    public partial class CartItem : ObservableObject
    {
        public int DrugId { get; set; }
        public int StockBatchId { get; set; }

        [ObservableProperty]
        private string _drugName = string.Empty;

        [ObservableProperty]
        private string _batchNumber = string.Empty;

        [ObservableProperty]
        private DateTime? _expirationDate;

        [ObservableProperty]
        private int _quantity;

        public int AvailableStock { get; set; }

        public string ExpirationDisplay => ExpirationDate?.ToString("dd/MM/yyyy") ?? "N/A";
        public string BatchDisplay => BatchNumber ?? "N/A";

        public bool IsExpired => ExpirationDate.HasValue && ExpirationDate.Value < DateTime.Now;
    }
}
