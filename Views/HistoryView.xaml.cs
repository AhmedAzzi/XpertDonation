using System.Windows.Controls;
using XpertPharm5Donation.ViewModels;

namespace XpertPharm5Donation.Views
{
    public partial class HistoryView : UserControl
    {
        public HistoryView(HistoryViewModel vm)
        {
            InitializeComponent();
            DataContext = vm;
            Loaded += async (_, _) => await vm.FilterCommand.ExecuteAsync(null);
        }
    }
}
