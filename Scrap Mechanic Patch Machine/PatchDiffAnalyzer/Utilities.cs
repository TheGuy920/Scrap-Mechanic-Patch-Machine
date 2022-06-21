// smp.Utilities
using System.Diagnostics;
using System.Reflection;

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

	public static string ReadResource(string name)
	{
		string name2 = name;
		string resourceName = Assembly.GetExecutingAssembly().GetManifestResourceNames().Single((string str) => str.EndsWith(name2));
		using Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName)!;
		using StreamReader reader = new StreamReader(stream);
		return reader.ReadToEnd();
	}
}
