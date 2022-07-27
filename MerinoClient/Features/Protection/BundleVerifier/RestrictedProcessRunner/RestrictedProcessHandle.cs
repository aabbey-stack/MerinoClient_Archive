using System;
using MerinoClient.Features.Protection.BundleVerifier.RestrictedProcessRunner.Interop;

namespace MerinoClient.Features.Protection.BundleVerifier.RestrictedProcessRunner;

internal sealed class RestrictedProcessHandle : IDisposable
{
    private readonly JobHandle _myJobHandle;
    private readonly ProcessHandle _myProcessHandle;

    public RestrictedProcessHandle(string processPath, string commandline)
    {
        _myJobHandle = new JobHandle();

        try
        {
            var cli = $"\"{processPath.Replace("\"", "")}\" {commandline}";
            _myProcessHandle = new ProcessHandle(cli, _myJobHandle);
        }
        catch
        {
            _myJobHandle.Dispose();
            throw;
        }
    }

    public void Dispose()
    {
        _myJobHandle.Dispose();
        _myProcessHandle.Dispose();
    }

    public int? WaitForExit(TimeSpan timeout)
    {
        return _myProcessHandle.WaitForExit(timeout);
    }

    public void SetLimits(TimeSpan? cpuTimeLimit, ulong? memoryBytes, bool allowNetwork, bool allowDesktop,
        bool allowChildProcesses)
    {
        _myJobHandle.SetLimits(cpuTimeLimit, memoryBytes, allowNetwork, allowDesktop, allowChildProcesses);
    }

    public void Start()
    {
        _myProcessHandle.Start();
    }
}