using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using XpertPharm5Donation.Data;
using XpertPharm5Donation.Models;

namespace XpertPharm5Donation.ViewModels
{
    public partial class DonationJournalViewModel : ObservableObject
    {
        private readonly AppDbContext _db;

        /// <summary>Fired when the user clicks Edit on a voucher</summary>
        public event Action<int>? EditVoucherRequested;

        public DonationJournalViewModel(AppDbContext db)
        {
            _db = db;
            Vouchers = [];
        }

        public ObservableCollection<DonationVoucher> Vouchers { get; }

        [ObservableProperty] private DonationVoucher? _selectedVoucher;
        [ObservableProperty] private bool _isBusy;
        [ObservableProperty] private string _statusMessage = string.Empty;
        [ObservableProperty] private bool _isStatusError;

        // ── Filters ──────────────────────────────────────────────────────────
        [ObservableProperty] private DateTime? _filterDateFrom;
        [ObservableProperty] private DateTime? _filterDateTo;
        [ObservableProperty] private string _filterDonor = string.Empty;
        [ObservableProperty] private string _filterStatus = "Tous"; // Tous, Validé

        // ── Stats ─────────────────────────────────────────────────────────────
        [ObservableProperty] private int _totalVouchers;
        [ObservableProperty] private int _totalValidated;
        [ObservableProperty] private int _totalUnitsReceived;

        // ═════════════════════════════════════════════════════════════════════
        //  COMMANDS
        // ═════════════════════════════════════════════════════════════════════

        [RelayCommand]
        public async Task LoadAsync()
        {
            if (IsBusy) return;
            IsBusy = true;
            try
            {
                var query = _db.DonationVouchers
                    .Include(v => v.Lines)
                    .AsQueryable();

                if (FilterDateFrom.HasValue)
                    query = query.Where(v => v.ReceiptDate >= FilterDateFrom.Value.Date);

                if (FilterDateTo.HasValue)
                    query = query.Where(v => v.ReceiptDate <= FilterDateTo.Value.Date.AddDays(1).AddSeconds(-1));

                if (!string.IsNullOrWhiteSpace(FilterDonor))
                    query = query.Where(v => v.DonorName.Contains(FilterDonor.Trim()));

                if (FilterStatus == "Validé")
                    query = query.Where(v => v.Status == VoucherStatus.Validated);

                var list = await query.OrderByDescending(v => v.ReceiptDate)
                                      .ThenByDescending(v => v.Id)
                                      .ToListAsync();

                Vouchers.Clear();
                foreach (var v in list) Vouchers.Add(v);

                // Compute stats
                TotalVouchers = Vouchers.Count;
                TotalValidated = Vouchers.Count(v => v.Status == VoucherStatus.Validated);
                TotalUnitsReceived = Vouchers
                    .Where(v => v.Status == VoucherStatus.Validated)
                    .Sum(v => v.TotalUnits);

                StatusMessage = $"{TotalVouchers} bon(s) trouvé(s).";
                IsStatusError = false;
            }
            catch (Exception ex) { StatusMessage = ex.Message; IsStatusError = true; }
            finally { IsBusy = false; }
        }

        [RelayCommand]
        private void ResetFilters()
        {
            FilterDateFrom = null;
            FilterDateTo = null;
            FilterDonor = string.Empty;
            FilterStatus = "Tous";
            _ = LoadAsync();
        }

        [RelayCommand]
        private void EditVoucher(DonationVoucher? voucher)
        {
            if (voucher == null) return;
            EditVoucherRequested?.Invoke(voucher.Id);
        }

    }
}
