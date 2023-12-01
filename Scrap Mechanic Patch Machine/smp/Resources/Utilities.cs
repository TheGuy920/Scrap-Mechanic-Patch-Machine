// smp.Utilities
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows;


namespace smp
{
	internal class Utilities
	{
		public static string GetAssemblyDirectory()
		{
			string location = Assembly.GetExecutingAssembly().Location;
			string backupLocation = AppDomain.CurrentDomain.BaseDirectory;
			location = Path.GetDirectoryName(location)!;
			if (!Directory.Exists(location))
			{
				return backupLocation;
			}
			return location;
		}

		public static string GetAssemblyLocation()
		{
			string text = Path.Combine(GetAssemblyDirectory(), Process.GetCurrentProcess().ProcessName + ".exe");
			if (!File.Exists(text))
			{
				throw new Exception("Unable to locate current executing assembly");
			}
			return text;
		}

		public static void RestartApp(string? args = null)
		{
			string location = GetAssemblyLocation();
			if (File.Exists(location))
			{
				Process.Start(location, args ?? string.Empty);
				Application.Current.Shutdown();
				return;
			}
			throw new Exception("Unable to restart. Please shutdown instead");
		}

		public static string ReadResource(string name)
		{
			string name2 = name;
			string resourceName = Assembly.GetExecutingAssembly().GetManifestResourceNames().Single((string str) => str.EndsWith(name2));
			using Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName)!;
			using StreamReader reader = new StreamReader(stream);
			return reader.ReadToEnd();
		}

		public static void DeleteAndExtract(string currentDir)
		{
			string psCommandBase64 = Convert.ToBase64String(Encoding.Unicode.GetBytes(ReadResource("update.ps1")));
			Process process = new Process();
			process.StartInfo.FileName = "C:\\Windows\\System32\\WindowsPowerShell\\v1.0\\powershell.exe";
			process.StartInfo.Arguments = "-NoProfile -ExecutionPolicy unrestricted -EncodedCommand " + psCommandBase64;
			process.StartInfo.UseShellExecute = false;
			process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
			process.StartInfo.CreateNoWindow = true;
			process.StartInfo.WorkingDirectory = currentDir;
			process.Start();
		}
	}
	internal class Debug
	{
		private static bool init = false;
		public static void Log(object info)
        {
#if DEBUG 
			System.Diagnostics.Debug.WriteLine(info);
#endif

			string log = Path.Combine(Utilities.GetAssemblyDirectory(), "log.txt");
			string time = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();

			if (!init) {
				File.AppendAllText(log, Environment.NewLine
					+ "===================================================="
					+ "===================================================="
					+ Environment.NewLine
					+ $"SCRAP MECHANIC PATCHMACHINE BOOT AND RECORD LOG {time}"
					+ Environment.NewLine
					+ "===================================================="
					+ "====================================================");
				init = !init;
			}

			File.AppendAllText(log, 
				Environment.NewLine +
				$"{time}: " +
				info.ToString());
		}
	}
}
