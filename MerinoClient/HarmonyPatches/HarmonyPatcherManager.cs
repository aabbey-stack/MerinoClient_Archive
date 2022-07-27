using System;
using System.Reflection;
using HarmonyLib;
using MelonLoader;
#if DEBUG
using System.Diagnostics;
#endif

namespace MerinoClient.HarmonyPatches;

internal static class HarmonyPatcherManager
{
    public static void Initialize()
    {
#if DEBUG
        var watch = new Stopwatch();
        watch.Start();
#endif
        InstanceCreator.LoadInstance(typeof(PatchObject));

#if DEBUG
        watch.Stop();
        MerinoLogger.Msg($"[Harmony] finished patching in {watch.ElapsedMilliseconds} ms");
#endif
    }
}

internal class PatchObject
{
    protected HarmonyMethod GetLocalPatch(string name)
    {
        return GetType().GetMethod(name, BindingFlags.NonPublic | BindingFlags.Static)?.ToNewHarmonyMethod();
    }

    protected static void Patch(MethodBase original, HarmonyMethod prefix = null, HarmonyMethod postfix = null,
        HarmonyMethod transpiler = null, HarmonyMethod finalizer = null,
        HarmonyMethod ilmanipulator = null)
    {
        try
        {
            Main.MerinoHarmony.Patch(original, prefix, postfix, transpiler, finalizer, ilmanipulator);
        }
        catch (Exception ex)
        {
            MerinoLogger.Error($"Error occurred while patching {original.Name}:\n{ex}");
        }
    }
}