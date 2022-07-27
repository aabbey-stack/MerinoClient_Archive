using System;
using System.Collections.Concurrent;
using System.Runtime.InteropServices;

namespace MerinoClient.Features.Protection.BundleVerifier;

internal static class BundleDlInterceptor
{
    internal static volatile bool ShouldIntercept = false;

    private static readonly ConcurrentDictionary<IntPtr, BundleDlContext> OurInterceptedContext = new();
    [ThreadStatic] private static byte[] _ourTransferBuffer;

    internal static unsafe void CreateCachedPatchPostfix(IntPtr thisPtr, NativePatchUtils.NativeString* urlPtr)
    {
        if (!ShouldIntercept) return;

        var url = BundleDownloadMethods.ExtractString(urlPtr);

        if (BundleVerifierMod.ForceAllowedCache.Contains(url)) return;
        OurInterceptedContext[thisPtr] = new BundleDlContext(thisPtr, url);
    }

    internal static int PreparePatch(IntPtr thisPtr)
    {
        if (OurInterceptedContext.TryGetValue(thisPtr, out var context) && context.IsBadUrl)
        {
            var cleanedUrl = BundleVerifierMod.SanitizeUrl(context.Url);
            MerinoLogger.Msg(
                $"Bundle for ptr {thisPtr} uid={cleanedUrl.Item1}+{cleanedUrl.Item2} is pre-marked as bad, faking failed download");
            // indicate that it should use the DL stream
            Marshal.WriteInt32(thisPtr + 0x90, 1);
            // and then indicate the download-er that we don't want the download
            return 1;
        }

        return BundleDownloadMethods.OriginalPrepare(thisPtr);
    }

    internal static IntPtr DestructorPatch(IntPtr thisPtr, long unk)
    {
        RemoveInterceptor(thisPtr);
        return BundleDownloadMethods.OriginalDestructor(thisPtr, unk);
    }

    internal static void CancelIntercept(BundleDlContext context)
    {
        RemoveInterceptor(context.OriginalBundleDownload);
    }

    private static void RemoveInterceptor(IntPtr thisPtr)
    {
        if (OurInterceptedContext.TryRemove(thisPtr, out var removed))
            removed.Dispose();
    }

    internal static unsafe int ReceivePatch(IntPtr thisPtr, IntPtr bytes, int byteCount)
    {
        if (!OurInterceptedContext.TryGetValue(thisPtr, out var intercepted))
            return BundleDownloadMethods.OriginalReceiveBytes(thisPtr, bytes, byteCount);

        if (!intercepted.PreProcessBytes())
        {
            RemoveInterceptor(thisPtr);
            return BundleDownloadMethods.OriginalReceiveBytes(thisPtr, bytes, byteCount);
        }

        if (_ourTransferBuffer == null || _ourTransferBuffer.Length < byteCount)
            _ourTransferBuffer = new byte[byteCount];
        Marshal.Copy(bytes, _ourTransferBuffer, 0, byteCount);
        var processedByteCount = intercepted.ProcessBytes(_ourTransferBuffer, 0, byteCount);

        var bundleOperationPtr = *(IntPtr*)(thisPtr + 0x80);
        if (bundleOperationPtr != IntPtr.Zero)
        {
            *(long*)(thisPtr + 0x40) = intercepted.GetDownloadedSize();
            *(float*)(bundleOperationPtr + 0x430) = intercepted.GetDownloadedSize() / (float)GetTotalSize(thisPtr);
        }

        return processedByteCount;
    }

    internal static unsafe int GetTotalSize(IntPtr thisPtr)
    {
        return *(int*)(thisPtr + 0x48);
    }

    internal static void CompletePatch(IntPtr thisPtr)
    {
        if (OurInterceptedContext.TryGetValue(thisPtr, out var intercepted))
        {
            intercepted.CompleteDownload();
            return;
        }

        BundleDownloadMethods.OriginalCompleteDownload(thisPtr);
    }
}