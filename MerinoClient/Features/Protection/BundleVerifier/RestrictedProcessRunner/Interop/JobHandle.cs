using System;
using System.Runtime.InteropServices;

namespace MerinoClient.Features.Protection.BundleVerifier.RestrictedProcessRunner.Interop;

internal sealed class JobHandle : IDisposable
{
    public JobHandle()
    {
        InteropMethods.SECURITY_ATTRIBUTES empty = default;
        Handle = InteropMethods.CreateJobObject(ref empty, null);
        if (Handle == IntPtr.Zero) throw new ApplicationException("Unable to create job object");
    }

    internal IntPtr Handle { get; private set; }

    public void Dispose()
    {
        ReleaseUnmanagedResources();
        GC.SuppressFinalize(this);
    }

    public unsafe void SetLimits(TimeSpan? cpuTimeLimit, ulong? memoryBytes, bool allowNetwork, bool allowDesktop,
        bool allowChildProcesses)
    {
        {
            InteropMethods.JOBOBJECT_EXTENDED_LIMIT_INFORMATION limits = default;

            if (cpuTimeLimit != null)
            {
                limits.BasicLimitInformation.LimitFlags |= InteropMethods.JobBasicLimitFlags.PROCESS_TIME;
                limits.BasicLimitInformation.PerProcessUserTimeLimit = cpuTimeLimit.Value.Ticks;
            }

            if (memoryBytes != null)
            {
                limits.BasicLimitInformation.LimitFlags |= InteropMethods.JobBasicLimitFlags.PROCESS_MEMORY;
                limits.ProcessMemoryLimit = (IntPtr)memoryBytes.Value;
            }

            if (!allowChildProcesses)
            {
                limits.BasicLimitInformation.ActiveProcessLimit = 1;
                limits.BasicLimitInformation.LimitFlags |= InteropMethods.JobBasicLimitFlags.ACTIVE_PROCESS;
            }

            limits.BasicLimitInformation.LimitFlags |= InteropMethods.JobBasicLimitFlags.KILL_ON_JOB_CLOSE;

            InteropMethods.SetInformationJobObject(Handle,
                InteropMethods.JobObjectInfoClass.JobObjectExtendedLimitInformation, (IntPtr)(&limits),
                Marshal.SizeOf<InteropMethods.JOBOBJECT_EXTENDED_LIMIT_INFORMATION>());
        }

        if (!allowDesktop)
        {
            var uiLimits = new InteropMethods.JOBOBJECT_BASIC_UI_RESTRICTIONS
                { UIRestrictionsClass = InteropMethods.UiRestrictionClass.ALL };
            InteropMethods.SetInformationJobObject(Handle,
                InteropMethods.JobObjectInfoClass.JobObjectBasicUIRestrictions, (IntPtr)(&uiLimits),
                Marshal.SizeOf<InteropMethods.JOBOBJECT_BASIC_UI_RESTRICTIONS>());
        }

        if (!allowNetwork)
        {
            var limits = new InteropMethods.JOBOBJECT_NET_RATE_CONTROL_INFORMATION
            {
                ControlFlags = InteropMethods.JOB_OBJECT_NET_RATE_CONTROL_FLAGS.JOB_OBJECT_NET_RATE_CONTROL_ENABLE |
                               InteropMethods.JOB_OBJECT_NET_RATE_CONTROL_FLAGS
                                   .JOB_OBJECT_NET_RATE_CONTROL_MAX_BANDWIDTH,
                MaxBandwidth = 0
            };

            InteropMethods.SetInformationJobObject(Handle,
                InteropMethods.JobObjectInfoClass.JobObjectNetRateControlInformation, (IntPtr)(&limits),
                Marshal.SizeOf<InteropMethods.JOBOBJECT_NET_RATE_CONTROL_INFORMATION>());
        }
    }

    private void ReleaseUnmanagedResources()
    {
        if (Handle != IntPtr.Zero) InteropMethods.CloseHandle(Handle);
        Handle = IntPtr.Zero;
    }

    ~JobHandle()
    {
        ReleaseUnmanagedResources();
    }
}