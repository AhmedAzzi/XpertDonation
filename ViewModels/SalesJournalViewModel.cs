using System;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace XDonation.ViewModels
{
    /// <summary>
    /// ViewModel for SalesJournalView
    /// Handles transaction history display and filtering
    /// </summary>
    public partial class SalesJournalViewModel : ObservableObject
    {
        [ObservableProperty] private DateTime startDate = DateTime.Now.Date;
        [ObservableProperty] private DateTime endDate = DateTime.Now.Date;
        [ObservableProperty] private string selectedClient = string.Empty;
        [ObservableProperty] private ObservableCollection<SaleViewModel> sales = new();
        [ObservableProperty] private SaleViewModel? selectedSale;
        [ObservableProperty] private ObservableCollection<SaleLineViewModel> selectedSaleDetails = new();

        public SalesJournalViewModel()
        {
            InitializeWithSampleData();
        }

        partial void OnSelectedSaleChanged(SaleViewModel? value)
        {
            UpdateSaleDetails();
        }

        private void InitializeWithSampleData()
        {
            Sales.Add(new SaleViewModel
            {
                SaleNumber = "82073/25",
                SaleType = "Vente Libre (CASH)",
                Amount = 911.96m,
                Reason = "=",
                SaleDate = new DateTime(2025, 12, 4, 8, 37, 15),
                ClientName = "COMPTOIR",
                GlobalDiscount = 0.00m,
                DiscountedAmount = 911.96m,
                PaidAmount = 911.96m,
                RemainingAmount = 0.00m,
                DueDate = new DateTime(2025, 12, 4),
                EnteredBy = "OUSSAMA BENAOUDA",
                EntryDate = new DateTime(2025, 12, 4, 8, 37, 15),
                MachineName = "CAISSE-PC"
            });
 
            Sales.Add(new SaleViewModel
            {
                SaleNumber = "82074/25",
                SaleType = "Vente Libre (CASH)",
                Amount = 965.04m,
                Reason = "=",
                SaleDate = new DateTime(2025, 12, 4, 8, 49, 38),
                ClientName = "COMPTOIR",
                GlobalDiscount = 0.00m,
                DiscountedAmount = 965.04m,
                PaidAmount = 965.04m,
                RemainingAmount = 0.00m,
                DueDate = new DateTime(2025, 12, 4),
                EnteredBy = "OUSSAMA BENAOUDA",
                EntryDate = new DateTime(2025, 12, 4, 8, 49, 38),
                MachineName = "CAISSE-PC"
            });
        }

        private void UpdateSaleDetails()
        {
            SelectedSaleDetails.Clear();
            if (SelectedSale != null)
            {
                SelectedSaleDetails.Add(new SaleLineViewModel
                {
                    Code = "010591",
                    ProductName = "D-THREE 200 000 UI/ML AMP.INJ. B01AMP 2ML CON...",
                    Quantity = 2,
                    LotNumber = "101026008",
                    ExpiryDate = new DateTime(2030, 2, 1),
                    LotBarcode = "56681463",
                    SalePrice = 156.48m,
                    LineAmount = 312.96m
                });
            }
        }
    }

    public partial class SaleViewModel : ObservableObject
    {
        [ObservableProperty] private string saleNumber = string.Empty;
        [ObservableProperty] private string saleType = string.Empty;
        [ObservableProperty] private decimal amount = 0m;
        [ObservableProperty] private string reason = string.Empty;
        [ObservableProperty] private DateTime saleDate = DateTime.MinValue;
        [ObservableProperty] private string clientName = string.Empty;
        [ObservableProperty] private decimal globalDiscount = 0m;
        [ObservableProperty] private decimal discountedAmount = 0m;
        [ObservableProperty] private decimal paidAmount = 0m;
        [ObservableProperty] private decimal remainingAmount = 0m;
        [ObservableProperty] private DateTime dueDate = DateTime.MinValue;
        [ObservableProperty] private string enteredBy = string.Empty;
        [ObservableProperty] private DateTime entryDate = DateTime.MinValue;
        [ObservableProperty] private string machineName = string.Empty;
        [ObservableProperty] private string withInfo = string.Empty;
    }

    public partial class SaleLineViewModel : ObservableObject
    {
        [ObservableProperty] private string code = string.Empty;
        [ObservableProperty] private string productName = string.Empty;
        [ObservableProperty] private int quantity = 0;
        [ObservableProperty] private string lotNumber = string.Empty;
        [ObservableProperty] private DateTime expiryDate = DateTime.MinValue;
        [ObservableProperty] private string lotBarcode = string.Empty;
        [ObservableProperty] private decimal salePrice = 0m;
        [ObservableProperty] private decimal lineAmount = 0m;
    }
}
