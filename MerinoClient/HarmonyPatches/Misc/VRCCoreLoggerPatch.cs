using UnityEngine;

namespace MerinoClient.HarmonyPatches.Misc;

internal class VRCCoreLoggerPatch : PatchObject
{
    public VRCCoreLoggerPatch()
    {
        Patch(typeof(Application).GetMethod(nameof(Application.CallLogCallback)),
            GetLocalPatch(nameof(CallLogCallbackPatch)));
    }

    private static bool CallLogCallbackPatch(string logString, LogType type)
    {
        switch (type)
        {
            case LogType.Error:
                if (logString.Contains("Remove it and launch unity again"))
                    MerinoLogger.Error("Encountered corrupted asset bundle! " + logString);
                else if (logString.Contains("Size overflow in allocator."))
                    MerinoLogger.Error("Encountered bad asset bundle! " + logString);
                break;

            case LogType.Warning:
                if (logString.Contains("GameObject contains a component type that is not recognized"))
                    MerinoLogger.Warning(logString);
                if (logString.Contains("on this Behaviour is missing"))
                    return false;
                break;
        }

        if (!Config.LogUnityErrors.Value) return true;
        if (type == LogType.Error && !logString.Contains("Connection refused"))
            MerinoLogger.Error(logString);

        return true;
    }
}