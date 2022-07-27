using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;

namespace MerinoClient.Features.Protection.BundleVerifier;

internal static unsafe class BundleDownloadMethods
{
    private static readonly Dictionary<string, (int VTable, int CreateCached)> OurBundleDownloadOffsets = new()
    {
        {
            "sgZUlX3+LSHKnTiTC+nXNcdtLOTrAB1fNjBLOwDdKzCyndlFLAdL0udR4S1szTC/q5pnFhG3Kdspsj5jvwLY1A==",
            (0x147F158, 0x33ED30)
        } // U2019.4.31 non-dev
    };

    // TODO: integrate into NativePatchUtils?
    // ReSharper disable once CollectionNeverQueried.Local
    private static readonly List<Delegate> OurPinnedDelegates = new();

    private static AssetBundleDownloadHandlerVTablePrefix _ourOriginalVTable;

    private static CreateCachedDelegate _ourOriginalCreateCached;

    [DllImport("kernel32.dll")]
    private static extern bool VirtualProtect(IntPtr lpAddress, UIntPtr dwSize, uint flNewProtect,
        out uint lpflOldProtect);

    internal static bool Init()
    {
        string unityPlayerHash;
        {
            using var sha = SHA512.Create();
            using var unityPlayerStream = File.OpenRead("UnityPlayer.dll");
            unityPlayerHash = Convert.ToBase64String(sha.ComputeHash(unityPlayerStream));
        }

        if (!OurBundleDownloadOffsets.TryGetValue(unityPlayerHash, out var offsets))
        {
            MerinoLogger.Error($"BV: Unknown UnityPlayer hash: {unityPlayerHash}, Bundle verifier will not work");
            return false;
        }

        foreach (ProcessModule module in Process.GetCurrentProcess().Modules)
        {
            if (!module.FileName.Contains("UnityPlayer")) continue;

            var vTableAddress = module.BaseAddress + offsets.VTable;
            _ourOriginalVTable = *(AssetBundleDownloadHandlerVTablePrefix*)vTableAddress;
            var patchedTable = ApplyVTablePatches();
            VirtualProtect(vTableAddress, (UIntPtr)Marshal.SizeOf<AssetBundleDownloadHandlerVTablePrefix>(), 0x4,
                out var oldProtect);
            *(AssetBundleDownloadHandlerVTablePrefix*)vTableAddress = patchedTable;
            VirtualProtect(vTableAddress, (UIntPtr)Marshal.SizeOf<AssetBundleDownloadHandlerVTablePrefix>(), oldProtect,
                out oldProtect);

            var createCachedTarget = module.BaseAddress + offsets.CreateCached;
            NativePatchUtils.NativePatch(createCachedTarget, out _ourOriginalCreateCached, CreateCachedPatch);

            return true;
        }

        return false;
    }

    internal static string ExtractString(NativePatchUtils.NativeString* nativeString)
    {
        if (nativeString == null) return null;
        if (nativeString->Length == 0) return "";

        var charsPointer = nativeString->Data;
        if (charsPointer == IntPtr.Zero)
            charsPointer = (IntPtr)(&nativeString->Capacity);

        return new string((sbyte*)charsPointer, 0, (int)nativeString->Length, Encoding.UTF8);
    }

    private static IntPtr CreateCachedPatch(IntPtr scriptingObjectPtr, NativePatchUtils.NativeString* url,
        NativePatchUtils.NativeString* name, IntPtr hash, int crc)
    {
        var result = _ourOriginalCreateCached(scriptingObjectPtr, url, name, hash, crc);
        try
        {
            BundleDlInterceptor.CreateCachedPatchPostfix(result, url);
        }
        catch (Exception ex)
        {
            MerinoLogger.Error($"Exception in CreateCached patch: {ex}");
        }

        return result;
    }

    private static IntPtr GetDelegatePointerAndPin<T>(T input) where T : MulticastDelegate
    {
        OurPinnedDelegates.Add(input);
        return Marshal.GetFunctionPointerForDelegate(input);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static AssetBundleDownloadHandlerVTablePrefix ApplyVTablePatches()
    {
        var patchedTable = _ourOriginalVTable;
        patchedTable.Prepare =
            (delegate*unmanaged[Cdecl]<IntPtr, int>)GetDelegatePointerAndPin<PrepareDelegate>(BundleDlInterceptor
                .PreparePatch);
        patchedTable.Destructor =
            (delegate*unmanaged[Cdecl]<IntPtr, long, IntPtr>)GetDelegatePointerAndPin<DtorDelegate>(BundleDlInterceptor
                .DestructorPatch);
        patchedTable.OnCompleteContent =
            (delegate*unmanaged[Cdecl]<IntPtr, void>)GetDelegatePointerAndPin<VoidDelegate>(BundleDlInterceptor
                .CompletePatch);
        patchedTable.OnReceiveData =
            (delegate*unmanaged[Cdecl]<IntPtr, IntPtr, int, int>)GetDelegatePointerAndPin<ReceiveDelegate>(
                BundleDlInterceptor.ReceivePatch);
        return patchedTable;
    }

    // returns number of bytes read
    internal static int OriginalReceiveBytes(IntPtr assetBundleDownload, IntPtr bytesPtr, int byteCount)
    {
        return _ourOriginalVTable.OnReceiveData(assetBundleDownload, bytesPtr, byteCount);
    }

    internal static void OriginalCompleteDownload(IntPtr assetBundleDownload)
    {
        _ourOriginalVTable.OnCompleteContent(assetBundleDownload);
    }

    internal static IntPtr OriginalDestructor(IntPtr assetBundleDownload, long unk)
    {
        return _ourOriginalVTable.Destructor(assetBundleDownload, unk);
    }

    internal static int OriginalPrepare(IntPtr assetBundleDownload)
    {
        return _ourOriginalVTable.Prepare(assetBundleDownload);
    }

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate IntPtr CreateCachedDelegate(IntPtr thisPtr, NativePatchUtils.NativeString* url,
        NativePatchUtils.NativeString* name, IntPtr hash, int crc);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate int PrepareDelegate(IntPtr thisPtr);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void VoidDelegate(IntPtr thisPtr);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate IntPtr DtorDelegate(IntPtr thisPtr, long unk);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate int ReceiveDelegate(IntPtr thisPtr, IntPtr bytes, int length);

    // mono no likey delegate*unmanaged fields
    [StructLayout(LayoutKind.Sequential, Pack = 1, Size = 8 * 14)]
    private struct AssetBundleDownloadHandlerVTablePrefix
    {
        private IntPtr DestructorValue;
        private readonly IntPtr unknown1;
        private readonly IntPtr unknown2;
        private IntPtr OnReceiveDataValue;
        private readonly IntPtr unknown3;
        private readonly IntPtr unknown4;
        private IntPtr OnCompleteContentValue;
        private readonly IntPtr unknown5;
        private readonly IntPtr unknown6;
        private readonly IntPtr GetMemorySize2Value;
        private readonly IntPtr GetMemorySize1Value;
        private readonly IntPtr GetProgressValue;
        private IntPtr PrepareValue;
        private readonly IntPtr OnAbortValue;

        public delegate* unmanaged[Cdecl]<IntPtr, long, IntPtr> Destructor
        {
            get => (delegate* unmanaged[Cdecl]<IntPtr, long, IntPtr>)DestructorValue;
            set => DestructorValue = (IntPtr)value;
        }

        public delegate* unmanaged[Cdecl]<IntPtr, IntPtr, int, int> OnReceiveData
        {
            get => (delegate* unmanaged[Cdecl]<IntPtr, IntPtr, int, int>)OnReceiveDataValue;
            set => OnReceiveDataValue = (IntPtr)value;
        }

        public delegate* unmanaged[Cdecl]<IntPtr, void> OnCompleteContent
        {
            get => (delegate* unmanaged[Cdecl]<IntPtr, void>)OnCompleteContentValue;
            set => OnCompleteContentValue = (IntPtr)value;
        }

        public delegate* unmanaged[Cdecl]<IntPtr, int> Prepare
        {
            get => (delegate* unmanaged[Cdecl]<IntPtr, int>)PrepareValue;
            set => PrepareValue = (IntPtr)value;
        }
    }
}