using MaterialDesignThemes.Wpf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

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
