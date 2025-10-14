using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace TaskFocusDesktop.CustomControls
{
    class CheckBoxButton : CheckBox
    {
        public static readonly DependencyProperty FocusEllipseHoverColorProperty =
            DependencyProperty.Register("CheckBoxFocusEllipseHoverColor", typeof(SolidColorBrush), typeof(CheckBoxButton), new FrameworkPropertyMetadata(null));

        public SolidColorBrush CheckBoxFocusEllipseHoverColor
        {
            get { return (SolidColorBrush)GetValue(FocusEllipseHoverColorProperty); }
            set { SetValue(FocusEllipseHoverColorProperty, value); }
        }

        public static readonly DependencyProperty FocusEllipsePressedColorProperty =
            DependencyProperty.Register("CheckBoxFocusEllipsePressedColor", typeof(SolidColorBrush), typeof(CheckBoxButton), new FrameworkPropertyMetadata(null));

        public SolidColorBrush CheckBoxFocusEllipsePressedColor
        {
            get { return (SolidColorBrush)GetValue(FocusEllipsePressedColorProperty); }
            set { SetValue(FocusEllipsePressedColorProperty, value); }
        }
    }
}
