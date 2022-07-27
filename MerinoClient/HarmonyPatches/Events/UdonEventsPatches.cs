using MerinoClient.Core.VRChat;
using VRC;
using VRC.Networking;
using VRC.Udon;
using VRC.Udon.Common.Interfaces;

// ReSharper disable SuggestBaseTypeForParameter
// ReSharper disable InconsistentNaming

namespace MerinoClient.HarmonyPatches.Events;

internal class UdonEventsPatches : PatchObject
{
    public UdonEventsPatches()
    {
        Patch(typeof(UdonSync).GetMethod(nameof(UdonSync.UdonSyncRunProgramAsRPC)),
            GetLocalPatch(nameof(UdonSyncRunProgramAsRPCPatch)));

        Patch(typeof(UdonBehaviour).GetMethod(nameof(UdonBehaviour.SendCustomEvent)),
            GetLocalPatch(nameof(SendCustomEventPatch)));

        Patch(typeof(UdonBehaviour).GetMethod(nameof(UdonBehaviour.SendCustomNetworkEvent)),
            GetLocalPatch(nameof(SendCustomNetworkEventPatch)));

        Patch(typeof(UdonBehaviour).GetMethod(nameof(UdonBehaviour.Interact)),
            GetLocalPatch(nameof(InteractPatch)));
    }

    private static void InteractPatch(UdonBehaviour __instance)
    {
        if (Config.LogUdonEvents.Value && !Config.OnlyGlobalUdonEvents.Value)
            MerinoLogger.Msg($"[UdonInteract] interacted with {__instance.gameObject.name}");
    }

    private static void SendCustomNetworkEventPatch(UdonBehaviour __instance, NetworkEventTarget __0, string __1)
    {
        if (Config.LogUdonEvents.Value && !Config.OnlyGlobalUdonEvents.Value)
            MerinoLogger.Msg(
                $"[UdonNetworkEvent] sent custom network event: {__0} {__1} to {__instance.gameObject.name}");
    }

    private static void SendCustomEventPatch(UdonBehaviour __instance, string __0)
    {
        if (Config.LogUdonEvents.Value && !Config.OnlyGlobalUdonEvents.Value)
            MerinoLogger.Msg($"[UdonEvent] sent custom event: {__0} to {__instance.gameObject.name}");
    }

    //UdonSyncRunProgramAsRPC(string eventName, Player instigator)
    private static bool UdonSyncRunProgramAsRPCPatch(UdonSync __instance, string __0, Player __1)
    {
        if (Config.LogUdonEvents.Value)
        {
            var apiUser = __1.GetAPIUser();

            if (apiUser == null) return true;

            MerinoLogger.Msg(apiUser.IsSelf
                ? $"[UdonRPC] Local player sent an event name of {__0} to {__instance.gameObject.name}"
                : $"[UdonRPC] User: {__1.GetAPIUser().displayName} sent an event name of {__0} to {__instance.gameObject.name}");
        }

        return Config.UdonEvents.Value;
    }
}