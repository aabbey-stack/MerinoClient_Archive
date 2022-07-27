using System;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using UnhollowerBaseLib;
using UnityEngine;
using VRC.SDKBase;
using static VRC.SDKBase.VRC_EventHandler;
using String = Il2CppSystem.String;

// ReSharper disable AccessToModifiedClosure

namespace MerinoClient.DetourHooks.Events;

internal class VRCEventDispatcherRFCHook : DetourHookManager
{
    private static SendRPCDelegate _sendRPCDelegate;
    private static MethodInfo _sendRPCMethod;

    public VRCEventDispatcherRFCHook()
    {
        try
        {
            _sendRPCMethod = typeof(VRC_EventDispatcherRFC).GetMethods().FirstOrDefault(m =>
                m.Name.Contains("Method_Private_Void_Int32_VrcTargetType_GameObject_String_ArrayOf_Byte_"));
        }
        catch (Exception e)
        {
            MerinoLogger.Error($"{GetType().Name} sendRPCMethod reflection exception: {e}");
        }

        if (_sendRPCMethod is null) return;

        try
        {
            NativePatchUtils.NativePatch(_sendRPCMethod!,
                out _sendRPCDelegate, SendRPCHook);
        }
        catch (Exception e)
        {
            MerinoLogger.Error($"Failed to hook: SendRPC\n{e}");
        }
    }

    // public unsafe void SendRPC(int Instigator, VRC_EventHandler.VrcTargetType targetType, GameObject target, string rpcMethodName, Il2CppStructArray<byte> parameters)
    private static void SendRPCHook(IntPtr thisPtr, IntPtr instigator, VrcTargetType targetType, IntPtr target,
        IntPtr rpcMethodName, IntPtr parameters, IntPtr nativeMethodInfo)
    {
        if (parameters == IntPtr.Zero)
        {
            _sendRPCDelegate(thisPtr, instigator, targetType, target, rpcMethodName, parameters, nativeMethodInfo);
            return;
        }

        var playerById = VRCPlayerApi.GetPlayerById(instigator.ToInt32());
        var gameObjectTarget = new GameObject(target);
        var rpcMethod = (string)new String(rpcMethodName);
        var byteParams = new Il2CppStructArray<byte>(parameters);

        if (Config.LogRPCs.Value && !string.IsNullOrEmpty(rpcMethod))
            switch (playerById.isLocal)
            {
                case true:
                    MerinoLogger.Msg(
                        $"[RPC] Local player sent {targetType} {rpcMethod} to {gameObjectTarget.name}, parameters: {Convert.ToBase64String(byteParams)}");
                    break;

                case false:
                    MerinoLogger.Msg(
                        $"[RPC] Player: {playerById.displayName} sent {targetType} {rpcMethod} to {gameObjectTarget.name}, parameters: {Convert.ToBase64String(byteParams)}");
                    break;
            }

        _sendRPCDelegate(thisPtr, instigator, targetType, target, rpcMethodName, parameters,
            nativeMethodInfo);
    }

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void SendRPCDelegate(IntPtr thisPtr, IntPtr instigator, VrcTargetType targetType,
        IntPtr target, IntPtr rpcMethodName, IntPtr parameters, IntPtr nativeMethodInfo);
}