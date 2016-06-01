using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace MCSLauncher
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void Timeout_TextChanged(object sender, TextChangedEventArgs e)
        {
            var tb = sender as TextBox;
            if (tb != null)
            {
                if (tb.Text.Length == 0 && e.Changes.Any(t => t.RemovedLength > 0) ||
                    e.Changes.Any(t => t.AddedLength + t.Offset <= tb.Text.Length && tb.Text.Substring(t.Offset, t.AddedLength).Any(c => !char.IsNumber(c))))
                {
                    Dispatcher.BeginInvoke(new Action(() => tb.Undo()));
                }
            }
        }
    }
}