using System.Windows.Controls;
using XDonation.ViewModels;

namespace XDonation.Views
{
    public partial class ManageDonationsView : UserControl
    {
        public ManageDonationsView(ManageDonationsViewModel vm)
        {
            InitializeComponent();
            DataContext = vm;
            Loaded += async (_, _) => await vm.LoadCommand.ExecuteAsync(null);
        }
    }
}
