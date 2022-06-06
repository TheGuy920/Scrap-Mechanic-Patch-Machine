using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using Newtonsoft.Json.Linq;
using smp;


namespace smp
{
    /// <summary>
    /// Interaction logic for Loading.xaml
    /// </summary>
    public partial class Loading : Window
    {
		private Timer? CloseSplashT;

		private Timer? UpdateT;

		private JObject? stats;

		public Loading()
		{
			base.Topmost = true;
			InitializeComponent();
		}

		private void Window_Loaded(object sender, RoutedEventArgs e)
		{
			App.GetApp!.MainWindow = new MainWindow();
			using HttpResponseMessage response = new HttpClient
			{
				DefaultRequestHeaders = { { "User-Agent", "smpm" } }
			}.GetAsync("https://api.github.com/repos/TheGuy920/Scrap-Mechanic-Patch-Machine/releases/latest").Result;
			using HttpContent content = response.Content;
			string api_results = content.ReadAsStringAsync().Result;
			stats = JObject.Parse(api_results);
			Version gitV = Version.Parse((string?)stats!["tag_name"]);
			Version asmV = Assembly.GetExecutingAssembly().GetName().Version;
			if (gitV.CompareTo(asmV) > 0)
			{
				Status.Text = "Downloading new update...";
				UpdateT = new Timer(Update, null, new TimeSpan(0, 0, 0, 0, 10), new TimeSpan(0, 0, 0, 0, -1));
				return;
			}
			if (gitV.CompareTo(asmV) < 0)
			{
				MainWindow.GetMainWindow!.ApplicationVersion = asmV.ToString() + " - Developer Edition";
			}
			else
			{
				MainWindow.GetMainWindow!.ApplicationVersion = asmV.ToString() + " - Main";
			}
			CloseSplashT = new Timer(CloseSplash, null, new TimeSpan(0, 0, 0, 1, 500), new TimeSpan(0, 0, 0, 0, -1));
		}

		private void Update(object? info)
		{
			string latestVurl = (string?)stats!["assets"]![0]!["browser_download_url"];
			using HttpResponseMessage response = new HttpClient().GetAsync(latestVurl).Result;
			using HttpContent FileContent = response.Content;
			FileContent.CopyToAsync(new FileStream(Path.Combine(Utilities.GetAssemblyDirectory(), "update.zip"), FileMode.Create));
			Utilities.DeleteAndExtract(Utilities.GetAssemblyDirectory());
			base.Dispatcher.Invoke(base.Close);
			base.Dispatcher.Invoke(Application.Current.Shutdown);
		}

		private void CloseSplash(object? info)
		{
			base.Dispatcher.Invoke(CloseSplashMain);
		}

		private void CloseSplashMain()
		{
			base.Dispatcher.Invoke(App.GetApp!.MainWindow.Show);
			base.Dispatcher.Invoke(base.Close);
		}

		private void Window_LostFocus(object sender, RoutedEventArgs e)
		{
			base.Dispatcher.Invoke((Func<bool>)base.Activate);
			base.Dispatcher.Invoke((Func<bool>)Focus);
		}
	}
}
