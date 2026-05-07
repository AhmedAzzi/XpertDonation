using System.Windows.Controls;
using XDonation.ViewModels;

namespace XDonation.Views
{
    public partial class DonationJournalView : UserControl
    {
        public DonationJournalView(DonationJournalViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
        }
    }
}
