// ReSharper disable RedundantAssignment
// ReSharper disable InconsistentNaming

using VRC;

namespace MerinoClient.HarmonyPatches.Misc;

internal class VrcStationInternalPatch : PatchObject
{
    public VrcStationInternalPatch()
    {
        Patch(
            typeof(VRC_StationInternal).GetMethod(
                nameof(VRC_StationInternal.Method_Public_Boolean_Player_Boolean_0)),
            GetLocalPatch(nameof(PlayerCanUseStation)));
    }

    // public unsafe bool PlayerCanUseStation(Player player, bool log = false)
    private static bool PlayerCanUseStation(ref bool __result, Player param_1)
    {
        if (Config.VRCStations.Value) return true;
        if (param_1 != Player.prop_Player_0) return true;
        __result = false;
        return false;
    }
}