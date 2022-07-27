using System;
using System.Diagnostics;

namespace MerinoClient.Features.Protection.BundleVerifier.RestrictedProcessRunner;

internal sealed class BundleVerifierProcessHandle : IDisposable
{
    private readonly RestrictedProcessHandle _myProcessHandle;

    public BundleVerifierProcessHandle(string executablePath, string sharedMemoryName, TimeSpan maxTime,
        ulong maxMemory, int minFps, int maxComponents)
    {
        var pid = Process.GetCurrentProcess().Id;

        _myProcessHandle = new RestrictedProcessHandle(executablePath,
            $"-batchmode -nolog -nographics {maxComponents} {pid} {sharedMemoryName} {minFps}");

        _myProcessHandle.SetLimits(maxTime, maxMemory, false, false, false);
        _myProcessHandle.Start();
    }

    public void Dispose()
    {
        _myProcessHandle.Dispose();
    }

    public int? WaitForExit(TimeSpan timeout)
    {
        return _myProcessHandle.WaitForExit(timeout);
    }
}