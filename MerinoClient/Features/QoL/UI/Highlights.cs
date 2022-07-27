using System;
using MerinoClient.Core.VRChat;
using UnityEngine;
using VRC;
using VRC.Core;

namespace MerinoClient.Features.QoL.UI;

/*
 * Original source code (slightly edited with the name ESP addition): https://github.com/RequiDev/ReModCE/blob/master/ReModCE/Components/AvatarFavoritesComponent.cs
 */

internal class Highlights : FeatureComponent
{
    private static HighlightsFXStandalone _friendsHighlights;
    private static HighlightsFXStandalone _othersHighlights;

    public override string FeatureName => GetType().Name;
    public override string OriginalAuthor => "Requi";

    public override void OnVRCUiManagerInited()
    {
        var highlightsFx = HighlightsFX.field_Private_Static_HighlightsFX_0;

        _friendsHighlights = highlightsFx.gameObject.AddComponent<HighlightsFXStandalone>();
        _friendsHighlights.highlightColor = Color.yellow;
        _othersHighlights = highlightsFx.GetComponent<HighlightsFXStandalone>();
        _othersHighlights.highlightColor = Color.white;
    }

    public static void ToggleESP(bool enabled)
    {
        foreach (var player in PlayerEx.PlayerManager.GetPlayers()) HighlightPlayer(player, enabled);
    }

    public static void HighlightPlayer(Player player, bool highlighted)
    {
        try
        {
            var selectRegion = player.transform.Find("SelectRegion");

            if (selectRegion == null)
                return;

            var apiUser = player.field_Private_APIUser_0;

            if (apiUser == null) return;

            if (apiUser.IsSelf)
                return;

            var highlightsFx = GetHighlightsFX(apiUser);

            if (highlightsFx == null) return;

            highlightsFx.Method_Public_Void_Renderer_Boolean_0(selectRegion.GetComponent<Renderer>(), highlighted);

            player.GetVRCPlayer().field_Public_PlayerNameplate_0.field_Public_TextMeshProUGUI_0.isOverlay = highlighted;
        }
        catch (Exception e)
        {
            MerinoLogger.Error(e);
        }
    }


    private static HighlightsFX GetHighlightsFX(APIUser apiUser)
    {
        return APIUser.IsFriendsWith(apiUser.id) ? _friendsHighlights : _othersHighlights;
    }
}