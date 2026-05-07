using System;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace XDonation.ViewModels
{
    /// <summary>
    /// ViewModel for ReceptionDocumentView
    /// Handles stock reception form and product tracking
    /// </summary>
    public partial class ReceptionDocumentViewModel : ObservableObject
    {
        [ObservableProperty] private string receptionNumber = string.Empty;
        [ObservableProperty] private string supplierName = string.Empty;
        [ObservableProperty] private DateTime receptionDate = DateTime.Now;
        [ObservableProperty] private string warehouse = string.Empty;
        [ObservableProperty] private string ttcType = string.Empty;
        [ObservableProperty] private string blNumber = string.Empty;
        [ObservableProperty] private string exchangeNumber = string.Empty;
        [ObservableProperty] private DateTime dueDate = DateTime.Now;
        [ObservableProperty] private string entryInfo = string.Empty;
        [ObservableProperty] private decimal balance = 0m;
        [ObservableProperty] private int totalReceivedQuantity = 0;
        [ObservableProperty] private int totalWeightQuantity = 0;
        [ObservableProperty] private ObservableCollection<ReceptionLineViewModel> lines;
        [ObservableProperty] private decimal totalPPA = 0m;
        [ObservableProperty] private decimal totalSHP = 0m;
        [ObservableProperty] private decimal totalHT = 0m;
        [ObservableProperty] private decimal totalTVA = 0m;
        [ObservableProperty] private decimal totalTTax = 0m;
        [ObservableProperty] private decimal totalTTC = 0m;

        public ReceptionDocumentViewModel()
        {
            lines = new ObservableCollection<ReceptionLineViewModel>();
            InitializeWithSampleData();
        }

        private void InitializeWithSampleData()
        {
            ReceptionNumber = "00428/20";
            SupplierName = "";
            ReceptionDate = new DateTime(2025, 4, 7);
            Warehouse = "PHARMACIE";
            TtcType = "-";
            Balance = -38980.10m;
            TotalReceivedQuantity = 0;
            TotalWeightQuantity = 0;

            Lines.Add(new ReceptionLineViewModel
            {
                SequenceNumber = 9,
                Barcode = "",
                ProductName = "BIB KIDS N LARGE A/BRAS KHBL 6...",
                ProductFullName = "BIB KIDS N LARGE A/BRAS KHBL 6...",
                QuantityReceived = 100,
                QuantityUG = 0,
                ExpiryDate = DateTime.MinValue,
                UnitPrice = 600.00m,
                Shp = 0.00m,
                Ppa = 720.00m,
                TaxRate = 0.00m,
                AmountHT = 60000.00m,
                LotBarcode = "67406903"
            });

            // Update totals
            TotalPPA = 72000.00m;
            TotalSHP = 0.00m;
            TotalHT = 60000.00m;
            TotalTVA = 0.00m;
            TotalTTax = 0.00m;
            TotalTTC = 60000.00m;
        }
    }

    public partial class ReceptionLineViewModel : ObservableObject
    {
        [ObservableProperty] private int sequenceNumber = 0;
        [ObservableProperty] private string barcode = string.Empty;
        [ObservableProperty] private string productName = string.Empty;
        [ObservableProperty] private string productFullName = string.Empty;
        [ObservableProperty] private int quantityReceived = 0;
        [ObservableProperty] private int quantityUG = 0;
        [ObservableProperty] private DateTime expiryDate = DateTime.MinValue;
        [ObservableProperty] private decimal unitPrice = 0m;
        [ObservableProperty] private decimal shp = 0m;
        [ObservableProperty] private decimal ppa = 0m;
        [ObservableProperty] private decimal taxRate = 0m;
        [ObservableProperty] private decimal amountHT = 0m;
        [ObservableProperty] private string lotBarcode = string.Empty;
    }
}
