using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
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
    }
}
