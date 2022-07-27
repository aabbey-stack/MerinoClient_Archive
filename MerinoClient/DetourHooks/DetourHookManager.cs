using System;
#if DEBUG
using System.Diagnostics;
#endif

// ReSharper disable CollectionNeverQueried.Global

namespace MerinoClient.DetourHooks;

internal class DetourHookManager
{
    public static void Initialize()
    {
        try
        {
#if DEBUG
            var watch = new Stopwatch();
            watch.Start();
#endif
            InstanceCreator.LoadInstance(typeof(DetourHookManager));

#if DEBUG
            watch.Stop();
            MerinoLogger.Msg($"[Hooking] finished hooking in {watch.ElapsedMilliseconds} ms");
#endif
        }
        catch (Exception e)
        {
            MerinoLogger.Error("Exception occurred while hooking " + e);
        }
    }
}