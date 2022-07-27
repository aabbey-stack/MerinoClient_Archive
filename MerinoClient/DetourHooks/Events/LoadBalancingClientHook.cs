using System.Runtime.InteropServices;
using System.Text;
using ExitGames.Client.Photon;
using Il2CppNewtonsoft.Json;
using Il2CppSystem;
using Il2CppSystem.Collections;
using Il2CppSystem.Collections.Generic;
using MerinoClient.Core.VRChat;
using MerinoClient.Features.QoL;
using Photon.Realtime;
using VRC.Core;
using DebugLevel = VRC.Core.DebugLevel;
using Exception = System.Exception;
using IntPtr = System.IntPtr;

namespace MerinoClient.DetourHooks.Events;

internal class LoadBalancingClientHook : DetourHookManager
{
    private static OnEventDelegate _onEventDelegate;
    private static RaiseEventDelegate _raiseEventDelegate;

    public LoadBalancingClientHook()
    {
        try
        {
            NativePatchUtils.NativePatch(
                typeof(LoadBalancingClient).GetMethod(nameof(LoadBalancingClient.OnEvent))!,
                out _onEventDelegate, OnEventHook);
        }
        catch (Exception e)
        {
            MerinoLogger.Error($"Failed to hook: OnEvent\n{e}");
        }

        try
        {
            NativePatchUtils.NativePatch(
                typeof(LoadBalancingClient).GetMethod(nameof(LoadBalancingClient
                    .Method_Public_Virtual_New_Boolean_Byte_Object_RaiseEventOptions_SendOptions_0))!,
                out _raiseEventDelegate, RaiseEventHook);
        }
        catch (Exception e)
        {
            MerinoLogger.Error($"Failed to hook: RaiseEvent\n{e}");
        }
    }

    public static Object AvatarDictCache { get; set; }

    private static IntPtr RaiseEventHook(IntPtr thisPtr, byte eventCode, IntPtr objectEventContentPtr,
        IntPtr raiseEventOptionsPtr, SendOptions sendOptions, IntPtr nativeMethodInfo)
    {
        switch (eventCode)
        {
            case 1:
            case 7:
            case 9:
                if (Config.AppearFrozen.Value) return IntPtr.Zero;
                break;
        }

        return _raiseEventDelegate(thisPtr, eventCode, objectEventContentPtr, raiseEventOptionsPtr, sendOptions,
            nativeMethodInfo);
    }

    private static void OnEventHook(IntPtr thisPtr, IntPtr eventDataPtr)
    {
        if (eventDataPtr == IntPtr.Zero)
        {
            _onEventDelegate(thisPtr, eventDataPtr);
            return;
        }

        try
        {
            var eventData = new EventData(eventDataPtr);

            HandlePhotonEventLog(eventData);

            switch (eventData.Code)
            {
                case 1:
                    if (Config.MuteEveryone.Value) return;
                    break;

                case 2:
                    var disconnectReason = eventData.Parameters[245].ToString();
                    MerinoLogger.Error($"You got disconnected from the server! Reason: {disconnectReason}");
                    return;

                case 7:
                    if (Config.FreezeEveryone.Value) return;
                    break;

                case 33:
                    var moderationDictionary = eventData.CustomData.TryCast<Dictionary<byte, Object>>();
                    if (!ModerationsHandler.ResolveModeration(moderationDictionary)) return;
                    break;

                //player properties updated
                case 42:
                    if (Config.LocalClone.Value)
                    {
                        var hashTable = eventData.Parameters[245].TryCast<Hashtable>();

                        if (AvatarDictCache == null)
                        {
                            _onEventDelegate(thisPtr, eventDataPtr);
                            return;
                        }

                        if (eventData.IsSelf())
                            hashTable["avatarDict"] = AvatarDictCache;
                    }

                    break;
            }
        }
        catch (Exception e)
        {
            MerinoLogger.Error("An exception occurred on OnEventHook:\n" + e);
        }


        _onEventDelegate(thisPtr, eventDataPtr);
    }

    private static void HandlePhotonEventLog(EventData eventData)
    {
        if (!Config.AdvancedPhotonLogging.Value) return;

        var sb = new StringBuilder();
        sb.Append($"OnEvent: {(eventData.Sender <= 0 ? "SYSTEM" : "PLAYER:  ")} {eventData.Code}\n");
        sb.Append(JsonConvert.SerializeObject(eventData, Formatting.Indented, default));
        Logger.Log(sb.ToString(), DebugLevel.NetworkData);
    }

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    // ReSharper disable once InconsistentNaming
    private delegate void OnEventDelegate(IntPtr thisPtr, IntPtr eventDataPtr);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate IntPtr RaiseEventDelegate(IntPtr thisPtr, byte eventCode, IntPtr objectEventContentPtr,
        IntPtr raiseEventOptionsPtr, SendOptions sendOptions, IntPtr nativeMethodInfo);
}