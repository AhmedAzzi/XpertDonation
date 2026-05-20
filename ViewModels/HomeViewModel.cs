using System;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using XDonation.Data;
using XDonation.ViewModels;

namespace XDonation.ViewModels
{
    public partial class HomeViewModel : ObservableObject
    {
        private readonly AppDbContext _db;

        public HomeViewModel(AppDbContext db)
        {
            _db = db;
            XDonation.Helpers.StockSync.StockChanged += OnStockChanged;
        }

        private void OnStockChanged()
        {
            System.Windows.Application.Current.Dispatcher.InvokeAsync(async () =>
            {
                await LoadDashboardAsync();
            });
        }
        [ObservableProperty] private int _totalDonations; // Multi-Product refs
        [ObservableProperty] private int _totalDrugs;     // Total Units
        [ObservableProperty] private int _expiredLots;    // Count of expired lots
        [ObservableProperty] private int _expiringSoon30;
        [ObservableProperty] private int _expiringSoon60;
        [ObservableProperty] private int _expiringSoon90;

        [ObservableProperty] private int _totalDonationsThisMonth; // Bons de don validés ce mois-ci

        [ObservableProperty] private bool _isBusy;
        [ObservableProperty] private string _statusMessage = string.Empty;

        // Event for navigation with pre-applied filter
        public Action<StockFilterType>? IndicatorFilterRequested;

        [RelayCommand]
        public void NavigateWithFilter(object? parameter)
        {
            if (parameter is StockFilterType filterType)
            {
                IndicatorFilterRequested?.Invoke(filterType);
            }
        }

        [RelayCommand]
        public async Task LoadDashboardAsync()
        {
            if (IsBusy) return;
            IsBusy = true;
            try
            {
                _db.ChangeTracker.Clear();
                // 1. Total unique drug references
                TotalDonations = await _db.Drugs.CountAsync();

                var batches = await _db.StockBatches
                    .Include(b => b.Drug)
                    .Include(b => b.Dispensations)
                    .ToListAsync();

                var drugs = await _db.Drugs
                    .Include(d => d.StockBatches)
                    .ThenInclude(b => b.Dispensations)
                    .ToListAsync();

                // Bons de don validés ce mois-ci
                var startOfMonth = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
                TotalDonationsThisMonth = await _db.DonationVouchers
                    .CountAsync(v => v.Status == Models.VoucherStatus.Validated && v.ValidatedAt >= startOfMonth);

                // 2. Total units available (only valid, non-expired ones)
                TotalDrugs = batches.Where(b => !b.IsExpired).Sum(b => b.QuantityRemaining);

                // 3. Count of expired lots
                ExpiredLots = batches.Count(b => b.IsExpired && b.QuantityRemaining > 0);

                // 4. Expiring soon in 30/60/90 days (Count of lots expiring within the window)
                var today = DateTime.Today;
                ExpiringSoon30 = batches.Count(b => !b.IsExpired && b.QuantityRemaining > 0
                    && b.ExpirationDate.HasValue && b.ExpirationDate.Value.Date <= today.AddDays(30));

                ExpiringSoon60 = batches.Count(b => !b.IsExpired && b.QuantityRemaining > 0
                    && b.ExpirationDate.HasValue && b.ExpirationDate.Value.Date <= today.AddDays(60));

                ExpiringSoon90 = batches.Count(b => !b.IsExpired && b.QuantityRemaining > 0
                    && b.ExpirationDate.HasValue && b.ExpirationDate.Value.Date <= today.AddDays(90));

                StatusMessage = "Tableau de bord actualisé.";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Erreur : {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }
    }
}
