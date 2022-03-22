using Microsoft.Win32;
using smp.Network;
using System;
using System.Linq;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Windows;
using System.Collections.Generic;
using System.Windows.Controls;
using System.Windows.Media;

namespace smp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public static Dictionary<string, Patch[]> PatchList;
        public static GameInfo GameVersionInfo;
        public static List<string> PatchesToApply;
        public static string GameDirectory;
        public static MainWindow GetMainWindow;
        public MainWindow()
        {
            GetMainWindow = this;

            GameDirectory = GetGameLocation();
            GameVersionInfo = FindGameVersion(GameDirectory);
            PatchList = LoadPatchList(GameVersionInfo);

            if (GameVersionInfo.Version == null)
            {
                ScrapeVersion(GameDirectory);
            }

            PatchesToApply = new();

            Activated += GetMainWindow.MainWindow_Activated;
            InitializeComponent();

            Activate();
            Focus();
        }

        private void MainWindow_Activated(object? sender, EventArgs e)
        {
            LoadPatchItems();
        }

        public void LoadPatchItems()
        {
            GAMEDIR.Text = GameDirectory;
            GAMEVER.Text = GameVersionInfo.Version == null ? "Unknown" : GameVersionInfo.Version;
            if (GameVersionInfo.PatchesInstalled > 0)
                GAMEVER.Text += " - PATCHED";
            PATCHCOUNT.Text = GameVersionInfo.PatchesInstalled.ToString();

            PatchListPanel.Children.Clear();

            foreach (var patch in PatchList)
            {
                Border PatchItem = (Border)((DataTemplate)FindResource("PatchTemplate")).LoadContent();
                ((TextBox)((StackPanel)((Grid)PatchItem.Child).Children[0]).Children[0]).Text = patch.Key.Replace("-", " ");

                int Supported = 0;
                int patched = 0;
                string description = "";

                foreach (var item in patch.Value)
                {
                    if (item.PatchInfo.sHash.Equals(GameVersionInfo.sHash))
                    {
                        patched = Convert.ToInt32(item.PatchInfo.Patched);
                        Supported = Convert.ToInt32(true);
                        description = item.PatchInfo.Description;
                        break;
                    }
                }
                ((Grid)((Grid)PatchItem.Child).Children[2]).Tag = Supported.ToString();

                if (Supported.Equals(1))
                    ((Grid)((Grid)PatchItem.Child).Children[2]).Background = Brushes.Transparent;

                ((TextBox)((Grid)PatchItem.Child).Children[1]).Text = description;

                ((CheckBox)((StackPanel)((Grid)PatchItem.Child).Children[0]).Children[1]).Tag = patched.ToString();
                ((CheckBox)((StackPanel)((Grid)PatchItem.Child).Children[0]).Children[1]).IsChecked = patched == 1;

                PatchListPanel.Children.Add(PatchItem);
            }
            ListUpdated();
        }
        public static string GetGameLocation()
        {
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

            // Search string array/list for the FIRST valid path that contains ScrapMechanic.exe
            foreach (string path in lbvdf_content)
            {
                string game_path = Path.Combine(path.Replace("\\\\", "\\"), "steamapps", "common", "Scrap Mechanic", "Release", "ScrapMechanic.exe");
                if (path.Contains(":\\\\") && File.Exists(game_path))
                    return game_path;
            }

            throw new Exception("Scrap Mechanic not detected. Check your steam installation");
        }
        public static GameInfo FindGameVersion(string sm_path)
        {
            try
            {
                // Open game binary as fs
                FileStream stream = new FileStream(sm_path, FileMode.Open);

                // The cryptographic service provider.
                SHA256 Sha256 = SHA256.Create();

                // Computes game hash
                byte[] GameHashByteArray = new byte[] { };
                GameHashByteArray = Sha256.ComputeHash(stream);

                // Close stream
                stream.Close();

                // Byte array to string
                string GameHash = string.Join(null, Array.ConvertAll(GameHashByteArray, s => s.ToString("x")));

                return GameVersion.GetVersion(GameHash);
            }
            catch (IOException e)
            {
                throw new IOException("Please close Scrap Mechanic and try again", e.InnerException);
            }
        }
        public static void ScrapeVersion(string location)
        {
            Dictionary<byte[], List<Patch>> VersionPatchList = new Dictionary<byte[], List<Patch>>();
            
            foreach (var item in PatchList)
                foreach (var patch in item.Value)
                    if(VersionPatchList.ContainsKey(patch.PatchInfo.bHash))
                        VersionPatchList[patch.PatchInfo.bHash].Add(patch);
                    else
                        VersionPatchList.Add(patch.PatchInfo.bHash, new List<Patch> { new Patch() { PatchName = item.Key, PatchInfo = patch.PatchInfo } });

            foreach (var version in new Dictionary<byte[], List<Patch>>(VersionPatchList))
            {
                FileStream stream = new FileStream(location, FileMode.Open);
                // Copy filestream to sm byte array
                MemoryStream memoryStream = new MemoryStream();
                stream.CopyTo(memoryStream);
                byte[] sm = memoryStream.ToArray();
                for (int i = 0; i < VersionPatchList[version.Key].Count; i++)
                {
                    var info = VersionPatchList[version.Key][i].PatchInfo;
                    int position = sm.Locate(info.Search);
                    stream.Position = position + info.Search.Length;

                    if (info.ByteList)
                    {
                        if (sm[(position + info.Search.Length)..(position + info.Search.Length + info.Patchbytes.Length)].equals(info.Patchbytes))
                        {
                            VersionPatchList[version.Key][i] = new Patch() { PatchName= VersionPatchList[version.Key][i].PatchName, PatchInfo = new PatchInfo(VersionPatchList[version.Key][i].PatchInfo) { Patched = true } };
                            foreach(var Byte in info.Targetbytes)
                                stream.WriteByte(Byte);
                        }
                    }
                    else
                    {
                        if (sm[stream.Position].Equals(info.Patchbyte))
                        {
                            VersionPatchList[version.Key][i] = new Patch() { PatchName = VersionPatchList[version.Key][i].PatchName, PatchInfo = new PatchInfo(VersionPatchList[version.Key][i].PatchInfo) { Patched = true } };
                            stream.WriteByte(info.Targetbyte);
                        }
                    }
                }

                stream.Close();
                stream = new FileStream(location, FileMode.Open);
                // The cryptographic service provider.
                SHA256 Sha256 = SHA256.Create();
                // Computes game hash
                byte[] GameHashByteArray = new byte[] { };
                GameHashByteArray = Sha256.ComputeHash(stream);
                stream.Close();
                // Byte array to string
                string GameHash = string.Join(null, Array.ConvertAll(GameHashByteArray, s => s.ToString("x")));

                PatchInfo patchInfo = new PatchInfo(GameVersion.GetVersion(GameHash));

                stream = new FileStream(location, FileMode.Open);
                // Copy filestream to sm byte array
                memoryStream = new MemoryStream();
                stream.CopyTo(memoryStream);
                sm = memoryStream.ToArray();

                int counter = 0;

                for (int i = 0; i < VersionPatchList[version.Key].Count; i++)
                {
                    var info = VersionPatchList[version.Key][i].PatchInfo;
                    int position = sm.Locate(info.Search);
                    stream.Position = position + info.Search.Length;

                    if (info.ByteList)
                    {
                        if (sm[(position + info.Search.Length)..(position + info.Search.Length + info.Targetbytes.Length)].equals(info.Targetbytes) && info.Patched)
                        {
                            VersionPatchList[version.Key][i] = new Patch() { PatchName = VersionPatchList[version.Key][i].PatchName, PatchInfo = new PatchInfo(VersionPatchList[version.Key][i].PatchInfo) { Patched = true } };
                            foreach (var Byte in info.Patchbytes)
                                stream.WriteByte(Byte);
                            counter++;
                        }
                    }
                    else
                    {
                        if (sm[stream.Position].Equals(info.Targetbyte) && info.Patched)
                        {
                            
                            VersionPatchList[version.Key][i] = new Patch() { PatchName = VersionPatchList[version.Key][i].PatchName, PatchInfo = new PatchInfo(VersionPatchList[version.Key][i].PatchInfo) { Patched = true } };
                            stream.WriteByte(info.Patchbyte);
                            counter++;
                        }
                    }
                }

                stream.Close();

                if (patchInfo.Version != null)
                {
                    PatchList.Clear();
                    foreach (var patchList in VersionPatchList)
                    {
                        foreach (var patch in patchList.Value)
                        {
                            var nPatch = new Patch(patch) { PatchInfo = new PatchInfo(patch.PatchInfo) { Version = patchInfo.Version } };
                            if(PatchList.ContainsKey(nPatch.PatchName))
                                PatchList[nPatch.PatchName].Concat(new Patch[] { nPatch });
                            else
                                PatchList.Add(nPatch.PatchName, new Patch[] { nPatch });
                        }
                    }
                    GameVersionInfo = new GameInfo(patchInfo, counter);
                }
            }
        }
        public static Dictionary<string, Patch[]> LoadPatchList(GameInfo gameInfo)
        {
            var tmp0 = new WebClient().DownloadString($"https://raw.githubusercontent.com/TheGuy920/Scrap-Mechanic-Patch-Machine/main/Patches/PatchList").Replace("\r", "").Replace("\n", ":");
           
            while (tmp0.Contains("::"))
                tmp0 = tmp0.Replace("::", ":");
            string[] list = tmp0.Split(":");

            Dictionary<string, Patch[]> map = new Dictionary<string, Patch[]>();
            
            for (var i = 0; i < list.Length; i++)
            {
                var tmp1 = new WebClient().DownloadString($"https://raw.githubusercontent.com/TheGuy920/Scrap-Mechanic-Patch-Machine/main/Patches/{list[i]}/{list[i]}").Replace("\r", "").Replace("\n", ":").Replace(Environment.NewLine, "");
                
                while (tmp1.Contains("::"))
                    tmp1 = tmp1.Replace("::", ":");
                
                string[] versions = tmp1.Replace(" ", "").Split(":");

                if(versions[^1].Length < 2)
                    versions = versions.SkipLast(1).ToArray();

                Patch[] LoadVInfo = new Patch[versions.Length];
                
                for (var j = 0; j < versions.Length; j++) 
                    LoadVInfo[j] = LoadPatch(new Patch { PatchName = list[i], PatchInfo = new PatchInfo(gameInfo) { sHash = versions[j] } });
                
                map[list[i]] = LoadVInfo;
            }

            return map;
        }

        public static Patch LoadPatch(Patch patch)
        {
            PatchInfo v = patch.PatchInfo;

            string[] contentList = new WebClient().DownloadString($"https://raw.githubusercontent.com/TheGuy920/Scrap-Mechanic-Patch-Machine/main/Patches/{patch.PatchName}/{patch.PatchInfo.sHash}").Split('\n');
            string description = new WebClient().DownloadString($"https://raw.githubusercontent.com/TheGuy920/Scrap-Mechanic-Patch-Machine/main/Patches/{patch.PatchName}/desc");

            if (contentList[0].Length == 64 && !contentList[0].Contains(","))
            {
                v.sHash = contentList[0];
                patch.PatchInfo = v;
                return LoadPatch(patch);
            }

            v.bHash = Array.ConvertAll(v.sHash.SplitInParts(2), s => { return (byte)Convert.ToInt32(s, 16); });
            v.Search = Array.ConvertAll(contentList[0].Split(","), s => byte.Parse(s));
            v.Description = description;

            if (contentList[1].Split(",").Length > 1 && contentList[2].Split(",").Length > 1)
            {
                v.ByteList = true;
                v.Targetbytes = Array.ConvertAll(contentList[1].Split(","), s => byte.Parse(s));
                v.Patchbytes = Array.ConvertAll(contentList[2].Split(","), s => byte.Parse(s));
            }
            else if (contentList[1].Split(",").Length == 1 && contentList[2].Split(",").Length == 1)
            {
                v.ByteList = false;
                v.Targetbyte = byte.Parse(contentList[1]);
                v.Patchbyte = byte.Parse(contentList[2]);
            }

            if (v.Targetbytes?.Length != v.Patchbytes?.Length)
                throw new Exception("Target and Patch length do not match");

            return new Patch { PatchName = patch.PatchName, PatchInfo = v };
        }

        public void ListUpdated()
        {
            if(PatchesToApply.Count() > 0)
            {
                APPLY.IsEnabled = true;
                CANCEL.IsEnabled = true;
            }
            else
            {
                APPLY.IsEnabled = false;
                CANCEL.IsEnabled = false;
            }
            if(GameVersionInfo.PatchesInstalled == 0)
            {
                REMOVEALL.IsEnabled = false;
            }
            else
            {
                REMOVEALL.IsEnabled = true;
            }
        }

        private void Button_PreviewMouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (((Grid)sender).Tag.Equals("1"))
            {
                ((Grid)sender).Opacity = 0.2;
                CheckBox cb = (CheckBox)((StackPanel)((Grid)((Grid)sender).Parent).Children[0]).Children[1];
                cb.IsChecked = !cb.IsChecked;
                CheckBoxChanged(cb, (TextBox)((StackPanel)((Grid)((Grid)sender).Parent).Children[0]).Children[0]);
            }
        }

        private void Grid_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (((Grid)sender).Tag.Equals("1"))
                ((Grid)sender).Background = Brushes.LightBlue;
        }

        private void Grid_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (((Grid)sender).Tag.Equals("1"))
                ((Grid)sender).Background = Brushes.Transparent;
        }

        private void Grid_PreviewMouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (((Grid)sender).Tag.Equals("1"))
                ((Grid)sender).Opacity = 0.4;
        }

        private void CheckBoxChanged(CheckBox sender, TextBox tb)
        {
            if(int.Parse(sender.Tag.ToString()) != Convert.ToInt32(sender.IsChecked))
                PatchesToApply.Add(tb.Text.Replace(" ", "-"));
            else
                if(PatchesToApply.Contains(tb.Text.Replace(" ", "-")))
                    PatchesToApply.Remove(tb.Text.Replace(" ", "-"));
            ListUpdated();
        }

        private void APPLY_PreviewMouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            foreach(var patchItem in PatchesToApply)
            {
                for(int p = 0; p < PatchList[patchItem].Length; p++)
                {
                    if(PatchList[patchItem][p].PatchInfo.sHash.Equals(GameVersionInfo.sHash))
                    {
                        Operations.Patch(GameDirectory, PatchList[patchItem][p].PatchInfo, !PatchList[patchItem][p].PatchInfo.Patched);
                        if (PatchList[patchItem][p].PatchInfo.Patched)
                            GameVersionInfo.PatchesInstalled -= 1;
                        else
                            GameVersionInfo.PatchesInstalled += 1;
                        PatchList[patchItem][p] = new Patch(PatchList[patchItem][p]) { PatchInfo = new PatchInfo(PatchList[patchItem][p].PatchInfo) { Patched = !PatchList[patchItem][p].PatchInfo.Patched } };
                    }
                }
            }
            PatchesToApply.Clear();
            LoadPatchItems();
        }

        private void REMOVEALL_PreviewMouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            foreach(var patchList in PatchList)
            {
                for (var p = 0; p < patchList.Value.Length; p++)
                {
                    if (patchList.Value[p].PatchInfo.Patched)
                    {
                        PatchList[patchList.Key][p].PatchInfo = new PatchInfo(patchList.Value[p].PatchInfo) { Patched = false };
                        Operations.Patch(GameDirectory, PatchList[patchList.Key][p].PatchInfo, false);
                    }
                }
            }
            GameVersionInfo.PatchesInstalled = 0;
            PatchesToApply.Clear();
            LoadPatchItems();
        }

        private void CANCEL_PreviewMouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            foreach(Border child in PatchListPanel.Children)
            {
                StackPanel stack = (StackPanel)((Grid)child.Child).Children[0];
                TextBox tb = (TextBox)stack.Children[0];
                CheckBox cb = (CheckBox)stack.Children[1];
                string PatchName = tb.Text.Replace(" ", "-");
                for (var p = 0; p < PatchList[PatchName].Length; p++)
                {
                    if (PatchList[PatchName][p].PatchInfo.Patched != cb.IsChecked)
                    {
                        cb.IsChecked = !cb.IsChecked;
                    }
                }
            }
            PatchesToApply.Clear();
            ListUpdated();
        }
    }
}
