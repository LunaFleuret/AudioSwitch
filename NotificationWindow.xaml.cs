using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Animation;

namespace AudioSwitch
{
    public partial class NotificationWindow : Window
    {
        public NotificationWindow(string message, bool isSuccess = true)
        {
            InitializeComponent();
            MessageText.Text = message;
            if (!isSuccess)
            {
                IconText.Text = "[!]";
                IconText.Foreground = System.Windows.Media.Brushes.Coral;
            }
            else
            {
                IconText.Text = "[Audio]";
                IconText.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0, 162, 255));
            }

            Loaded += OnLoaded;
        }

        private async void OnLoaded(object sender, RoutedEventArgs e)
        {
            // Position at bottom-right above taskbar
            var workArea = SystemParameters.WorkArea;
            Left = workArea.Right - ActualWidth - 20;
            Top = workArea.Bottom - ActualHeight - 20;

            // Fade in
            var fadeIn = new DoubleAnimation(0.0, 1.0, TimeSpan.FromMilliseconds(200));
            BeginAnimation(OpacityProperty, fadeIn);

            // Wait 1.3 seconds
            await Task.Delay(1300);

            // Fade out
            var fadeOut = new DoubleAnimation(1.0, 0.0, TimeSpan.FromMilliseconds(250));
            fadeOut.Completed += (s, ev) =>
            {
                Close();
            };
            BeginAnimation(OpacityProperty, fadeOut);
        }
    }
}
