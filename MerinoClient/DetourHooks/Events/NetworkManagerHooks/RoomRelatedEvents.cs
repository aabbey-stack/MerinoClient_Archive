using System;
using System.Runtime.InteropServices;
using MerinoClient.Core.VRChat;
using MerinoClient.Features.Protection.BundleVerifier;
using MerinoClient.Utilities;
using UnityEngine;
using VRC.Core;
using DisconnectCause = EnumPublicSealedvaNoExDnSeClExSeDiInUnique;

// ReSharper disable AccessToModifiedClosure
// ReSharper disable PossibleNullReferenceException

namespace MerinoClient.DetourHooks.Events.NetworkManagerHooks;

internal class RoomRelatedEvents : DetourHookManager
{
    public static bool IsInRoom;

    private static OnLeftRoomDelegate _onLeftRoomDelegate;
    private static OnJoinedRoomDelegate _onJoinedRoomDelegate;
    private static OnConnectionFailDelegate _onConnectionFailDelegate;

    public RoomRelatedEvents()
    {
        try
        {
            NativePatchUtils.NativePatch(
                typeof(NetworkManager).GetMethod(nameof(NetworkManager.OnLeftRoom))!,
                out _onLeftRoomDelegate, OnLeftRoomHook);

            void OnLeftRoomHook(IntPtr thisPtr)
            {
                IsInRoom = false;
                BundleVerifierMod.OnLeftRoom();
                _onLeftRoomDelegate(thisPtr);
            }
        }
        catch (Exception e)
        {
            MerinoLogger.Error($"Failed to hook: OnLeftRoom\n{e}");
        }

        try
        {
            NativePatchUtils.NativePatch(
                typeof(NetworkManager).GetMethod(nameof(NetworkManager.OnJoinedRoom))!,
                out _onJoinedRoomDelegate, OnJoinedRoomHook);

            void OnJoinedRoomHook(IntPtr thisPtr)
            {
                var i = RoomManager.field_Internal_Static_ApiWorldInstance_0;

                MerinoLogger.Msg(
                    $"Entered {RoomManager.field_Internal_Static_ApiWorld_0.name} #{i.ParseOnlyId()} {i.type.TranslateInstanceType()}");

                PhotonUtils.CheckAllPhotonPlayers();

                GameObject.Find("UserInterface/MenuContent/Screens/Settings/ComfortSafetyPanel/AllowUntrustedURL")
                    .GetComponent<UiSettingConfig>().SetEnable(!IsPublic(i));

                IsInRoom = true;
                BundleVerifierMod.OnJoinedRoom();
                _onJoinedRoomDelegate(thisPtr);
            }
        }
        catch (Exception e)
        {
            MerinoLogger.Error($"Failed to hook: OnJoinedRoom\n{e}");
        }

        try
        {
            NativePatchUtils.NativePatch(
                typeof(NetworkManager).GetMethod(nameof(NetworkManager
                    .Method_Public_Virtual_Final_New_Void_EnumPublicSealedvaNoExDnSeClExSeDiInUnique_0))!,
                out _onConnectionFailDelegate, OnConnectionFailHook);

            void OnConnectionFailHook(IntPtr thisPtr, DisconnectCause cause)
            {
                MerinoLogger.Error($"OnConnectionFail: {cause}");
                _onConnectionFailDelegate(thisPtr, cause);
            }
        }
        catch (Exception e)
        {
            MerinoLogger.Error($"Failed to hook: OnConnectionFail\n{e}");
        }
    }

    private static bool IsPublic(ApiWorldInstance instance)
    {
        return instance.type == InstanceAccessType.Public;
    }

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void OnJoinedRoomDelegate(IntPtr thisPtr);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void OnLeftRoomDelegate(IntPtr thisPtr);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void OnConnectionFailDelegate(IntPtr thisPtr, DisconnectCause cause);
}