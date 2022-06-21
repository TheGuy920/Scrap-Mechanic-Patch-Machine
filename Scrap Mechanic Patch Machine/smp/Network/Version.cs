// smp.Network.GameVersion
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
        public static GameInfo GetVersion(string hash, bool patched = false)
        {
            try
            {
                string url = "https://raw.githubusercontent.com/TheGuy920/Scrap-Mechanic-Patch-Machine/main/Versions/" + hash;
#if DEBUG
                Debug.Log(url);
#endif
                return ParseVersion(new WebClient().DownloadString(url), patched, hash);
            }
            catch (WebException ex)
            {
                if (new StreamReader(ex.Response!.GetResponseStream()).ReadToEnd().Equals("404: Not Found"))
                {
                    return new GameInfo() { sHash = hash };
                }
            }
            throw new Exception("Uknown error fetching version");
        }

        public static GameInfo ParseVersion(string i, bool patched, string hash)
        {
            GameInfo v = default;
            string[] contentList = i.Split('\n').Where(x => !string.IsNullOrEmpty(x)).ToArray();

            if (contentList.Length != 1)
                throw new Exception("Version not supported: Length " + contentList.Length + "\r\nContent: " + i.Replace("\n","\\n"));
            
            v.Version = contentList[0];
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
            Version = patchInfo.Version;
            Description = patchInfo.Description;
            sHash = patchInfo.sHash;
            bHash = patchInfo.bHash;
            Search = patchInfo.Search;
            Patched = patchInfo.Patched;
            ByteList = patchInfo.ByteList;
            Targetbyte = patchInfo.Targetbyte;
            Patchbyte = patchInfo.Patchbyte;
            Targetbytes = patchInfo.Targetbytes;
            Patchbytes = patchInfo.Patchbytes;
        }

        public PatchInfo(GameInfo patchInfo)
        {
            this = default(PatchInfo);
            Version = patchInfo.Version;
            sHash = patchInfo.sHash;
            bHash = patchInfo.bHash;
        }

        public override string ToString()
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
        public string Version;

        public int PatchesInstalled;

        public string sHash;

        public byte[] bHash;

        public GameInfo(GameInfo patchInfo)
        {
            this = default(GameInfo);
            this = patchInfo;
        }

        public GameInfo(PatchInfo patchInfo, int p = 0)
        {
            this = default(GameInfo);
            Version = patchInfo.Version;
            sHash = patchInfo.sHash;
            bHash = patchInfo.bHash;
            PatchesInstalled = p;
        }

        public override string ToString()
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
            this = default(Patch);
            this = patch;
        }
    }
}