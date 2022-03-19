/// <summary>
/// Scrap Mechanic offline patcher © TheGuy920 2022
/// could not have done it without the help of trbodev#0001
/// </summary>
using Microsoft.Win32;
using System.Net;
using System.Security.Cryptography;

// Initialization
LogLine(@$"
===========================================================


    Scrap Mechanic offline patcher (©) TheGuy920 2022

===========================================================

    Patch Version: 1.0
    Contributor: trbodev

===========================================================
", ConsoleColor.Gray);

// Patch byte
byte PatchByte = 235;

// Get steam install path
string? steampath = (string?)Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Valve\Steam", "InstallPath", null);

// Steam not installed (at least not properly)
if (steampath == null)
    throw new Exception("Steam not detected");

// Get libraryfolders.vdf file path
string paths = Path.Combine(steampath, "steamapps", "libraryfolders.vdf");

// Load libraryfolders.vdf content
string lbvdf = File.ReadAllText(paths);

// Split content based off of quotes
string[] lbvdf_content = lbvdf.Split('\"');

// Initialize sm path variable
string sm_path = "";

// Search string array/list for the FIRST valid path that contains ScrapMechanic.exe
foreach (string path in lbvdf_content)
{
    string game_path = Path.Combine(path.Replace("\\\\", "\\"), "steamapps", "common", "Scrap Mechanic", "Release", "ScrapMechanic.exe");
    if (path.Contains(":\\\\") && File.Exists(game_path))
    {
        sm_path = game_path;
        LogLine($"Game Path detected: {sm_path}");
    }
}


FileStream stream = new FileStream(sm_path, FileMode.Open);

// The cryptographic service provider.
SHA256 Sha256 = SHA256.Create();

// Computes game hash
byte[] GameHashByteArray = Sha256.ComputeHash(stream);

// Close stream
stream.Close();

// Version
string sm_ver = "Uknown";

// Byte array to string
string GameHash = string.Join(null, Array.ConvertAll(GameHashByteArray, s => s.ToString("x")));

// These are the bytes right before [75 2b] (JNZ) that will be used as a search reference
byte[] search = { 169, 0, 255, 21, 240, 233, 136, 0, 72, 139, 8, 72, 139, 1, 255, 80, 8, 132, 192 };

// Download search array using hash/version from github
try
{
    search = Array.ConvertAll(new WebClient().DownloadString($"https://raw.githubusercontent.com/TheGuy920/Scrap-Mechanic-Offline-Patch/main/{GameHash}").Split(","), s => byte.Parse(s));
    sm_ver = new WebClient().DownloadString($"https://raw.githubusercontent.com/TheGuy920/Scrap-Mechanic-Offline-Patch/main/{GameHash}-ver");
    LogLine($"Scrap Mechanic Version: {sm_ver}", ConsoleColor.White);
}
catch (WebException e)
{
    if (new StreamReader(e.Response.GetResponseStream()).ReadToEnd().Equals("404: Not Found"))
    {
        WarnLine("Version not supported: Press Enter to continue anyway");
        _ = Console.ReadLine();
        ResetConsoleLine();
    }
}
catch(FormatException e)
{
    if (new WebClient().DownloadString($"https://raw.githubusercontent.com/TheGuy920/Scrap-Mechanic-Offline-Patch/main/Versions/{GameHash}").Equals("true"))
    {
        sm_ver = new WebClient().DownloadString($"https://raw.githubusercontent.com/TheGuy920/Scrap-Mechanic-Offline-Patch/main/Versions/{GameHash}-ver");
        LogLine($"Scrap Mechanic Version: {sm_ver}", ConsoleColor.White);
        LogLine("Scrap Mechanic already patched");
        LogLine("Press Enter to begin unpatch (Close to cancel)", ConsoleColor.Red);
        _ = Console.ReadLine();
        ResetConsoleLine();
        PatchByte = 117;
    }
}

// Initializes sm as byte array
byte[] sm = new byte[] { };

// Opens ScrapMechanic.exe as filestream
stream = new FileStream(sm_path, FileMode.Open);

// Copy filestream to sm byte array
MemoryStream memoryStream = new MemoryStream();
stream.CopyTo(memoryStream);
sm = memoryStream.ToArray();

// Locates the search reference (above) in the sm byte array as an index
long position = sm.Locate(search);

// Uses the index to querry the byte
byte b = sm[position + search.Length];

// Continues if byte not patched...
LogLine($"Patching byte {b} in position {position + search.Length} to {PatchByte}");
LogLine("Press Enter to patch Scrap Mechanic (Close to cancel)", ConsoleColor.Red);
_ = Console.ReadLine();
ResetConsoleLine();

// Sets the stream position to the position of the byte in need of patching
stream.Position = position + search.Length;

// Patches the byte
stream.WriteByte(PatchByte);

// Closes the stream
stream.Close();

// SM Patched!
LogLine("Scrap Mechanic patched", ConsoleColor.Cyan);

// Wait for exit
LogLine("Press Enter to exit", ConsoleColor.DarkGray);
_ = Console.ReadLine();

void LogLine(string s, ConsoleColor TextColor = ConsoleColor.DarkGreen)
{
    Console.ForegroundColor = TextColor;
    Console.BackgroundColor = ConsoleColor.Black;
    Console.WriteLine(s);
}

void WarnLine(string s)
{
    Console.ForegroundColor = ConsoleColor.DarkYellow;
    Console.BackgroundColor = ConsoleColor.Black;
    Console.WriteLine(s);
}

void ResetConsoleLine()
{
    Console.SetCursorPosition(0, Console.CursorTop - 1);
    int currentLineCursor = Console.CursorTop;
    Console.SetCursorPosition(0, Console.CursorTop);
    Console.Write(new string(' ', Console.WindowWidth));
    Console.SetCursorPosition(0, currentLineCursor);
}

static class Base
{
    /// <summary>
    /// Stolen code used to find a byte array in a byte array
    /// </summary>
    /// <param name="sub_array"></param>
    /// <returns></returns>
    public static long Locate(this byte[] self, byte[] sub_array)
    {
        if (IsEmptyLocate(self, sub_array))
            return 0;

        var list = new List<int>();

        for (int i = 0; i < self.Length; i++)
        {
            if (!IsMatch(self, i, sub_array))
                continue;

            list.Add(i);
        }

        return list.Count == 0 ? 0 : list.ToArray()[0];
    }

    static bool IsMatch(byte[] array, int position, byte[] sub_array)
    {
        if (sub_array.Length > (array.Length - position))
            return false;

        for (int i = 0; i < sub_array.Length; i++)
            if (array[position + i] != sub_array[i])
                return false;

        return true;
    }
    static bool IsEmptyLocate(byte[] array, byte[] sub_array)
    {
        return array == null
            || sub_array == null
            || array.Length == 0
            || sub_array.Length == 0
            || sub_array.Length > array.Length;
    }
}