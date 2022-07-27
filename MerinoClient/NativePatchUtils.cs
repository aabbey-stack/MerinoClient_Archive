using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;
using HarmonyLib;
using MelonLoader;
using UnhollowerBaseLib;

namespace MerinoClient;

/*
 * All credits go to knah for this improved native hooking "library": https://github.com/knah/VRCMods/tree/93157d243bd8fac6e4a166f7fdddc201e13c94ee/Common
 */

internal static class NativePatchUtils
{
    /*extra news for those of you who use native hooks via Import.Hook instead of harmony: native methods have an extra IntPtr argument at the end of parameter list.
     It's some native junk you don't need to worry about (in fact, most of the time it's exactly zero), but you need to pass it to the original when calling it, 
    otherwise you have a chance to corrupt the stack, consequences of which are completely random, from nothing happening to game crashing.
    If your patch method had three or less total parameters (say, MyPatch(IntPtr, IntPtr, float), you're fine for now - first 4 parameters don't touch the stack. 
    If it had 4 or more parameters, it can break.
    In case of DynamicBoneSafety, the native delegate type should look like this: private delegate bool 
    AvatarAttachDelegate(IntPtr thisPtr, IntPtr gameObjectPtr, IntPtr someString, float scale, IntPtr nativeMethodInfo);
    Oh, also, on the same topic, if you have bool in fifth or later parameter, you must use byte instead of it*/

    // ReSharper disable once CollectionNeverQueried.Local
    private static readonly List<Delegate> OurPinnedDelegates = new();
    private static readonly Dictionary<IntPtr, IntPtr> OurOriginalPointers = new();

    internal static void NativePatch<T>(MethodInfo original, out T callOriginal, MethodInfo patch)
        where T : MulticastDelegate
    {
        var patchDelegate = (T)Delegate.CreateDelegate(typeof(T), patch);
        NativePatch(original, out callOriginal, patchDelegate);
    }

    internal static void NativePatch<T>(IntPtr originalPointer, out T callOriginal, MethodInfo patch,
        string context = null)
        where T : MulticastDelegate
    {
        var patchDelegate = (T)Delegate.CreateDelegate(typeof(T), patch);
        NativePatch(originalPointer, out callOriginal, patchDelegate, context);
    }

    internal static unsafe void NativePatch<T>(MethodInfo original, out T callOriginal, T patchDelegate)
        where T : MulticastDelegate
    {
        if (original == null) throw new ArgumentNullException(nameof(original));

        var originalPointer =
            *(IntPtr*)(IntPtr)UnhollowerUtils.GetIl2CppMethodInfoPointerFieldForGeneratedMethod(original)
                .GetValue(null);
        NativePatch(originalPointer, out callOriginal, patchDelegate, original.FullDescription());
    }

    internal static unsafe void NativePatch<T>(IntPtr originalPointer, out T callOriginal, T patchDelegate,
        string context = null) where T : MulticastDelegate
    {
        OurPinnedDelegates.Add(patchDelegate);

        var patchPointer = Marshal.GetFunctionPointerForDelegate(patchDelegate);
        MelonUtils.NativeHookAttach((IntPtr)(&originalPointer), patchPointer);
        if (OurOriginalPointers.ContainsKey(originalPointer))
            MerinoLogger.Warning(
                $"Method {context ?? patchDelegate.Method.FullDescription()} has multiple native patches within single mod. Bug?");
        OurOriginalPointers[originalPointer] = patchPointer;
        callOriginal = Marshal.GetDelegateForFunctionPointer<T>(originalPointer);
    }

    internal static unsafe void UnpatchAll()
    {
        foreach (var keyValuePair in OurOriginalPointers)
        {
            var pointer = keyValuePair.Key;
            MelonUtils.NativeHookDetach((IntPtr)(&pointer), keyValuePair.Value);
        }

        OurOriginalPointers.Clear();
        OurPinnedDelegates.Clear();
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct NativeString
    {
        public IntPtr Data;
        public long Capacity;
        public long Unknown;
        public long Length;
        public int Unknown2;
    }
}