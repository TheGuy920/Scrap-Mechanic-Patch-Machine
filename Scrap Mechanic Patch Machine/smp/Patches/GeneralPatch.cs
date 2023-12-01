using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using smp.Network;

namespace smp.Patches
{

    /// <summary>
    /// Scrap Mechanic offline patcher © TheGuy920 2022
    /// </summary>
    public static class Operations
    {
        public static void Patch(string sm_path, PatchInfo info, bool patch = false)
        {
            byte[] search = info.Search;

            FileStream stream = new(sm_path, FileMode.Open);
            MemoryStream memoryStream = new();
            stream.CopyTo(memoryStream);
            byte[] sm = memoryStream.ToArray();

            int position = sm.Locate(search);
            if (position < 10)
            {
                stream.Close();
                throw new IOException("Unable to locate search bytes");
            }
            stream.Position = position + search.Length;
            if (info.ByteList)
            {
                int start2 = position + search.Length;
                int end2 = start2 + info.Targetbytes.Length;
                byte[] b4 = sm[start2..end2];
                if (patch)
                {
                    if (!b4.equals(info.Targetbytes))
                    {
                        throw new IOException("Not target bytes. Version support error?");
                    }
                    byte[] patchbytes = info.Patchbytes;
                    foreach (byte Byte2 in patchbytes)
                    {
                        stream.WriteByte(Byte2);
                    }
                }
                else
                {
                    if (!b4.equals(info.Patchbytes))
                    {
                        System.Diagnostics.Debug.WriteLine(Convert.ToHexString(b4));
                        System.Diagnostics.Debug.WriteLine(Convert.ToHexString(info.Patchbytes));
                        throw new IOException("Not target bytes. Version support error?");
                    }
                    byte[] patchbytes = info.Targetbytes;
                    foreach (byte Byte in patchbytes)
                    {
                        stream.WriteByte(Byte);
                    }
                }
            }
            else
            {
                byte b3 = sm[position + search.Length];
                if (patch)
                {
                    if (!b3.Equals(info.Targetbyte))
                    {
                        System.Diagnostics.Debug.WriteLine(Convert.ToHexString(new byte[] { b3 }));
                        System.Diagnostics.Debug.WriteLine(Convert.ToHexString(new byte[] { info.Targetbyte }));
                        throw new IOException("Not target byte. Version support error?");
                    }
                    stream.WriteByte(info.Patchbyte);
                }
                else
                {
                    if (!b3.Equals(info.Patchbyte))
                    {
                        throw new IOException($"Target Byte ({b3}) not patch byte ({info.Patchbyte}). Version support error?");
                    }
                    stream.WriteByte(info.Targetbyte);
                }
            }

            stream.Close();
            stream = new FileStream(sm_path, FileMode.Open);
            memoryStream = new MemoryStream();
            stream.CopyTo(memoryStream);
            sm = memoryStream.ToArray();
            position = sm.Locate(search);

            if (position < 2)
            {
                throw new IOException("Cannot verify correct patch. Game Exectuable ruined!");
            }

            if (info.ByteList)
            {
                int start = position + search.Length;
                int end = start + info.Targetbytes.Length;
                byte[] b2 = sm[start..end];
                if (patch)
                {
                    if (!b2.equals(info.Patchbytes))
                    {
                        throw new IOException("Cannot verify correct patch. Game Exectuable ruined!");
                    }
                }
                else if (!b2.equals(info.Targetbytes))
                {
                    throw new IOException("Cannot verify correct patch. Game Exectuable ruined!");
                }
            }
            else
            {
                byte b = sm[position + search.Length];
                if (patch)
                {
                    if (!b.Equals(info.Patchbyte))
                    {
                        throw new IOException("Cannot verify correct patch. Game Exectuable ruined!");
                    }
                }
                else if (!b.Equals(info.Targetbyte))
                {
                    throw new IOException("Cannot verify correct patch. Game Exectuable ruined!");
                }
            }
            stream.Close();
        }
    }
}