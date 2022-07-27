using System;
using System.Linq;
using System.Reflection;
using MerinoClient.Core;
using MerinoClient.Core.VRChat;
using UnityEngine;
using VRC.Core;
using VRC.SDKBase;
using Object = UnityEngine.Object;

namespace MerinoClient.Utilities;

internal static class PortalUtils
{
    private static CreatePortalDelegate _createPortal;

    //ApiWorld param_0, ApiWorldInstance param_1, Vector3 param_2, Vector3 param_3, [Optional] Il2CppSystem.Action<string> param_4 18/07/2022
    private static bool CreatePortal(ApiWorld apiWorld, ApiWorldInstance apiWorldInstance, Vector3 position,
        Vector3 forward, Action<string> additionalAction = null)
    {
        _createPortal ??= (CreatePortalDelegate)Delegate.CreateDelegate(typeof(CreatePortalDelegate),
            typeof(PortalInternal)
                .GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly).First(
                    m => m.ReturnType == typeof(bool)
                         && XrefUtils.CheckMethod(m, "admin_dont_allow_portal")));

        return _createPortal(apiWorld, apiWorldInstance, position, forward, additionalAction);
    }

    public static void DropPortal(string worldId, string instanceId)
    {
        if (Object.FindObjectOfType<VRC_SceneDescriptor>()?.ForbidUserPortals == true)
        {
            VRCUiPopupManager.prop_VRCUiPopupManager_0.ShowAlert("Cannot Create Portal",
                "User portals are forbidden in this world");
            return;
        }

        API.Fetch<ApiWorld>(
            worldId.Trim(),
            new Action<ApiContainer>(
                container =>
                {
                    var apiWorld = container.Model.Cast<ApiWorld>();

                    if (apiWorld.tags.Contains("admin_dont_allow_portals"))
                    {
                        VRCUiPopupManager.prop_VRCUiPopupManager_0.ShowAlert("Cannot Create Portal",
                            "Creating portals to this world is not allowed");
                        return;
                    }

                    var apiWorldInstance = new ApiWorldInstance(apiWorld, instanceId.Trim());

                    apiWorldInstance.Fetch(new Action<ApiContainer>(apiContainer =>
                        {
                            var fetchedInstance = apiContainer.Model.Cast<ApiWorldInstance>();

                            if (!CreatePortal(apiWorld, fetchedInstance,
                                    PlayerEx.VRCPlayer.transform.position,
                                    PlayerEx.VRCPlayer.transform.forward))
                                VRCUiPopupManager.prop_VRCUiPopupManager_0.ShowAlert("Cannot Create Portal",
                                    "Couldn't create a portal, check your surroundings for players or if you are too close to a spawn");
                        }),
                        new Action<ApiContainer>(errorContainer =>
                            VRCUiPopupManager.prop_VRCUiPopupManager_0.ShowAlert("Error Fetching Instance",
                                errorContainer.Error)));
                }),
            new Action<ApiContainer>(container =>
                VRCUiPopupManager.prop_VRCUiPopupManager_0.ShowAlert("Error Fetching World", container.Error)));
    }

    private delegate bool CreatePortalDelegate(ApiWorld apiWorld, ApiWorldInstance apiWorldInstance, Vector3 position,
        Vector3 forward, Il2CppSystem.Action<string> additionalAction);
}