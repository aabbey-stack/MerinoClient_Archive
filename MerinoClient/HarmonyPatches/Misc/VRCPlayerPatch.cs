using VRC;
// ReSharper disable InconsistentNaming

namespace MerinoClient.HarmonyPatches.Misc;

internal class VRCPlayerPatch : PatchObject
{
    public VRCPlayerPatch()
    {
        Patch(typeof(VRCPlayer).GetMethod(nameof(VRCPlayer.SpawnEmojiRPC)), GetLocalPatch(nameof(SpawnEmojiRPC)));
    }

    private static bool SpawnEmojiRPC(int param_1, Player param_2)
    {
        var local = Player.prop_Player_0;
        return Config.Emojis || param_2 == local;
    }
}