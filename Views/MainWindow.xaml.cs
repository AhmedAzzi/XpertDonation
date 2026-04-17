using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using Microsoft.Extensions.DependencyInjection;
using XpertPharm5Donation.ViewModels;
using XpertPharm5Donation.Data;

namespace XpertPharm5Donation.Views
{
    public partial class MainWindow : Window
    {
        private readonly DispatcherTimer _clockTimer;
        private readonly HomeViewModel _homeVm;
        private readonly HomeView _homeView;
        private readonly PosView _posView;
        private readonly HistoryView _historyView;
        private readonly ManageDonationsView _stockView;
        private readonly ProductsView _productsView;
        private readonly DonationVoucherView _voucherView;
        private readonly DonationJournalView _journalView;

        // Expose PosVM so Window-level F8/ESC shortcuts work
        public MainViewModel PosVM { get; }

        public MainWindow(HomeViewModel homeVm, MainViewModel posVm, HistoryViewModel histVm, ManageDonationsViewModel manageVm,
                          DonationVoucherViewModel voucherVm, DonationJournalViewModel journalVm, AppDbContext db)
        {
            InitializeComponent();
            DataContext = this;

            PosVM = posVm;
            _homeVm = homeVm;

            _homeView    = new HomeView(homeVm);
            _posView     = new PosView(posVm);
            _historyView = new HistoryView(histVm);
            _stockView   = new ManageDonationsView(manageVm);
            _productsView = new ProductsView(manageVm, db);
            _voucherView = new DonationVoucherView(voucherVm);
            _journalView = new DonationJournalView(journalVm);

            // Wiring events for internal navigation
            voucherVm.NavigateToJournal += () => BtnJournal_Click(this, new RoutedEventArgs());
            journalVm.EditVoucherRequested += async (id) =>
            {
                BtnVouchers_Click(this, new RoutedEventArgs());
                await voucherVm.LoadVoucherAsync(id);
            };

            manageVm.RequestNewVoucherForDrug += (drug) =>
            {
                BtnVouchers_Click(this, new RoutedEventArgs());
                voucherVm.PrepareVoucherForDrug(drug);
            };

            NavigateTo(_homeView, "Page d'accueil", BtnHome);

            _clockTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _clockTimer.Tick += (_, _) => UpdateClock();
            _clockTimer.Start();
            UpdateClock();
        }

        private void UpdateClock()
        {
            ClockText.Text = DateTime.Now.ToString("HH:mm:ss");
            DateText.Text  = DateTime.Now.ToString("dddd dd MMMM yyyy", new System.Globalization.CultureInfo("fr-FR"));
        }

        private Button? _activeNav;

        private void NavigateTo(UIElement view, string title, Button navBtn)
        {
            MainContent.Content = view;
            PageTitle.Text = title;

            if (_activeNav != null) _activeNav.Style = (Style)FindResource("NavTabStyle");
            navBtn.Style = (Style)FindResource("NavTabActiveStyle");
            _activeNav = navBtn;
        }

        public void BtnHome_Click(object sender, RoutedEventArgs e)
        {
            NavigateTo(_homeView, "Page d'accueil", BtnHome);
            _homeVm.LoadDashboardCommand.Execute(null);
        }

        public void BtnComptoir_Click(object sender, RoutedEventArgs e)
            => NavigateTo(_posView, "Comptoir — Dispensation Don", BtnComptoir);

        public void BtnHistory_Click(object sender, RoutedEventArgs e)
            => NavigateTo(_historyView, "Historique des dispensations", BtnHistory);

        public void BtnStock_Click(object sender, RoutedEventArgs e)
            => NavigateTo(_stockView, "Stock par lot", BtnStock);

        public void BtnProducts_Click(object sender, RoutedEventArgs e)
            => NavigateTo(_productsView, "Catalogue produits", BtnProducts);

        public void BtnVouchers_Click(object sender, RoutedEventArgs e)
            => NavigateTo(_voucherView, "Bon de don", BtnVouchers);

        public void BtnJournal_Click(object sender, RoutedEventArgs e)
        {
            NavigateTo(_journalView, "Journal des bons de don", BtnJournal);
            if (_journalView.DataContext is DonationJournalViewModel vm)
            {
                vm.LoadCommand.Execute(null);
            }
        }
    }
}
