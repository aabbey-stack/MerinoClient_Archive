using System;
using System.Runtime.InteropServices;
using MerinoClient.Core.Unity;
using UnityEngine;
using VRC.SDKBase;

// ReSharper disable PossibleNullReferenceException
// ReSharper disable AccessToModifiedClosure

namespace MerinoClient.DetourHooks.Protection;

internal class NetworkingHook : DetourHookManager
{
    private static SetOwnerDelegate _setOwnerDelegate;

    public NetworkingHook()
    {
        try
        {
            NativePatchUtils.NativePatch(
                typeof(Networking).GetMethod(nameof(Networking.SetOwner))!,
                out _setOwnerDelegate, SetOwnerHook);

            void SetOwnerHook(IntPtr vrcPlayerApiPtr, IntPtr gameObjectPtr)
            {
                var gameObject = new GameObject(gameObjectPtr);

                if (!gameObject.transform.position.IsSafe() ||
                    !gameObject.transform.rotation.eulerAngles.IsSafe()) return;

                _setOwnerDelegate(vrcPlayerApiPtr, gameObjectPtr);
            }
        }
        catch (Exception e)
        {
            MerinoLogger.Error($"Failed to hook: SetOwner\n{e}");
        }
    }


    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void SetOwnerDelegate(IntPtr vrcPlayerApiPtr, IntPtr gameObjectPtr);
}