using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace XpertPharm5Donation.Helpers
{
    public static class FocusExtensions
    {
        public static void RegisterGlobalEnterNavigation()
        {
            // Register for TextBox
            EventManager.RegisterClassHandler(typeof(TextBox), UIElement.KeyDownEvent, new KeyEventHandler(OnKeyDown));
            
            // Register for ComboBox
            EventManager.RegisterClassHandler(typeof(ComboBox), UIElement.KeyDownEvent, new KeyEventHandler(OnKeyDown));
            
            // Register for DatePicker
            EventManager.RegisterClassHandler(typeof(DatePicker), UIElement.KeyDownEvent, new KeyEventHandler(OnKeyDown));
        }

        private static void OnKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                var element = e.OriginalSource as UIElement;
                if (element == null) return;

                // If it's a multi-line TextBox, let the Enter key work normally
                if (element is TextBox textBox && textBox.AcceptsReturn)
                {
                    return;
                }

                // If the element has a custom InputBinding for Enter, let it handle it!
                foreach (InputBinding binding in element.InputBindings)
                {
                    if (binding is KeyBinding kb && kb.Key == Key.Enter)
                    {
                        if (kb.Command != null && kb.Command.CanExecute(kb.CommandParameter))
                        {
                            kb.Command.Execute(kb.CommandParameter);
                        }
                        break; // Command executed, now fall through to move focus
                    }
                }

                // Move focus to next element
                e.Handled = true;
                element.MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));
            }
        }
    }
}
