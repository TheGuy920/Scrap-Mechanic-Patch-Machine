// smp.Network.GameVersion
using Newtonsoft.Json;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;


namespace smp.Network
{
    public static class GameVersion
    {
        static readonly DirectoryInfo CacheDirectory = new(Path.Combine(Utilities.GetAssemblyDirectory(), "Cache"));
        static readonly DirectoryInfo VersionDirectory = new(Path.Combine(CacheDirectory.FullName, "Version"));
        public static readonly DirectoryInfo PatchDirectory = new(Path.Combine(CacheDirectory.FullName, "Patches"));
        public static void InitDirectories()
        {
            if (!CacheDirectory.Exists)
            {
                CacheDirectory.Create();
            }
            if (!VersionDirectory.Exists)
            {
                VersionDirectory.Create();
            }
            if (!PatchDirectory.Exists)
            {
                PatchDirectory.Create();
            }
        }   
        public static void CacheVersion(string hash, string content)
        {
            InitDirectories();
            File.WriteAllText(Path.Combine(VersionDirectory.FullName, hash), content);
        }
        public static string? GetCache(string hash)
        {
            var file = new FileInfo(Path.Combine(VersionDirectory.FullName, hash));
            if (file.Exists)
            {
                return File.ReadAllText(file.FullName);
            }
            return null;
        }
        public static GameInfo GetVersion(string hash)
        {
            string cached = GetCache(hash) ?? string.Empty;
            // Debug.Log(cached);  
            GameInfo cachedVersion = ParseVersion(cached, hash);
            try
            {
                string url = "https://raw.githubusercontent.com/TheGuy920/Scrap-Mechanic-Patch-Machine/main/Versions/" + hash;
#if DEBUG
                Debug.Log(url);
#endif
                string content = new WebClient().DownloadString(url);

                CacheVersion(hash, content);

                return ParseVersion(content, hash);
            }
            catch (WebException ex)
            {
                try
                {
                    Stream? s = (ex.Response?.GetResponseStream()) ?? throw new WebException("No Internet?");
                    if (new StreamReader(s).ReadToEnd().Equals("404: Not Found"))
                    {
                        return new GameInfo() { sHash = hash };
                    }
                }
                catch (WebException)
                {
                    return string.IsNullOrWhiteSpace(cached) ? new GameInfo() { sHash = hash } : cachedVersion;
                }
            }
            return string.IsNullOrWhiteSpace(cached) ?
                throw new ApplicationException("Uknown error fetching version")
                : cachedVersion;
        }

        public static GameInfo ParseVersion(string i, string hash)
        {
            GameInfo v = default;
            string[] contentList = i.Split('\n').Where(x => !string.IsNullOrEmpty(x)).ToArray();

            if (contentList.Length > 1)
                throw new Exception("Version not supported: Length " + contentList.Length + "\r\nContent: " + i.Replace("\n","\\n"));
            
            v.Version = contentList.Length > 0 ? contentList[0] : null;
            v.sHash = hash;
            v.bHash = Array.ConvertAll(hash.SplitInParts(2), (string s) => (byte)Convert.ToInt32(s, 16));
            return v;
        }
    }
    public struct PatchInfo
    {
        public string Version;

        public string Description;

        public string sHash;

        public byte[] bHash;

        public byte[] Search;

        public bool Patched;

        public bool ByteList;

        public byte Targetbyte;

        public byte Patchbyte;

        public byte[] Targetbytes;

        public byte[] Patchbytes;

        public PatchInfo(PatchInfo patchInfo)
        {
            this.Version = patchInfo.Version;
            this.Description = patchInfo.Description;
            this.sHash = patchInfo.sHash;
            this.bHash = patchInfo.bHash;
            this.Search = patchInfo.Search;
            this.Patched = patchInfo.Patched;
            this.ByteList = patchInfo.ByteList;
            this.Targetbyte = patchInfo.Targetbyte;
            this.Patchbyte = patchInfo.Patchbyte;
            this.Targetbytes = patchInfo.Targetbytes;
            this.Patchbytes = patchInfo.Patchbytes;
        }

        public PatchInfo(GameInfo patchInfo)
        {
            this = default;
            this.Version = patchInfo.Version;
            this.sHash = patchInfo.sHash;
            this.bHash = patchInfo.bHash;
        }

        public override readonly string ToString()
        {
            DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(26, 3);
            defaultInterpolatedStringHandler.AppendLiteral("Version: ");
            defaultInterpolatedStringHandler.AppendFormatted(Version);
            defaultInterpolatedStringHandler.AppendLiteral("\nHash: ");
            defaultInterpolatedStringHandler.AppendFormatted(sHash);
            defaultInterpolatedStringHandler.AppendLiteral("\nPatched: ");
            defaultInterpolatedStringHandler.AppendFormatted(Patched);
            string s2 = defaultInterpolatedStringHandler.ToStringAndClear();
            if (Search != null)
            {
                s2 = s2 + "\nSearch: " + string.Join(" ", Array.ConvertAll(Search, (byte s) => s.ToString("x").ToUpper()));
            }
            if (ByteList && Targetbytes.Length > 1 && Patchbytes.Length > 1)
            {
                s2 = s2 + "\nTarget Bytes: " + string.Join(" ", Array.ConvertAll(Targetbytes, (byte s) => s.ToString("x").ToUpper()));
                s2 = s2 + "\nPatch Bytes: " + string.Join(" ", Array.ConvertAll(Patchbytes, (byte s) => s.ToString("x").ToUpper()));
            }
            else if (Targetbyte != 0 && Patchbyte != 0)
            {
                s2 = s2 + "\nTarget Byte: " + Targetbyte.ToString("x").ToUpper();
                s2 = s2 + "\nPatch Byte: " + Patchbyte.ToString("x").ToUpper();
            }
            return s2;
        }
    }

    public struct GameInfo
    {
        public string? Version;

        public int PatchesInstalled;

        public string sHash;

        public byte[] bHash;

        public GameInfo(GameInfo patchInfo)
        {
            this = default;
            this = patchInfo;
        }

        public GameInfo(PatchInfo patchInfo, int p = 0)
        {
            this = default;
            this.Version = patchInfo.Version;
            this.sHash = patchInfo.sHash;
            this.bHash = patchInfo.bHash;
            this.PatchesInstalled = p;
        }

        public override readonly string ToString()
        {
            return "Version: " + Version + "\nHash: " + sHash;
        }
    }

    public struct Patch
    {
        public string PatchName;

        public PatchInfo PatchInfo;

        public Patch(Patch patch)
        {
            this = patch;
        }
    }
}