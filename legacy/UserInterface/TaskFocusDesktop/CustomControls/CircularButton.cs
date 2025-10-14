using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace TaskFocusDesktop.CustomControls
{
    class CircularButton : Button
    {
        public static readonly DependencyProperty FocusEllipseHoverColorProperty =
            DependencyProperty.Register("FocusEllipseHoverColor", typeof(SolidColorBrush), typeof(CircularButton), new FrameworkPropertyMetadata(null));

        public SolidColorBrush FocusEllipseHoverColor
        {
            get { return (SolidColorBrush)GetValue(FocusEllipseHoverColorProperty); }
            set { SetValue(FocusEllipseHoverColorProperty, value); }
        }

        public static readonly DependencyProperty FocusEllipsePressedColorProperty =
            DependencyProperty.Register("FocusEllipsePressedColor", typeof(SolidColorBrush), typeof(CircularButton), new FrameworkPropertyMetadata(null));

        public SolidColorBrush FocusEllipsePressedColor
        {
            get { return (SolidColorBrush)GetValue(FocusEllipsePressedColorProperty); }
            set { SetValue(FocusEllipsePressedColorProperty, value); }
        }
    }
}
