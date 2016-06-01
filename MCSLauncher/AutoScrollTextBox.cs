using System.Windows;
using System.Windows.Controls;

namespace MCSLauncher
{
    public class AutoScrollTextBox : TextBox
    {
        public static DependencyProperty AutoScrollProperty = DependencyProperty.RegisterAttached("AutoScroll", typeof(bool), typeof(AutoScrollTextBox));
        private double _lastVerticalOffset = 0;
        private double _scrollableHeight = 0;
        private ScrollViewer _scrollViewer;

        public AutoScrollTextBox()
        {
            Loaded += AutoScrollTextBox_Loaded;
        }

        public bool AutoScroll
        {
            get { return (bool)GetValue(AutoScrollProperty); }
            set { SetValue(AutoScrollProperty, value); }
        }

        private void _scrollViewer_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            if (e.VerticalChange == 0 - _lastVerticalOffset && _lastVerticalOffset > 0 && AutoScroll)
            {
                _scrollViewer.ScrollToEnd();
            }

            if (e.ExtentHeightChange > 0 && Text.Length > 0)
            {
                if (e.VerticalOffset == _scrollableHeight && AutoScroll)
                {
                    _scrollViewer.ScrollToEnd();
                }
            }

            _lastVerticalOffset = e.VerticalOffset;
            _scrollableHeight = _scrollViewer.ScrollableHeight;
        }

        private void AutoScrollTextBox_Loaded(object sender, RoutedEventArgs e)
        {
            _scrollViewer = (GetVisualChild(0) as Border)?.Child as ScrollViewer;
            if (_scrollViewer != null)
            {
                _scrollableHeight = _scrollViewer.ScrollableHeight;
                _scrollViewer.ScrollChanged += _scrollViewer_ScrollChanged;
            }
        }
    }
}