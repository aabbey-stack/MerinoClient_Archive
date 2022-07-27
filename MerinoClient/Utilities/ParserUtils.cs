using System;

namespace MerinoClient.Utilities;

internal static class ParserUtils
{
    public static string ParseAvatarId(string input)
    {
        if (string.IsNullOrEmpty(input))
            throw new ArgumentException("AvatarId can't be an empty string");

        if (!input.Contains("avtr_"))
            throw new ArgumentException("AvatarId doesn't contain an invalid prefix \"avtr_\"");

        return input.Contains("vrchat.com") ? StringUtils.GetAfter(input, "avatar/") : input;
    }

    public static string[] ParseInstanceId(string input)
    {
        if (string.IsNullOrEmpty(input))
            throw new ArgumentException("InstanceId can't be an empty string");

        if (!input.Contains("wrld_"))
            throw new ArgumentException("InstanceId doesn't contain an valid prefix \"wrld_\"");

        //https://stackoverflow.com/a/8727172
        var locationStringArray = new string[2];

        var isLink = input.Contains("worldId=") && input.Contains("&instanceId=") && !input.Contains("&shortName=");

        var isShortLink = input.Contains("worldId=") && input.Contains("&instanceId=") &&
                          input.Contains("&shortName=");

        if (isLink)
        {
            locationStringArray[0] = StringUtils.GetBetween(input, "worldId=", "&instanceId=");
            locationStringArray[1] = StringUtils.GetAfter(input, "instanceId=");
            return locationStringArray;
        }

        if (isShortLink)
        {
            locationStringArray[0] = StringUtils.GetBetween(input, "worldId=", "&instanceId=");
            locationStringArray[1] = StringUtils.GetBetween(input, "instanceId=", "&shortName=");
            return locationStringArray;
        }

        return input.Split(':');
    }
}