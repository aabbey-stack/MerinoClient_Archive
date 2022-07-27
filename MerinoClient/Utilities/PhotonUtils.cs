using MerinoClient.Core.VRChat;
using Photon.Realtime;

namespace MerinoClient.Utilities;

internal static class PhotonUtils
{
    public static void CheckAllPhotonPlayers()
    {
        foreach (var player in PlayerEx.PlayersInRoom)
            CheckPhotonPlayer(player.Value);
    }

    public static void CheckPhotonPlayer(Player player)
    {
        if (player == null) return;

        if (player.GetDeveloperType() != "none")
            MerinoLogger.Warning(
                $"{player.GetPlayerType()} {player.GetDisplayName()} has \"{player.GetDeveloperType()}\" developerType");

        var hashtable = player.field_Private_Hashtable_0;

        var avatarEyeHeight = hashtable["avatarEyeHeight"]?.Unbox<int>();
        var canModerateInstance = hashtable["canModerateInstance"]?.Unbox<bool>();

        if (canModerateInstance == true)
            MerinoLogger.Warning($"{player.GetPlayerType()} {player.GetDisplayName()} can moderate the instance");

        if (avatarEyeHeight <= 0)
            MerinoLogger.Warning(
                $"{player.GetPlayerType()} {player.GetDisplayName()} has an invalid \"avatarEyeHeight\": {avatarEyeHeight}");
    }
}