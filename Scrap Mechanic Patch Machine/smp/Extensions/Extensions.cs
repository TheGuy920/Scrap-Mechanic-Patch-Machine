using smp.Network;
using System;
using System.Collections.Generic;

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
            return 0;

        List<int> list = new();

        for (int i = 0; i < self.Length; i++)
            if (IsMatch(self, i, sub_array))
                list.Add(i);

        if (list.Count > 1)
            throw new System.Exception($"More than one instance detected: {list.Count}");

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
    /// <summary>
    /// i wrote this code, only useful for debug
    /// </summary>
    /// <param name="self"></param>
    /// <returns></returns>
    public static string toString(this Dictionary<string, Patch[]> self)
    {
        string returnVal = "";
        foreach (var item in self)
            foreach(var patch in item.Value)
                returnVal += $"Patch: {patch.PatchName}\n{string.Join(", ", patch.PatchInfo.ToString())}\n";
        return returnVal;
    }
    /// <summary>
    /// i wrote this code, only useful for debug
    /// </summary>
    /// <param name="self"></param>
    /// <returns></returns>
    public static string toString(this Dictionary<byte[], List<PatchInfo>> self)
    {
        string returnVal = "";
        foreach (var item in self)
        {
            returnVal += $"Version Hash: {string.Join(" ", Array.ConvertAll(item.Key, s => s.ToString("x").ToUpper()))}";
            foreach (var patch in item.Value)
                returnVal += $"\n{patch}";
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
    public static string[] SplitInParts(this String s, Int32 partLength)
    {
        if (s == null)
            throw new ArgumentNullException(nameof(s));
        if (partLength <= 0)
            throw new ArgumentException("Part length has to be positive.", nameof(partLength));

        List<string> returnS = new();

        for (var i = 0; i < s.Length; i += partLength)
            returnS.Add(s.Substring(i, Math.Min(partLength, s.Length - i)));

        return returnS.ToArray();
    }
    public static bool equals(this byte[] a, byte[] b)
    {
        for (var i = 0; i < a.Length; i++)
            if (!a[i].Equals(b[i]))
                return false;
        return true;
    }
}