using System.Collections.Generic;
using System.Linq;
using MerinoClient.Core.VRChat;
using UnityEngine;
using VRC.SDK3.Components;
using VRCSDK2;

namespace MerinoClient.Utilities;

internal static class PickupUtils
{
    private static readonly List<VRCPickup> VRCSDK3Pickups = new();
    private static readonly List<VRC_Pickup> VRCSDK2Pickups = new();

    public static void FetchPickups()
    {
        ClearCollection();

        var vrcsdk3Pickups = GetVRCPickups();
        if (vrcsdk3Pickups != null)
            foreach (var pickup in vrcsdk3Pickups)
            {
                if (VRCSDK3Pickups.Contains(pickup)) return;
                if (pickup.isActiveAndEnabled) VRCSDK3Pickups.Add(pickup);
            }

        var vrcsdk2Pickups = GetVRC_Pickups();
        if (vrcsdk2Pickups == null) return;

        foreach (var pickup in vrcsdk2Pickups)
        {
            if (VRCSDK2Pickups.Contains(pickup)) return;
            if (pickup.isActiveAndEnabled) VRCSDK2Pickups.Add(pickup);
        }
    }

    private static void ClearCollection()
    {
        VRCSDK2Pickups?.Clear();
        VRCSDK3Pickups?.Clear();
    }

    public static void SetActive(bool setActive)
    {
        if (VRCSDK3Pickups is not null) VRCSDK3Pickups.SetActive(setActive);
        else
            VRCSDK2Pickups?.SetActive(setActive);
    }

    public static void Destroy()
    {
        if (VRCSDK3Pickups is not null) VRCSDK3Pickups.Destroy();
        else
            VRCSDK2Pickups?.Destroy();
    }

    private static IEnumerable<VRC_Pickup> GetVRC_Pickups()
    {
        var pickups = Resources.FindObjectsOfTypeAll<VRC_Pickup>()
            .Where(pickup => pickup.name != "ViewFinder" && pickup.name != "PhotoCamera" &&
                             pickup.name != "AvatarDebugConsole" && pickup.name != "OscDebugConsole");
        return pickups;
    }

    private static IEnumerable<VRCPickup> GetVRCPickups()
    {
        var pickups = Resources.FindObjectsOfTypeAll<VRCPickup>()
            .Where(pickup => pickup.name != "ViewFinder" && pickup.name != "PhotoCamera" &&
                             pickup.name != "AvatarDebugConsole" && pickup.name != "OscDebugConsole");
        return pickups;
    }
}