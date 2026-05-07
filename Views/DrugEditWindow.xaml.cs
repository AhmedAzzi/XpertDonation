using System;
using System.Windows;
using XDonation.ViewModels;

namespace XDonation.Views
{
    public partial class DrugEditWindow : Window
    {
        public DrugEditWindow(DrugEditViewModel vm)
        {
            InitializeComponent();
            DataContext = vm;

            // Permettre le déplacement de la fenêtre
            this.MouseLeftButtonDown += (s, e) =>
            {
                if (e.LeftButton == System.Windows.Input.MouseButtonState.Pressed)
                    DragMove();
            };
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
