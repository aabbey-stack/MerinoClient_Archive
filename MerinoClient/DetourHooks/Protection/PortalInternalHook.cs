using System;
using System.Runtime.InteropServices;
using MerinoClient.Core.VRChat;
using MerinoClient.Features.QoL;
using VRC;
using VRC.Core;
using Object = UnityEngine.Object;
using String = Il2CppSystem.String;

// ReSharper disable AccessToModifiedClosure
// ReSharper disable PossibleNullReferenceException

namespace MerinoClient.DetourHooks.Protection;

internal class PortalInternalHook : DetourHookManager
{
    private static APIUser _instanceCreator;

    private static ConfigurePortalDelegate _configurePortalDelegate;
    private static AwakeDelegate _awakeDelegate;

    public PortalInternalHook()
    {
        try
        {
            NativePatchUtils.NativePatch(
                typeof(PortalInternal).GetMethod(nameof(PortalInternal.ConfigurePortal))!,
                out _configurePortalDelegate, ConfigurePortalHook);
        }
        catch (Exception e)
        {
            MerinoLogger.Error("Failed to hook: ConfigurePortal\n", e);
        }

        try
        {
            NativePatchUtils.NativePatch(
                typeof(PortalInternal).GetMethod(nameof(PortalInternal.Awake))!,
                out _awakeDelegate, AwakeHook);
        }
        catch (Exception e)
        {
            MerinoLogger.Error("Failed to hook: Awake\n", e);
        }
    }

    private static void AwakeHook(IntPtr thisPtr)
    {
        var portalInternal = new PortalInternal(thisPtr);

        if (!Config.Portals) portalInternal.gameObject.SetActive(false);

        _awakeDelegate(thisPtr);
    }

    private static void ConfigurePortalHook(IntPtr thisPtr, IntPtr roomIdPtr, IntPtr idWithTagsPtr, IntPtr playerCount,
        IntPtr instigatorPlayerPtr, IntPtr nativeMethodInfoPtr)
    {
        var portalInternal = new PortalInternal(thisPtr);
        var idWithTags = (string)new String(idWithTagsPtr);
        var instigator = new Player(instigatorPlayerPtr);
        var roomId = (string)new String(roomIdPtr);

        AskToPortal.cachedDroppers.Add(portalInternal.GetInstanceID(), instigator);

        var apiUser = instigator.GetAPIUser();

        if (apiUser == null)
        {
            _configurePortalDelegate(thisPtr, roomIdPtr, idWithTagsPtr, playerCount, instigatorPlayerPtr,
                nativeMethodInfoPtr);
            return;
        }

        if (Config.AllPortals.Value && !apiUser.IsSelf)
        {
            Object.Destroy(portalInternal.gameObject);
            MerinoLogger.Msg(
                $"[ConfigurePortal] User: {instigator.GetAPIUser().displayName} tried to spawn a portal");
            AskToPortal.cachedDroppers.Clear();
        }
        else if (Config.NonFriendsPortals.Value && !apiUser.IsSelf &&
                 !APIUser.IsFriendsWith(apiUser.id))
        {
            Object.Destroy(portalInternal.gameObject);
            MerinoLogger.Msg(
                $"[ConfigurePortal] User: {apiUser.displayName} tried to spawn a portal");
            AskToPortal.cachedDroppers.Clear();
        }

        if (!Config.Notifications.Value)
        {
            _configurePortalDelegate(thisPtr, roomIdPtr, idWithTagsPtr, playerCount, instigatorPlayerPtr,
                nativeMethodInfoPtr);
            return;
        }

        if (APIUser.IsFriendsWith(apiUser.id) && XSNotificationsLite.CanNotify)
        {
            var instanceAccessType = idWithTags.GetInstanceAccessType();
            var networkRegion = idWithTags.GetNetworkRegion();
            var instanceCreatorId = idWithTags.GetInstanceCreator();

            if (apiUser.id == instanceCreatorId)
                _instanceCreator = apiUser;
            else
                API.Fetch<APIUser>(instanceCreatorId,
                    new Action<ApiContainer>(container => { _instanceCreator = container.Model.Cast<APIUser>(); }),
                    new Action<ApiContainer>(container =>
                        VRCUiPopupManager.prop_VRCUiPopupManager_0.ShowAlert("Error Fetching APIUser",
                            container.Error)));

            API.Fetch<ApiWorld>(
                roomId,
                new Action<ApiContainer>(
                    container =>
                    {
                        var apiWorld = container.Model.Cast<ApiWorld>();
                        XSNotificationsLite.SendNotification(
                            $"{apiUser.GetAPIUserType()}: {apiUser.displayName} has spawned a portal to {apiWorld.name}",
                            instanceAccessType == InstanceAccessType.Public
                                ? $"Instance Access Type: {instanceAccessType.TranslateInstanceType()}, Region: {networkRegion}, Player Count: {playerCount.ToInt32()}"
                                : $"Instance Creator: {_instanceCreator.displayName}, Instance Access Type: {instanceAccessType.TranslateInstanceType()}, Region: {networkRegion}, Player Count: {playerCount.ToInt32()}",
                            "warning", "warning", 5f, 175f, 1f, 0.3f);
                    }),
                new Action<ApiContainer>(container =>
                    VRCUiPopupManager.prop_VRCUiPopupManager_0.ShowAlert("Error Fetching World",
                        container.Error)));
        }

        _configurePortalDelegate(thisPtr, roomIdPtr, idWithTagsPtr, playerCount, instigatorPlayerPtr,
            nativeMethodInfoPtr);
    }

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void ConfigurePortalDelegate(IntPtr thisPtr, IntPtr roomIdPtr, IntPtr idWithTagsPtr,
        IntPtr playerCount, IntPtr instigatorPlayerPtr, IntPtr nativeMethodInfoPtr);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void AwakeDelegate(IntPtr thisPtr);
}