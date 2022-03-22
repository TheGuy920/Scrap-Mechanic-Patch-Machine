using System;
using System.Windows;
using System.Threading;

namespace smp
{
    /// <summary>
    /// Interaction logic for Loading.xaml
    /// </summary>
    public partial class Loading : Window
    {
        private Timer t;
        public Loading()
        {
            Topmost = true;
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            App.GetApp.MainWindow = new MainWindow(); // Creates but wont show

            t = new Timer(new TimerCallback(CloseSplash), null, new TimeSpan(0, 0, 0, 1, 500), new TimeSpan(0, 0, 0, 0, -1));
        }
        private void CloseSplash(object info)
        {
            // Dispatch to UI Thread
            Dispatcher.Invoke(CloseSplashMain);
        }

        private void CloseSplashMain()
        {
            App.GetApp.MainWindow.Show();
            Close();
        }

        private void Window_LostFocus(object sender, RoutedEventArgs e)
        {
            Activate();
            Focus();
        }
    }
}
