using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;

namespace XDonation.Controls
{
    /// <summary>
    /// Modern segmented date input control: [ DD ] / [ MM ] / [ YYYY ]
    /// Bindable via the <see cref="Date"/> dependency property (DateTime?).
    /// </summary>
    public partial class DateInputBox : UserControl
    {
        // ═══════════════════════════════════════════════════════════════════
        //  DEPENDENCY PROPERTY
        // ═══════════════════════════════════════════════════════════════════

        public static readonly DependencyProperty DateProperty =
            DependencyProperty.Register(
                nameof(Date),
                typeof(DateTime?),
                typeof(DateInputBox),
                new FrameworkPropertyMetadata(
                    null,
                    FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                    OnDateChanged));

        public DateTime? Date
        {
            get => (DateTime?)GetValue(DateProperty);
            set => SetValue(DateProperty, value);
        }

        // ═══════════════════════════════════════════════════════════════════
        //  FIELDS
        // ═══════════════════════════════════════════════════════════════════

        private string _day = "";
        private string _month = "";
        private string _year = "";
        private bool _suppressCommit;

        // Animation durations
        private static readonly Duration AnimDuration = new(TimeSpan.FromMilliseconds(180));

        // Colors (cached from resources at construction)
        private SolidColorBrush _accentBrush = null!;
        private SolidColorBrush _fieldBorderBrush = null!;
        private SolidColorBrush _borderHoverBrush = null!;
        private SolidColorBrush _errorBrush = null!;

        // ═══════════════════════════════════════════════════════════════════
        //  CONSTRUCTOR
        // ═══════════════════════════════════════════════════════════════════

        public DateInputBox()
        {
            InitializeComponent();
            Loaded += (_, _) =>
            {
                _accentBrush = (SolidColorBrush)FindResource("AccentBrush");
                _fieldBorderBrush = (SolidColorBrush)FindResource("FieldBorderBrush");
                _borderHoverBrush = (SolidColorBrush)FindResource("BorderHoverBrush");
                _errorBrush = (SolidColorBrush)FindResource("ErrorBrush");
            };
        }

        // ═══════════════════════════════════════════════════════════════════
        //  DATE ↔ UI SYNC
        // ═══════════════════════════════════════════════════════════════════

        private static void OnDateChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not DateInputBox box) return;

            box._suppressCommit = true;
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
            box.UpdateBoxes();
            box._suppressCommit = false;
        }

        private void UpdateBoxes()
        {
            DayBox.Text = _day;
            MonthBox.Text = _month;
            YearBox.Text = _year;
        }

        private void TryCommitDate()
        {
            if (_suppressCommit) return;

            if (int.TryParse(_day, out int d) &&
                int.TryParse(_month, out int m) &&
                int.TryParse(_year, out int y))
            {
                if (d >= 1 && d <= 31 && m >= 1 && m <= 12 && y >= 1900 && y <= 2100)
                {
                    try
                    {
                        var newDate = new DateTime(y, m, d);
                        if (Date != newDate)
                            Date = newDate;
                        return;
                    }
                    catch { /* invalid combination like Feb 30 */ }
                }
            }

            // If any segment is empty/incomplete, set null
            if (string.IsNullOrEmpty(_day) && string.IsNullOrEmpty(_month) && string.IsNullOrEmpty(_year))
            {
                if (Date != null) Date = null;
            }
        }

        // ═══════════════════════════════════════════════════════════════════
        //  BORDER ANIMATIONS
        // ═══════════════════════════════════════════════════════════════════

        private bool IsAnySegmentFocused =>
            DayBox.IsFocused || MonthBox.IsFocused || YearBox.IsFocused;

        private void AnimateBorderTo(Color targetColor, double targetThickness, double shadowOpacity = 0)
        {
            // Border color
            var borderAnim = new ColorAnimation(targetColor, AnimDuration)
            { EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut } };
            RootBorder.BorderBrush = new SolidColorBrush();
            RootBorder.BorderBrush.BeginAnimation(SolidColorBrush.ColorProperty, borderAnim);

            // Border thickness (uniform)
            var thicknessAnim = new ThicknessAnimation(
                new Thickness(targetThickness), AnimDuration)
            { EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut } };
            RootBorder.BeginAnimation(Border.BorderThicknessProperty, thicknessAnim);

            // Shadow glow
            if (RootBorder.Effect is DropShadowEffect shadow)
            {
                var opacityAnim = new DoubleAnimation(shadowOpacity, AnimDuration);
                shadow.BeginAnimation(DropShadowEffect.OpacityProperty, opacityAnim);

                var blurAnim = new DoubleAnimation(shadowOpacity > 0 ? 6 : 0, AnimDuration);
                shadow.BeginAnimation(DropShadowEffect.BlurRadiusProperty, blurAnim);
            }
        }

        private void ShowFocusState()
        {
            AnimateBorderTo(
                ((SolidColorBrush)FindResource("AccentBrush")).Color,
                1.5, 0.12);
        }

        private void ShowNormalState()
        {
            AnimateBorderTo(
                ((SolidColorBrush)FindResource("FieldBorderBrush")).Color,
                1, 0);
        }

        private void ShowErrorState()
        {
            AnimateBorderTo(
                ((SolidColorBrush)FindResource("ErrorBrush")).Color,
                1.5, 0.10);
        }

        // ═══════════════════════════════════════════════════════════════════
        //  SHARED SEGMENT EVENTS
        // ═══════════════════════════════════════════════════════════════════

        private void Segment_GotFocus(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox tb)
                tb.SelectAll();
            ShowFocusState();
        }

        private void Segment_LostFocus(object sender, RoutedEventArgs e)
        {
            // Pad single-digit day/month with leading zero
            if (sender is TextBox tb)
            {
                if (tb == DayBox && _day.Length == 1)
                {
                    _day = _day.PadLeft(2, '0');
                    _suppressCommit = true;
                    DayBox.Text = _day;
                    _suppressCommit = false;
                }
                else if (tb == MonthBox && _month.Length == 1)
                {
                    _month = _month.PadLeft(2, '0');
                    _suppressCommit = true;
                    MonthBox.Text = _month;
                    _suppressCommit = false;
                }
            }

            TryCommitDate();

            // Delay border reset to check if another segment got focus
            Dispatcher.BeginInvoke(new Action(() =>
            {
                if (!IsAnySegmentFocused)
                    ShowNormalState();
            }), System.Windows.Threading.DispatcherPriority.Input);
        }

        private void Segment_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is TextBox tb)
            {
                if (!tb.IsFocused)
                    tb.Focus();
                tb.SelectAll();
                e.Handled = true;
            }
        }

        private void RootBorder_MouseDown(object sender, MouseButtonEventArgs e)
        {
            // Walk up to find if a TextBox was clicked
            var source = e.OriginalSource as DependencyObject;
            while (source != null)
            {
                if (source is TextBox) return;
                source = VisualTreeHelper.GetParent(source);
            }

            // Click on empty space → focus nearest segment
            var pt = e.GetPosition(RootBorder);
            double x = pt.X;
            double w = RootBorder.ActualWidth;
            if (w <= 0) return;

            double ratio = x / w;
            if (ratio < 0.33)
            {
                DayBox.Focus();
                DayBox.SelectAll();
            }
            else if (ratio < 0.66)
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

        // ═══════════════════════════════════════════════════════════════════
        //  UNIFIED KEYBOARD NAVIGATION
        // ═══════════════════════════════════════════════════════════════════

        private TextBox? GetPreviousSegment(TextBox current)
        {
            if (current == MonthBox) return DayBox;
            if (current == YearBox) return MonthBox;
            return null;
        }

        private TextBox? GetNextSegment(TextBox current)
        {
            if (current == DayBox) return MonthBox;
            if (current == MonthBox) return YearBox;
            return null;
        }

        private void FocusSegment(TextBox tb)
        {
            tb.Focus();
            tb.SelectAll();
        }

        private void Segment_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (sender is not TextBox tb) return;

            switch (e.Key)
            {
                // ── Arrow Right → next segment ──
                case Key.Right:
                    if (tb.CaretIndex >= tb.Text.Length || tb.SelectionLength == tb.Text.Length)
                    {
                        var next = GetNextSegment(tb);
                        if (next != null) { FocusSegment(next); e.Handled = true; }
                    }
                    break;

                // ── Arrow Left → previous segment ──
                case Key.Left:
                    if (tb.CaretIndex == 0 || tb.SelectionLength == tb.Text.Length)
                    {
                        var prev = GetPreviousSegment(tb);
                        if (prev != null) { FocusSegment(prev); e.Handled = true; }
                    }
                    break;

                // ── Arrow Up → increment value ──
                case Key.Up:
                    IncrementSegment(tb, +1);
                    e.Handled = true;
                    break;

                // ── Arrow Down → decrement value ──
                case Key.Down:
                    IncrementSegment(tb, -1);
                    e.Handled = true;
                    break;

                // ── Tab → next segment (Shift+Tab → previous) ──
                case Key.Tab:
                    if (Keyboard.Modifiers == ModifierKeys.Shift)
                    {
                        var prev = GetPreviousSegment(tb);
                        if (prev != null) { FocusSegment(prev); e.Handled = true; }
                    }
                    else
                    {
                        var next = GetNextSegment(tb);
                        if (next != null) { FocusSegment(next); e.Handled = true; }
                    }
                    break;

                // ── Backspace on empty → go to previous ──
                case Key.Back:
                    if (string.IsNullOrEmpty(tb.Text))
                    {
                        var prev = GetPreviousSegment(tb);
                        if (prev != null) { FocusSegment(prev); e.Handled = true; }
                    }
                    break;

                // ── Enter → validate and commit ──
                case Key.Return:
                    TryCommitDate();
                    e.Handled = true;
                    break;

                // ── Delete → clear this segment ──
                case Key.Delete:
                    if (tb.SelectionLength == tb.Text.Length)
                    {
                        tb.Text = "";
                        e.Handled = true;
                    }
                    break;

                // ── "/" or "-" → move to next (common date typing pattern) ──
                case Key.OemQuestion:  // "/" key
                case Key.Divide:
                case Key.OemMinus:
                case Key.Subtract:
                    {
                        var next = GetNextSegment(tb);
                        if (next != null) { FocusSegment(next); }
                        e.Handled = true;
                    }
                    break;
            }
        }

        private void IncrementSegment(TextBox tb, int delta)
        {
            _suppressCommit = true;

            if (tb == DayBox)
            {
                int val = int.TryParse(_day, out int d) ? d : 0;
                val = Math.Clamp(val + delta, 1, 31);
                _day = val.ToString("D2");
                DayBox.Text = _day;
            }
            else if (tb == MonthBox)
            {
                int val = int.TryParse(_month, out int m) ? m : 0;
                val = Math.Clamp(val + delta, 1, 12);
                _month = val.ToString("D2");
                MonthBox.Text = _month;
            }
            else if (tb == YearBox)
            {
                int val = int.TryParse(_year, out int y) ? y : DateTime.Today.Year;
                val = Math.Clamp(val + delta, 1900, 2100);
                _year = val.ToString();
                YearBox.Text = _year;
            }

            _suppressCommit = false;
            tb.SelectAll();
            TryCommitDate();
        }

        // ═══════════════════════════════════════════════════════════════════
        //  DAY INPUT
        // ═══════════════════════════════════════════════════════════════════

        private void DayBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            if (!IsDigit(e.Text)) { e.Handled = true; return; }

            var tb = (TextBox)sender;
            string projected = ProjectText(tb, e.Text);
            if (projected.Length > 2) { e.Handled = true; return; }

            if (int.TryParse(projected, out int val) && val > 31)
                e.Handled = true;
        }

        private void DayBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_suppressCommit) return;
            _day = FilterDigits(DayBox.Text);

            // Smart auto-advance:
            // - 2 digits typed → advance
            // - Single digit 4-9 → can only be 04-09, so pad & advance
            if (_day.Length >= 2)
            {
                FocusSegment(MonthBox);
            }
            else if (_day.Length == 1 && _day[0] >= '4' && _day[0] <= '9')
            {
                _day = "0" + _day;
                _suppressCommit = true;
                DayBox.Text = _day;
                _suppressCommit = false;
                FocusSegment(MonthBox);
            }

            TryCommitDate();
        }

        // ═══════════════════════════════════════════════════════════════════
        //  MONTH INPUT
        // ═══════════════════════════════════════════════════════════════════

        private void MonthBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            if (!IsDigit(e.Text)) { e.Handled = true; return; }

            var tb = (TextBox)sender;
            string projected = ProjectText(tb, e.Text);
            if (projected.Length > 2) { e.Handled = true; return; }

            if (int.TryParse(projected, out int val) && val > 12)
                e.Handled = true;
        }

        private void MonthBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_suppressCommit) return;
            _month = FilterDigits(MonthBox.Text);

            // Smart auto-advance:
            // - 2 digits typed → advance
            // - Single digit 2-9 → can only be 02-09, so pad & advance
            if (_month.Length >= 2)
            {
                FocusSegment(YearBox);
            }
            else if (_month.Length == 1 && _month[0] >= '2' && _month[0] <= '9')
            {
                _month = "0" + _month;
                _suppressCommit = true;
                MonthBox.Text = _month;
                _suppressCommit = false;
                FocusSegment(YearBox);
            }

            TryCommitDate();
        }

        // ═══════════════════════════════════════════════════════════════════
        //  YEAR INPUT
        // ═══════════════════════════════════════════════════════════════════

        private void YearBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            if (!IsDigit(e.Text)) { e.Handled = true; return; }

            var tb = (TextBox)sender;
            string projected = ProjectText(tb, e.Text);
            if (projected.Length > 4) e.Handled = true;
        }

        private void YearBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_suppressCommit) return;
            _year = FilterDigits(YearBox.Text);
            TryCommitDate();
        }

        // ═══════════════════════════════════════════════════════════════════
        //  HELPERS
        // ═══════════════════════════════════════════════════════════════════

        /// <summary>
        /// Predicts what the TextBox text will look like after the pending input
        /// (accounting for any selected text that will be replaced).
        /// </summary>
        private static string ProjectText(TextBox tb, string newChar)
        {
            if (tb.SelectionLength > 0)
                return tb.Text[..tb.SelectionStart] + newChar + tb.Text[(tb.SelectionStart + tb.SelectionLength)..];

            return tb.Text[..tb.CaretIndex] + newChar + tb.Text[tb.CaretIndex..];
        }

        private static bool IsDigit(string text) =>
            text.Length == 1 && char.IsDigit(text[0]);

        private static string FilterDigits(string text) =>
            new(text.Where(char.IsDigit).ToArray());
    }
}
