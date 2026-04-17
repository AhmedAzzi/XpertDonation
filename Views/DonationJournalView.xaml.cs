using System.Windows.Controls;
using XpertPharm5Donation.ViewModels;

namespace XpertPharm5Donation.Views
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
