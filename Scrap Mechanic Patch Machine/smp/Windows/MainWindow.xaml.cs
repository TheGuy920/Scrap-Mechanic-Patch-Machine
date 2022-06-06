// smp.MainWindow
using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using Microsoft.Win32;
using smp;
using smp.Network;

namespace smp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
		public static Dictionary<string, Patch[]>? PatchList;

		public static GameInfo GameVersionInfo;

		public static List<string>? PatchesToApply;

		public static string? GameDirectory;

		public static MainWindow? GetMainWindow;

		public string? ApplicationVersion;
		public string? GameHash;

        private static string? BaseGitUrl = "https://raw.githubusercontent.com/TheGuy920/Scrap-Mechanic-Patch-Machine/main";

		private static HttpClient? LocalWebClient = new HttpClient();

		public MainWindow()
		{
			GetMainWindow = this;
			GameDirectory = GetGameLocation();
			GameVersionInfo = FindGameVersion(GameDirectory);
			GameHash = GameVersionInfo.sHash;
			PatchList = LoadPatchList(GameVersionInfo);
			if (GameVersionInfo.Version == null)
			{
				ScrapeVersion(GameDirectory);
			}
			PatchesToApply = new List<string>();
			base.Activated += GetMainWindow!.MainWindow_Activated;
			InitializeComponent();
			LoadUIInfo();
			LocalWebClient!.DefaultRequestHeaders.Add("User-Agent", "smpm");
			Activate();
			Focus();
		}

		private void LoadUIInfo() 
		{
			GAMEHASH.Text = GameHash;
			GAMEDIR.Text = GameDirectory;
			GAMEVER.Text = ((GameVersionInfo.Version == null) ? "Unknown" : GameVersionInfo.Version);
			if (GameVersionInfo.PatchesInstalled > 0)
			{
				GAMEVER.Text += " - PATCHED";
			}
			PATCHCOUNT.Text = GameVersionInfo.PatchesInstalled.ToString();
			AssemblyVersion.Text = ApplicationVersion;
		}

		private void UpdateUI()
        {
			GAMEHASH.Text = string.Join(null, Convert.ToHexString(GetGameHash(GameDirectory)));
			PATCHCOUNT.Text = GameVersionInfo.PatchesInstalled.ToString();
			GAMEVER.Text = ((GameVersionInfo.Version == null) ? "Unknown" : GameVersionInfo.Version);
			if (GameVersionInfo.PatchesInstalled > 0)
			{
				GAMEVER.Text += " - PATCHED";
			}

		}

		private void MainWindow_Activated(object? sender, EventArgs e)
		{
			LoadPatchItems();
		}

		public void LoadPatchItems()
		{
			PatchListPanel.Children.Clear();
			foreach (KeyValuePair<string, Patch[]> patch in PatchList!)
			{
				Border PatchItem = (Border)((DataTemplate)FindResource("PatchTemplate")).LoadContent();
				((TextBox)((StackPanel)((Grid)PatchItem.Child).Children[0]).Children[0]).Text = patch.Key.Replace("-", " ");
				int Supported = 0;
				int patched = 0;
				string description = "";
				Patch[] value = patch.Value;
				for (int i = 0; i < value.Length; i++)
				{
					Patch item = value[i];
					if (item.PatchInfo.sHash.Equals(GameVersionInfo.sHash))
					{
						patched = Convert.ToInt32(item.PatchInfo.Patched);
						Supported = Convert.ToInt32(value: true);
						description = item.PatchInfo.Description;
						break;
					}
				}
				((Grid)((Grid)PatchItem.Child).Children[2]).Tag = Supported.ToString();
				if (Supported.Equals(1))
				{
					((Grid)((Grid)PatchItem.Child).Children[2]).Background = Brushes.Transparent;
				}
				((TextBox)((Grid)PatchItem.Child).Children[1]).Text = description;
				((CheckBox)((StackPanel)((Grid)PatchItem.Child).Children[0]).Children[1]).Tag = patched.ToString();
				((CheckBox)((StackPanel)((Grid)PatchItem.Child).Children[0]).Children[1]).IsChecked = patched == 1;
				PatchListPanel.Children.Add(PatchItem);
			}
			ListUpdated();
		}

		public static string GetGameLocation()
		{
			string? steamPath = Registry.GetValue("HKEY_LOCAL_MACHINE\\SOFTWARE\\WOW6432Node\\Valve\\Steam", "InstallPath", null) as string;
			if (steamPath is null) throw new Exception("Steam not detected");

			string[] array = File.ReadAllText(
				Path.Combine(steamPath, "steamapps", "libraryfolders.vdf")).Split('"');
			
			foreach (string path in array)
			{
				string game_path = Path.Combine(path, "steamapps", "common", "Scrap Mechanic", "Release", "ScrapMechanic.exe");
				if (File.Exists(game_path))
				{
					return game_path.Replace("\\\\", "\\");
				}
			}
			
			throw new Exception("Scrap Mechanic not detected. Check your steam installation");
		}

		private static byte[] GetGameHash(string sm_path)
        {
			try {
				FileStream stream = new FileStream(sm_path, FileMode.Open);
				SHA256 Sha256 = SHA256.Create();
				byte[] GameHashByteArray = Sha256.ComputeHash(stream);
				stream.Close();
				return GameHashByteArray;
			} 
			catch (IOException e)
            {
				throw new IOException("Please close Scrap Mechanic and try again", e.InnerException);
			}
		}

		public static GameInfo FindGameVersion(string sm_path)
		{
			string hash = string.Join(null, Convert.ToHexString(GetGameHash(sm_path)));
			return GameVersion.GetVersion(hash);
		}

		public static void ScrapeVersion(string location)
		{
			Dictionary<byte[], List<Patch>> VersionPatchList = new Dictionary<byte[], List<Patch>>();
			foreach (KeyValuePair<string, Patch[]> item in PatchList!)
			{
				Patch[] value = item.Value;
				for (int k = 0; k < value.Length; k++)
				{
					Patch patch2 = value[k];
					if (VersionPatchList.ContainsKey(patch2.PatchInfo.bHash))
					{
						VersionPatchList[patch2.PatchInfo.bHash].Add(patch2);
						continue;
					}
					VersionPatchList.Add(patch2.PatchInfo.bHash, new List<Patch>
					{
						new Patch
						{
							PatchName = item.Key,
							PatchInfo = patch2.PatchInfo
						}
					});
				}
			}
			foreach (KeyValuePair<byte[], List<Patch>> version in new Dictionary<byte[], List<Patch>>(VersionPatchList))
			{
				FileStream stream = new FileStream(location, FileMode.Open);
				MemoryStream memoryStream = new MemoryStream();
				stream.CopyTo(memoryStream);
				byte[] sm = memoryStream.ToArray();
				for (int j = 0; j < VersionPatchList[version.Key].Count; j++)
				{
					PatchInfo info = VersionPatchList[version.Key][j].PatchInfo;
					int position = sm.Locate(info.Search);
					stream.Position = position + info.Search.Length;
					if (info.ByteList)
					{
						if (sm[(position + info.Search.Length)..(position + info.Search.Length + info.Patchbytes.Length)].equals(info.Patchbytes))
						{
							VersionPatchList[version.Key][j] = new Patch
							{
								PatchName = VersionPatchList[version.Key][j].PatchName,
								PatchInfo = new PatchInfo(VersionPatchList[version.Key][j].PatchInfo)
								{
									Patched = true
								}
							};
							byte[] targetbytes = info.Targetbytes;
							foreach (byte Byte in targetbytes)
							{
								stream.WriteByte(Byte);
							}
						}
					}
					else if (sm[stream.Position].Equals(info.Patchbyte))
					{
						VersionPatchList[version.Key][j] = new Patch
						{
							PatchName = VersionPatchList[version.Key][j].PatchName,
							PatchInfo = new PatchInfo(VersionPatchList[version.Key][j].PatchInfo)
							{
								Patched = true
							}
						};
						stream.WriteByte(info.Targetbyte);
					}
				}

				//Debug.WriteLine(VersionPatchList.toString());

				stream.Close();
				stream = new FileStream(location, FileMode.Open);
				SHA256 Sha256 = SHA256.Create();
				byte[] GameHashByteArray = new byte[0];
				GameHashByteArray = Sha256.ComputeHash(stream);
				stream.Close();
				string GameHash = string.Join(null, Array.ConvertAll(GameHashByteArray, (byte s) => s.ToString("x")));
				PatchInfo patchInfo = new PatchInfo(GameVersion.GetVersion(GameHash));
				stream = new FileStream(location, FileMode.Open);
				memoryStream = new MemoryStream();
				stream.CopyTo(memoryStream);
				sm = memoryStream.ToArray();
				int counter = 0;
				for (int i = 0; i < VersionPatchList[version.Key].Count; i++)
				{
					PatchInfo info2 = VersionPatchList[version.Key][i].PatchInfo;
					int position2 = sm.Locate(info2.Search);
					stream.Position = position2 + info2.Search.Length;
					if (info2.ByteList)
					{
						if (sm[(position2 + info2.Search.Length)..(position2 + info2.Search.Length + info2.Targetbytes.Length)].equals(info2.Targetbytes) && info2.Patched)
						{
							VersionPatchList[version.Key][i] = new Patch
							{
								PatchName = VersionPatchList[version.Key][i].PatchName,
								PatchInfo = new PatchInfo(VersionPatchList[version.Key][i].PatchInfo)
								{
									Patched = true
								}
							};
							byte[] targetbytes = info2.Patchbytes;
							foreach (byte Byte2 in targetbytes)
							{
								stream.WriteByte(Byte2);
							}
							counter++;
						}
					}
					else if (sm[stream.Position].Equals(info2.Targetbyte) && info2.Patched)
					{
						VersionPatchList[version.Key][i] = new Patch
						{
							PatchName = VersionPatchList[version.Key][i].PatchName,
							PatchInfo = new PatchInfo(VersionPatchList[version.Key][i].PatchInfo)
							{
								Patched = true
							}
						};
						stream.WriteByte(info2.Patchbyte);
						counter++;
					}
				}
				stream.Close();
				if (patchInfo.Version == null)
				{
					continue;
				}
				PatchList.Clear();
				foreach (KeyValuePair<byte[], List<Patch>> item2 in VersionPatchList)
				{
					foreach (Patch patch in item2.Value)
					{
						Patch patch3 = new Patch(patch);
						patch3.PatchInfo = new PatchInfo(patch.PatchInfo)
						{
							Version = patchInfo.Version
						};
						Patch nPatch = patch3;
						if (PatchList.ContainsKey(nPatch.PatchName))
						{
							Patch[] NewPatchArray = new Patch[PatchList[nPatch.PatchName].Length+1];
							for(int m = 0; m < PatchList[nPatch.PatchName].Length; m++)
                            {
								NewPatchArray[m] = PatchList[nPatch.PatchName][m];
							}
							NewPatchArray[NewPatchArray.Length-1] = nPatch;
							PatchList[nPatch.PatchName] = NewPatchArray;
						}
						else
						{
							PatchList.Add(nPatch.PatchName, new Patch[1] { nPatch });
						}
					}
				}
				GameVersionInfo = new GameInfo(patchInfo, counter);
			}
		}

		public static Dictionary<string, Patch[]> LoadPatchList(GameInfo gameInfo)
		{
			using HttpResponseMessage response = LocalWebClient!.GetAsync(BaseGitUrl + "/Patches/PatchList").Result;
			using HttpContent content = response.Content;
			string tmp0 = content.ReadAsStringAsync().Result.Replace("\r", "").Replace("\n", ":");
			while (tmp0.Contains("::"))
			{
				tmp0 = tmp0.Replace("::", ":");
			}
			string[] list = tmp0.Split(":");
			Dictionary<string, Patch[]> map = new Dictionary<string, Patch[]>();
			for (int i = 0; i < list.Length; i++)
			{
				HttpClient? localWebClient = LocalWebClient;
				string url = BaseGitUrl + "/Patches/" + list[i] + "/" + list[i];
				using HttpResponseMessage r = localWebClient!.GetAsync(url).Result;
				using HttpContent c = r.Content;
				string tmp1 = c.ReadAsStringAsync().Result.Replace("\r", "").Replace("\n", ":").Replace(Environment.NewLine, "");
				while (tmp1.Contains("::"))
				{
					tmp1 = tmp1.Replace("::", ":");
				}
				string[] versions = tmp1.Replace(" ", "").Split(":");
				if (versions[^1].Length < 2)
				{
					versions = versions.SkipLast(1).ToArray();
				}
				Patch[] LoadVInfo = new Patch[versions.Length];
				for (int j = 0; j < versions.Length; j++)
				{
					LoadVInfo[j] = LoadPatch(new Patch
					{
						PatchName = list[i],
						PatchInfo = new PatchInfo(gameInfo)
						{
							sHash = versions[j]
						}
					});
				}
				map[list[i]] = LoadVInfo;
			}
			return map;
		}

		public static Patch LoadPatch(Patch patch)
		{
			PatchInfo v = patch.PatchInfo;
			HttpClient? localWebClient = LocalWebClient;
			string url = $"{BaseGitUrl}/Patches/{patch.PatchName}/{patch.PatchInfo.sHash}";
			string[] contentList = localWebClient!.GetAsync(url).Result.Content.ReadAsStringAsync().Result.Split('\n');
			string description = LocalWebClient!.GetAsync(BaseGitUrl + "/Patches/" + patch.PatchName + "/desc").Result.Content.ReadAsStringAsync().Result;
			if (contentList[0].Length == 64 && !contentList[0].Contains(','))
			{
				v.sHash = contentList[0];
				patch.PatchInfo = v;
				return LoadPatch(patch);
			}
			v.bHash = Array.ConvertAll(v.sHash.SplitInParts(2), (string s) => (byte)Convert.ToInt32(s, 16));
			v.Search = Array.ConvertAll(contentList[0].Split(","), (string s) => byte.Parse(s));
			v.Description = description;
			if (contentList[1].Split(",").Length > 1 && contentList[2].Split(",").Length > 1)
			{
				v.ByteList = true;
				v.Targetbytes = Array.ConvertAll(contentList[1].Split(","), (string s) => byte.Parse(s));
				v.Patchbytes = Array.ConvertAll(contentList[2].Split(","), (string s) => byte.Parse(s));
			}
			else if (contentList[1].Split(",").Length == 1 && contentList[2].Split(",").Length == 1)
			{
				v.ByteList = false;
				v.Targetbyte = byte.Parse(contentList[1]);
				v.Patchbyte = byte.Parse(contentList[2]);
			}
			if (v.Targetbytes?.Length != v.Patchbytes?.Length)
			{
				throw new Exception("Target and Patch length do not match");
			}
			Patch result = default(Patch);
			result.PatchName = patch.PatchName;
			result.PatchInfo = v;
			return result;
		}

		public void ListUpdated()
		{
			if (PatchesToApply!.Count > 0)
			{
				APPLY.IsEnabled = true;
				CANCEL.IsEnabled = true;
			}
			else
			{
				APPLY.IsEnabled = false;
				CANCEL.IsEnabled = false;
			}
			if (GameVersionInfo.PatchesInstalled == 0)
			{
				REMOVEALL.IsEnabled = false;
			}
			else
			{
				REMOVEALL.IsEnabled = true;
			}
			UpdateUI();
		}

		private void Button_PreviewMouseUp(object sender, MouseButtonEventArgs e)
		{
			if (((Grid)sender).Tag.Equals("1"))
			{
				((Grid)sender).Opacity = 0.2;
				CheckBox cb = (CheckBox)((StackPanel)((Grid)((Grid)sender).Parent).Children[0]).Children[1];
				cb.IsChecked = !cb.IsChecked;
				CheckBoxChanged(cb, (TextBox)((StackPanel)((Grid)((Grid)sender).Parent).Children[0]).Children[0]);
			}
		}

		private void Grid_MouseEnter(object sender, MouseEventArgs e)
		{
			if (((Grid)sender).Tag.Equals("1"))
			{
				((Grid)sender).Background = Brushes.LightBlue;
			}
		}

		private void Grid_MouseLeave(object sender, MouseEventArgs e)
		{
			if (((Grid)sender).Tag.Equals("1"))
			{
				((Grid)sender).Background = Brushes.Transparent;
			}
		}

		private void Grid_PreviewMouseDown(object sender, MouseButtonEventArgs e)
		{
			if (((Grid)sender).Tag.Equals("1"))
			{
				((Grid)sender).Opacity = 0.4;
			}
		}

		private void CheckBoxChanged(CheckBox sender, TextBox tb)
		{
			if (int.Parse(sender.Tag.ToString()) != Convert.ToInt32(sender.IsChecked))
			{
				PatchesToApply!.Add(tb.Text.Replace(" ", "-"));
			}
			else if (PatchesToApply!.Contains(tb.Text.Replace(" ", "-")))
			{
				PatchesToApply!.Remove(tb.Text.Replace(" ", "-"));
			}
			ListUpdated();
		}

		private void APPLY_PreviewMouseUp(object sender, MouseButtonEventArgs e)
		{
			foreach (string patchItem in PatchesToApply!)
			{
				for (int p = 0; p < PatchList![patchItem].Length; p++)
				{
					if (PatchList![patchItem][p].PatchInfo.sHash.Equals(GameVersionInfo.sHash))
					{
						Operations.Patch(GameDirectory, PatchList![patchItem][p].PatchInfo, !PatchList![patchItem][p].PatchInfo.Patched);
						if (PatchList![patchItem][p].PatchInfo.Patched)
						{
							GameVersionInfo.PatchesInstalled--;
						}
						else
						{
							GameVersionInfo.PatchesInstalled++;
						}
						PatchList![patchItem][p] = new Patch(PatchList![patchItem][p])
						{
							PatchInfo = new PatchInfo(PatchList![patchItem][p].PatchInfo)
							{
								Patched = !PatchList![patchItem][p].PatchInfo.Patched
							}
						};
					}
				}
			}
			PatchesToApply!.Clear();
			LoadPatchItems();
		}

		private void REMOVEALL_PreviewMouseUp(object sender, MouseButtonEventArgs e)
		{
			foreach (KeyValuePair<string, Patch[]> patchList in PatchList!)
			{
				for (int p = 0; p < patchList.Value.Length; p++)
				{
					if (patchList.Value[p].PatchInfo.Patched)
					{
						PatchList![patchList.Key][p].PatchInfo = new PatchInfo(patchList.Value[p].PatchInfo)
						{
							Patched = false
						};
						Operations.Patch(GameDirectory, PatchList![patchList.Key][p].PatchInfo);
					}
				}
			}
			GameVersionInfo.PatchesInstalled = 0;
			PatchesToApply!.Clear();
			LoadPatchItems();
		}

		private void CANCEL_PreviewMouseUp(object sender, MouseButtonEventArgs e)
		{
			foreach (Border child in PatchListPanel.Children)
			{
				StackPanel obj = (StackPanel)((Grid)child.Child).Children[0];
				TextBox tb = (TextBox)obj.Children[0];
				CheckBox cb = (CheckBox)obj.Children[1];
				string PatchName = tb.Text.Replace(" ", "-");
				for (int p = 0; p < PatchList![PatchName].Length; p++)
				{
					if (PatchList![PatchName][p].PatchInfo.Patched != cb.IsChecked)
					{
						cb.IsChecked = !cb.IsChecked;
					}
				}
			}
			PatchesToApply!.Clear();
			ListUpdated();
		}
	}
}
