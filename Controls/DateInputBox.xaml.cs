using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace XDonation.Controls
{
    public partial class DateInputBox : UserControl
    {
        public static readonly DependencyProperty DateProperty =
            DependencyProperty.Register(nameof(Date), typeof(DateTime?), typeof(DateInputBox),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnDateChanged));

        public DateTime? Date
        {
            get => (DateTime?)GetValue(DateProperty);
            set => SetValue(DateProperty, value);
        }

        private string _day = "";
        private string _month = "";
        private string _year = "";
        private bool _suppressChange;

        public DateInputBox()
        {
            InitializeComponent();
        }

        private static void OnDateChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DateInputBox box)
            {
                box._suppressChange = true;
                if (e.NewValue is DateTime dt)
                {
                    box._day = dt.Day.ToString("D2");
                    box._month = dt.Month.ToString("D2");
                    box._year = dt.Year.ToString();
                }
                else
                {
                    box._day = "";
                    box._month = "";
                    box._year = "";
                }
                box._suppressChange = false;
                box.UpdateBoxes();
            }
        }

        private void UpdateBoxes()
        {
            DayBox.Text = _day;
            MonthBox.Text = _month;
            YearBox.Text = _year;
        }

        private void TryCommitDate()
        {
            if (int.TryParse(_day, out int d) && int.TryParse(_month, out int m) && int.TryParse(_year, out int y))
            {
                if (d >= 1 && d <= 31 && m >= 1 && m <= 12 && y >= 1900 && y <= 2100)
                {
                    try
                    {
                        var newDate = new DateTime(y, m, d);
                        if (Date != newDate)
                            Date = newDate;
                    }
                    catch { }
                }
            }
            else if (string.IsNullOrEmpty(_day) || string.IsNullOrEmpty(_month) || string.IsNullOrEmpty(_year))
            {
                Date = null;
            }
        }

        private void HighlightBorder()
        {
            RootBorder.BorderBrush = (SolidColorBrush)FindResource("AccentBrush");
            RootBorder.BorderThickness = new Thickness(2);
        }

        private void NormalBorder()
        {
            if (!DayBox.IsFocused && !MonthBox.IsFocused && !YearBox.IsFocused)
            {
                RootBorder.BorderBrush = (SolidColorBrush)FindResource("BorderBrush");
                RootBorder.BorderThickness = new Thickness(1);
            }
        }

        private void HandleBack(TextBox current, TextBox prev)
        {
            if (string.IsNullOrEmpty(current.Text))
            {
                prev.Focus();
                prev.SelectAll();
            }
        }

        // ── Click handling ──

        private void Root_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            // Walk up to find if a TextBox was clicked
            var source = e.OriginalSource as DependencyObject;
            while (source != null)
            {
                if (source is TextBox tb)
                {
                    // Clicked inside a TextBox, let its own handlers manage it
                    return;
                }
                source = VisualTreeHelper.GetParent(source);
            }

            // Clicked on empty space or separator, find nearest box
            var pt = e.GetPosition(RootBorder);
            double x = pt.X;
            double w = RootBorder.ActualWidth;
            if (w <= 0) return;

            // Adjust for padding (4px left)
            x -= 4;

            // Widths: Day(28) + Slash(10) + Month(28) + Slash(10) + Year(40) = 116
            // We split the space into 3 zones roughly
            if (x < 35)
            {
                DayBox.Focus();
                DayBox.SelectAll();
            }
            else if (x < 75)
            {
                MonthBox.Focus();
                MonthBox.SelectAll();
            }
            else
            {
                YearBox.Focus();
                YearBox.SelectAll();
            }
            e.Handled = true;
        }

        // ── Day TextBox ──

        private void DayBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !IsDigit(e.Text);
            if (e.Handled) return;

            var tb = (TextBox)sender;
            string newText = GetTextAfterSelection(tb, e.Text);
            if (newText.Length > 2) { e.Handled = true; return; }

            if (int.TryParse(newText, out int val) && val > 31)
            {
                e.Handled = true;
            }
        }

        private void DayBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            var tb = (TextBox)sender;
            if (e.Key == Key.Tab && Keyboard.Modifiers == ModifierKeys.None)
            {
                MonthBox.Focus();
                e.Handled = true;
            }
            else if (e.Key == Key.Right && tb.CaretIndex >= tb.Text.Length)
            {
                MonthBox.Focus();
                e.Handled = true;
            }
            else if (e.Key == Key.Back && tb.SelectionStart == 0 && tb.SelectionLength == 0 && string.IsNullOrEmpty(tb.Text))
            {
                e.Handled = true;
            }
        }

        private void DayBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_suppressChange) return;
            _day = FilterDigits(DayBox.Text);
            if (_day.Length >= 2)
            {
                MonthBox.Focus();
            }
            TryCommitDate();
        }

        private void DayBox_GotFocus(object sender, RoutedEventArgs e)
        {
            DayBox.SelectAll();
            HighlightBorder();
        }

        private void DayBox_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (!DayBox.IsFocused)
            {
                DayBox.Focus();
            }
            DayBox.SelectAll();
            e.Handled = true;
        }

        // ── Month TextBox ──

        private void MonthBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !IsDigit(e.Text);
            if (e.Handled) return;

            var tb = (TextBox)sender;
            string newText = GetTextAfterSelection(tb, e.Text);
            if (newText.Length > 2) { e.Handled = true; return; }

            if (int.TryParse(newText, out int val) && val > 12)
            {
                e.Handled = true;
            }
        }

        private void MonthBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            var tb = (TextBox)sender;
            if (e.Key == Key.Tab && Keyboard.Modifiers == ModifierKeys.None)
            {
                YearBox.Focus();
                e.Handled = true;
            }
            else if (e.Key == Key.Tab && Keyboard.Modifiers == ModifierKeys.Shift)
            {
                DayBox.Focus();
                e.Handled = true;
            }
            else if (e.Key == Key.Right && tb.CaretIndex >= tb.Text.Length)
            {
                YearBox.Focus();
                e.Handled = true;
            }
            else if (e.Key == Key.Left && tb.CaretIndex == 0 && tb.SelectionLength == 0)
            {
                DayBox.Focus();
                e.Handled = true;
            }
            else if (e.Key == Key.Back && tb.SelectionStart == 0 && tb.SelectionLength == 0 && string.IsNullOrEmpty(tb.Text))
            {
                DayBox.Focus();
                e.Handled = true;
            }
        }

        private void MonthBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_suppressChange) return;
            _month = FilterDigits(MonthBox.Text);
            if (_month.Length >= 2)
            {
                YearBox.Focus();
            }
            TryCommitDate();
        }

        private void MonthBox_GotFocus(object sender, RoutedEventArgs e)
        {
            MonthBox.SelectAll();
            HighlightBorder();
        }

        private void MonthBox_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (!MonthBox.IsFocused)
            {
                MonthBox.Focus();
            }
            MonthBox.SelectAll();
            e.Handled = true;
        }

        // ── Year TextBox ──

        private void YearBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !IsDigit(e.Text);
            if (e.Handled) return;

            var tb = (TextBox)sender;
            string newText = GetTextAfterSelection(tb, e.Text);
            if (newText.Length > 4) { e.Handled = true; }
        }

        private void YearBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            var tb = (TextBox)sender;
            if (e.Key == Key.Tab && Keyboard.Modifiers == ModifierKeys.Shift)
            {
                MonthBox.Focus();
                e.Handled = true;
            }
            else if (e.Key == Key.Left && tb.CaretIndex == 0 && tb.SelectionLength == 0)
            {
                MonthBox.Focus();
                e.Handled = true;
            }
            else if (e.Key == Key.Back && tb.SelectionStart == 0 && tb.SelectionLength == 0 && string.IsNullOrEmpty(tb.Text))
            {
                MonthBox.Focus();
                e.Handled = true;
            }
            else if (e.Key == Key.Right && tb.CaretIndex >= tb.Text.Length)
            {
                e.Handled = true;
            }
        }

        private void YearBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_suppressChange) return;
            _year = FilterDigits(YearBox.Text);
            TryCommitDate();
        }

        private void YearBox_GotFocus(object sender, RoutedEventArgs e)
        {
            YearBox.SelectAll();
            HighlightBorder();
        }

        private void YearBox_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (!YearBox.IsFocused)
            {
                YearBox.Focus();
            }
            YearBox.SelectAll();
            e.Handled = true;
        }

        private void DateBox_LostFocus(object sender, RoutedEventArgs e)
        {
            NormalBorder();
        }

        // ── Helpers ──

        private string GetTextAfterSelection(TextBox tb, string newText)
        {
            if (tb.SelectionLength > 0)
            {
                return tb.Text.Substring(0, tb.SelectionStart) + newText;
            }
            string before = tb.Text.Substring(0, tb.CaretIndex);
            string after = tb.Text.Substring(tb.CaretIndex);
            return before + newText + after;
        }

        private static bool IsDigit(string text) => text.Length == 1 && char.IsDigit(text[0]);

        private static string FilterDigits(string text) => new(text.Where(char.IsDigit).ToArray());
    }
}
