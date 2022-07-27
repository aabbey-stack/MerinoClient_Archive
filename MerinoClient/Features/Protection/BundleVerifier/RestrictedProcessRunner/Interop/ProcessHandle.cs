using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;

namespace MerinoClient.Features.Protection.BundleVerifier.RestrictedProcessRunner.Interop;

internal sealed class ProcessHandle : IDisposable
{
    private int _myIsStarted;
    private IntPtr _myProcessHandle;
    private IntPtr _myThreadHandle;

    private ProcessHandle(string commandLine)
    {
        InteropMethods.SECURITY_ATTRIBUTES empty = default;
        var startupInfo = new InteropMethods.STARTUPINFO
        {
            cb = Marshal.SizeOf<InteropMethods.STARTUPINFO>()
        };
        var created = InteropMethods.CreateProcess(null, commandLine, ref empty, ref empty, false,
            InteropMethods.ProcessCreationFlags.CREATE_NO_WINDOW |
            InteropMethods.ProcessCreationFlags.CREATE_SUSPENDED,
            IntPtr.Zero, null, ref startupInfo, out var processInfo);
        if (!created)
            throw new ArgumentException("Can't create process");

        _myProcessHandle = processInfo.hProcess;
        _myThreadHandle = processInfo.hThread;
    }

    public ProcessHandle(string commandLine, JobHandle job) : this(commandLine)
    {
        InteropMethods.AssignProcessToJobObject(job.Handle, _myProcessHandle);
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    public void Start()
    {
        if (Interlocked.CompareExchange(ref _myIsStarted, 1, 0) == 0)
            InteropMethods.ResumeThread(_myThreadHandle);
        else
            throw new ApplicationException("Already started");
    }

    public int? WaitForExit(TimeSpan timeout)
    {
        var waitTime = Stopwatch.StartNew();
        do
        {
            InteropMethods.GetExitCodeProcess(_myProcessHandle, out var exitCode);
            if (exitCode != InteropMethods.STILL_ACTIVE)
                return exitCode;
            Thread.Sleep(33);
        } while (waitTime.Elapsed < timeout);

        return null;
    }

    public int? GetExitCode()
    {
        InteropMethods.GetExitCodeProcess(_myProcessHandle, out var exitCode);
        if (exitCode != InteropMethods.STILL_ACTIVE)
            return exitCode;
        return null;
    }

    private void ReleaseUnmanagedResources()
    {
        if (_myProcessHandle != IntPtr.Zero) InteropMethods.CloseHandle(_myProcessHandle);
        _myProcessHandle = IntPtr.Zero;

        if (_myThreadHandle != IntPtr.Zero) InteropMethods.CloseHandle(_myThreadHandle);
        _myThreadHandle = IntPtr.Zero;
    }

    // ReSharper disable once UnusedParameter.Local
    private void Dispose(bool disposing)
    {
        ReleaseUnmanagedResources();
    }

    ~ProcessHandle()
    {
        Dispose(false);
    }
}