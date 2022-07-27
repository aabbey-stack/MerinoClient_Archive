using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Threading;
using UnhollowerBaseLib;

namespace MerinoClient.Features.Protection.AdvancedSafetyMod;

internal static class ReaderPatches
{
    // Why these numbers? Check wrld_b9f80349-74af-4840-8ce9-a1b783436590 for how *horribly* things break even on 10^6. Nothing belongs outside these bounds. The significand is that of MaxValue.
    private const float MaxAllowedValueTop = 3.402823E+7f;
    private const float MaxAllowedValueBottom = -3.402823E+7f;

    private static volatile AudioMixerReadDelegate _ourAudioMixerReadDelegate;

    private static FloatReadDelegate _ourFloatReadDelegate;

    private static readonly string[] OurAllowedFields =
        { "m_BreakForce", "m_BreakTorque", "collisionSphereDistance", "maxDistance", "inSlope", "outSlope" };

    private static readonly Dictionary<string, (int ProduceMixer, int TransferFloat, int CountNodes, int DebugAssert,
        int ReaderOOB, int ReallocateString, int TransferMonoObject, int TransferUEObjectSBR)> OurOffsets = new()
    {
        {
            "sgZUlX3+LSHKnTiTC+nXNcdtLOTrAB1fNjBLOwDdKzCyndlFLAdL0udR4S1szTC/q5pnFhG3Kdspsj5jvwLY1A==",
            (0xA86270, 0xC8230, 0xDF29F0, 0xDDBDC0, 0x7B9EB0, 0xC69F0, 0x8D1160, 0x8E5CD0)
        } // U2019.4.31 non-dev
    };

    private static ReaderOobDelegate _ourOriginalReaderOob;

    [ThreadStatic] private static int _ourReaderOobDepth;

    [ThreadStatic] private static Stack<IntPtr> _ourCurrentSafeTransferStack;
    private static TransferObjectSbrDelegate _ourOriginalTransferUeObjectSbr;

    private static TransferMonoObjectDelegate _ourOriginalTransferMonoObject;

    private static DebugAssertDelegate _ourOriginalAssert;

    private static StringReallocateDelegate _ourOriginalRealloc;

    [ThreadStatic] private static unsafe NativePatchUtils.NativeString* _ourLastReallocatedString;
    [ThreadStatic] private static int _ourLastReallocationCount;

    internal static void ApplyPatches()
    {
        string unityPlayerHash;
        {
            using var sha = SHA512.Create();
            using var unityPlayerStream = File.OpenRead("UnityPlayer.dll");
            unityPlayerHash = Convert.ToBase64String(sha.ComputeHash(unityPlayerStream));
        }

        if (!OurOffsets.TryGetValue(unityPlayerHash, out var offsets))
        {
            MerinoLogger.Error($"RP: Unknown UnityPlayer hash: {unityPlayerHash}, patches will not work");
            return;
        }

        foreach (ProcessModule module in Process.GetCurrentProcess().Modules)
        {
            if (!module.FileName.Contains("UnityPlayer")) continue;

            unsafe
            {
                // ProduceHelper<AudioMixer,0>::Produce, thanks to Ben for finding an adjacent method
#pragma warning disable CS0420
                DoPatch(module, offsets.ProduceMixer, AudioMixerReadPatch, out _ourAudioMixerReadDelegate);
#pragma warning restore CS0420

                // SafeBinaryRead::Transfer<float>
                DoPatch(module, offsets.TransferFloat, FloatTransferPatch, out _ourFloatReadDelegate);

                // CountNodesDeep, thanks to Requi and Ben for this and idea for next two
                DoPatch<CountNodesDeepDelegate>(module, offsets.CountNodes, CountNodesDeepThunk, out _);

                // DebugStringToFilePostprocessedStacktrace
                DoPatch(module, offsets.DebugAssert, DebugAssertPatch, out _ourOriginalAssert);

                // CachedReader::OutOfBoundsError
                DoPatch(module, offsets.ReaderOOB, ReaderOobPatch, out _ourOriginalReaderOob);

                // core::StringStorageDefault<char>::reallocate, identified to be an issue by Requi&Ben
                DoPatch(module, offsets.ReallocateString, ReallocateStringPatch, out _ourOriginalRealloc);

                // TransferPPtrToMonoObject
                DoPatch(module, offsets.TransferMonoObject, TransferMonoObjectPatch,
                    out _ourOriginalTransferMonoObject);

                // TransferField_NonArray<SafeBinaryRead,Converter_UnityEngineObject>
                DoPatch(module, offsets.TransferUEObjectSBR, TransferUeObjectSbrPatch,
                    out _ourOriginalTransferUeObjectSbr);
            }

            break;
        }
    }

    private static void DoPatch<T>(ProcessModule module, int offset, T patchDelegate, out T delegateField)
        where T : MulticastDelegate
    {
        delegateField = null;
        if (offset == 0) return;
        var targetPtr = module.BaseAddress + offset;

        NativePatchUtils.NativePatch(targetPtr, out delegateField, patchDelegate);
    }

    private static unsafe void FloatTransferPatch(IntPtr reader, float* result, byte* fieldName)
    {
        _ourFloatReadDelegate(reader, result, fieldName);

        if (AdvancedSafetyMod.CanReadBadFloats ||
            (*result > MaxAllowedValueBottom && *result < MaxAllowedValueTop)) return;

        if (float.IsNaN(*result)) goto clamp;

        if (fieldName != null)
            foreach (var allowedField in OurAllowedFields)
            {
                for (var j = 0; j < allowedField.Length; j++)
                    if (fieldName[j] == 0 || fieldName[j] != allowedField[j])
                        goto next;
                return;
                next: ;
            }

        clamp:

        *result = 0;
    }

    private static IntPtr AudioMixerReadPatch(IntPtr thisPtr, IntPtr readerPtr)
    {
        if (!AdvancedSafetyMod.CanReadAudioMixers) return IntPtr.Zero;

        // just in case something ever races
        while (_ourAudioMixerReadDelegate == null) Thread.Sleep(15);
        return _ourAudioMixerReadDelegate(thisPtr, readerPtr);
    }

    private static void ReaderOobPatch(IntPtr thisPtr, long a, long b)
    {
        _ourReaderOobDepth++;
        try
        {
            _ourOriginalReaderOob(thisPtr, a, b);
        }
        finally
        {
            _ourReaderOobDepth--;
        }
    }

    private static void TransferUeObjectSbrPatch(IntPtr staticInfo, IntPtr runtimeInfo, IntPtr converter)
    {
        _ourCurrentSafeTransferStack ??= new Stack<IntPtr>();
        _ourCurrentSafeTransferStack.Push(staticInfo);
        try
        {
            _ourOriginalTransferUeObjectSbr(staticInfo, runtimeInfo, converter);
        }
        finally
        {
            _ourCurrentSafeTransferStack.Pop();
        }
    }

    private static unsafe IntPtr TransferMonoObjectPatch(ref IntPtr hiddenThisReturn, IntPtr instanceId,
        IntPtr il2CppClass, IntPtr dataToCreateNull, IntPtr transferFlags)
    {
        var result = _ourOriginalTransferMonoObject(ref hiddenThisReturn, instanceId, il2CppClass, dataToCreateNull,
            transferFlags);

        if (hiddenThisReturn == IntPtr.Zero || _ourCurrentSafeTransferStack == null ||
            _ourCurrentSafeTransferStack.Count == 0) return result;

        var objectType = *(IntPtr*)hiddenThisReturn;

        var topStaticInfo = (StaticTransferInfoPrefix*)_ourCurrentSafeTransferStack.Peek();
        var staticFieldInfoPtr = topStaticInfo->field;

        var fieldType = IL2CPP.il2cpp_class_from_type(staticFieldInfoPtr->typePtr);
        if (IL2CPP.il2cpp_class_is_assignable_from(fieldType, objectType)) return result;

        hiddenThisReturn = IntPtr.Zero;

        return result;
    }

    private static unsafe void DebugAssertPatch(IntPtr data)
    {
        if (_ourReaderOobDepth > 0)
            *(byte*)(data + 0x30) &= 0xef;

        _ourOriginalAssert(data);
    }

    private static unsafe long CountNodesDeepThunk(NodeContainer* thisPtr)
    {
        try
        {
            return CountNodesDeepImpl(thisPtr, new HashSet<IntPtr>());
        }
        catch (Exception ex)
        {
            MerinoLogger.Error($"Exception in CountNodes patch: {ex}");
            return 1;
        }
    }

    private static unsafe long CountNodesDeepImpl(NodeContainer* thisPtr, HashSet<IntPtr> parents)
    {
        if (thisPtr == null) return 1;

        var directSubsCount = thisPtr->DirectSubCount;

        long totalNodes = 1;
        if (directSubsCount <= 0)
            return totalNodes;

        parents.Add((IntPtr)thisPtr);

        var subsBase = thisPtr->Subs;
        if (subsBase == null)
        {
            // Unlikely, but better be safe
            thisPtr->DirectSubCount = 0;
            return totalNodes;
        }

        for (var i = 0; i < directSubsCount; ++i)
        {
            var subNode = subsBase[i];

            if (subNode == null)
            {
                thisPtr->DirectSubCount = 0;
                return totalNodes;
            }

            if (parents.Contains((IntPtr)subNode))
            {
                subNode->DirectSubCount = thisPtr->DirectSubCount = 0;
                return totalNodes;
            }

            totalNodes += CountNodesDeepImpl(subNode, parents);
        }

        return totalNodes;
    }

    private static unsafe IntPtr ReallocateStringPatch(NativePatchUtils.NativeString* str, long newSize)
    {
        if (str != null && newSize > 128 && str->Data != IntPtr.Zero)
        {
            if (_ourLastReallocatedString != str)
            {
                _ourLastReallocatedString = str;
                _ourLastReallocationCount = 0;
            }
            else
            {
                _ourLastReallocationCount++;
                if (_ourLastReallocationCount >= 8 && newSize <= str->Capacity + 16 && str->Capacity > 16)
                    newSize = str->Capacity * 2;
            }
        }

        while (_ourOriginalRealloc == null) Thread.Sleep(15);
        return _ourOriginalRealloc(str, newSize);
    }

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate IntPtr AudioMixerReadDelegate(IntPtr thisPtr, IntPtr readerPtr);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private unsafe delegate void FloatReadDelegate(IntPtr readerPtr, float* result, byte* fieldName);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void ReaderOobDelegate(IntPtr thisPtr, long a, long b);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void TransferObjectSbrDelegate(IntPtr staticInfo, IntPtr runtimeInfo, IntPtr converter);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate IntPtr TransferMonoObjectDelegate(ref IntPtr hiddenThisReturn, IntPtr instanceId,
        IntPtr il2CppClass, IntPtr dataToCreateNull, IntPtr transferFlags);

    [StructLayout(LayoutKind.Sequential)]
    private unsafe struct StaticTransferInfoPrefix
    {
        public readonly Il2CppFieldInfo241* field;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct Il2CppFieldInfo241
    {
        private readonly IntPtr name; // const char*
        public readonly IntPtr typePtr; // const
        private readonly IntPtr parentClassPtr; // non-const?
        private readonly int offset; // If offset is -1, then it's thread static
        private readonly uint token;
    }


    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void DebugAssertDelegate(IntPtr data);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private unsafe delegate long CountNodesDeepDelegate(NodeContainer* thisPtr);

    [StructLayout(LayoutKind.Explicit, Size = 0x88)]
    private unsafe struct NodeContainer
    {
        [FieldOffset(0x70)] public readonly NodeContainer** Subs;
        [FieldOffset(0x80)] public long DirectSubCount;
    }

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private unsafe delegate IntPtr StringReallocateDelegate(NativePatchUtils.NativeString* str, long newSize);
}