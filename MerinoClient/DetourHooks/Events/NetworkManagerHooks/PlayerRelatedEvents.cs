using System;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using MerinoClient.Core;
using MerinoClient.Core.VRChat;
using MerinoClient.Features.QoL;
using MerinoClient.Features.QoL.UI;
using MerinoClient.Utilities;
using Photon.Realtime;
using UnityEngine;
using VRC.Core;

// ReSharper disable AccessToModifiedClosure
// ReSharper disable PossibleNullReferenceException

namespace MerinoClient.DetourHooks.Events.NetworkManagerHooks;

internal class PlayerRelatedEvents : DetourHookManager
{
    private static OnPlayerJoinedDelegate _onPlayerJoinedDelegate;
    private static OnPlayerLeftDelegate _onPlayerLeftDelegate;

    private static OnPlayerLeftRoomDelegate _onPlayerLeftRoomDelegate;
    private static OnPlayerEnteredRoomDelegate _onPlayerEnteredRoomDelegate;

    private readonly MethodInfo _onPlayerEnteredRoomMethod;

    private readonly MethodInfo _onPlayerJoinedMethod;
    private readonly MethodInfo _onPlayerLeftMethod;
    private readonly MethodInfo _onPlayerLeftRoomMethod;

    public PlayerRelatedEvents()
    {
        try
        {
            try
            {
                _onPlayerJoinedMethod = typeof(NetworkManager).GetMethods()
                    .First(mb =>
                        mb.Name.StartsWith("Method_Public_Void_Player_") &&
                        XrefUtils.CheckMethod(mb, "OnPlayerJoined {0}"));
            }
            catch (Exception e)
            {
                MerinoLogger.Error($"{GetType().Name} onPlayerJoinedMethod reflection exception: {e}");
            }

            if (_onPlayerJoinedMethod is null) return;

            NativePatchUtils.NativePatch(
                typeof(NetworkManager).GetMethod(_onPlayerJoinedMethod.Name)!,
                out _onPlayerJoinedDelegate, OnPlayerJoinedHook);
        }
        catch (Exception e)
        {
            MerinoLogger.Error($"Failed to hook: OnPlayerJoined\n{e}");
        }

        try
        {
            try
            {
                _onPlayerLeftMethod = typeof(NetworkManager).GetMethods()
                    .First(mb =>
                        mb.Name.StartsWith("Method_Public_Void_Player_") &&
                        XrefUtils.CheckMethod(mb, "OnPlayerLeft {0}"));
            }
            catch (Exception e)
            {
                MerinoLogger.Error($"{GetType().Name} onPlayerLeftMethod reflection exception: {e}");
            }

            if (_onPlayerLeftMethod is null) return;

            NativePatchUtils.NativePatch(
                typeof(NetworkManager).GetMethod(_onPlayerLeftMethod.Name)!,
                out _onPlayerLeftDelegate, OnPlayerLeftHook);
        }
        catch (Exception e)
        {
            MerinoLogger.Error($"Failed to hook: OnPlayerLeft\n{e}");
        }

        try
        {
            try
            {
                _onPlayerLeftRoomMethod = typeof(NetworkManager).GetMethods()
                    .First(mb =>
                        mb.Name.StartsWith("Method_Public_Virtual_Final_New_Void_Player_") &&
                        XrefUtils.CheckMethod(mb, "OnPlayerLeftRoom"));
            }
            catch (Exception e)
            {
                MerinoLogger.Error($"{GetType().Name} onPlayerLeftRoomMethod reflection exception: {e}");
            }

            if (_onPlayerLeftRoomMethod is null) return;

            NativePatchUtils.NativePatch(
                typeof(NetworkManager).GetMethod(_onPlayerLeftRoomMethod.Name)!,
                out _onPlayerLeftRoomDelegate, OnPlayerLeftRoomHook);

            void OnPlayerLeftRoomHook(IntPtr thisPtr, IntPtr playerPtr)
            {
                if (playerPtr == IntPtr.Zero)
                {
                    _onPlayerLeftRoomDelegate(thisPtr, playerPtr);
                    return;
                }

                var photonPlayer = new Player(playerPtr);

                if ((Config.LogPlayerEntries.Value || Config.LogOnlyPhotonEntries.Value) &&
                    RoomRelatedEvents.IsInRoom)
                {
                    MerinoLogger.Msg(ConsoleColor.Cyan,
                        $"{photonPlayer.GetPlayerType()} {photonPlayer.GetActorNumber()}: {photonPlayer.GetDisplayName()} has left the room");
                    PhotonUtils.CheckPhotonPlayer(photonPlayer);
                }

                _onPlayerLeftRoomDelegate(thisPtr, playerPtr);
            }
        }
        catch (Exception e)
        {
            MerinoLogger.Error($"Failed to hook: OnPlayerLeftRoom\n{e}");
        }

        try
        {
            try
            {
                _onPlayerEnteredRoomMethod = typeof(NetworkManager).GetMethods()
                    .First(mb =>
                        mb.Name.StartsWith("Method_Public_Virtual_Final_New_Void_Player_") &&
                        XrefUtils.CheckMethod(mb, "OnPlayerEnteredRoom"));
            }
            catch (Exception e)
            {
                MerinoLogger.Error($"{GetType().Name} onPlayerEnteredRoomMethod reflection exception: {e}");
            }

            if (_onPlayerEnteredRoomMethod is null) return;

            NativePatchUtils.NativePatch(
                typeof(NetworkManager).GetMethod(_onPlayerEnteredRoomMethod.Name)!,
                out _onPlayerEnteredRoomDelegate, OnPlayerEnteredRoomHook);

            void OnPlayerEnteredRoomHook(IntPtr thisPtr, IntPtr playerPtr)
            {
                if (playerPtr == IntPtr.Zero)
                {
                    _onPlayerEnteredRoomDelegate(thisPtr, playerPtr);
                    return;
                }

                var photonPlayer = new Player(playerPtr);

                if ((Config.LogPlayerEntries.Value || Config.LogOnlyPhotonEntries.Value) &&
                    RoomRelatedEvents.IsInRoom)
                {
                    MerinoLogger.Msg(ConsoleColor.Cyan,
                        $"{photonPlayer.GetPlayerType()} {photonPlayer.GetActorNumber()}: {photonPlayer.GetDisplayName()} has joined the room");
                    PhotonUtils.CheckPhotonPlayer(photonPlayer);
                }

                _onPlayerEnteredRoomDelegate(thisPtr, playerPtr);
            }
        }
        catch (Exception e)
        {
            MerinoLogger.Error($"Failed to hook: OnPlayerEnteredRoom\n{e}");
        }
    }

    private static void OnPlayerJoinedHook(IntPtr thisPtr, IntPtr playerPtr)
    {
        if (playerPtr == IntPtr.Zero)
        {
            _onPlayerJoinedDelegate(thisPtr, playerPtr);
            return;
        }

        var player = new VRC.Player(playerPtr);

        if (player == VRC.Player.prop_Player_0)
        {
            _onPlayerJoinedDelegate(thisPtr, playerPtr);
            return;
        }

        var apiUser = player.GetAPIUser();

        if (apiUser == null)
        {
            _onPlayerJoinedDelegate(thisPtr, playerPtr);
            return;
        }

        if (Config.LogPlayerEntries.Value && !Config.LogOnlyPhotonEntries.Value)
        {
            MerinoLogger.Msg($"{apiUser.GetAPIUserType()}: {apiUser.displayName} has joined");

            if (!Config.Notifications.Value)
            {
                _onPlayerJoinedDelegate(thisPtr, playerPtr);
                return;
            }

            if (APIUser.IsFriendsWith(apiUser.id))
                if (!XSNotificationsLite.SendNotification(
                        $"{apiUser.GetAPIUserType()}: {apiUser.displayName} has joined the lobby",
                        null))
                    VRCUiManager.prop_VRCUiManager_0.QueueHudMessage(
                        $"{apiUser.GetAPIUserType()}: {apiUser.displayName} has joined the lobby",
                        Color.white);
        }

        Highlights.HighlightPlayer(player, Config.Highlights.Value);

        _onPlayerJoinedDelegate(thisPtr, playerPtr);
    }

    private static void OnPlayerLeftHook(IntPtr thisPtr, IntPtr playerPtr)
    {
        if (playerPtr == IntPtr.Zero)
        {
            _onPlayerLeftDelegate(thisPtr, playerPtr);
            return;
        }

        var player = new VRC.Player(playerPtr);

        if (player == VRC.Player.prop_Player_0)
        {
            _onPlayerLeftDelegate(thisPtr, playerPtr);
            return;
        }

        var apiUser = player.GetAPIUser();

        if (apiUser == null)
        {
            _onPlayerLeftDelegate(thisPtr, playerPtr);
            return;
        }

        if (Config.LogPlayerEntries.Value && !Config.LogOnlyPhotonEntries.Value && RoomRelatedEvents.IsInRoom)
        {
            MerinoLogger.Msg($"[World] {apiUser.GetAPIUserType()}: {apiUser.displayName} has left");

            if (!Config.Notifications.Value)
            {
                _onPlayerLeftDelegate(thisPtr, playerPtr);
                return;
            }

            if (APIUser.IsFriendsWith(apiUser.id))
                if (!XSNotificationsLite.SendNotification(
                        $"{apiUser.GetAPIUserType()}: {apiUser.displayName} has left the lobby",
                        null))
                    VRCUiManager.prop_VRCUiManager_0.QueueHudMessage(
                        $"{apiUser.GetAPIUserType()}: {apiUser.displayName} has left the lobby",
                        Color.white);
        }

        _onPlayerLeftDelegate(thisPtr, playerPtr);
    }

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void OnPlayerJoinedDelegate(IntPtr thisPtr, IntPtr player);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void OnPlayerLeftDelegate(IntPtr thisPtr, IntPtr player);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void OnPlayerLeftRoomDelegate(IntPtr thisPtr, IntPtr player);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void OnPlayerEnteredRoomDelegate(IntPtr thisPtr, IntPtr player);
}