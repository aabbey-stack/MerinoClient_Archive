using System.Reflection;
using MerinoClient.Core;
using UnityEngine;

// ReSharper disable InconsistentNaming

namespace MerinoClient.HarmonyPatches.Protection;

internal class VRCFlowManagerVRCPatch : PatchObject
{
    private static PropertyInfo _settleStartTime;

    public VRCFlowManagerVRCPatch()
    {
        foreach (var nestedType in typeof(VRCFlowManagerVRC).GetNestedTypes())
        foreach (var methodInfo in nestedType.GetMethods())
        {
            if (methodInfo.Name != "MoveNext") continue;

            if (!XrefUtils.CheckMethod(methodInfo, "Executing Buffered Events")) continue;

            _settleStartTime = nestedType.GetProperty("field_Private_Single_0");

            Patch(methodInfo, GetLocalPatch(nameof(MoveNextPatch)));
        }
    }

    private static void MoveNextPatch(object __instance)
    {
        if (__instance == null) return;
        var eventReplicator = VRC_EventLog.field_Internal_Static_VRC_EventLog_0?.field_Internal_EventReplicator_0;

        if (eventReplicator != null && !eventReplicator.field_Private_Boolean_0 &&
            Time.realtimeSinceStartup - (float)_settleStartTime.GetValue(__instance) >= 10.0)
        {
            eventReplicator.field_Private_Boolean_0 = true;
            MerinoLogger.Warning("Instance Master is not sending any events, joining anyway to prevent lock abuse");
        }
    }
}