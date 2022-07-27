using System;
using System.Runtime.InteropServices;
using VRC.SDKBase.Validation.Performance;

// ReSharper disable AccessToModifiedClosure
// ReSharper disable PossibleNullReferenceException

namespace MerinoClient.DetourHooks.Misc;

internal class GetPerformanceScannerSetHook : DetourHookManager
{
    private static GetPerformanceScannerSetDelegate _getPerformanceScannerSetDelegate;

    public GetPerformanceScannerSetHook()
    {
        try
        {
            NativePatchUtils.NativePatch(
                typeof(AvatarPerformance).GetMethod(nameof(AvatarPerformance.GetPerformanceScannerSet))!,
                out _getPerformanceScannerSetDelegate, GetPerformanceScannerSetHook);

            IntPtr GetPerformanceScannerSetHook(bool mobilePlatform)
            {
                return Config.PerformanceStats.Value
                    ? _getPerformanceScannerSetDelegate(mobilePlatform)
                    : IntPtr.Zero;
            }
        }
        catch (Exception e)
        {
            MerinoLogger.Error($"Failed to hook: GetPerformanceScannerSet\n{e}");
        }
    }

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate IntPtr GetPerformanceScannerSetDelegate(bool mobilePlatform);
}