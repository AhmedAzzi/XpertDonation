using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using XpertPharm5Donation.Data;
using XpertPharm5Donation.Models;

namespace XpertPharm5Donation.ViewModels
{
    public partial class HistoryViewModel : ObservableObject
    {
        private readonly AppDbContext _db;

        public HistoryViewModel(AppDbContext db)
        {
            _db = db;
            Sessions = [];
            DetailItems = [];
            StartDate = DateTime.Today.AddDays(-30);
            EndDate = DateTime.Today;
        }

        public ObservableCollection<SessionGroup> Sessions { get; }
        public ObservableCollection<Dispensation> DetailItems { get; }

        [ObservableProperty] private bool _isBusy;
        [ObservableProperty] private string _statusMessage = string.Empty;
        [ObservableProperty] private bool _isStatusError;

        [ObservableProperty] private DateTime _startDate;
        [ObservableProperty] private DateTime _endDate;
        [ObservableProperty] private int _totalSessions;
        [ObservableProperty] private int _totalUnits;

        private SessionGroup? _selectedSession;
        public SessionGroup? SelectedSession
        {
            get => _selectedSession;
            set
            {
                SetProperty(ref _selectedSession, value);
                LoadDetails();
            }
        }

        [RelayCommand]
        private async Task FilterAsync()
        {
            await LoadAsync();
        }

        public async Task LoadAsync()
        {
            IsBusy = true;
            try
            {
                var dispensations = await _db.Dispensations
                    .Include(u => u.StockBatch)
                    .ThenInclude(b => b.Drug)
                    .Where(u => u.Date.Date >= StartDate.Date && u.Date.Date <= EndDate.Date)
                    .OrderByDescending(u => u.Date)
                    .ToListAsync();

                var grouped = dispensations.GroupBy(u => u.SessionId)
                    .Select(g => new SessionGroup
                    {
                        SessionId = g.Key,
                        Date = g.First().Date,
                        DateDisplay = g.First().Date.ToString("dd/MM/yyyy HH:mm:ss"),
                        TotalItems = g.Sum(x => x.Quantity),
                        TotalLines = g.Count(),
                        Dispensations = [.. g]
                    })
                    .OrderByDescending(g => g.Date)
                    .ToList();

                Sessions.Clear();
                foreach (var g in grouped) Sessions.Add(g);

                TotalSessions = Sessions.Count;
                TotalUnits = Sessions.Sum(s => s.TotalUnits);

                StatusMessage = $"{Sessions.Count} session(s) de dispensation trouvée(s).";
                IsStatusError = false;
                DetailItems.Clear();
            }
            catch (Exception ex)
            {
                StatusMessage = ex.Message;
                IsStatusError = true;
            }
            finally
            {
                IsBusy = false;
            }
        }

        private void LoadDetails()
        {
            DetailItems.Clear();
            if (SelectedSession == null) return;
            foreach (var item in SelectedSession.Dispensations)
            {
                DetailItems.Add(item);
            }
        }
    }

    public class SessionGroup
    {
        public Guid SessionId { get; set; }
        public DateTime Date { get; set; }
        public string DateDisplay { get; set; } = string.Empty;
        public int TotalItems { get; set; }
        public int TotalUnits => TotalItems; // for binding alias
        public int TotalLines { get; set; }
        public System.Collections.Generic.List<Dispensation> Dispensations { get; set; } = [];
    }
}
