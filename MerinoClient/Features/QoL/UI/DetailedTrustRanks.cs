using System.Linq;
using System.Reflection;
using HarmonyLib;
using MerinoClient.Core.VRChat;
using UnityEngine;
using VRC.Core;

//ReSharper disable RedundantAssignment
//ReSharper disable InconsistentNaming

namespace MerinoClient.Features.QoL.UI;

/*
 * main inspiration and resource for this "feature": https://github.com/RequiDev/OGTrustRanks
 * this cs file used be my slightly edited version of OGTrustRanks, which wouldn't show legendary users per say but only that they have a legend tag
 * like VRCX for example and other small changes, after VRChat has removed these unused tags I've decided to use this mostly for people who are devs, but not
 * on accounts that have any admin powers to restrain my confrontational behaviour I tend to have, also showed people with system_probable_troll as nuisance,
 * I just found it as a nice addition
 */

internal class DetailedTrustRanks : FeatureComponent
{
    private static readonly Color _vrchatTeamColor = new Color32(255, 38, 38, byte.MaxValue);
    private static readonly Color _nuisanceColor = new Color32(120, 47, 47, byte.MaxValue);

    public DetailedTrustRanks()
    {
        var userIdsList = PlayerExtensions.UserIdsList;
        userIdsList.AddRange(HiddenDevelopers);

        foreach (var method in typeof(VRCPlayer).GetMethods())
        {
            if (!method.Name.StartsWith("Method_Public_Static_String_APIUser_") || method.Name.Length != 37)
                continue;

            Main.MerinoHarmony.Patch(method,
                new HarmonyMethod(typeof(DetailedTrustRanks).GetMethod(nameof(GetFriendlyDetailedNameForSocialRank),
                    BindingFlags.NonPublic | BindingFlags.Static)));
        }

        var colorForRankTargetMethods = typeof(VRCPlayer).GetMethods().Where(it =>
            it.ReturnType.ToString().Equals("UnityEngine.Color") && it.GetParameters().Length == 1 &&
            it.GetParameters()[0].ParameterType.ToString().Equals("VRC.Core.APIUser")).ToList();
        colorForRankTargetMethods.ForEach(it =>
            Main.MerinoHarmony.Patch(it,
                new HarmonyMethod(typeof(DetailedTrustRanks).GetMethod(nameof(GetColorForSocialRank),
                    BindingFlags.NonPublic | BindingFlags.Static)))
        );
    }

    public override string FeatureName => GetType().Name;
    public override string OriginalAuthor => "abbey";

    private static bool GetColorForSocialRank(APIUser __0, ref Color __result)
    {
        if (__0 == null || APIUser.IsFriendsWith(__0.id)) return true;

        var rank = __0.GetTrustLevel();

        switch (rank)
        {
            case PlayerExtensions.TrustLevel.VRChatTeam:
                __result = _vrchatTeamColor;
                return false;
            case PlayerExtensions.TrustLevel.Nuisance:
                __result = _nuisanceColor;
                return false;
            case PlayerExtensions.TrustLevel.Trusted:
            case PlayerExtensions.TrustLevel.Known:
            case PlayerExtensions.TrustLevel.User:
            case PlayerExtensions.TrustLevel.New:
            case PlayerExtensions.TrustLevel.Visitor:
            default:
                return true;
        }
    }

    private static bool GetFriendlyDetailedNameForSocialRank(APIUser __0, ref string __result)
    {
        if (__0 == null) return true;

        var rank = __0.GetTrustLevel();

        __result = GetRank(rank);
        return false;
    }

    private static string GetRank(PlayerExtensions.TrustLevel rank)
    {
        if (rank is PlayerExtensions.TrustLevel.User or PlayerExtensions.TrustLevel.Visitor
            or PlayerExtensions.TrustLevel.Nuisance)
            return rank.ToString();

        return rank == PlayerExtensions.TrustLevel.VRChatTeam ? "VRChat Team" : $"{rank} User";
    }
}