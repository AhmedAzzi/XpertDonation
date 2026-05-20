using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using XDonation.Data;
using XDonation.Models;

namespace XDonation.ViewModels
{
    public partial class ManageDonationsViewModel : ObservableObject
    {
        private readonly AppDbContext _db;
        private readonly SemaphoreSlim _dbLock = new(1, 1);
        private CancellationTokenSource? _batchLoadCts;
        private CancellationTokenSource? _searchCts;

        public ManageDonationsViewModel(AppDbContext db)
        {
            _db = db;
            XDonation.Helpers.StockSync.StockChanged += OnStockChanged;
        }

        private void OnStockChanged()
        {
            Application.Current.Dispatcher.InvokeAsync(async () =>
            {
                await LoadAsync();
            });
        }

        public ObservableCollection<Drug> Drugs { get; } = [];
        public ObservableCollection<StockBatch> SelectedDrugStockBatches { get; } = [];

        [ObservableProperty] private Drug? _selectedDrug;
        [ObservableProperty] private StockBatch? _selectedStockBatch;

        [ObservableProperty] private bool _isBusy;
        [ObservableProperty] private string _statusMessage = string.Empty;
        [ObservableProperty] private bool _isStatusError;
        [ObservableProperty] private string _searchText = string.Empty;
        [ObservableProperty] private bool _isSearchDropDownOpen;

        public event Action<Drug?>? RequestDrugEdit;
        public event Action<Drug>? RequestNewVoucherForDrug;

        // Form for StockBatch
        [ObservableProperty] private bool _isStockFormVisible;
        [ObservableProperty] private bool _isStockEditMode;
        [ObservableProperty] private string _formBatchNumber = string.Empty;
        [ObservableProperty] private string _formBarcode = string.Empty;
        [ObservableProperty] private DateTime _formExpirationDate = DateTime.Today.AddYears(1);
        [ObservableProperty] private int _formInitialQuantity;
        public string StockFormTitle => IsStockEditMode ? "Corriger le lot existant" : "Ajouter du Stock (Nouveau Lot)";

        partial void OnSelectedDrugChanged(Drug? value)
        {
            if (value != null)
                IsSearchDropDownOpen = false;

            _batchLoadCts?.Cancel();
            _batchLoadCts = new CancellationTokenSource();
            _ = LoadBatchesForSelectedDrugAsync(_batchLoadCts.Token);
        }

        partial void OnSearchTextChanged(string value)
        {
            IsSearchDropDownOpen = !string.IsNullOrWhiteSpace(value);
            _searchCts?.Cancel();
            _searchCts = new CancellationTokenSource();
            _ = SearchAfterTypingAsync(_searchCts.Token);
        }

        private async Task SearchAfterTypingAsync(CancellationToken ct)
        {
            try
            {
                await Task.Delay(250, ct);
                await SearchAsync(ct);
            }
            catch (OperationCanceledException) { }
        }

        private async Task LoadBatchesForSelectedDrugAsync(CancellationToken ct)
        {
            SelectedDrugStockBatches.Clear();
            if (SelectedDrug == null) return;

            await _dbLock.WaitAsync(ct);
            try
            {
                _db.ChangeTracker.Clear();
                var batches = await _db.StockBatches
                    .Include(b => b.Dispensations)
                    .Where(b => b.DrugId == SelectedDrug.Id)
                    .OrderBy(b => b.ExpirationDate)
                    .ToListAsync(ct);

                foreach (var b in batches) SelectedDrugStockBatches.Add(b);
            }
            catch (OperationCanceledException) { /* Normal when selection changes fast */ }
            catch (Exception ex)
            {
                StatusMessage = $"Erreur lors du chargement des lots : {ex.Message}";
                IsStatusError = true;
            }
            finally { _dbLock.Release(); }
        }

        [RelayCommand]
        private async Task LoadAsync()
        {
            if (IsBusy) return;
            IsBusy = true;
            
            await _dbLock.WaitAsync();
            try
            {
                _db.ChangeTracker.Clear();
                var list = await _db.Drugs
                    .Include(d => d.StockBatches)
                    .ThenInclude(b => b.Dispensations)
                    .OrderBy(d => d.Name).ToListAsync();

                Drugs.Clear();
                foreach (var d in list) Drugs.Add(d);
                StatusMessage = $"{Drugs.Count} médicament(s) dans le catalogue.";
                IsStatusError = false;

                if (SelectedDrug != null)
                {
                    SelectedDrug = Drugs.FirstOrDefault(d => d.Id == SelectedDrug.Id);
                }
            }
            catch (Exception ex) { StatusMessage = ex.Message; IsStatusError = true; }
            finally 
            { 
                _dbLock.Release();
                IsBusy = false; 
            }
        }

        [RelayCommand]
        private async Task SearchAsync()
        {
            await SearchAsync(CancellationToken.None);
        }

        private async Task SearchAsync(CancellationToken ct)
        {
            if (IsBusy) return;
            IsBusy = true;
            var lockTaken = false;
            try
            {
                await _dbLock.WaitAsync(ct);
                lockTaken = true;

                _db.ChangeTracker.Clear();
                var term = SearchText?.Trim().ToLower() ?? string.Empty;
                var query = _db.Drugs
                    .Include(d => d.StockBatches)
                    .ThenInclude(b => b.Dispensations).AsQueryable();

                if (!string.IsNullOrEmpty(term))
                    query = query.Where(d => d.Name.ToLower().Contains(term) || d.StockBatches.Any(b => b.Barcode != null && b.Barcode.Contains(term)));

                var list = await query.OrderBy(d => d.Name).Take(60).ToListAsync(ct);
                Drugs.Clear();
                foreach (var d in list) Drugs.Add(d);
                IsStatusError = false;
                StatusMessage = string.IsNullOrWhiteSpace(term)
                    ? $"{Drugs.Count} médicament(s) dans le catalogue."
                    : $"{Drugs.Count} résultat(s) pour \"{SearchText?.Trim()}\".";
                IsSearchDropDownOpen = !string.IsNullOrWhiteSpace(term) && Drugs.Count > 0;
            }
            catch (OperationCanceledException) { }
            catch (Exception ex) { StatusMessage = ex.Message; IsStatusError = true; }
            finally 
            { 
                if (lockTaken)
                    _dbLock.Release();
                IsBusy = false; 
            }
        }

        [RelayCommand]
        private void ShowAddDrugForm()
        {
            RequestDrugEdit?.Invoke(null);
        }

        [RelayCommand]
        private void ShowEditDrugForm()
        {
            if (SelectedDrug != null)
                RequestDrugEdit?.Invoke(SelectedDrug);
        }

        [RelayCommand]
        private void ShowAddStockForm()
        {
            if (SelectedDrug == null)
            {
                StatusMessage = "Veuillez sélectionner un médicament dans la liste.";
                IsStatusError = true;
                return;
            }
            // Instead of showing the internal form, we request a new voucher
            RequestNewVoucherForDrug?.Invoke(SelectedDrug);
        }

        [RelayCommand]
        private void ShowEditStockForm(StockBatch batch)
        {
            if (batch == null) return;
            SelectedStockBatch = batch;
            IsStockEditMode = true;
            OnPropertyChanged(nameof(StockFormTitle)); // Notify title change
            FormBatchNumber = batch.BatchNumber ?? string.Empty;
            FormBarcode = batch.Barcode ?? string.Empty;
            FormExpirationDate = batch.ExpirationDate ?? DateTime.Today;
            FormInitialQuantity = batch.InitialQuantity;
            IsStockFormVisible = true;
        }

        [RelayCommand]
        private void CancelForm()
        {
            IsStockFormVisible = false;
        }


        [RelayCommand]
        private async Task SaveStockFormAsync()
        {
            if (SelectedDrug == null) return;
            if (FormInitialQuantity <= 0)
            {
                StatusMessage = "La quantité doit être supérieure à zéro.";
                IsStatusError = true;
                return;
            }

            IsBusy = true;
            await _dbLock.WaitAsync();
            try
            {
                if (IsStockEditMode && SelectedStockBatch != null)
                {
                    SelectedStockBatch.BatchNumber = string.IsNullOrWhiteSpace(FormBatchNumber) ? null : FormBatchNumber.Trim();
                    SelectedStockBatch.Barcode = string.IsNullOrWhiteSpace(FormBarcode) ? null : FormBarcode.Trim();
                    SelectedStockBatch.ExpirationDate = FormExpirationDate;
                    SelectedStockBatch.InitialQuantity = FormInitialQuantity;
                    _db.StockBatches.Update(SelectedStockBatch);
                    StatusMessage = "Lot mis à jour.";
                }
                else
                {
                    // Check if an identical batch already exists for this drug
                    var trimmedBatchNumber = string.IsNullOrWhiteSpace(FormBatchNumber) ? null : FormBatchNumber.Trim();
                    var existingBatch = await _db.StockBatches
                        .FirstOrDefaultAsync(b => b.DrugId == SelectedDrug.Id
                            && b.BatchNumber == trimmedBatchNumber
                            && b.ExpirationDate == FormExpirationDate);

                    if (existingBatch != null)
                    {
                        existingBatch.InitialQuantity += FormInitialQuantity;
                        existingBatch.Barcode = string.IsNullOrWhiteSpace(FormBarcode) ? existingBatch.Barcode : FormBarcode.Trim();
                        _db.StockBatches.Update(existingBatch);
                        StatusMessage = $"Quantité ajoutée au lot existant '{trimmedBatchNumber ?? "S/N"}'.";
                    }
                    else
                    {
                        var newBatch = new StockBatch
                        {
                            DrugId = SelectedDrug.Id,
                            BatchNumber = trimmedBatchNumber,
                            Barcode = string.IsNullOrWhiteSpace(FormBarcode) ? null : FormBarcode.Trim(),
                            ExpirationDate = FormExpirationDate,
                            InitialQuantity = FormInitialQuantity,
                            CreatedAt = DateTime.Now
                        };
                        _db.StockBatches.Add(newBatch);
                        StatusMessage = "Nouveau lot ajouté avec succès.";
                    }
                }

                await _db.SaveChangesAsync();
                
                // Notify all active views of stock change
                XDonation.Helpers.StockSync.NotifyStockChanged();

                IsStatusError = false;
                IsStockFormVisible = false;
            }
            catch (Exception ex) { StatusMessage = ex.Message; IsStatusError = true; }
            finally 
            { 
                _dbLock.Release();
                IsBusy = false; 
            }
            
            // Refresh outside the lock to avoid double wait (LoadAsync has its own lock)
            await LoadAsync();
            SelectedDrug = Drugs.FirstOrDefault(d => d.Id == SelectedDrug?.Id);
        }

        [RelayCommand]
        private async Task DeleteDrugAsync()
        {
            if (SelectedDrug == null) return;

            var hasDispensations = await _db.StockBatches
                .Where(b => b.DrugId == SelectedDrug.Id)
                .Include(b => b.Dispensations)
                .AnyAsync(b => b.Dispensations.Any());

            if (hasDispensations)
            {
                StatusMessage = "Suppression impossible : ce produit a deja des dispensations dans l'historique.";
                IsStatusError = true;
                return;
            }

            var result = MessageBox.Show(
                $"Supprimer '{SelectedDrug.Name}' ?\nTous les lots de stock et historiques associés seront définitivement supprimés.",
                "Confirmation critique",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes) return;

            IsBusy = true;
            await _dbLock.WaitAsync();
            try
            {
                var voucherLines = await _db.DonationVoucherLines
                    .Where(l => l.DrugId == SelectedDrug.Id)
                    .ToListAsync();

                foreach (var line in voucherLines)
                {
                    line.DrugId = null;
                    line.StockBatchId = null;
                }

                _db.Drugs.Remove(SelectedDrug);
                await _db.SaveChangesAsync();

                // Notify all active views of stock change
                XDonation.Helpers.StockSync.NotifyStockChanged();

                StatusMessage = "Médicament et tous ses lots supprimés.";
                IsStatusError = false;
                SelectedDrug = null;
            }
            catch (Exception ex) { StatusMessage = ex.Message; IsStatusError = true; }
            finally 
            { 
                _dbLock.Release();
                IsBusy = false; 
            }
            await LoadAsync();
        }

        [RelayCommand]
        private async Task DeleteStockBatchAsync(StockBatch batch)
        {
            if (batch == null) return;

            var dbBatch = await _db.StockBatches
                .Include(b => b.Dispensations)
                .FirstOrDefaultAsync(b => b.Id == batch.Id);

            if (dbBatch == null)
            {
                StatusMessage = "Lot introuvable.";
                IsStatusError = true;
                return;
            }

            if (dbBatch.Dispensations.Count != 0)
            {
                var count = dbBatch.Dispensations.Sum(d => d.Quantity);
                var forceResult = MessageBox.Show(
                    $"Attention : ce lot a déjà {count} unité(s) dispensée(s) dans l'historique.\n\n" +
                    "Si vous le supprimez, l'historique de ces dispensations sera définitivement perdu.\n\n" +
                    "Voulez-vous quand même forcer la suppression ?",
                    "Avertissement de perte de données",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Error);

                if (forceResult != MessageBoxResult.Yes) return;
            }
            else
            {
                var result = MessageBox.Show(
                    $"Supprimer le lot '{batch.BatchNumber}' avec QTE '{batch.InitialQuantity}' ?\nCela altèrera l'historique.",
                    "Suppression du lot",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result != MessageBoxResult.Yes) return;
            }

            IsBusy = true;
            await _dbLock.WaitAsync();
            try
            {
                var linkedVoucherLines = await _db.DonationVoucherLines
                    .Where(l => l.StockBatchId == dbBatch.Id)
                    .ToListAsync();

                foreach (var line in linkedVoucherLines)
                {
                    line.StockBatchId = null;
                }

                _db.StockBatches.Remove(dbBatch);
                await _db.SaveChangesAsync();
                
                // Notify all active views of stock change
                XDonation.Helpers.StockSync.NotifyStockChanged();

                StatusMessage = "Lot supprimé.";
                IsStatusError = false;
            }
            catch (Exception ex)
            {
                StatusMessage = $"Suppression impossible : {ex.Message}";
                IsStatusError = true;
            }
            finally 
            { 
                _dbLock.Release();
                IsBusy = false; 
            }
            await LoadAsync();
        }

        [RelayCommand]
        private async Task ClearSelectedDrugStockAsync()
        {
            if (SelectedDrug == null) return;

            var stockBatches = await _db.StockBatches
                .Include(b => b.Dispensations)
                .Where(b => b.DrugId == SelectedDrug.Id)
                .ToListAsync();

            if (stockBatches.Count == 0)
            {
                StatusMessage = "Aucun stock a supprimer pour ce produit.";
                IsStatusError = false;
                return;
            }

            var batchesWithDispensations = stockBatches.Where(b => b.Dispensations.Any()).ToList();
            if (batchesWithDispensations.Any())
            {
                var count = batchesWithDispensations.Sum(b => b.Dispensations.Sum(d => d.Quantity));
                var forceResult = MessageBox.Show(
                    $"Attention : certains lots de ce produit ont déjà {count} unité(s) dispensée(s) au total.\n\n" +
                    "Retirer tout le stock supprimera définitivement cet historique.\n\n" +
                    "Voulez-vous quand même continuer ?",
                    "Avertissement critique",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Error);

                if (forceResult != MessageBoxResult.Yes) return;
            }
            else
            {
                var result = MessageBox.Show(
                    $"Retirer tout le stock du produit '{SelectedDrug.Name}' ?\nLe produit restera dans le catalogue.",
                    "Retirer le stock",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result != MessageBoxResult.Yes) return;
            }

            IsBusy = true;
            await _dbLock.WaitAsync();
            try
            {
                var stockBatchIds = stockBatches.Select(b => b.Id).ToHashSet();
                var linkedVoucherLines = await _db.DonationVoucherLines
                    .Where(l => l.StockBatchId != null && stockBatchIds.Contains(l.StockBatchId.Value))
                    .ToListAsync();

                foreach (var line in linkedVoucherLines)
                {
                    line.StockBatchId = null;
                }

                _db.StockBatches.RemoveRange(stockBatches);
                await _db.SaveChangesAsync();

                // Notify all active views of stock change
                XDonation.Helpers.StockSync.NotifyStockChanged();

                StatusMessage = "Le stock du produit a ete retire. La fiche produit reste disponible dans le catalogue.";
                IsStatusError = false;
            }
            catch (Exception ex)
            {
                StatusMessage = $"Retrait du stock impossible : {ex.Message}";
                IsStatusError = true;
            }
            finally 
            { 
                _dbLock.Release();
                IsBusy = false; 
            }
            
            await LoadAsync();
            SelectedDrug = Drugs.FirstOrDefault(d => d.Id == SelectedDrug?.Id);
        }
    }
}
