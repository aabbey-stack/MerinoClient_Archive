using System;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Text;
using MerinoClient.Features.Protection.BundleVerifier.RestrictedProcessRunner;

namespace MerinoClient.Features.Protection.BundleVerifier;

internal class BundleDlContext : IDisposable
{
    internal readonly bool IsBadUrl;
    internal readonly IntPtr OriginalBundleDownload;
    internal readonly string Url;

    private MemoryMappedFile _myMemoryMap;
    private BundleVerifierProcessHandle _myVerifierProcess;
    private MemoryMapWriterStream _myWriterStream;

    public BundleDlContext(IntPtr originalBundleDownload, string url)
    {
        OriginalBundleDownload = originalBundleDownload;
        Url = url;
        IsBadUrl = BundleVerifierMod.BadBundleCache?.Contains(url) == true;
    }


    public void Dispose()
    {
        _myVerifierProcess?.Dispose();
        _myWriterStream?.Dispose();
        _myMemoryMap?.Dispose();
    }

    internal bool PreProcessBytes()
    {
        if (_myMemoryMap != null) return true;

        var declaredSize = BundleDlInterceptor.GetTotalSize(OriginalBundleDownload);
        if (declaredSize <= 0) return false;

        try
        {
            var memName = "BundleVerifier-" + Guid.NewGuid();
            _myMemoryMap = MemoryMappedFile.CreateNew(memName, declaredSize + 8,
                MemoryMappedFileAccess.ReadWrite, MemoryMappedFileOptions.None, HandleInheritability.None);
            _myWriterStream = new MemoryMapWriterStream(_myMemoryMap);
            _myWriterStream.SetLength(declaredSize);


            _myVerifierProcess = new BundleVerifierProcessHandle(BundleVerifierMod.BundleVerifierPath, memName,
                TimeSpan.FromSeconds(BundleVerifierMod.TimeLimit.Value),
                (ulong)BundleVerifierMod.MemoryLimit.Value * 1024L * 1024L, 20,
                BundleVerifierMod.ComponentLimit.Value);
        }
        catch (Exception ex)
        {
            MerinoLogger.Error($"Error while initializing verifier internals: {ex}");
            return false;
        }

        return true;
    }

    internal int ProcessBytes(byte[] bytes, int offset, int length)
    {
        try
        {
            _myWriterStream.Write(bytes, offset, length);
        }
        catch (IOException ex)
        {
            MerinoLogger.Error(
                $"Received more bytes than declared for bundle URL {Url} (declared: {BundleDlInterceptor.GetTotalSize(OriginalBundleDownload)})");
            MerinoLogger.Error(ex.ToString());
            DoBackSpew();
            unsafe
            {
                fixed (byte* bytesPtr = bytes)
                {
                    BundleDownloadMethods.OriginalReceiveBytes(OriginalBundleDownload, (IntPtr)bytesPtr, length);
                }
            }

            BundleDlInterceptor.CancelIntercept(this);
        }

        return length;
    }

    internal long GetDownloadedSize()
    {
        return _myWriterStream.Position;
    }

    internal void CompleteDownload()
    {
        if (_myMemoryMap == null)
        {
            BundleDownloadMethods.OriginalCompleteDownload(OriginalBundleDownload);
            return;
        }

        var exitCode = _myVerifierProcess.WaitForExit(TimeSpan.FromSeconds(BundleVerifierMod.TimeLimit.Value));

        if (exitCode != 0)
        {
            var cleanedUrl = BundleVerifierMod.SanitizeUrl(Url);
            MerinoLogger.Warning(
                $"Bundle-verifier process failed with exit code {exitCode} ({VerifierExitCodes.GetExitCodeDescription(exitCode)}) for bundle uid={cleanedUrl.Item1}+{cleanedUrl.Item2}");
            BundleVerifierMod.BadBundleCache.Add(Url);
            // feed some garbage into it, otherwise it dies
            unsafe
            {
                *(long*)(OriginalBundleDownload + 0x40) = 0;
                var stub = "UnityFS\0";
                var bytes = Encoding.UTF8.GetBytes(stub);
                fixed (byte* bytesPtr = bytes)
                {
                    BundleDownloadMethods.OriginalReceiveBytes(OriginalBundleDownload, (IntPtr)bytesPtr, bytes.Length);
                }
            }

            BundleDownloadMethods.OriginalCompleteDownload(OriginalBundleDownload);
            return;
        }

        DoBackSpew();
        BundleDownloadMethods.OriginalCompleteDownload(OriginalBundleDownload);
    }

    private unsafe void DoBackSpew()
    {
        const int returnSizeStep = 65536;
        // reset it back to zero
        *(long*)(OriginalBundleDownload + 0x40) = 0;

        var rawPointer = _myWriterStream.GetPointer() + 8;
        var currentPosition = 0;
        var totalLength = (int)_myWriterStream.Length;

        while (currentPosition < totalLength)
        {
            var currentRead = Math.Min(returnSizeStep, totalLength - currentPosition);
            var bytesConsumed = BundleDownloadMethods.OriginalReceiveBytes(OriginalBundleDownload,
                (IntPtr)(rawPointer + currentPosition), currentRead);
            currentPosition += currentRead;

            if (bytesConsumed != currentRead)
                // The thing refused to eat our data?
                break;
        }

        _myWriterStream.ReleasePointer();
    }
}