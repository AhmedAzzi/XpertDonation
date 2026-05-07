using System.Windows;
using System.Windows.Controls;
using XDonation.ViewModels;

namespace XDonation.Views
{
    public partial class HomeView : UserControl
    {
        private readonly HomeViewModel _vm;

        public HomeView(HomeViewModel vm)
        {
            InitializeComponent();
            _vm = vm;
            DataContext = vm;
            Loaded += async (_, _) => await vm.LoadDashboardCommand.ExecuteAsync(null);
        }

        private void BtnGoComptoir_Click(object sender, RoutedEventArgs e)
            => (Window.GetWindow(this) as MainWindow)?.BtnComptoir_Click(sender, e);

        private void BtnGoStock_Click(object sender, RoutedEventArgs e)
            => (Window.GetWindow(this) as MainWindow)?.BtnStock_Click(sender, e);

        private void BtnGoHistory_Click(object sender, RoutedEventArgs e)
            => (Window.GetWindow(this) as MainWindow)?.BtnHistory_Click(sender, e);

        private void BtnGoVouchers_Click(object sender, RoutedEventArgs e)
            => (Window.GetWindow(this) as MainWindow)?.BtnVouchers_Click(sender, e);

        private void BtnGoJournal_Click(object sender, RoutedEventArgs e)
            => (Window.GetWindow(this) as MainWindow)?.BtnJournal_Click(sender, e);

        private void BtnGoProducts_Click(object sender, RoutedEventArgs e)
            => (Window.GetWindow(this) as MainWindow)?.BtnProducts_Click(sender, e);
    }
}
