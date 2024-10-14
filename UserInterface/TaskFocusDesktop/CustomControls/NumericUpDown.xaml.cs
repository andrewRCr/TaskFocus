using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace TaskFocusDesktop.CustomControls
{
    /// <summary>
    /// Interaction logic for NumericUpDown.xaml
    /// </summary>
    public partial class NumericUpDown : UserControl
    {
        public static readonly DependencyProperty ValueProperty = 
            DependencyProperty.Register("Value", typeof(int), typeof(NumericUpDown), new PropertyMetadata(default(int), 
                new PropertyChangedCallback(ValueProperty_PropertyChanged)));

        public int Value
        {
            get { return (int)GetValue(ValueProperty); }
            set {  SetValue(ValueProperty, value); }
        }

        private static void ValueProperty_PropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var thisControl = (NumericUpDown)d;
            if (thisControl != null)
            {
                thisControl.txtNum.Text = e.NewValue.ToString();
                thisControl.CanDecrease = thisControl.Value > thisControl.Minimum;
                thisControl.CanIncrease = thisControl.Value < thisControl.Maximum;
                thisControl.DecreaseButtonColor = thisControl.CanDecrease ? thisControl.EnabledButtonColor : thisControl.DisabledButtonColor;
                thisControl.IncreaseButtonColor = thisControl.CanIncrease ? thisControl.EnabledButtonColor : thisControl.DisabledButtonColor;
            }
        }

        public static readonly DependencyProperty MinimumProperty =
            DependencyProperty.Register("Minimum", typeof(int), typeof(NumericUpDown), new PropertyMetadata(0, new PropertyChangedCallback(MinimumProperty_PropertyChanged)));

        private static void MinimumProperty_PropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var thisControl = (NumericUpDown)d;
            if (thisControl != null)
            {
                thisControl.CanDecrease = thisControl.Value > thisControl.Minimum;
                thisControl.DecreaseButtonColor = thisControl.CanDecrease ? thisControl.EnabledButtonColor : thisControl.DisabledButtonColor;
            }
        }

        public int Minimum
        {
            get { return (int)GetValue(MinimumProperty); }
            set { SetValue(MinimumProperty, value); }
        }

        public static readonly DependencyProperty MaximumProperty =
            DependencyProperty.Register("Maximum", typeof(int), typeof(NumericUpDown), new PropertyMetadata(0, new PropertyChangedCallback(MaximumProperty_PropertyChanged)));

        private static void MaximumProperty_PropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var thisControl = (NumericUpDown)d;
            if (thisControl != null)
            {
                thisControl.CanIncrease = thisControl.Value < thisControl.Maximum;
                thisControl.IncreaseButtonColor = thisControl.CanIncrease ? thisControl.EnabledButtonColor : thisControl.DisabledButtonColor;
            }
        }

        public int Maximum
        {
            get { return (int)GetValue(MaximumProperty); }
            set { SetValue(MaximumProperty, value); }
        }

        public static readonly DependencyProperty CanDecreaseProperty =
            DependencyProperty.Register("CanDecrease", typeof(bool), typeof(NumericUpDown), new PropertyMetadata(false));

        public bool CanDecrease
        {
            get { return (bool)GetValue(CanDecreaseProperty); }
            set 
            { 
                SetValue(CanDecreaseProperty, value);
                DecreaseButtonColor = value ? EnabledButtonColor : DisabledButtonColor;
            }
        }

        public static readonly DependencyProperty CanIncreaseProperty =
            DependencyProperty.Register("CanIncrease", typeof(bool), typeof(NumericUpDown), new PropertyMetadata(false));

        public bool CanIncrease
        {
            get { return (bool)GetValue(CanIncreaseProperty); }
            set
            {
                SetValue(CanIncreaseProperty, value);
                IncreaseButtonColor = value ? EnabledButtonColor : DisabledButtonColor;
            }
        }

        public static readonly DependencyProperty IncreaseButtonColorProperty =
            DependencyProperty.Register("IncreaseButtonColor", typeof(Brush), typeof(NumericUpDown));

        public Brush IncreaseButtonColor
        {
            get { return (Brush)GetValue(IncreaseButtonColorProperty); }
            set { SetValue(IncreaseButtonColorProperty, value); }
        }

        public static readonly DependencyProperty DecreaseButtonColorProperty =
            DependencyProperty.Register("DecreaseButtonColor", typeof(Brush), typeof(NumericUpDown));

        public Brush DecreaseButtonColor
        {
            get { return (Brush)GetValue(DecreaseButtonColorProperty); }
            set { SetValue(DecreaseButtonColorProperty, value); }
        }

        public static readonly DependencyProperty EnabledButtonColorProperty =
            DependencyProperty.Register("EnabledButtonColor", typeof(Brush), typeof(NumericUpDown), 
                new PropertyMetadata(new SolidColorBrush(Color.FromRgb(119, 107, 231)), 
                    new PropertyChangedCallback(EnabledButtonColorProperty_PropertyChanged)));

        private static void EnabledButtonColorProperty_PropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var thisControl = (NumericUpDown)d;
            if (thisControl != null)
            {
                thisControl.EnabledButtonColor = (Brush)e.NewValue;
            }
        }

        public Brush EnabledButtonColor
        {
            get { return (Brush)GetValue(EnabledButtonColorProperty); }
            set { SetValue(EnabledButtonColorProperty, value); }
        }

        public static readonly DependencyProperty DisabledButtonColorProperty =
            DependencyProperty.Register("DisabledButtonColor", typeof(Brush), typeof(NumericUpDown),
                new PropertyMetadata(new SolidColorBrush(Color.FromRgb(100, 100, 100)), 
                    new PropertyChangedCallback(DisabledButtonColorProperty_PropertyChanged)));

        private static void DisabledButtonColorProperty_PropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var thisControl = (NumericUpDown)d;
            if (thisControl != null)
            {
                thisControl.DisabledButtonColor = (Brush)e.NewValue;
            }
        }

        public Brush DisabledButtonColor
        {
            get { return (Brush)GetValue(DisabledButtonColorProperty); }
            set { SetValue(DisabledButtonColorProperty, value); }
        }

        public NumericUpDown()
        {
            InitializeComponent();
            txtNum.Text = Value.ToString();
        }

        private void CmdUp_Click(object sender, RoutedEventArgs e)
        {
            if (Value < Maximum) {
                Value++;
            }
        }

        private void CmdDown_Click(object sender, RoutedEventArgs e)
        {
            if (Value > Minimum) {
                Value--;
            }
        }

        private void TxtNum_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (txtNum == null)
            {
                return;
            }

            if (!int.TryParse(txtNum.Text, out var val))
            {
                Value = val;
                txtNum.Text = val.ToString();
            }
                
        }

        private void UserControl_IsEnabledChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (this.IsEnabled) // selective re-enable when parent control is enabled
            {
                if (this.Value < this.Maximum) { CanIncrease = true; }
                if (this.Value > this.Minimum) { CanDecrease = true; }
            }
            else // always disable with parent control
            {
                CanIncrease = false;
                CanDecrease = false;
            }
        }
    }
}
