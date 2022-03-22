using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Windows;

namespace smp
{

    public partial class WnException
    {

        public WnException(Exception error)
        {
            InitializeComponent();
            MessageText.Text = error.Message;
            StackTraceText.Text = error.StackTrace ?? "This exception doesn't contain stack trace data.";
        }

        private void Restart(object sender, RoutedEventArgs args)
        {
            Utilities.RestartApp();
        }

        private void Exit(object sender, RoutedEventArgs args)
        {
            Application.Current.Shutdown();
        }

    }
    internal static class Utilities
    {
        public static void RestartApp(string args = null)
        {
            var location = Assembly.GetExecutingAssembly().Location;
            
            if (location.EndsWith(".dll", StringComparison.CurrentCultureIgnoreCase))
                location = Path.Combine(Path.GetDirectoryName(location)!, Path.GetFileNameWithoutExtension(location) + ".exe");

            if (location != null)
                Process.Start(location, args ?? string.Empty);
            else
                throw new Exception("Unable to restart. Please shutdown instead");            
            
            Application.Current.Shutdown();
        }
    }

}