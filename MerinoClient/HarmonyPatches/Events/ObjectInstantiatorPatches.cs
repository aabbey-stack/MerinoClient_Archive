using MerinoClient.Core.VRChat;
using UnityEngine;
using VRC;
using VRC.SDKBase;

namespace MerinoClient.HarmonyPatches.Events;

internal class ObjectInstantiatorPatches : PatchObject
{
    public ObjectInstantiatorPatches()
    {
        Patch(typeof(ObjectInstantiator).GetMethod(nameof(ObjectInstantiator._InstantiateObject)),
            GetLocalPatch(nameof(InstantiateObjectPatch)));
        Patch(
            typeof(ObjectInstantiator).GetMethod(nameof(ObjectInstantiator
                .Method_Public_GameObject_VrcBroadcastType_String_Vector3_Quaternion_0)),
            GetLocalPatch(nameof(TryInstantiateObjectPatch)));
    }

    // 	public unsafe GameObject InstantiateObject(VRC_EventHandler.VrcBroadcastType broadcast, string prefabName, Vector3 position, Quaternion rotation)
    private static void TryInstantiateObjectPatch(VRC_EventHandler.VrcBroadcastType __0, string __1)
    {
        Config.SpoofPing.SoftSetValue(false);
        if (Config.LogObjectInstantiate.Value)
            MerinoLogger.Msg(
                $"[InstantiateObject] Prefab: {__1} is trying to be spawned with broadcast type of {__0}");
    }

    //_InstantiateObject(string prefabName, Vector3 position, Quaternion rotation, int viewID, Player instantiator)
    private static void InstantiateObjectPatch(string __0, Vector3 __1, Quaternion __2, int __3, Player __4)
    {
        if (Config.SpoofPing.DefaultValue) Config.SpoofPing.SoftSetValue(true);

        if (!Config.LogObjectInstantiate.Value) return;

        var apiUser = __4.GetAPIUser();

        if (apiUser == null) return;

        MerinoLogger.Msg(
            apiUser.IsSelf
                ? $"[_InstantiateObject] Local player spawned {__0} (viewID: {__3}) pos:{__1.ToString()}, rot:{__2.ToString()})"
                : $"[_InstantiateObject] User: {__4.GetAPIUser().displayName} spawned {__0} (viewID: {__3}) pos:{__1.ToString()}, rot:{__2.ToString()})");
    }
}