using System;
using System.IO;
using smp.Network;

/// <summary>
/// Scrap Mechanic offline patcher © TheGuy920 2022
/// could not have done it without the help of trbodev#0001
/// </summary>
public static class Operations
{
    public static void Patch(string sm_path, PatchInfo info, bool patch = false)
    {
        // These are the bytes right before [TARGET] that will be used as a search reference
        byte[] search = info.Search;

        // Opens ScrapMechanic.exe as filestream
        FileStream stream = new FileStream(sm_path, FileMode.Open);

        // Copy filestream to sm byte array
        MemoryStream memoryStream = new MemoryStream();
        stream.CopyTo(memoryStream);
        byte[] sm = memoryStream.ToArray();

        // Locates the search reference (above) in the sm byte array as an index
        int position = sm.Locate(search);

        if (position < 10)
            throw new Exception("Unable to locate search bytes");

        // Sets the stream position to the position of the byte in need of patching
        stream.Position = position + search.Length;

        if (info.ByteList)
        {
            int start = position + search.Length;
            int end = start + info.Targetbytes.Length;

            // Uses the index to querry the byte
            byte[] b = sm[start..end];

            // Patches the byte
            if (patch)
            {
                if (!b.equals(info.Targetbytes))
                    throw new Exception("Not target bytes. Version support error?");
                else
                    foreach (var Byte in info.Patchbytes)
                        stream.WriteByte(Byte);
            }
            else
            {
                if (!b.equals(info.Patchbytes))
                    throw new Exception("Not target bytes. Version support error?");
                else
                    foreach (var Byte in info.Targetbytes)
                        stream.WriteByte(Byte);
            }
        }
        else
        {
            // Uses the index to querry the byte
            byte b = sm[position + search.Length];

            // Patches the byte
            if (patch)
                if (!b.Equals(info.Targetbyte))
                    throw new Exception("Not target byte. Version support error?");
                else
                    stream.WriteByte(info.Patchbyte);
            else
                if (!b.Equals(info.Patchbyte))
                throw new Exception($"Target Byte ({b}) not patch byte ({info.Patchbyte}). Version support error?");
            else
                stream.WriteByte(info.Targetbyte);
        }

        // Closes the stream
        stream.Close();

        // Verify patch is correct

        // Opens ScrapMechanic.exe as filestream
        stream = new FileStream(sm_path, FileMode.Open);

        // Copy filestream to sm byte array
        memoryStream = new MemoryStream();
        stream.CopyTo(memoryStream);
        sm = memoryStream.ToArray();

        // Locates the search reference (above) in the sm byte array as an index
        position = sm.Locate(search);

        if (position < 2)
            throw new Exception("Cannot verify correct patch. Game Exectuable ruined!");

        if (info.ByteList)
        {
            int start = position + search.Length;
            int end = start + info.Targetbytes.Length;

            // Uses the index to querry the byte
            byte[] b = sm[start..end];

            if (patch)
            {
                if (!b.equals(info.Patchbytes))
                    throw new Exception("Cannot verify correct patch. Game Exectuable ruined!");
            }
            else
            {
                if (!b.equals(info.Targetbytes))
                    throw new Exception("Cannot verify correct patch. Game Exectuable ruined!");
            }
        }
        else
        {
            // Uses the index to querry the byte
            byte b = sm[position + search.Length];

            if (patch)
            {
                if (!b.Equals(info.Patchbyte))
                    throw new Exception("Cannot verify correct patch. Game Exectuable ruined!");
            }
            else
            {
                if (!b.Equals(info.Targetbyte))
                    throw new Exception($"Cannot verify correct patch. Game Exectuable ruined!");
            }
        }
        stream.Close();
    }
}
