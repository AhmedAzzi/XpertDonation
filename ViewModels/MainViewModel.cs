using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using XpertPharm5Donation.Data;
using XpertPharm5Donation.Models;

namespace XpertPharm5Donation.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        private readonly AppDbContext _db;



        public MainViewModel(AppDbContext db)
        {
            _db = db;
            CartItems = [];
            DrugSuggestions = [];
            _ = LoadDrugCacheAsync();
        }

        partial void OnSearchTextChanged(string value)
        {
            DebouncedSearch(value);
        }

        partial void OnSelectedSuggestionChanged(Drug? value)
        {
            if (value != null)
            {
                _ = SelectFromSearchAsync(value);
                SelectedSuggestion = null;
            }
        }

        // ── Inputs ──────────────────────────────────────────────────────────────
        [ObservableProperty] private string _barcodeInput = string.Empty;
        [ObservableProperty] private string _searchText = string.Empty;
        [ObservableProperty] private int _quantity = 1;

        // ── Selected drug info panel ─────────────────────────────────────────────
        [ObservableProperty] private Drug? _selectedDrug;
        [ObservableProperty] private StockBatch? _selectedStockBatch;
        [ObservableProperty] private int _selectedDrugRemaining;
        [ObservableProperty] private bool _hasExpiredBatches;

        // ── Collections ─────────────────────────────────────────────────────────
        public ObservableCollection<CartItem> CartItems { get; }
        public ObservableCollection<Drug> DrugSuggestions { get; }

        [ObservableProperty] private CartItem? _selectedCartItem;
        [ObservableProperty] private string _statusMessage = string.Empty;
        [ObservableProperty] private bool _isStatusError;
        [ObservableProperty] private bool _isBusy;
        [ObservableProperty] private bool _isSuggestionOpen;
        [ObservableProperty] private Drug? _selectedSuggestion;

        // Search cache for fast autocomplete
        private List<Drug> _allDrugsCache = [];
        private CancellationTokenSource? _searchCts;

        // ── Computed ─────────────────────────────────────────────────────────────
        public int TotalItemsInCart => CartItems.Sum(c => c.Quantity);
        public int TotalLinesInCart => CartItems.Count;

        // ── Commands ─────────────────────────────────────────────────────────────

        [RelayCommand]
        private async Task ScanBarcodeAsync()
        {
            if (string.IsNullOrWhiteSpace(BarcodeInput)) return;
            var barcode = BarcodeInput.Trim();
            var batch = await FindBatchByBarcodeAsync(barcode);
            
            if (batch != null)
            {
                await SelectBatchAsync(batch);
                BarcodeInput = string.Empty;

                // Auto add to cart if successful
                if (SelectedDrugRemaining > 0)
                {
                    AddToCart();
                }
            }
            else
            {
                // Fallback: Check if it's a Drug barcode
                var drug = await _db.Drugs
                    .Include(d => d.StockBatches)
                    .ThenInclude(sb => sb.Dispensations)
                    .FirstOrDefaultAsync(d => d.Barcode == barcode);

                if (drug != null)
                {
                    await SelectDrugAsync(drug);
                    BarcodeInput = string.Empty;

                    // When adding a drug directly (not a specific batch), AddToCart handles picking the oldest batch.
                    if (SelectedDrugRemaining > 0)
                    {
                        AddToCart();
                    }
                }
                else
                {
                    SetStatus($"Code barre introuvable : {barcode}", error: true);
                }
            }
        }



        [RelayCommand]
        private async Task SelectFromSearchAsync(Drug? drug)
        {
            if (drug == null) return;
            await SelectDrugAsync(drug);
        }

        // ═════════════════════════════════════════════════════════════════════
        //  DRUG SEARCH / AUTOCOMPLETE
        // ═════════════════════════════════════════════════════════════════════

        private async Task LoadDrugCacheAsync()
        {
            try
            {
                _allDrugsCache = await _db.Drugs
                    .Include(d => d.StockBatches)
                    .ThenInclude(b => b.Dispensations)
                    .OrderBy(d => d.Name)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to load drug cache: {ex.Message}");
            }
        }

        private void DebouncedSearch(string term)
        {
            _searchCts?.Cancel();
            _searchCts = new CancellationTokenSource();
            var ct = _searchCts.Token;

            Task.Run(async () =>
            {
                await Task.Delay(150, ct);
                if (ct.IsCancellationRequested) return;
                await Application.Current.Dispatcher.InvokeAsync(() => PerformSearch(term));
            }, ct);
        }

        private void PerformSearch(string term)
        {
            if (string.IsNullOrWhiteSpace(term))
            {
                DrugSuggestions.Clear();
                IsSuggestionOpen = false;
                return;
            }

            var lowerTerm = term.ToLowerInvariant();

            var results = _allDrugsCache
                .Where(d => d.Name.ToLowerInvariant().Contains(lowerTerm)
                         || (!string.IsNullOrEmpty(d.Dci) && d.Dci.ToLowerInvariant().Contains(lowerTerm))
                         || d.StockBatches.Any(b => !string.IsNullOrEmpty(b.Barcode) && b.Barcode.Contains(lowerTerm)))
                .OrderBy(d => d.Name.ToLowerInvariant().StartsWith(lowerTerm) ? 0 : 1)
                .ThenBy(d => d.Name)
                .Take(25)
                .ToList();

            DrugSuggestions.Clear();
            foreach (var d in results)
                DrugSuggestions.Add(d);

            IsSuggestionOpen = results.Count > 0;
        }

        [RelayCommand]
        private void FilterSuggestions()
        {
            PerformSearch(SearchText);
        }

        private async Task LoadSuggestionsAsync()
        {
            var list = await _db.Drugs
                .Include(d => d.StockBatches)
                .ThenInclude(b => b.Dispensations)
                .OrderBy(d => d.Name).Take(50).ToListAsync();

            DrugSuggestions.Clear();
            foreach (var d in list) DrugSuggestions.Add(d);
        }

        [RelayCommand]
        private void AddToCart()
        {
            if (SelectedDrug == null)
            {
                SetStatus("Veuillez sélectionner un médicament.", error: true);
                return;
            }

            // If no specific batch is selected (e.g. search by name), find the oldest non-expired batch
            SelectedStockBatch ??= SelectedDrug.StockBatches
                    .Where(b => !b.IsExpired && b.QuantityRemaining > 0)
                    .OrderBy(b => b.ExpirationDate ?? DateTime.MaxValue)
                    .FirstOrDefault();

            if (SelectedStockBatch == null)
            {
                SetStatus($"❌ Aucun stock disponible (valide) pour {SelectedDrug.Name}", error: true);
                return;
            }

            if (Quantity <= 0)
            {
                SetStatus("La quantité doit être supérieure à zéro.", error: true);
                return;
            }

            // Check stock for the specific batch
            int alreadyInCartForBatch = CartItems.Where(c => c.StockBatchId == SelectedStockBatch.Id).Sum(c => c.Quantity);
            int remainingInBatch = SelectedStockBatch.QuantityRemaining - alreadyInCartForBatch;

            if (Quantity > remainingInBatch)
            {
                SetStatus($"❌ Stock insuffisant pour le lot {SelectedStockBatch.BatchNumber}. Restant : {remainingInBatch}", error: true);
                return;
            }

            // Check if lot already exists in cart
            var existing = CartItems.FirstOrDefault(c => c.StockBatchId == SelectedStockBatch.Id);
            if (existing != null)
            {
                existing.Quantity += Quantity;
            }
            else
            {
                CartItems.Add(new CartItem
                {
                    DrugId = SelectedDrug.Id,
                    StockBatchId = SelectedStockBatch.Id,
                    DrugName = SelectedDrug.Name,
                    BatchNumber = SelectedStockBatch.BatchNumber ?? "S/N",
                    ExpirationDate = SelectedStockBatch.ExpirationDate,
                    Quantity = Quantity,
                    AvailableStock = SelectedStockBatch.QuantityRemaining
                });
            }

            OnPropertyChanged(nameof(TotalItemsInCart));
            OnPropertyChanged(nameof(TotalLinesInCart));
            SetStatus($"✔ {SelectedDrug.Name} (Lot: {SelectedStockBatch.BatchNumber}) ajouté au panier.", error: false);
            Quantity = 1;
            // Clear specific lot selection if it was a selection, or keep if user scans again
        }

        [RelayCommand]
        private void RemoveItem()
        {
            if (SelectedCartItem == null) return;
            CartItems.Remove(SelectedCartItem);
            SelectedCartItem = null;
            UpdateCartTotals();
            SetStatus("Ligne supprimée.", error: false);
        }

        public void UpdateCartTotals()
        {
            OnPropertyChanged(nameof(TotalItemsInCart));
            OnPropertyChanged(nameof(TotalLinesInCart));
        }



        [RelayCommand]
        private async Task ValidateAsync()
        {
            if (CartItems.Count == 0)
            {
                SetStatus("Le panier est vide.", error: true);
                return;
            }

            var result = MessageBox.Show(
                $"Confirmer la dispensation de {TotalLinesInCart} ligne(s) ({TotalItemsInCart} unité(s)) ?",
                "Valider la dispensation",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes) return;

            IsBusy = true;
            try
            {
                var sessionId = Guid.NewGuid();
                var dispensations = new List<Dispensation>();

                foreach (var item in CartItems)
                {
                    var batch = await _db.StockBatches
                        .Include(b => b.Drug)
                        .Include(b => b.Dispensations)
                        .FirstOrDefaultAsync(b => b.Id == item.StockBatchId);

                    if (batch == null) continue;

                    if (item.Quantity > batch.QuantityRemaining)
                        throw new InvalidOperationException($"Stock insuffisant pour le lot {batch.BatchNumber} de {batch.Drug.Name}.");

                    dispensations.Add(new Dispensation
                    {
                        StockBatchId = batch.Id,
                        Quantity = item.Quantity,
                        Date = DateTime.Now,
                        SessionId = sessionId
                    });
                }

                _db.Dispensations.AddRange(dispensations);
                await _db.SaveChangesAsync();

                CartItems.Clear();
                SelectedDrug = null;
                SelectedStockBatch = null;
                OnPropertyChanged(nameof(TotalItemsInCart));
                OnPropertyChanged(nameof(TotalLinesInCart));
                SetStatus($"✅ Dispensation validée — {dispensations.Sum(d => d.Quantity)} produit(s) distribué(s).", error: false);
            }
            catch (Exception ex)
            {
                SetStatus($"Erreur : {ex.Message}", error: true);
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        private void Cancel()
        {
            if (CartItems.Count > 0)
            {
                var result = MessageBox.Show("Annuler et vider le panier ?", "Annuler", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (result != MessageBoxResult.Yes) return;
                CartItems.Clear();
            }

            SelectedDrug = null;
            SelectedStockBatch = null;
            SearchText = string.Empty;
            DrugSuggestions.Clear();
            IsSuggestionOpen = false;
            UpdateCartTotals();
            SetStatus("Prêt", error: false);
        }

        // ── Helpers ───────────────────────────────────────────────────────────────

        private async Task SelectBatchAsync(StockBatch batch)
        {
            var b = await _db.StockBatches
                .Include(x => x.Drug)
                .ThenInclude(d => d.StockBatches)
                .ThenInclude(sb => sb.Dispensations)
                .FirstOrDefaultAsync(x => x.Id == batch.Id);

            if (b == null) return;

            SelectedStockBatch = b;
            SelectedDrug = b.Drug;

            SelectedDrugRemaining = b.Drug.StockBatches.Where(sb => !sb.IsExpired).Sum(sb => sb.QuantityRemaining);
            HasExpiredBatches = b.Drug.HasExpiredBatches;

            Quantity = 1;
            SetStatus($"Lot sélectionné : {b.BatchNumber} ({b.Drug.Name})  |  Dispo. lot : {b.QuantityRemaining}", error: false);
        }

        private async Task SelectDrugAsync(Drug drug)
        {
            var d = await _db.Drugs
                .Include(x => x.StockBatches)
                .ThenInclude(b => b.Dispensations)
                .FirstOrDefaultAsync(x => x.Id == drug.Id);

            if (d == null) return;

            SelectedDrug = d;
            SelectedStockBatch = null; // No batch selected until scannned or auto-assigned in AddToCart

            SelectedDrugRemaining = d.StockBatches.Where(b => !b.IsExpired).Sum(b => b.QuantityRemaining);
            HasExpiredBatches = d.HasExpiredBatches;

            Quantity = 1;

            if (SelectedDrugRemaining <= 0)
            {
                if (HasExpiredBatches)
                    SetStatus($"⚠ Aucun stock valide pour {d.Name}. Des lots périmés existent.", error: true);
                else
                    SetStatus($"❌ Rupture de stock pour {d.Name}.", error: true);
            }
            else
                SetStatus($"Produit sélectionné : {d.Name}  |  Stock valide : {SelectedDrugRemaining}", error: false);
        }

        private async Task<StockBatch?> FindBatchByBarcodeAsync(string barcode)
        {
            var b = await _db.StockBatches
                .Include(x => x.Drug)
                .ThenInclude(d => d.StockBatches)
                .ThenInclude(sb => sb.Dispensations)
                .FirstOrDefaultAsync(x => x.Barcode == barcode);
            return b;
        }

        private void SetStatus(string msg, bool error)
        {
            StatusMessage = msg;
            IsStatusError = error;
        }
    }
}
