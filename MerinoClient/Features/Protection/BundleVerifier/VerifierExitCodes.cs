using System.Collections.Generic;

namespace MerinoClient.Features.Protection.BundleVerifier;

internal static class VerifierExitCodes
{
    private static readonly Dictionary<int, string> OurCodeDescriptions = new()
    {
        { 179, "Generic exception (report a bug!)" },
        { 180, "CLI argument parse error (report a bug!)" },
        { 181, "VRChat process suddenly died" },
        { 182, "Loaded bundle was null" },
        { 183, "Loaded bundle did not contain a prefab" },
        { 184, "Prefab instantiation failed" },
        { 185, "Minimum frame-rate not achieved" },
        { 186, "Over component limit" },
        { -1073741819, "Generic crash (corrupted bundle)" }, // 0xc0000005
        { -2147483645, "Breakpoint crash (corrupted bundle)" } // 0x80000003
    };

    internal static string GetExitCodeDescription(int? exitCode)
    {
        if (!exitCode.HasValue) return "Timed out";

        return OurCodeDescriptions.TryGetValue(exitCode.Value, out var result) ? result : "Unknown";
    }
}