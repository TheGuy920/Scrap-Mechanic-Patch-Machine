using System;
using System.IO;
using System.Net;

namespace smp.Network
{
    public static class GameVersion
    {
        public static GameInfo GetVersion(string hash, bool patched = false)
        {
            try
            {
                return ParseVersion(
                    new WebClient().DownloadString($"https://raw.githubusercontent.com/TheGuy920/Scrap-Mechanic-Patch-Machine/main/Versions/{hash}"),
                    patched, hash);
            }
            catch (WebException e)
            {
                if (new StreamReader(e.Response.GetResponseStream()).ReadToEnd().Equals("404: Not Found"))
                    return new GameInfo();
            }

            throw new Exception("Uknown error fetching version");
        }
        public static GameInfo ParseVersion(string i, bool patched, string hash)
        {
            GameInfo v = new GameInfo();
            string[] contentList = i.Split('\n');

            if (contentList.Length != 1)
                throw new Exception($"Version not supported: Length {contentList.Length}");

            v.Version = contentList[0];
            v.sHash = hash;
            v.bHash = Array.ConvertAll(hash.SplitInParts(2), s => { return (byte)Convert.ToInt32(s, 16); });

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

        public PatchInfo(PatchInfo patchInfo) : this()
        {
            this = patchInfo;
        }

        public PatchInfo(GameInfo patchInfo) : this()
        {
            this.Version = patchInfo.Version;
            this.sHash = patchInfo.sHash;
            this.bHash = patchInfo.bHash;
        }

        public override string ToString()
        {
            string s = $"Version: {Version}\nHash: {sHash}\nPatched: {Patched}";

            if (Search != null)
                s += "\nSearch: " + string.Join(" ", Array.ConvertAll(Search, s => s.ToString("x").ToUpper()));

            if (ByteList && Targetbytes.Length > 1 && Patchbytes.Length > 1)
            {
                s += "\nTarget Bytes: " + string.Join(" ", Array.ConvertAll(Targetbytes, s => s.ToString("x").ToUpper()));
                s += "\nPatch Bytes: " + string.Join(" ", Array.ConvertAll(Patchbytes, s => s.ToString("x").ToUpper()));
            }
            else if (Targetbyte != 0 && Patchbyte != 0)
            {
                s += "\nTarget Byte: " + Targetbyte.ToString("x").ToUpper();
                s += "\nPatch Byte: " + Patchbyte.ToString("x").ToUpper();
            }
            return s;
        }

    }


    public struct GameInfo
    {
        public string Version;
        public int PatchesInstalled;

        public string sHash;
        public byte[] bHash;

        public GameInfo(GameInfo patchInfo) : this()
        {
            this = patchInfo;
        }

        public GameInfo(PatchInfo patchInfo, int p = 0) : this()
        {
            this.Version = patchInfo.Version;
            this.sHash = patchInfo.sHash;
            this.bHash = patchInfo.bHash;
            this.PatchesInstalled = p;
        }

        public override string ToString()
        {
            return $"Version: {Version}\nHash: {sHash}";
        }

    }


    public struct Patch
    {
        public string PatchName;
        public PatchInfo PatchInfo;

        public Patch(Patch patch) : this()
        {
            this = patch;
        }
    }
}