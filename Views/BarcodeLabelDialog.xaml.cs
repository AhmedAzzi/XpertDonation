using System.Windows;
using XpertPharm5Donation.ViewModels;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Markup;
using System.Windows.Media;
using System.Printing;

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
            if (DataContext is not BarcodeLabelDialogViewModel vm) return;

            PrintDialog dlg = new PrintDialog();
            if (dlg.ShowDialog() == true)
            {
                // Build a label-sized page so PDF/thermal output has no A4 whitespace.
                double labelWidth = 40.0 / 25.4 * 96.0;
                double labelHeight = 20.0 / 25.4 * 96.0;

                dlg.PrintTicket.PageMediaSize = new PageMediaSize(labelWidth, labelHeight);
                dlg.PrintTicket.PageOrientation = PageOrientation.Landscape;

                var label = new BarcodeLabelPreview { DataContext = vm };
                label.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                label.Arrange(new Rect(label.DesiredSize));
                label.UpdateLayout();

                var page = new FixedPage
                {
                    Width = labelWidth,
                    Height = labelHeight,
                    Background = Brushes.White
                };

                var content = new Viewbox
                {
                    Width = labelWidth,
                    Height = labelHeight,
                    Stretch = Stretch.Fill,
                    Child = label
                };
                FixedPage.SetLeft(content, 0);
                FixedPage.SetTop(content, 0);
                page.Children.Add(content);
                page.Measure(new Size(labelWidth, labelHeight));
                page.Arrange(new Rect(0, 0, labelWidth, labelHeight));
                page.UpdateLayout();

                var fixedDocument = new FixedDocument();
                fixedDocument.DocumentPaginator.PageSize = new Size(labelWidth, labelHeight);

                var pageContent = new PageContent();
                ((IAddChild)pageContent).AddChild(page);
                fixedDocument.Pages.Add(pageContent);

                dlg.PrintDocument(fixedDocument.DocumentPaginator, "Impression étiquette code-barres");
            }
        }
    }
}
