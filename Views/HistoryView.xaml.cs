using System.Windows.Controls;
using XDonation.ViewModels;

namespace XDonation.Views
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
