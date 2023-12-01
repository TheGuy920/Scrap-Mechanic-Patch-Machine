using Microsoft.Win32;
using System.Security.Cryptography;

static string GetGameLocation()
{
	string? steamPath = Registry.GetValue("HKEY_LOCAL_MACHINE\\SOFTWARE\\WOW6432Node\\Valve\\Steam", "InstallPath", null) as string;
	if (steamPath is null) throw new Exception("Steam not detected");

	string FileContents = File.ReadAllText(Path.Combine(steamPath, "steamapps", "libraryfolders.vdf"));

	string[] array = FileContents.Split('"');

	foreach (string path in array)
	{
		string game_path = Path.Combine(path, "steamapps", "common", "Scrap Mechanic", "Release", "ScrapMechanic.exe").Replace("\n", "");

		if (File.Exists(game_path))
		{
			return game_path.Replace("\\\\", "\\").Replace("//", "/");
		}
	}

	throw new Exception("Scrap Mechanic not detected. Check your steam installation");
}

string path = GetGameLocation();

FileStream stream = new FileStream(path, FileMode.Open);
SHA256 Sha256 = SHA256.Create();
byte[] GameHashByteArray = Sha256.ComputeHash(stream);
stream.Close();
Console.WriteLine(Convert.ToHexString(GameHashByteArray).ToLower());

stream = new FileStream(path, FileMode.Open);
MemoryStream memoryStream = new();
stream.CopyTo(memoryStream);
byte[] sm = memoryStream.ToArray();

stream = new FileStream(path.Replace(".exe", "_old.exe"), FileMode.Open);
memoryStream = new MemoryStream();
stream.CopyTo(memoryStream);
byte[] sm_m = memoryStream.ToArray();

float len = MathF.Max(sm_m.Length, sm.Length);

for (int b = 0; b < len; b++)
{
	if (!sm[b].Equals(sm_m[b]))
	{
		List<byte> bxt = new();
		List<byte> bxs = new();
		List<byte> bst = new();
		int bx = b;
		while (!sm[bx].Equals(sm_m[bx]))
		{
			bxt.Add(sm[bx]);
			bxs.Add(sm_m[bx]);
			bx++;
		}
		for (int bs = 1; bs < 30; bs++)
        {
			bst.Add(sm[b-bs]);
		}
		b = bx;
		bst.Reverse();
		Console.WriteLine(string.Join(",",bst));
		Console.WriteLine(string.Join(",",bxt));
		Console.WriteLine(string.Join(",",bxs));
		Console.ReadLine();
		Console.WriteLine("\n");
	}
}