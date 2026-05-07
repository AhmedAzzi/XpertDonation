using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using Microsoft.Extensions.DependencyInjection;
using XDonation.ViewModels;
using XDonation.Data;

namespace XDonation.Views
{
    public partial class MainWindow : Window
    {
        private readonly DispatcherTimer _clockTimer;
        private readonly HomeViewModel _homeVm;
        private readonly HomeView _homeView;
        private readonly PosView _posView;
        private readonly HistoryView _historyView;
        private readonly ManageDonationsView _manageDonationsView;
        private readonly StockLotsView _stockLotsView;
        private readonly ProductsView _productsView;
        private readonly DonationVoucherView _voucherView;
        private readonly DonationJournalView _journalView;

        public MainViewModel PosVM { get; }

        public RelayCommand NavHomeCommand => new RelayCommand(_ => BtnHome_Click(this, new RoutedEventArgs()));
        public RelayCommand NavComptoirCommand => new RelayCommand(_ => BtnComptoir_Click(this, new RoutedEventArgs()));
        public RelayCommand NavStockCommand => new RelayCommand(_ => BtnStock_Click(this, new RoutedEventArgs()));
        public RelayCommand NavProductsCommand => new RelayCommand(_ => BtnProducts_Click(this, new RoutedEventArgs()));
        public RelayCommand NavVouchersCommand => new RelayCommand(_ => BtnVouchers_Click(this, new RoutedEventArgs()));
        public RelayCommand NavJournalCommand => new RelayCommand(_ => BtnJournal_Click(this, new RoutedEventArgs()));
        public RelayCommand NavHistoryCommand => new RelayCommand(_ => BtnHistory_Click(this, new RoutedEventArgs()));

        public MainWindow(HomeViewModel homeVm, MainViewModel posVm, HistoryViewModel histVm, ManageDonationsViewModel manageVm,
                          DonationVoucherViewModel voucherVm, DonationJournalViewModel journalVm, StockLotsViewModel stockLotsVm, AppDbContext db)
        {
            InitializeComponent();
            DataContext = this;

            PosVM = posVm;
            _homeVm = homeVm;

            _homeView    = new HomeView(homeVm);
            _posView     = new PosView(posVm);
            _historyView = new HistoryView(histVm);
            _manageDonationsView = new ManageDonationsView(manageVm);
            _stockLotsView = new StockLotsView(stockLotsVm);
            _productsView = new ProductsView(manageVm, db);
            _voucherView = new DonationVoucherView(voucherVm, db);
            _journalView = new DonationJournalView(journalVm);

            // Wiring events for internal navigation
            voucherVm.NavigateToJournal += () => BtnJournal_Click(this, new RoutedEventArgs());
            journalVm.EditVoucherRequested += async (id) =>
            {
                BtnVouchers_Click(this, new RoutedEventArgs());
                await voucherVm.LoadVoucherAsync(id);
            };
            stockLotsVm.EditVoucherRequested += async (id) =>
            {
                BtnVouchers_Click(this, new RoutedEventArgs());
                await voucherVm.LoadVoucherAsync(id);
            };
            journalVm.NewVoucherRequested += () =>
            {
                BtnVouchers_Click(this, new RoutedEventArgs());
                voucherVm.ResetFormCommand.Execute(null);
            };

            manageVm.RequestNewVoucherForDrug += (drug) =>
            {
                BtnVouchers_Click(this, new RoutedEventArgs());
                voucherVm.PrepareVoucherForDrug(drug);
            };

            // Home indicator click → navigate to Stock or Products with pre-applied filter
            homeVm.IndicatorFilterRequested += (filter) =>
            {
                if (filter == StockFilterType.All)
                {
                    // Références totales → Products catalog
                    BtnProducts_Click(this, new RoutedEventArgs());
                }
                else
                {
                    // All others → Stock with filter
                    BtnStock_Click(this, new RoutedEventArgs());
                    stockLotsVm.SelectedFilter = filter;
                    stockLotsVm.LoadCommand.Execute(null);
                }
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
            => NavigateTo(_posView, "Comptoir Don", BtnComptoir);

        public void BtnHistory_Click(object sender, RoutedEventArgs e)
            => NavigateTo(_historyView, "Journal de Comptoir", BtnHistory);

        public void BtnStock_Click(object sender, RoutedEventArgs e)
            => NavigateTo(_stockLotsView, "Stock", BtnStock);

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

        public void AboutButton_Click(object sender, RoutedEventArgs e)
        {
            var aboutWindow = new AboutWindow
            {
                Owner = this
            };
            aboutWindow.ShowDialog();
        }
    }
}
