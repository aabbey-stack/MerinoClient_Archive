using System;
using System.IO;
using System.Runtime.InteropServices;
using HarmonyLib;
using MelonLoader;

namespace MerinoClient.Features.Spoofing;

/*
 * Original source code: https://github.com/knah/ML-UniversalMods/tree/main/NoSteamAtAll
 */

internal class NoSteamAtAll : FeatureComponent
{
    public NoSteamAtAll()
    {
        if (IsModAlreadyPresent("No Steam. At all.", "knah") || Main.streamerMode) return;

        var path = MelonUtils.GetGameDataDirectory() + "\\Plugins\\steam_api64.dll";
        if (!File.Exists(path)) path = MelonUtils.GetGameDataDirectory() + "\\Plugins\\x86_64\\steam_api64.dll";
        if (!File.Exists(path)) path = MelonUtils.GetGameDataDirectory() + "\\Plugins\\x86\\steam_api64.dll";
        var library = LoadLibrary(path);
        if (library == IntPtr.Zero)
        {
            MerinoLogger.Error($"[NoSteamAtAll] Library load failed; used path: {path}");
            return;
        }

        var names = new[]
        {
            "SteamAPI_Init",
            "SteamAPI_RestartAppIfNecessary",
            "SteamAPI_GetHSteamUser",
            "SteamAPI_RegisterCallback",
            "SteamAPI_UnregisterCallback",
            "SteamAPI_RunCallbacks",
            "SteamAPI_Shutdown"
        };

        var success = false;
        if (Config.SteamAPI.Value) return;
        foreach (var name in names)
            unsafe
            {
                var address = GetProcAddress(library, name);
                if (address == IntPtr.Zero)
                {
                    MerinoLogger.Error($"Procedure {name} not found");
                    continue;
                }

                MelonUtils.NativeHookAttach((IntPtr)(&address),
                    AccessTools.Method(typeof(NoSteamAtAll), nameof(InitFail)).MethodHandle
                        .GetFunctionPointer());
                success = true;
            }

        if (success) MerinoLogger.Msg("Disabled SteamAPI");
    }

    public override string FeatureName => GetType().Name;
    public override string OriginalAuthor => "knah";

    [DllImport("kernel32", SetLastError = true, CharSet = CharSet.Ansi)]
    private static extern IntPtr LoadLibrary([MarshalAs(UnmanagedType.LPStr)] string lpFileName);

    [DllImport("kernel32", CharSet = CharSet.Ansi, ExactSpelling = true, SetLastError = true)]
    private static extern IntPtr GetProcAddress(IntPtr hModule, string procName);

    public static bool InitFail()
    {
        return false;
    }
}