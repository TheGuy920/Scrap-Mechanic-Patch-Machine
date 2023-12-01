using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics.Arm;
using System.Security.Cryptography;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Xml.XPath;
using Microsoft.Win32;
using smp;
using smp.Network;
using smp.Patches;

namespace smp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
		public Dictionary<string, Patch[]> PatchList { get; set; } = new();
		public GameInfo GameVersionInfo = default;
		public List<string> PatchesToApply { get; set; } = new();
        public string GamePath { get; set; } = string.Empty;
		public static MainWindow GetMainWindow { get; set; } = new();
        public string ApplicationVersion { get; set; }
        public string GameHash { get; set; } = string.Empty;
        private HttpClient LocalWebClient { get; } = new();

        private static readonly string BaseGitUrl = "https://raw.githubusercontent.com/TheGuy920/Scrap-Mechanic-Patch-Machine/main";
		public MainWindow()
		{
			GetMainWindow = this;
			this.InitializeComponent();
            this.Closed += MainWindow_Closed;
        }

        private void MainWindow_Closed(object? sender, EventArgs e)
        {
			Task.Run(Suicide);
        }

		private async void Suicide()
		{
			await Task.Delay(1000);
			this.Dispatcher.Invoke(()=> { App.GetApp!.Shutdown(); });
		}

        public Task Init()
		{
            this.GamePath = GetGamePath();
            this.GameVersionInfo = FindGameVersion(this.GamePath);
            this.GameHash = this.GameVersionInfo.sHash;
            this.PatchList = this.LoadPatchList(this.GameVersionInfo);
            if (this.GameVersionInfo.Version == null)
            {
                this.GameVersionInfo = this.ScrapeVersion(this.GamePath);
            }
            this.PatchesToApply = new List<string>();
            this.LocalWebClient.DefaultRequestHeaders.Add("User-Agent", "smpm");
            this.Dispatcher.Invoke(LoadPatchItems);

			return Task.CompletedTask;
        }

		private void LoadUIInfo()
		{
            this.GAMEHASH.Text = Convert.ToHexString(GetGameHash(this.GamePath));
            this.GAMEDIR.Text = this.GamePath;
            this.GAMEVER.Text = this.GameVersionInfo.Version ?? "Unknown";
			if (this.GameVersionInfo.PatchesInstalled > 0)
			{
                this.GAMEVER.Text += " - PATCHED";
			}
            this.PATCHCOUNT.Text = this.GameVersionInfo.PatchesInstalled.ToString();
            this.AssemblyVersion.Text = this.ApplicationVersion;
#if DEBUG
			Debug.Log("UI Updated");
			Debug.Log($"GamePath: {GamePath!}");
			Debug.Log($"PatchesInstalled: {GameVersionInfo.PatchesInstalled}");
			Debug.Log($"GameHash: {this.GameHash!}");
			Debug.Log($"GameVersionInfo: {GameVersionInfo.Version ?? "Unknown"}");
			Debug.Log($"ApplicationVersion: {this.ApplicationVersion ?? "Version Not Loaded"}");
#endif
		}

		public void LoadPatchItems()
		{
            this.PatchListPanel.Children.Clear();
			foreach (KeyValuePair<string, Patch[]> patch in this.PatchList)
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
					if (item.PatchInfo.sHash.Equals(this.GameVersionInfo.sHash))
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
                    ((Grid)((Grid)PatchItem.Child).Children[2]).Background = patched == 1 ? Brushes.Green : Brushes.Transparent;
                }
				((TextBox)((Grid)PatchItem.Child).Children[1]).Text = description;
				((CheckBox)((StackPanel)((Grid)PatchItem.Child).Children[0]).Children[1]).Tag = patched.ToString();
				((CheckBox)((StackPanel)((Grid)PatchItem.Child).Children[0]).Children[1]).IsChecked = patched == 1;
                this.PatchListPanel.Children.Add(PatchItem);
			}
			this.ListUpdated();

            this.Activate();
            this.Focus();
            // this.LoadUIInfo();
        }

		public static string GetGamePath()
		{
            if (Registry.GetValue("HKEY_LOCAL_MACHINE\\SOFTWARE\\WOW6432Node\\Valve\\Steam", "InstallPath", null) is not string steamPath)
				throw new Exception("Steam not detected");

            string FileContents = File.ReadAllText(Path.Combine(steamPath, "steamapps", "libraryfolders.vdf"));

#if DEBUG
			Debug.Log($"Loaded Steam Location: {steamPath}");
			Debug.Log($"File Contents:\n{FileContents}\n================================================================================================\n");
#endif
			
			string[] array = FileContents.Split('"');
			
			foreach (string path in array)
			{
				string game_path = Path.Combine(path, "steamapps", "common", "Scrap Mechanic", "Release", "ScrapMechanic.exe").Replace("\n", "");

#if DEBUG
				Debug.Log($"Checking Path: {game_path}");
#endif

				if (File.Exists(game_path))
				{
					return game_path.Replace("\\\\", "\\").Replace("//", "/");
				}
			}
			
			throw new Exception("Scrap Mechanic not detected. Check your steam installation");
		}

		private static byte[] GetGameHash(string sm_path)
        {
			try 
			{
				FileStream stream = new(sm_path, FileMode.Open);
                byte[] GameHashByteArray = GetHash(stream);
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
			string hash = Convert.ToHexString(GetGameHash(sm_path)).ToLower();
			return GameVersion.GetVersion(hash);
		}

		public static byte[] GetHash(Stream stream)
		{
			MemoryStream memoryStream = new();
			if (stream is not MemoryStream mstream)
				stream.CopyTo(memoryStream);
			else
                memoryStream = mstream;

            memoryStream.Position = 0;
            SHA256 Sha256 = SHA256.Create();
            return Sha256.ComputeHash(memoryStream);
        }

		public GameInfo ScrapeVersion(string location)
		{
			Dictionary<byte[], List<Patch>> VersionPatchList = new();
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
						new() 
						{
							PatchName = item.Key,
							PatchInfo = patch2.PatchInfo
						}
					});
				}
			}

            FileStream stream = new(location, FileMode.Open);
            MemoryStream memoryStream = new();
            stream.CopyTo(memoryStream);
			stream.Close();
            int counter = 0;

            foreach (KeyValuePair<byte[], List<Patch>> version in new Dictionary<byte[], List<Patch>>(VersionPatchList))
			{
                memoryStream.Position = 0;
                byte[] sm = memoryStream.GetBuffer();

                for (int j = 0; j < VersionPatchList[version.Key].Count; j++)
				{
					PatchInfo info = VersionPatchList[version.Key][j].PatchInfo;
					int position = sm.Locate(info.Search);
                    memoryStream.Position = position + info.Search.Length;
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
                                memoryStream.WriteByte(Byte);
                            }
						}
					}
					else if (sm[memoryStream.Position].Equals(info.Patchbyte))
					{
						VersionPatchList[version.Key][j] = new Patch
						{
							PatchName = VersionPatchList[version.Key][j].PatchName,
							PatchInfo = new PatchInfo(VersionPatchList[version.Key][j].PatchInfo)
							{
								Patched = true
							}
						};
                        memoryStream.WriteByte(info.Targetbyte);
                    }
				}

                // Compute new hash
                byte[] GameHashByteArray = GetHash(memoryStream);
				string GameHash = string.Join(null, Array.ConvertAll(GameHashByteArray, (byte s) => s.ToString("x2")));
				PatchInfo patchInfo = new(GameVersion.GetVersion(GameHash));
                memoryStream.Position = 0;
                sm = memoryStream.GetBuffer();
				
                for (int i = 0; i < VersionPatchList[version.Key].Count; i++)
				{
					PatchInfo info2 = VersionPatchList[version.Key][i].PatchInfo;
					int position2 = sm.Locate(info2.Search);
                    memoryStream.Position = position2 + info2.Search.Length;
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
							counter++;
						}
					}
					else if (sm[memoryStream.Position].Equals(info2.Targetbyte) && info2.Patched)
					{
						VersionPatchList[version.Key][i] = new Patch
						{
							PatchName = VersionPatchList[version.Key][i].PatchName,
							PatchInfo = new PatchInfo(VersionPatchList[version.Key][i].PatchInfo)
							{
								Patched = true
							}
						};
                        counter++;
					}
                }

				if (patchInfo.Version == null)
				{
					continue;
				}

                this.PatchList.Clear();

				foreach (KeyValuePair<byte[], List<Patch>> item2 in VersionPatchList)
				{
					foreach (Patch patch in item2.Value)
					{
                        Patch patch3 = new(patch)
                        {
                            PatchInfo = new PatchInfo(patch.PatchInfo)
                            {
                                Version = patchInfo.Version
                            }
                        };
                        Patch nPatch = patch3;
						if (this.PatchList.ContainsKey(nPatch.PatchName))
						{
							Patch[] NewPatchArray = new Patch[this.PatchList[nPatch.PatchName].Length+1];
							for(int m = 0; m < this.PatchList[nPatch.PatchName].Length; m++)
                            {
								NewPatchArray[m] = this.PatchList[nPatch.PatchName][m];
							}
							NewPatchArray[^1] = nPatch;
                            this.PatchList[nPatch.PatchName] = NewPatchArray;
						}
						else
						{
                            this.PatchList.Add(nPatch.PatchName, new Patch[1] { nPatch });
						}
					}
				}
				return new(patchInfo, counter);
			}
			return new();
		}

		public Dictionary<string, Patch[]> LoadPatchList(GameInfo gameInfo)
		{
			string tmp0 = string.Empty;
            Dictionary<string, Patch[]> map = new();
            var patchesFile = new FileInfo(Path.Combine(GameVersion.PatchDirectory.FullName, "PatchList"));
            try
			{
				using HttpResponseMessage response = this.LocalWebClient.GetAsync(BaseGitUrl + "/Patches/PatchList").Result;
				using HttpContent content = response.Content;
                tmp0 = content.ReadAsStringAsync().Result.Replace("\r", "").Replace("\n", ":");
				if (!patchesFile.Directory.Exists)
				{
                    patchesFile.Directory.Create();
                }
				foreach (string item in tmp0.Split(":"))
				{
                    patchesFile.Directory.CreateSubdirectory(item);
                }
                File.WriteAllText(patchesFile.FullName, tmp0);
            }
			catch
			{
                if (patchesFile.Exists)
				{
					this.Dispatcher.Invoke(() => { 
						this.Title = "Offline - " + this.Title;
                    });
                    tmp0 = new StreamReader(patchesFile.OpenRead()).ReadToEnd();
                }
			}

			if (tmp0 == string.Empty)
				return map;
			
			while (tmp0.Contains("::"))
			{
				tmp0 = tmp0.Replace("::", ":");
			}
			string[] list = tmp0.Split(":");

			for (int i = 0; i < list.Length; i++)
			{
				string tmp1 = string.Empty;
                var patchFile = new FileInfo(Path.Combine(GameVersion.PatchDirectory.FullName, list[i], list[i]));
                try
				{
					HttpClient? localWebClient = this.LocalWebClient;
					string url = BaseGitUrl + "/Patches/" + list[i] + "/" + list[i];
					using HttpResponseMessage r = localWebClient!.GetAsync(url).Result;
					using HttpContent c = r.Content;
					tmp1 = c.ReadAsStringAsync().Result.Replace("\r", "").Replace("\n", ":").Replace(Environment.NewLine, "");
					if (!patchFile.Directory.Exists)
						patchFile.Directory.Create();
                    File.WriteAllText(patchFile.FullName, tmp1);
                }
				catch
				{
                    if (patchFile.Exists)
                    {
                        tmp1 = new StreamReader(patchFile.OpenRead()).ReadToEnd();
                    }
                }

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

		public Patch LoadPatch(Patch patch)
		{
			PatchInfo v = patch.PatchInfo;
			string[] contentList;
			string description;
            var descriptionFile = new FileInfo(Path.Combine(GameVersion.PatchDirectory.FullName, patch.PatchName, "desc"));
			var contentFile = new FileInfo(Path.Combine(GameVersion.PatchDirectory.FullName, patch.PatchName, patch.PatchInfo.sHash));
            try
			{
				HttpClient? localWebClient = this.LocalWebClient;
				string url = $"{BaseGitUrl}/Patches/{patch.PatchName}/{patch.PatchInfo.sHash}";
#if DEBUG
				Debug.Log(url);
#endif
                contentList = localWebClient!.GetAsync(url).Result.Content.ReadAsStringAsync().Result.Split('\n');
                description = this.LocalWebClient.GetAsync(BaseGitUrl + "/Patches/" + patch.PatchName + "/desc").Result.Content.ReadAsStringAsync().Result;
				
				if (!descriptionFile.Directory.Exists)
                    descriptionFile.Directory.Create();

                if (!contentFile.Directory.Exists)
                    contentFile.Directory.Create();

                File.WriteAllText(descriptionFile.FullName, description);
                File.WriteAllLines(contentFile.FullName, contentList);
            }
			catch
			{
				contentList = File.ReadAllLines(contentFile.FullName);
				description = File.ReadAllText(descriptionFile.FullName);
			}

            if (contentList[0].Length == 64 && !contentList[0].Contains(','))
			{
				v.sHash = contentList[0];
				patch.PatchInfo = v;
				return this.LoadPatch(patch);
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
			Patch result = default;
			result.PatchName = patch.PatchName;
			result.PatchInfo = v;
			return result;
		}

		public void ListUpdated()
		{
			if (this.PatchesToApply.Count > 0)
			{
				this.APPLY.IsEnabled = true;
                this.CANCEL.IsEnabled = true;
			}
			else
			{
                this.APPLY.IsEnabled = false;
                this.CANCEL.IsEnabled = false;
			}
			if (this.GameVersionInfo.PatchesInstalled == 0)
			{
                this.REMOVEALL.IsEnabled = false;
			}
			else
			{
                this.REMOVEALL.IsEnabled = true;
			}
			this.LoadUIInfo();
		}

		private void Button_PreviewMouseUp(object sender, MouseButtonEventArgs e)
		{
			if (((Grid)sender).Tag.Equals("1"))
			{
				((Grid)sender).Opacity = 0.2;
				CheckBox cb = (CheckBox)((StackPanel)((Grid)((Grid)sender).Parent).Children[0]).Children[1];
				cb.IsChecked = !cb.IsChecked;
                this.CheckBoxChanged(cb, (TextBox)((StackPanel)((Grid)((Grid)sender).Parent).Children[0]).Children[0]);
			}
		}

		private void Grid_MouseEnter(object sender, MouseEventArgs e)
		{
			if (((Grid)sender).Tag.Equals("1"))
			{
				((Grid)sender).Background = Brushes.Blue;
			}
		}

		private void Grid_MouseLeave(object sender, MouseEventArgs e)
		{
			if (((Grid)sender).Tag.Equals("1"))
			{
                CheckBox cb = (CheckBox)((StackPanel)((Grid)((Grid)sender).Parent).Children[0]).Children[1];
				string patchItem = ((TextBox)((StackPanel)((Grid)((Grid)sender).Parent).Children[0]).Children[0]).Text.Replace(" ", "-");
				var brush = cb.IsChecked == true ? Brushes.Green : Brushes.Transparent;
				for (int p = 0; p < this.PatchList[patchItem].Length; p++)
				{
					if (this.PatchList[patchItem][p].PatchInfo.sHash.Equals(this.GameVersionInfo.sHash))
					{
						if (this.PatchList[patchItem][p].PatchInfo.Patched
							&& cb.IsChecked == false)
                            brush = Brushes.IndianRed;
                        else if (!this.PatchList[patchItem][p].PatchInfo.Patched
                            && cb.IsChecked == true)
							brush = Brushes.Orange;
						break;
					}
				}
                ((Grid)sender).Background = brush;
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
			if (int.Parse(sender.Tag.ToString()!) != Convert.ToInt32(sender.IsChecked))
			{
                this.PatchesToApply.Add(tb.Text.Replace(" ", "-"));
			}
			else if (this.PatchesToApply.Contains(tb.Text.Replace(" ", "-")))
			{
                this.PatchesToApply.Remove(tb.Text.Replace(" ", "-"));
			}
            this.ListUpdated();
		}

		private void APPLY_PreviewMouseUp(object sender, MouseButtonEventArgs e)
		{
			foreach (string patchItem in this.PatchesToApply)
			{
				for (int p = 0; p < this.PatchList[patchItem].Length; p++)
				{
					if (this.PatchList[patchItem][p].PatchInfo.sHash.Equals(this.GameVersionInfo.sHash))
					{
						Operations.Patch(this.GamePath, this.PatchList[patchItem][p].PatchInfo, !this.PatchList[patchItem][p].PatchInfo.Patched);
						if (this.PatchList[patchItem][p].PatchInfo.Patched)
						{
                            this.GameVersionInfo.PatchesInstalled--;
						}
						else
						{
                            this.GameVersionInfo.PatchesInstalled++;
						}
                        this.PatchList[patchItem][p] = new Patch(this.PatchList[patchItem][p])
						{
							PatchInfo = new PatchInfo(this.PatchList[patchItem][p].PatchInfo)
							{
								Patched = !this.PatchList[patchItem][p].PatchInfo.Patched
							}
						};
					}
				}
			}
            this.PatchesToApply.Clear();
            this.LoadPatchItems();
		}

		private void REMOVEALL_PreviewMouseUp(object sender, MouseButtonEventArgs e)
		{
			foreach (KeyValuePair<string, Patch[]> patchList in this.PatchList)
			{
				for (int p = 0; p < patchList.Value.Length; p++)
				{
					if (patchList.Value[p].PatchInfo.Patched)
					{
                        this.PatchList![patchList.Key][p].PatchInfo = new PatchInfo(patchList.Value[p].PatchInfo)
						{
							Patched = false
						};
						Operations.Patch(GamePath, this.PatchList[patchList.Key][p].PatchInfo);
					}
				}
			}
            this.GameVersionInfo.PatchesInstalled = 0;
            this.PatchesToApply.Clear();
            this.LoadPatchItems();
		}

		private void CANCEL_PreviewMouseUp(object sender, MouseButtonEventArgs e)
		{
			foreach (Border child in this.PatchListPanel.Children)
			{
				StackPanel obj = (StackPanel)((Grid)child.Child).Children[0];
				TextBox tb = (TextBox)obj.Children[0];
				CheckBox cb = (CheckBox)obj.Children[1];
				string PatchName = tb.Text.Replace(" ", "-");
				for (int p = 0; p < this.PatchList[PatchName].Length; p++)
				{
					if (this.PatchList[PatchName][p].PatchInfo.Patched != cb.IsChecked)
					{
						cb.IsChecked = !cb.IsChecked;
					}
				}
			}
            this.PatchesToApply.Clear();
            this.ListUpdated();
		}
	}
}
