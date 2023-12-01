using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
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
        private JObject? stats;
        public Loading()
		{
			this.Topmost = true;
            this.InitializeComponent();
		}

		private void Window_Loaded(object sender, RoutedEventArgs e)
		{
            MainWindow main = new();
            App.GetApp!.MainWindow = main;
            Version gitV;
            Version asmV = Assembly.GetExecutingAssembly().GetName().Version!;
            try
            {
                using HttpResponseMessage response = new HttpClient
                {
                    DefaultRequestHeaders = { { "User-Agent", "smpm" } }
                }.GetAsync("https://api.github.com/repos/TheGuy920/Scrap-Mechanic-Patch-Machine/releases/latest").Result;
                using HttpContent content = response.Content;
                string api_results = content.ReadAsStringAsync().Result;
                stats = JObject.Parse(api_results);
                gitV = Version.Parse((string)stats!["tag_name"]!);
            }
            catch
            {
                gitV = asmV;
            }

            if (gitV.CompareTo(asmV) < 0)
            {
                main.ApplicationVersion = asmV!.ToString() + " - Developer Edition";
                Task.Run(() =>
                {
                    main.Init().ContinueWith(CloseSplashMain);
                });
            }
            else if (gitV.CompareTo(asmV) > 0)
            {
                this.Status.Text = "Downloading new update...";
                main.ApplicationVersion = asmV!.ToString() + " - Outdated";
                this.Dispatcher.Invoke(Update);
            }
            else if (gitV.CompareTo(asmV) == 0)
            {
                main.ApplicationVersion = asmV!.ToString() + " - Main";
                Task.Run(() =>
                {
                    main.Init().ContinueWith(CloseSplashMain);
                });
            }
        }
        private void Update()
        {
            Task.Run(() => 
            { 
                string latestVurl = (string)stats!["assets"]![0]!["browser_download_url"]!;
                using HttpResponseMessage response = new HttpClient().GetAsync(latestVurl).Result;
                using HttpContent FileContent = response.Content;
                FileContent.CopyToAsync(new FileStream(Path.Combine(Utilities.GetAssemblyDirectory(), "update.zip"), FileMode.Create));
                Utilities.DeleteAndExtract(Utilities.GetAssemblyDirectory());
                this.Dispatcher.Invoke(Application.Current.Shutdown);
            });
        }

        private void CloseSplashMain(Task t)
		{
			this.Dispatcher.Invoke(()=> { App.GetApp!.MainWindow.Show(); });
            this.Dispatcher.Invoke(Close);
		}

		private void Window_LostFocus(object sender, RoutedEventArgs e)
		{
            this.Dispatcher.Invoke(Activate);
            this.Dispatcher.Invoke(Focus);
		}
	}
}
