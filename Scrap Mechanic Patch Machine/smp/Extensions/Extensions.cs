// Extensions
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using smp.Network;

static class Extensions
{
    /// <summary>
    /// Stolen code used to find a byte array in a byte array
    /// </summary>
    /// <param name="sub_array"></param>
    /// <returns></returns>
    public static int Locate(this byte[] self, byte[] sub_array)
    {
        if (IsEmptyLocate(self, sub_array))
        {
            return 0;
        }
        List<int> list = new List<int>();
        for (int i = 0; i < self.Length; i++)
        {
            if (IsMatch(self, i, sub_array))
            {
                list.Add(i);
            }
        }
        if (list.Count > 1)
        {
            DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(33, 1);
            defaultInterpolatedStringHandler.AppendLiteral("More than one instance detected: ");
            defaultInterpolatedStringHandler.AppendFormatted(list.Count);
            throw new Exception(defaultInterpolatedStringHandler.ToStringAndClear());
        }
        if (list.Count != 0)
        {
            return list.ToArray()[0];
        }
        return 0;
    }

    private static bool IsMatch(byte[] array, int position, byte[] sub_array)
    {
        if (sub_array.Length > array.Length - position)
        {
            return false;
        }
        for (int i = 0; i < sub_array.Length; i++)
        {
            if (array[position + i] != sub_array[i])
            {
                return false;
            }
        }
        return true;
    }

    private static bool IsEmptyLocate(byte[] array, byte[] sub_array)
    {
        if (array != null && sub_array != null && array.Length != 0 && sub_array.Length != 0)
        {
            return sub_array.Length > array.Length;
        }
        return true;
    }

    /// <summary>
    /// i wrote this code, only useful for debug
    /// </summary>
    /// <param name="self"></param>
    /// <returns></returns>
    public static string toString(this Dictionary<string, Patch[]> self)
    {
        string returnVal = "";
        foreach (KeyValuePair<string, Patch[]> item in self)
        {
            Patch[] value = item.Value;
            for (int i = 0; i < value.Length; i++)
            {
                Patch patch = value[i];
                string text = returnVal;
                DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(9, 2);
                defaultInterpolatedStringHandler.AppendLiteral("Patch: ");
                defaultInterpolatedStringHandler.AppendFormatted(patch.PatchName);
                defaultInterpolatedStringHandler.AppendLiteral("\n");
                string[] array = new string[1];
                PatchInfo patchInfo = patch.PatchInfo;
                array[0] = patchInfo.ToString();
                defaultInterpolatedStringHandler.AppendFormatted(string.Join(", ", array));
                defaultInterpolatedStringHandler.AppendLiteral("\n");
                returnVal = text + defaultInterpolatedStringHandler.ToStringAndClear();
            }
        }
        return returnVal;
    }
    /// <summary>
    /// i wrote this code, only useful for debug
    /// </summary>
    /// <param name="self"></param>
    /// <returns></returns>
    public static string toString(this Dictionary<byte[], List<Patch>> self)
    {
        string returnVal = "";
        foreach (KeyValuePair<byte[], List<Patch>> item in self)
        {
            returnVal = returnVal + "Version Hash: " + string.Join(" ", Convert.ToHexString(item.Key));
            foreach (Patch patch in item.Value)
            {
                returnVal += "\n" + patch.PatchInfo;
            }
            returnVal += "\n";
        }
        return returnVal;
    }
    /// <summary>
    /// more stolen code
    /// </summary>
    /// <param name="s"></param>
    /// <param name="partLength"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException"></exception>
    /// <exception cref="ArgumentException"></exception>
    public static string[] SplitInParts(this string s, int partLength)
    {
        if (s == null)
        {
            throw new ArgumentNullException("s");
        }
        if (partLength <= 0)
        {
            throw new ArgumentException("Part length has to be positive.", "partLength");
        }
        List<string> returnS = new List<string>();
        for (int i = 0; i < s.Length; i += partLength)
        {
            returnS.Add(s.Substring(i, Math.Min(partLength, s.Length - i)));
        }
        return returnS.ToArray();
    }
    public static bool equals(this byte[] a, byte[] b)
    {
        for (int i = 0; i < a.Length; i++)
        {
            if (!a[i].Equals(b[i]))
            {
                return false;
            }
        }
        return true;
    }
}