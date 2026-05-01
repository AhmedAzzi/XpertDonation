using System.Windows;
using XpertPharm5Donation.ViewModels;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace XpertPharm5Donation.Views
{
    public partial class BarcodeLabelDialog : Window
    {
        public BarcodeLabelDialog(BarcodeLabelDialogViewModel vm)
        {
            InitializeComponent();
            DataContext = vm;
            vm.RefreshBarcode();

            if (vm != null)
                vm.PrintRequested += () => PrintLabel();
        }

        private void PrintLabel()
        {
            // Find the BarcodeLabelPreview control
            var preview = FindPreviewControl(this);
            if (preview == null)
            {
                MessageBox.Show("Aperçu non trouvé.");
                return;
            }

            PrintDialog dlg = new PrintDialog();
            if (dlg.ShowDialog() == true)
            {
                // Optionally scale for printer DPI
                var scale = dlg.PrintableAreaWidth / preview.ActualWidth;
                var transform = preview.LayoutTransform;
                preview.LayoutTransform = new ScaleTransform(scale, scale);
                dlg.PrintVisual(preview, "Impression étiquette code-barres");
                preview.LayoutTransform = transform;
            }
        }

        private BarcodeLabelPreview? FindPreviewControl(DependencyObject parent)
        {
            if (parent is BarcodeLabelPreview preview)
                return preview;
            int count = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < count; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                var result = FindPreviewControl(child);
                if (result != null) return result;
            }
            return null;
        }
    }
}
