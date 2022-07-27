using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using MelonLoader;
using MerinoClient.Core.VRChat;
using Photon.Pun;
using UnhollowerRuntimeLib.XrefScans;
using UnityEngine.SceneManagement;
using VRC;
using VRC.Core;

// ReSharper disable InconsistentNaming

namespace MerinoClient.Features.QoL;

/*
 * Most of the source code is: https://github.com/RequiDev/ReModCE/blob/master/ReModCE/Components/PortalConfirmationComponent.cs
 * some of parts like the cachedDroppers system is from loukylor's old mod
 */

internal class AskToPortal : FeatureComponent
{
    public static readonly Dictionary<int, Player> cachedDroppers = new();
    private static bool _bypassPortals;

    public AskToPortal()
    {
        if (Main.streamerMode) return;

        foreach (var t in typeof(PortalInternal).GetMethods().ToList().FindAll(x =>
                 {
                     if (!x.Name.Contains("Method_Public_Void_"))
                         return false;
                     try
                     {
                         if (XrefScanner.XrefScan(x).Any(z =>
                                 z.Type == XrefType.Global && z.ReadAsObject() != null &&
                                 z.ReadAsObject().ToString() == " was at capacity, cannot enter."))
                             return true;
                     }
                     catch
                     {
                         return false;
                     }

                     return false;
                 }))
            Main.MerinoHarmony.Patch(t, typeof(AskToPortal)
                .GetMethod(nameof(EnterConfirm), BindingFlags.NonPublic | BindingFlags.Static)
                ?.ToNewHarmonyMethod());

        SceneManager.add_sceneUnloaded(new Action<Scene>(_ => { cachedDroppers?.Clear(); }));
    }

    public override string FeatureName => GetType().Name;
    public override string OriginalAuthor => "loukylor & Kiokuu";

    private static bool EnterConfirm(PortalInternal __instance, MethodBase __originalMethod)
    {
        if (!Config.AskToPortal.Value) return true;

        var photonView = __instance.gameObject.GetComponent<PhotonView>();
        var dropper = photonView == null
            ? new APIUser(displayName: "Not Player Dropped", id: "")
            : cachedDroppers[__instance.GetInstanceID()].prop_APIUser_0;

        if (dropper.IsSelf) return true;

        if (APIUser.IsFriendsWith(dropper.id)) return true;

        var apiWorld = __instance.field_Private_ApiWorld_0;
        var instanceId = __instance.field_Private_String_4;
        var instanceAccessType = instanceId.GetInstanceAccessType();

        if (!_bypassPortals)
        {
            VRCUiPopupManager.prop_VRCUiPopupManager_0.ShowStandardPopupV2("Portal Enter Dialog",
                string.IsNullOrEmpty(dropper.id)
                    ? $"Do you really wish to enter this portal? Leads to: {apiWorld.name}"
                    : $"Do you really wish to enter this portal? Dropped by {dropper.displayName} to {apiWorld.name} {instanceAccessType.TranslateInstanceType()}",
                "yes",
                () =>
                {
                    _bypassPortals = true;
                    __originalMethod.Invoke(__instance, null);
                    VRCUiPopupManager.prop_VRCUiPopupManager_0.HideCurrentPopup();
                }, "no", () => { VRCUiPopupManager.prop_VRCUiPopupManager_0.HideCurrentPopup(); }, null);

            return false;
        }

        _bypassPortals = false;
        return true;
    }
}