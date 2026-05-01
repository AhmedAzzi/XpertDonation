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
    public partial class DonationVoucherViewModel : ObservableObject
    {
    public RelayCommand<DonationVoucherLine> PrintBarcodeCommand => new RelayCommand<DonationVoucherLine>(OnPrintBarcode);

        private readonly AppDbContext _db;

        // ── Events ───────────────────────────────────────────────────────────
        /// <summary>Fired when the user wants to view the journal (after save)</summary>
        public event Action? NavigateToJournal;
        public event Action<Drug?>? RequestDrugEdit;

        public DonationVoucherViewModel(AppDbContext db)
        {
            _db = db;
            Lines = [];
            DrugSuggestions = [];
            InitNewVoucher();
        }

        // ── Current Voucher Header ───────────────────────────────────────────
        [ObservableProperty] private int _voucherId;        // 0 = new
        [ObservableProperty] private string _voucherNumber = string.Empty;
        [ObservableProperty] private string _donorName = string.Empty;
        [ObservableProperty] private string _donorType = "Particulier";
        [ObservableProperty] private DateTime _receiptDate = DateTime.Today;
        [ObservableProperty] private string _notes = string.Empty;
        [ObservableProperty] private VoucherStatus _voucherStatus = VoucherStatus.Draft;

        public bool IsEditMode => VoucherId > 0;
        public bool IsValidated => VoucherStatus == VoucherStatus.Validated;
        public bool CanEdit => true;
        public string StatusLabel => IsValidated ? "Validé" : "Nouveau";
        public string ValidateActionLabel => IsValidated ? "RE-VALIDER (F8)" : "VALIDER (F8)";
        public string VoucherModeLabel => IsValidated ? "Bon de don validé" : "Bon de don en cours";

        // ── Lines ────────────────────────────────────────────────────────────
        public ObservableCollection<DonationVoucherLine> Lines { get; }

        [ObservableProperty] private DonationVoucherLine? _selectedLine;

        // ── Line Input Form ──────────────────────────────────────────────────
        [ObservableProperty] private string _inputBarcode = string.Empty;
        [ObservableProperty] private string _inputDrugName = string.Empty;
        [ObservableProperty] private string _inputDci = string.Empty;
        [ObservableProperty] private string _inputBatchNumber = string.Empty;
        [ObservableProperty] private DateTime? _inputExpirationDate = DateTime.Today.AddYears(1);
        [ObservableProperty] private int _inputQuantity = 1;

        [ObservableProperty] private int? _inputDrugId;

        // Editing existing line
        [ObservableProperty] private bool _isLineEditMode;
        [ObservableProperty] private int _editingLineIndex = -1;

        // ── Search / Suggestions ─────────────────────────────────────────────
        public ObservableCollection<Drug> DrugSuggestions { get; }
        [ObservableProperty] private bool _isSuggestionOpen;

        // ── Status ───────────────────────────────────────────────────────────
        [ObservableProperty] private string _statusMessage = string.Empty;
        [ObservableProperty] private bool _isStatusError;
        [ObservableProperty] private bool _isBusy;

        // ── Computed ─────────────────────────────────────────────────────────
        public int TotalLines => Lines.Count;
        public int TotalUnits => Lines.Sum(l => l.Quantity);

        // ═════════════════════════════════════════════════════════════════════
        //  COMMANDS
        // ═════════════════════════════════════════════════════════════════════

        [RelayCommand]
        public void NewVoucher()
        {
            if (Lines.Count > 0 || !string.IsNullOrWhiteSpace(DonorName))
            {
                var r = MessageBox.Show("Créer un nouveau bon ?\nLes données non enregistrées seront perdues.",
                    "Nouveau bon", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (r != MessageBoxResult.Yes) return;
            }
            InitNewVoucher();
        }

        public void PrepareVoucherForDrug(Drug drug)
        {
            if (drug == null) return;
            
            // If already validated, we MUST start a new one
            if (IsValidated)
            {
                InitNewVoucher();
            }
            // If draft has lines, we ask to clear or add (simple version: just pre-fill the search box if possible)
            
            InputDrugId = drug.Id;
            InputDrugName = drug.Name;
            InputDci = drug.Dci ?? string.Empty;
            
            SetStatus($"Prêt à ajouter du stock pour : {drug.Name}", false);
        }

        [RelayCommand]
        private async Task FilterSuggestionsAsync()
        {
            var term = InputDrugName.Trim().ToLower();
            if (string.IsNullOrWhiteSpace(term))
            {
                DrugSuggestions.Clear();
                IsSuggestionOpen = false;
                return;
            }
            var list = await _db.Drugs
                .Where(d => d.Name.Contains(term) || (d.Dci != null && d.Dci.Contains(term)))
                .OrderBy(d => d.Name).Take(20).ToListAsync();
            DrugSuggestions.Clear();
            foreach (var d in list) DrugSuggestions.Add(d);
            IsSuggestionOpen = DrugSuggestions.Count > 0;
        }

        [RelayCommand]
        private void SelectDrugSuggestion(Drug drug)
        {
            if (drug == null) return;
            InputDrugId = drug.Id;
            InputDrugName = drug.Name;
            InputDci = drug.Dci ?? string.Empty;
            IsSuggestionOpen = false;
        }

        [RelayCommand]
        private async Task ShowDrugDialogAsync()
        {
            Drug? drug = null;
            if (InputDrugId.HasValue)
            {
                drug = await _db.Drugs.FirstOrDefaultAsync(d => d.Id == InputDrugId.Value);
            }
            else if (!string.IsNullOrWhiteSpace(InputDrugName))
            {
                // Try to find by name if ID is missing but name is typed
                var term = InputDrugName.Trim().ToLower();
                drug = await _db.Drugs.FirstOrDefaultAsync(d => d.Name.ToLower() == term);
            }

            RequestDrugEdit?.Invoke(drug);
        }

        [RelayCommand]
        private async Task ScanBarcodeAsync()
        {
            if (string.IsNullOrWhiteSpace(InputBarcode)) return;
            var barcode = InputBarcode.Trim();

            // Try to find existing batch/drug by barcode
            var batch = await _db.StockBatches
                .Include(b => b.Drug)
                .FirstOrDefaultAsync(b => b.Barcode == barcode);

            if (batch != null)
            {
                InputDrugId = batch.DrugId;
                InputDrugName = batch.Drug.Name;
                InputDci = batch.Drug.Dci ?? string.Empty;
                InputBatchNumber = batch.BatchNumber ?? string.Empty;
                SetStatus($"✓ Médicament trouvé : {batch.Drug.Name}", false);
            }
            else
            {
                // Fallback: Try by global Drug barcode
                var drug = await _db.Drugs.FirstOrDefaultAsync(d => d.Barcode == barcode);
                if (drug != null)
                {
                    InputDrugId = drug.Id;
                    InputDrugName = drug.Name;
                    InputDci = drug.Dci ?? string.Empty;
                    SetStatus($"✓ Médicament trouvé : {drug.Name}", false);
                }
                else
                {
                    SetStatus($"Code-barres non trouvé. Saisissez le médicament manuellement.", false);
                }
            }
        }

        [RelayCommand]
        private void AddLine()
        {
            if (string.IsNullOrWhiteSpace(InputDrugName))
            {
                SetStatus("Le nom du médicament est obligatoire.", true); return;
            }
            if (InputQuantity <= 0)
            {
                SetStatus("La quantité doit être > 0.", true); return;
            }

            var line = new DonationVoucherLine
            {
                DrugId = InputDrugId,
                DrugName = InputDrugName.Trim(),
                Dci = string.IsNullOrWhiteSpace(InputDci) ? null : InputDci.Trim(),
                Barcode = string.IsNullOrWhiteSpace(InputBarcode) ? null : InputBarcode.Trim(),
                BatchNumber = string.IsNullOrWhiteSpace(InputBatchNumber) ? null : InputBatchNumber.Trim(),
                ExpirationDate = InputExpirationDate,
                Quantity = InputQuantity,
            };

            if (IsLineEditMode && EditingLineIndex >= 0 && EditingLineIndex < Lines.Count)
            {
                // Copy values into the existing line (preserve Id/StockBatchId)
                var existing = Lines[EditingLineIndex];
                existing.DrugId = line.DrugId;
                existing.DrugName = line.DrugName;
                existing.Dci = line.Dci;
                existing.Barcode = line.Barcode;
                existing.BatchNumber = line.BatchNumber;
                existing.ExpirationDate = line.ExpirationDate;
                existing.Quantity = line.Quantity;
                Lines[EditingLineIndex] = existing; // force UI refresh
                IsLineEditMode = false;
                EditingLineIndex = -1;
                SetStatus("Ligne mise à jour.", false);
            }
            else
            {
                Lines.Add(line);
                SetStatus($"✓ {line.DrugName} ajouté ({line.Quantity} unités).", false);
            }

            ClearLineInputs();
            OnPropertyChanged(nameof(TotalLines));
            OnPropertyChanged(nameof(TotalUnits));
        }

        [RelayCommand]
        private void EditLine(DonationVoucherLine? line)
        {
            if (line == null) return;
            EditingLineIndex = Lines.IndexOf(line);
            IsLineEditMode = true;
            InputDrugId = line.DrugId;
            InputDrugName = line.DrugName;
            InputDci = line.Dci ?? string.Empty;
            InputBarcode = line.Barcode ?? string.Empty;
            InputBatchNumber = line.BatchNumber ?? string.Empty;
            InputExpirationDate = line.ExpirationDate;
            InputQuantity = line.Quantity;
        }

        [RelayCommand]
        private void RemoveLine(DonationVoucherLine? line)
        {
            if (line == null) return;
            Lines.Remove(line);
            OnPropertyChanged(nameof(TotalLines));
            OnPropertyChanged(nameof(TotalUnits));
            SetStatus("Ligne supprimée.", false);
        }

            private void OnPrintBarcode(DonationVoucherLine? line)
            {
                if (line == null) return;
                var vm = new BarcodeLabelDialogViewModel
                {
                    PharmacyName = "PHARMACIE ARAB", // TODO: Bind from settings
                    BarcodeNumber = GenerateInternalBarcode(line),
                    ProductName = line.DrugName,
                    Price = "GRATUIT", // Or line.Price if available
                    ExpiryDate = line.ExpirationDate?.ToString("dd-MM-yyyy"),
                    LotNumber = line.BatchNumber
                };
                var dlg = new Views.BarcodeLabelDialog(vm)
                {
                    Owner = Application.Current.Windows.OfType<Window>().FirstOrDefault(w => w.IsActive)
                };
                dlg.ShowDialog();
            }

            private string GenerateInternalBarcode(DonationVoucherLine line)
            {
                // Example: use line index + voucher id for uniqueness
                return $"{VoucherId:0000}{Lines.IndexOf(line):0000}";
            }

        [RelayCommand]
        private void CancelLineEdit()
        {
            IsLineEditMode = false;
            EditingLineIndex = -1;
            ClearLineInputs();
        }

        [RelayCommand]
        private async Task ValidateVoucherAsync()
        {
            if (!ValidateHeader()) return;
            if (Lines.Count == 0)
            {
                SetStatus("Le bon ne contient aucune ligne.", true); return;
            }

            var confirm = MessageBox.Show(
                $"{(IsValidated ? "Re-valider" : "Valider")} le bon {VoucherNumber} ?\n\n" +
                $"• {TotalLines} ligne(s)  — {TotalUnits} unité(s)\n\n" +
                "Le stock par lot sera synchronisé automatiquement.",
                "Validation du bon de don",
                MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (confirm != MessageBoxResult.Yes) return;
            await SaveVoucherAsync(validate: true);
        }

        [RelayCommand]
        private void Cancel()
        {
            InitNewVoucher();
            SetStatus("Opération annulée.", false);
        }

        [RelayCommand]
        private void ResetForm()
        {
            InitNewVoucher();
        }

        /// <summary>Load an existing voucher for editing.</summary>
        public async Task LoadVoucherAsync(int id)
        {
            IsBusy = true;
            try
            {
                var voucher = await _db.DonationVouchers
                    .Include(v => v.Lines)
                    .ThenInclude(l => l.Drug)
                    .FirstOrDefaultAsync(v => v.Id == id);

                if (voucher == null)
                {
                    SetStatus("Bon introuvable.", true); return;
                }

                VoucherId = voucher.Id;
                VoucherNumber = voucher.VoucherNumber;
                DonorName = voucher.DonorName;
                DonorType = voucher.DonorType ?? "Particulier";
                ReceiptDate = voucher.ReceiptDate;
                Notes = voucher.Notes ?? string.Empty;
                VoucherStatus = voucher.Status;

                Lines.Clear();
                foreach (var l in voucher.Lines.OrderBy(l => l.Id))
                    Lines.Add(l);

                OnPropertyChanged(nameof(IsEditMode));
                OnPropertyChanged(nameof(IsValidated));
                OnPropertyChanged(nameof(CanEdit));
                OnPropertyChanged(nameof(TotalLines));
                OnPropertyChanged(nameof(TotalUnits));
                SetStatus($"Bon {voucher.VoucherNumber} chargé.", false);
            }
            catch (Exception ex) { SetStatus(ex.Message, true); }
            finally { IsBusy = false; }
        }

        // ═════════════════════════════════════════════════════════════════════
        //  PRIVATE HELPERS
        // ═════════════════════════════════════════════════════════════════════

        private async Task SaveVoucherAsync(bool validate)
        {
            IsBusy = true;
            try
            {
                var wasValidated = VoucherStatus == VoucherStatus.Validated;
                DonationVoucher voucher;
                var removedValidatedLines = new Collection<DonationVoucherLine>();

                if (VoucherId == 0)
                {
                    // New voucher
                    voucher = new DonationVoucher
                    {
                        VoucherNumber = await GenerateVoucherNumberAsync(),
                        CreatedAt = DateTime.Now
                    };
                    _db.DonationVouchers.Add(voucher);
                }
                else
                {
                    // Existing voucher
                    voucher = await _db.DonationVouchers
                        .Include(v => v.Lines)
                        .FirstOrDefaultAsync(v => v.Id == VoucherId)
                        ?? throw new InvalidOperationException("Bon introuvable.");
                }

                // Update header
                voucher.DonorName = DonorName.Trim();
                voucher.DonorType = string.IsNullOrWhiteSpace(DonorType) ? null : DonorType.Trim();
                voucher.ReceiptDate = ReceiptDate;
                voucher.Notes = string.IsNullOrWhiteSpace(Notes) ? null : Notes.Trim();

                if (validate)
                {
                    voucher.Status = VoucherStatus.Validated;
                    voucher.ValidatedAt ??= DateTime.Now;
                }
                else
                {
                    voucher.Status = VoucherStatus.Draft;
                }

                // Sync lines
                var currentIds = Lines.Where(l => l.Id > 0).Select(l => l.Id).ToHashSet();
                var toRemove = voucher.Lines.Where(l => !currentIds.Contains(l.Id)).ToList();
                if (wasValidated)
                {
                    foreach (var removedLine in toRemove)
                    {
                        removedValidatedLines.Add(removedLine);
                    }
                }
                _db.DonationVoucherLines.RemoveRange(toRemove);

                foreach (var line in Lines)
                {
                    line.DrugName = line.DrugName.Trim();
                    line.Dci = string.IsNullOrWhiteSpace(line.Dci) ? null : line.Dci.Trim();
                    line.Barcode = string.IsNullOrWhiteSpace(line.Barcode) ? null : line.Barcode.Trim();
                    line.BatchNumber = string.IsNullOrWhiteSpace(line.BatchNumber) ? null : line.BatchNumber.Trim();

                    if (line.Id == 0)
                    {
                        voucher.Lines.Add(line);
                    }
                    else
                    {
                        var dbLine = voucher.Lines.First(l => l.Id == line.Id);
                        dbLine.DrugId = line.DrugId;
                        dbLine.DrugName = line.DrugName;
                        dbLine.Dci = line.Dci;
                        dbLine.Barcode = line.Barcode;
                        dbLine.BatchNumber = line.BatchNumber;
                        dbLine.ExpirationDate = line.ExpirationDate;
                        dbLine.Quantity = line.Quantity;
                    }
                }

                if (validate)
                {
                    foreach (var line in voucher.Lines)
                    {
                        line.DrugId = await ResolveDrugIdAsync(line);
                    }
                }

                await _db.SaveChangesAsync();

                if (validate)
                {
                    var savedVoucher = await _db.DonationVouchers
                        .Include(v => v.Lines)
                        .ThenInclude(l => l.StockBatch)
                        .FirstAsync(v => v.Id == voucher.Id);

                    await SynchronizeRemovedLinesAsync(removedValidatedLines);

                    foreach (var line in savedVoucher.Lines)
                    {
                        await SyncLineStockBatchAsync(line);
                    }

                    savedVoucher.ValidatedAt = DateTime.Now;
                    await _db.SaveChangesAsync();
                }

                VoucherId = voucher.Id;
                VoucherNumber = voucher.VoucherNumber;
                VoucherStatus = voucher.Status;
                OnPropertyChanged(nameof(IsEditMode));
                OnPropertyChanged(nameof(IsValidated));
                OnPropertyChanged(nameof(CanEdit));
                OnPropertyChanged(nameof(StatusLabel));
                OnPropertyChanged(nameof(ValidateActionLabel));
                OnPropertyChanged(nameof(VoucherModeLabel));

                var action = wasValidated ? "re-validé et stock resynchronisé" : "validé";
                SetStatus($"✅ Bon {VoucherNumber} {action}.", false);

                if (validate)
                    NavigateToJournal?.Invoke();
            }
            catch (Exception ex) { SetStatus($"Erreur : {ex.Message}", true); }
            finally { IsBusy = false; }
        }

        private bool ValidateHeader()
        {
            if (string.IsNullOrWhiteSpace(DonorName))
            {
                SetStatus("Le nom du donneur est obligatoire.", true); return false;
            }
            return true;
        }

        private async Task<string> GenerateVoucherNumberAsync()
        {
            var year = DateTime.Now.Year;
            var lastNum = await _db.DonationVouchers
                .Where(v => v.VoucherNumber.StartsWith($"BON-{year}-"))
                .CountAsync();
            return $"BON-{year}-{(lastNum + 1):D4}";
        }

        private void InitNewVoucher()
        {
            VoucherId = 0;
            VoucherNumber = "(nouveau)";
            DonorName = string.Empty;
            DonorType = "Particulier";
            ReceiptDate = DateTime.Today;
            Notes = string.Empty;
            VoucherStatus = VoucherStatus.Draft;
            Lines.Clear();
            ClearLineInputs();
            IsLineEditMode = false;
            EditingLineIndex = -1;
            OnPropertyChanged(nameof(IsEditMode));
            OnPropertyChanged(nameof(IsValidated));
            OnPropertyChanged(nameof(CanEdit));
            OnPropertyChanged(nameof(StatusLabel));
            OnPropertyChanged(nameof(ValidateActionLabel));
            OnPropertyChanged(nameof(VoucherModeLabel));
            OnPropertyChanged(nameof(TotalLines));
            OnPropertyChanged(nameof(TotalUnits));
            SetStatus("Nouveau bon de don prêt.", false);
        }

        private void ClearLineInputs()
        {
            InputBarcode = string.Empty;
            InputDrugName = string.Empty;
            InputDci = string.Empty;
            InputBatchNumber = string.Empty;
            InputExpirationDate = DateTime.Today.AddYears(1);
            InputQuantity = 1;
            InputDrugId = null;
        }

        private void SetStatus(string msg, bool error)
        {
            StatusMessage = msg;
            IsStatusError = error;
        }

        private async Task<int?> ResolveDrugIdAsync(DonationVoucherLine line)
        {
            if (line.DrugId.HasValue)
            {
                var linkedDrug = await _db.Drugs.FirstOrDefaultAsync(d => d.Id == line.DrugId.Value);
                if (linkedDrug != null)
                {
                    if (!string.IsNullOrWhiteSpace(line.Dci) && string.IsNullOrWhiteSpace(linkedDrug.Dci))
                    {
                        linkedDrug.Dci = line.Dci;
                    }

                    return linkedDrug.Id;
                }
            }

            var normalizedName = line.DrugName.Trim().ToLower();
            var existingDrug = await _db.Drugs.FirstOrDefaultAsync(d => d.Name.ToLower() == normalizedName);
            if (existingDrug != null)
            {
                if (!string.IsNullOrWhiteSpace(line.Dci) && string.IsNullOrWhiteSpace(existingDrug.Dci))
                {
                    existingDrug.Dci = line.Dci;
                }

                return existingDrug.Id;
            }

            var newDrug = new Drug
            {
                Name = line.DrugName.Trim(),
                Dci = line.Dci,
                CreatedAt = DateTime.Now
            };

            _db.Drugs.Add(newDrug);
            await _db.SaveChangesAsync();
            return newDrug.Id;
        }

        private async Task SyncLineStockBatchAsync(DonationVoucherLine line)
        {
            if (!line.DrugId.HasValue)
            {
                throw new InvalidOperationException($"Le médicament '{line.DrugName}' n'a pas pu être résolu.");
            }

            StockBatch? batch = null;
            if (line.StockBatchId.HasValue)
            {
                batch = await _db.StockBatches
                    .Include(b => b.Dispensations)
                    .FirstOrDefaultAsync(b => b.Id == line.StockBatchId.Value);
            }

            // If not found by ID, try to find by matching attributes (Drug, BatchNumber, Expiration)
            if (batch == null)
            {
                batch = await _db.StockBatches
                    .Include(b => b.Dispensations)
                    .FirstOrDefaultAsync(b => b.DrugId == line.DrugId.Value
                        && b.BatchNumber == line.BatchNumber
                        && b.ExpirationDate == line.ExpirationDate);
            }

            if (batch == null)
            {
                batch = new StockBatch
                {
                    DrugId = line.DrugId.Value,
                    BatchNumber = line.BatchNumber,
                    Barcode = line.Barcode,
                    ExpirationDate = line.ExpirationDate,
                    InitialQuantity = line.Quantity, // Temporary, will be recalculated
                    CreatedAt = DateTime.Now
                };

                _db.StockBatches.Add(batch);
                await _db.SaveChangesAsync();
                line.StockBatchId = batch.Id;
            }
            else
            {
                line.StockBatchId = batch.Id;
            }

            batch.DrugId = line.DrugId.Value;
            batch.BatchNumber = line.BatchNumber;
            batch.Barcode = line.Barcode;
            batch.ExpirationDate = line.ExpirationDate;

            // Enforce saving the StockBatchId association to the database BEFORE running the SUM query
            await _db.SaveChangesAsync();

            // Recalculate InitialQuantity as the sum of all voucher lines pointing to this batch
            var totalInVouchers = await _db.DonationVoucherLines
                .Where(l => l.StockBatchId == batch.Id)
                .SumAsync(l => l.Quantity);

            // Note: If the batch was created manually (not via voucher), we might want to preserve 
            // that initial amount? But usually it's better to keep it synced.
            // For now, let's assume InitialQuantity = sum(vouchers) + manual_base (if any).
            // But the user said "increment the stock", implying it accumulates.

            var minimumInitialQuantity = batch.QuantityUsed;
            if (totalInVouchers < minimumInitialQuantity)
            {
                // If the sum of vouchers is less than what was dispensed, 
                // we keep it at the minimum to avoid negative stock.
                batch.InitialQuantity = minimumInitialQuantity;
            }
            else
            {
                batch.InitialQuantity = totalInVouchers;
            }
        }

        private async Task SynchronizeRemovedLinesAsync(Collection<DonationVoucherLine> removedLines)
        {
            foreach (var removedLine in removedLines.Where(l => l.StockBatchId.HasValue))
            {
                var batch = await _db.StockBatches
                    .Include(b => b.Dispensations)
                    .FirstOrDefaultAsync(b => b.Id == removedLine.StockBatchId!.Value);

                if (batch == null)
                {
                    continue;
                }

                if (batch.QuantityUsed == 0)
                {
                    // Check if there are ANY other voucher lines for this batch
                    var otherLinesCount = await _db.DonationVoucherLines
                        .AnyAsync(l => l.StockBatchId == batch.Id && l.Id != removedLine.Id);

                    if (!otherLinesCount)
                    {
                        _db.StockBatches.Remove(batch);
                        continue;
                    }
                }

                // Recalculate InitialQuantity for the batch
                var totalInVouchers = await _db.DonationVoucherLines
                    .Where(l => l.StockBatchId == batch.Id)
                    .SumAsync(l => l.Quantity);

                batch.InitialQuantity = Math.Max(batch.QuantityUsed, totalInVouchers);
            }
        }
    }
}
