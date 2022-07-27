using MerinoClient.Core;
using MerinoClient.Core.Managers;
using MerinoClient.Core.VRChat;
using MerinoClient.Features.QoL;
using MerinoClient.Features.QoL.UI;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;

namespace MerinoClient.MenuComponents.MainMenu;

internal class VisualsComponent : MenuComponent
{
    public VisualsComponent()
    {
        Config.PerformanceStats = new ConfigValue<bool>(nameof(Config.PerformanceStats), true, "Performance Stats");

        Config.PostProcessing = new ConfigValue<bool>(nameof(Config.PostProcessing), true, "Post Processing");
        Config.PostProcessing.OnValueChanged += () => SetPostProcessVolumeScene(Config.PostProcessing);

        Config.SocialInformation = new ConfigValue<bool>(nameof(Config.SocialInformation), true, "Social Information");
        Config.SocialInformation.OnValueChanged +=
            () => SocialPageExtensions.ChangeSocialInformation(Config.SocialInformation);

        Config.Notifications = new ConfigValue<bool>(nameof(Config.Notifications), true, "Notifications");

        Config.XSIntegration = new ConfigValue<bool>(nameof(Config.XSIntegration), true, "Notifications");
        Config.XSIntegrationSound = new ConfigValue<bool>(nameof(Config.XSIntegrationSound), true, "Sound");
    }

    public override void OnQuickMenuInited(UiManager uiManager)
    {
        var visualsMenuPage = uiManager.MainMenu.GetMenuPage("Visuals");

        visualsMenuPage.AddToggle(Config.PerformanceStats.DisplayName,
            "Disables performance stats on avatars, increasing speed of loading RAM cached models",
            Config.PerformanceStats);

        visualsMenuPage.AddToggle(Config.PostProcessing.DisplayName,
            "Disables world' post processing volume, if world has one", Config.PostProcessing);

        visualsMenuPage.AddToggle(Config.SocialInformation.DisplayName,
            "Disables social menu information such join_date and username", Config.SocialInformation);

        visualsMenuPage.AddToggle(Config.Notifications.DisplayName,
            "Disables on screen notifications such friend joins/leave and portal dropped",
            Config.Notifications);

        if (!XSNotificationsLite.CanNotify) return;

        var xsOverlayMenuPage = visualsMenuPage.AddMenuPage("XSOverlay",
            "Advanced settings for client's XSOverlay integration",
            ResourceManager.GetSprite("XSOverlay"));

        xsOverlayMenuPage.AddToggle(Config.XSIntegration.DisplayName,
            "Makes some events in-game to be sent directly to XSOverlay using their Notifications API",
            Config.XSIntegration);

        xsOverlayMenuPage.AddToggle(Config.XSIntegrationSound.DisplayName,
            "Disables notification sound sent from my client to XSOverlay",
            Config.XSIntegrationSound);
    }

    public override void OnSceneLoaded()
    {
        SetPostProcessVolumeScene(Config.PostProcessing);
    }

    private static void SetPostProcessVolumeScene(bool enabled)
    {
        var postProcessingVolume = Resources.FindObjectsOfTypeAll<PostProcessVolume>();
        if (postProcessingVolume == null)
        {
            QuickMenuEx.Instance.GetModalAlert().ShowModalAlert("No post processing found in the world");
            return;
        }

        foreach (var postProcessVolume in postProcessingVolume) postProcessVolume.enabled = enabled;
    }
}