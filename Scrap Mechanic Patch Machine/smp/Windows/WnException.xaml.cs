using System;
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
}