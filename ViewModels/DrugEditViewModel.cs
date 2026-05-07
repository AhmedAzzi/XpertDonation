using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using XDonation.Data;
using XDonation.Models;
using System;

namespace XDonation.ViewModels
{
    public partial class DrugEditViewModel : ObservableObject
    {
        private readonly AppDbContext _db;
        private readonly Drug? _originalDrug;

        [ObservableProperty] private string _name = string.Empty;
        [ObservableProperty] private string _dci = string.Empty;
        [ObservableProperty] private string _form = string.Empty;
        [ObservableProperty] private string _barcode = string.Empty;
        [ObservableProperty] private string _title = "Nouveau produit";
        [ObservableProperty] private string _statusMessage = string.Empty;
        [ObservableProperty] private bool _isStatusError;
        [ObservableProperty] private bool _isBusy;

        public bool IsEditMode => _originalDrug != null;
        public Drug? SavedDrug { get; private set; }

        public DrugEditViewModel(AppDbContext db, Drug? drug = null)
        {
            _db = db;
            _originalDrug = drug;

            if (drug != null)
            {
                Title = "Modifier le produit";
                Name = drug.Name;
                Dci = drug.Dci ?? string.Empty;
                Form = drug.Form ?? string.Empty;
                Barcode = drug.Barcode ?? string.Empty;
            }
        }

        [RelayCommand]
        private async Task SaveAsync(Window window)
        {
            if (string.IsNullOrWhiteSpace(Name))
            {
                StatusMessage = "Le nom du médicament est obligatoire.";
                IsStatusError = true;
                return;
            }

            IsBusy = true;
            try
            {
                if (IsEditMode && _originalDrug != null)
                {
                    _originalDrug.Name = Name.Trim();
                    _originalDrug.Dci = string.IsNullOrWhiteSpace(Dci) ? null : Dci.Trim();
                    _originalDrug.Form = string.IsNullOrWhiteSpace(Form) ? null : Form.Trim();
                    _originalDrug.Barcode = string.IsNullOrWhiteSpace(Barcode) ? null : Barcode.Trim();
                    _db.Drugs.Update(_originalDrug);
                    SavedDrug = _originalDrug;
                }
                else
                {
                    var newDrug = new Drug
                    {
                        Name = Name.Trim(),
                        Dci = string.IsNullOrWhiteSpace(Dci) ? null : Dci.Trim(),
                        Form = string.IsNullOrWhiteSpace(Form) ? null : Form.Trim(),
                        Barcode = string.IsNullOrWhiteSpace(Barcode) ? null : Barcode.Trim(),
                        CreatedAt = DateTime.Now
                    };
                    _db.Drugs.Add(newDrug);
                    SavedDrug = newDrug;
                }

                await _db.SaveChangesAsync();
                if (window != null) window.DialogResult = true;
                window?.Close();
            }
            catch (Exception ex)
            {
                StatusMessage = $"Erreur : {ex.Message}";
                IsStatusError = true;
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        private void Cancel(Window window)
        {
            window?.Close();
        }
    }
}
