using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using XpertPharm5Donation.Data;
using XpertPharm5Donation.Models;

namespace XpertPharm5Donation.ViewModels
{
    public enum StockFilterType
    {
        All,
        NonExpired,
        Expired,
        Blocked,
        Exp90Days,
        Exp30Days,
        Exp60Days
    }

    public class LotHistoryEntry
    {
        public int VoucherId { get; set; }
        public string VoucherNumber { get; set; } = string.Empty;
        public string EntryType { get; set; } = string.Empty;
        public DateTime? Date { get; set; }
        public string DonorName { get; set; } = string.Empty;
        public int QuantityReceived { get; set; }
    }

    public partial class StockLotsViewModel : ObservableObject
    {
        private readonly AppDbContext _db;
        private readonly SemaphoreSlim _dbLock = new(1, 1);
        private List<StockBatch> _allBatches = new();


        public ObservableCollection<StockBatch> StockLots { get; } = [];
        public ObservableCollection<LotHistoryEntry> LotHistory { get; } = [];

        public event Action<int>? EditVoucherRequested;

        [ObservableProperty] private string _codeBarreFilter = string.Empty;
        [ObservableProperty] private string _dciFilter = string.Empty;
        [ObservableProperty] private bool _showZeroQuantities;
        [ObservableProperty] private StockFilterType _selectedFilter = StockFilterType.All;
        [ObservableProperty] private StockBatch? _selectedLot;
        [ObservableProperty] private LotHistoryEntry? _selectedHistoryEntry;

        [ObservableProperty] private bool _isBusy;
        [ObservableProperty] private string _statusMessage = string.Empty;
        [ObservableProperty] private bool _isStatusError;

        // Counts for radio button labels
        [ObservableProperty] private int _expiredCount;
        [ObservableProperty] private int _blockedCount;
        [ObservableProperty] private int _exp90Count;
        [ObservableProperty] private int _exp30Count;
        [ObservableProperty] private int _exp60Count;
        [ObservableProperty] private int _totalLotsDisplayed;

        public StockLotsViewModel(AppDbContext db)
        {
            _db = db;
        }

        partial void OnSelectedFilterChanged(StockFilterType value) => ApplyFilters();
        partial void OnShowZeroQuantitiesChanged(bool value) => ApplyFilters();

        partial void OnSelectedLotChanged(StockBatch? value)
        {
            _ = LoadLotHistoryAsync();
        }

        public void FilterByText()
        {
            ApplyFilters();
        }



        [RelayCommand]
        public async Task LoadAsync()
        {
            if (IsBusy) return;
            IsBusy = true;

            await _dbLock.WaitAsync();
            try
            {
                _allBatches = await _db.StockBatches
                    .Include(b => b.Drug)
                    .Include(b => b.Dispensations)
                    .OrderBy(b => b.Drug.Name)
                    .ThenBy(b => b.ExpirationDate)
                    .ToListAsync();

                // Compute counts
                ExpiredCount = _allBatches.Count(b => b.IsExpired);
                BlockedCount = _allBatches.Count(b => b.IsBlocked);
                Exp90Count = _allBatches.Count(b => b.ExpirationDate.HasValue
                    && b.ExpirationDate.Value.Date >= DateTime.Today
                    && b.ExpirationDate.Value.Date < DateTime.Today.AddDays(90));
                Exp60Count = _allBatches.Count(b => b.ExpirationDate.HasValue
                    && b.ExpirationDate.Value.Date >= DateTime.Today
                    && b.ExpirationDate.Value.Date < DateTime.Today.AddDays(60));
                Exp30Count = _allBatches.Count(b => b.ExpirationDate.HasValue
                    && b.ExpirationDate.Value.Date >= DateTime.Today
                    && b.ExpirationDate.Value.Date < DateTime.Today.AddDays(30));

                ApplyFilters();
                IsStatusError = false;
            }
            catch (Exception ex)
            {
                StatusMessage = $"Erreur : {ex.Message}";
                IsStatusError = true;
            }
            finally
            {
                _dbLock.Release();
                IsBusy = false;
            }
        }

        [RelayCommand]
        private void EditVoucher(LotHistoryEntry? entry)
        {
            var e = entry ?? SelectedHistoryEntry;
            if (e != null && e.VoucherId > 0)
            {
                EditVoucherRequested?.Invoke(e.VoucherId);
            }
        }

        private void ApplyFilters()
        {
            var filtered = _allBatches.AsEnumerable();

            // Text filters
            if (!string.IsNullOrWhiteSpace(CodeBarreFilter))
            {
                var term = CodeBarreFilter.Trim().ToLower();
                filtered = filtered.Where(b =>
                    (b.Barcode != null && b.Barcode.ToLower().Contains(term)) ||
                    (b.Drug?.Barcode != null && b.Drug.Barcode.ToLower().Contains(term)));
            }

            if (!string.IsNullOrWhiteSpace(DciFilter))
            {
                var term = DciFilter.Trim().ToLower();
                filtered = filtered.Where(b =>
                    b.Drug?.Dci != null && b.Drug.Dci.ToLower().Contains(term));
            }

            // Radio filter
            filtered = SelectedFilter switch
            {
                StockFilterType.NonExpired => filtered.Where(b => !b.IsExpired),
                StockFilterType.Expired => filtered.Where(b => b.IsExpired),
                StockFilterType.Blocked => filtered.Where(b => b.IsBlocked),
                StockFilterType.Exp90Days => filtered.Where(b => b.ExpirationDate.HasValue
                    && b.ExpirationDate.Value.Date >= DateTime.Today
                    && b.ExpirationDate.Value.Date < DateTime.Today.AddDays(90)),
                StockFilterType.Exp60Days => filtered.Where(b => b.ExpirationDate.HasValue
                    && b.ExpirationDate.Value.Date >= DateTime.Today
                    && b.ExpirationDate.Value.Date < DateTime.Today.AddDays(60)),
                StockFilterType.Exp30Days => filtered.Where(b => b.ExpirationDate.HasValue
                    && b.ExpirationDate.Value.Date >= DateTime.Today
                    && b.ExpirationDate.Value.Date < DateTime.Today.AddDays(30)),
                _ => filtered
            };

            // Zero quantities
            if (!ShowZeroQuantities)
            {
                filtered = filtered.Where(b => b.QuantityRemaining > 0);
            }

            StockLots.Clear();
            foreach (var b in filtered)
                StockLots.Add(b);

            TotalLotsDisplayed = StockLots.Count;
        }

        private async Task LoadLotHistoryAsync()
        {
            LotHistory.Clear();
            if (SelectedLot == null) return;

            await _dbLock.WaitAsync();
            try
            {
                var lines = await _db.DonationVoucherLines
                    .Include(l => l.DonationVoucher)
                    .Where(l => l.StockBatchId == SelectedLot.Id)
                    .OrderByDescending(l => l.DonationVoucher.ReceiptDate)
                    .ToListAsync();

                foreach (var line in lines)
                {
                    LotHistory.Add(new LotHistoryEntry
                    {
                        VoucherId = line.DonationVoucherId,
                        VoucherNumber = line.DonationVoucher?.VoucherNumber ?? "-",
                        EntryType = "Réception Don",
                        Date = line.DonationVoucher?.ReceiptDate,
                        DonorName = line.DonationVoucher?.DonorName ?? "-",
                        QuantityReceived = line.Quantity
                    });
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Erreur : {ex.Message}";
                IsStatusError = true;
            }
            finally
            {
                _dbLock.Release();
            }
        }
    }
}
