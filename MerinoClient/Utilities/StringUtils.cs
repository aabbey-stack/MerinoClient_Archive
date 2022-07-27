using System;

namespace MerinoClient.Utilities;

internal static class StringUtils
{
    //https://stackoverflow.com/a/17252672
    public static string GetBetween(string strSource, string strStart, string strEnd)
    {
        if (!strSource.Contains(strStart))
            throw new Exception($"start string: {strStart} is not present in the string source: {strSource}");

        if (!strSource.Contains(strEnd))
            throw new Exception($"end string: {strEnd} is not present in the string source: {strSource}");

        var pFrom = strSource.IndexOf(strStart, StringComparison.Ordinal) + strStart.Length;
        var pTo = strSource.LastIndexOf(strEnd, StringComparison.Ordinal);
        return strSource.Substring(pFrom, pTo - pFrom);
    }

    public static string GetAfter(string strSource, string strStart)
    {
        if (!strSource.Contains(strStart)) return "";
        var start = strSource.IndexOf(strStart, 0, StringComparison.Ordinal) + strStart.Length;
        return strSource.Substring(start);
    }
}