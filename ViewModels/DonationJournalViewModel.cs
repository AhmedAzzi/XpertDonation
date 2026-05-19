using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using XDonation.Data;
using XDonation.Models;

namespace XDonation.ViewModels
{
    public partial class DonationJournalViewModel : ObservableObject
    {
        private readonly AppDbContext _db;

        /// <summary>Fired when the user clicks Edit on a voucher</summary>
        public event Action<int>? EditVoucherRequested;
        public event Action? NewVoucherRequested;

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
        [ObservableProperty] private string _filterBarcode = string.Empty;

        partial void OnSelectedVoucherChanged(DonationVoucher? value)
        {
            UpdateLinesMatchStatus();
        }

        partial void OnFilterBarcodeChanged(string value)
        {
            UpdateLinesMatchStatus();
        }

        private void UpdateLinesMatchStatus()
        {
            if (SelectedVoucher == null) return;
            foreach (var line in SelectedVoucher.Lines)
            {
                if (!string.IsNullOrWhiteSpace(FilterBarcode))
                {
                    var term = FilterBarcode.Trim().ToLower();
                    bool systemMatch = line.Barcode?.ToLower().Contains(term) ?? false;
                    bool fabMatch = line.CodeBarresFabricant?.ToLower().Contains(term) ?? false;
                    if (systemMatch && fabMatch) line.MatchStatus = "Système & Fab.";
                    else if (systemMatch) line.MatchStatus = "Système";
                    else if (fabMatch) line.MatchStatus = "Fabricant";
                    else line.MatchStatus = string.Empty;
                }
                else
                {
                    line.MatchStatus = string.Empty;
                }
            }
            OnPropertyChanged(nameof(SelectedVoucher));
        }

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

                if (!string.IsNullOrWhiteSpace(FilterBarcode))
                {
                    var term = FilterBarcode.Trim().ToLower();
                    query = query.Where(v => v.Lines.Any(l => 
                        (l.Barcode != null && l.Barcode.ToLower().Contains(term)) ||
                        (l.CodeBarresFabricant != null && l.CodeBarresFabricant.ToLower().Contains(term))
                    ));
                }

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

                StatusMessage = $"{TotalVouchers} Entreé(s) trouvée(s).";
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
            FilterBarcode = string.Empty;
            FilterStatus = "Tous";
            _ = LoadAsync();
        }

        [RelayCommand]
        private void EditVoucher(DonationVoucher? voucher)
        {
            var v = voucher ?? SelectedVoucher;
            if (v == null) return;
            EditVoucherRequested?.Invoke(v.Id);
        }

        [RelayCommand]
        private void NewVoucher()
        {
            NewVoucherRequested?.Invoke();
        }

        [RelayCommand]
        private async Task DeleteVoucherAsync()
        {
            if (SelectedVoucher == null) return;
            
            var result = MessageBox.Show($"Voulez-vous vraiment supprimer l'Entreé n° {SelectedVoucher.VoucherNumber} ?", "Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result == MessageBoxResult.Yes)
            {
                _db.DonationVouchers.Remove(SelectedVoucher);
                await _db.SaveChangesAsync();
                await LoadAsync();
            }
        }

        [RelayCommand]
        private void PrintVoucher()
        {
            if (SelectedVoucher == null) return;
            MessageBox.Show("Impression de l'Entreé " + SelectedVoucher.VoucherNumber);
        }
    }
}
