using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;

namespace XpertPharm5Donation.ViewModels
{
    /// <summary>
    /// ViewModel for DashboardView
    /// Handles navigation and KPI indicators
    /// </summary>
    public partial class DashboardViewModel : ObservableObject
    {
        [ObservableProperty] private int missingCount = 0;
        [ObservableProperty] private int missingListCount = 0;
        [ObservableProperty] private int expiredLotsCount = 0;
        [ObservableProperty] private int expiredLotsThirtyDaysCount = 0;
        [ObservableProperty] private int expiredLotsSixtyDaysCount = 0;
        [ObservableProperty] private int expiredLotsNinetyDaysCount = 0;
        [ObservableProperty] private int supplierPaymentPendingCount = 0;

        public ICommand? NavigateToSalesCounterCommand { get; }
        public ICommand? NavigateToSalesJournalCommand { get; }
        public ICommand? NavigateToProductsCommand { get; }
        public ICommand? NavigateToReceptionCommand { get; }

        public DashboardViewModel()
        {
            InitializeWithSampleData();
        }

        private void InitializeWithSampleData()
        {
            MissingCount = 0;
            MissingListCount = 1857;
            ExpiredLotsCount = 0;
            ExpiredLotsThirtyDaysCount = 15;
            ExpiredLotsSixtyDaysCount = 45;
            ExpiredLotsNinetyDaysCount = 65;
            SupplierPaymentPendingCount = 0;
        }
    }
}
