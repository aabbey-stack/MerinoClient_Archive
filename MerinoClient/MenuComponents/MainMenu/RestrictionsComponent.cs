using System.Diagnostics;
using MerinoClient.Core;
using MerinoClient.Core.Managers;
using MerinoClient.Core.UI.QuickMenu;
using MerinoClient.Core.VRChat;
using MerinoClient.Features.Protection.BundleVerifier;
using MerinoClient.Features.Protection.TSAC;
using MerinoClient.Utilities;

namespace MerinoClient.MenuComponents.MainMenu;

internal class RestrictionsComponent : MenuComponent
{
    public RestrictionsComponent()
    {
        Config.VRCStations = new ConfigValue<bool>(nameof(Config.VRCStations), true, "VRC Stations");

        Config.UdonEvents = new ConfigValue<bool>(nameof(Config.UdonEvents), true, "Udon Events");

        Config.VideoPlayers = new ConfigValue<bool>(nameof(Config.VideoPlayers), true, "Video Players");

        Config.Portals = new ConfigValue<bool>(nameof(Config.Portals), true, "Enabled");

        Config.AllPortals = new ConfigValue<bool>(nameof(Config.AllPortals), false, "All Portals");

        Config.NonFriendsPortals = new ConfigValue<bool>(nameof(Config.NonFriendsPortals), false, "Non Friends");

        Config.AskToPortal = new ConfigValue<bool>(nameof(Config.AskToPortal), true, "Ask To Portal");

        Config.Pickups = new ConfigValue<bool>(nameof(Config.Pickups), true, "Enabled");
        Config.Pickups.OnValueChanged += () => { PickupUtils.SetActive(Config.Pickups); };

        Config.MuteEveryone = new ConfigValue<bool>(nameof(Config.MuteEveryone), false, "Mute Everyone");

        Config.FreezeEveryone = new ConfigValue<bool>(nameof(Config.FreezeEveryone), false, "Freeze Everyone");
    }

    public override void OnQuickMenuInited(UiManager uiManager)
    {
        var restrictionsMenuPage = uiManager.MainMenu.GetMenuPage("Restrictions");

        restrictionsMenuPage.AddToggle(Config.VRCStations.DisplayName,
            "Disables VRC_Stations and makes you unable to interact with the chairs",
            Config.VRCStations);

        var pickupsMenuPage = restrictionsMenuPage.AddMenuPage($"{RichText("Pickups")}",
            "Advanced Pickup options such as destroying pickups and respawning pickups",
            ResourceManager.GetSprite("pen-writing-tool"));

        #region pickupsMenuPage

        pickupsMenuPage.AddToggle(Config.Pickups.DisplayName,
            "Disables pickups in the world by making them not rendered and un-interactable",
            Config.Pickups);

        pickupsMenuPage.AddButton("Destroy", "Destroys all the pickups in the world locally", PickupUtils.Destroy);

        #endregion

        restrictionsMenuPage.AddToggle(Config.VideoPlayers.DisplayName,
            "Disables video-player's ability to play videos, rejoining the lobby or clicking \"Re-sync\" will make them work again",
            Config.VideoPlayers);

        restrictionsMenuPage.AddToggle(Config.UdonEvents.DisplayName,
            "Disables networked udon events, useful against people abusing broken map's events",
            Config.UdonEvents);

        var ugcSanityCategoryPage = restrictionsMenuPage.AddCategoryPage($"{RichText("UGC")} Sanity",
            "Options and features for user-generated content such as bundle verifier and shader anti-crash",
            ResourceManager.GetSprite("unity"));

        #region ugcSanityCategoryPage

        CreateUGCSanityMenu(ugcSanityCategoryPage);

        #endregion

        var portalsMenuPage = restrictionsMenuPage.AddMenuPage($"{RichText("Portals")}",
            "Advanced portal options such as anti portal and AskToPortal", ResourceManager.GetSprite("portal"));

        #region portalsMenuPage

        portalsMenuPage.AddToggle(Config.Portals.DisplayName,
            "Disables portals in the world and dropped by the players", Config.Portals);

        portalsMenuPage.AddToggle(Config.AllPortals.DisplayName,
            "Auto-deletes all spawned portals by remote users",
            Config.AllPortals);

        portalsMenuPage.AddToggle(Config.NonFriendsPortals.DisplayName,
            "Auto-deletes all spawned portals by non-friends",
            Config.NonFriendsPortals);

        portalsMenuPage.AddToggle(Config.AskToPortal.DisplayName,
            "Brings a portal enter dialog with some additional information when you are trying to enter a portal",
            Config.AskToPortal);

        #endregion

        restrictionsMenuPage.AddToggle(Config.MuteEveryone.DisplayName, "Mutes everyone for you locally",
            Config.MuteEveryone);

        restrictionsMenuPage.AddToggle(Config.FreezeEveryone.DisplayName, "Freezes everyone for you locally",
            Config.FreezeEveryone);
    }

    private static void CreateUGCSanityMenu(CategoryPage ugcSanityCategoryPage)
    {
        if (TrueShaderAntiCrash.shouldLoad)
        {
            var tsacOptionsCategory = ugcSanityCategoryPage.AddCategory($"{RichText("TSAC")} Options");

            #region tsacOptionsCategory

            tsacOptionsCategory.AddToggle(TrueShaderAntiCrash.LoopsEnabled.DisplayName,
                "Limits the amount of loop iterations",
                TrueShaderAntiCrash.LoopsEnabled);

            tsacOptionsCategory.AddToggle(TrueShaderAntiCrash.GeometryEnabled.DisplayName,
                "Limits the amount of geometry shader outputs", TrueShaderAntiCrash.GeometryEnabled);

            tsacOptionsCategory.AddToggle(TrueShaderAntiCrash.TessEnabled.DisplayName,
                "Limits the max tesselation power",
                TrueShaderAntiCrash.TessEnabled);

            tsacOptionsCategory.AddButton("Reset Limiters", "Resets the limiters values in TSAC to their default one",
                TrueShaderAntiCrash.ResetLimiterValues, ResourceManager.GetSprite("update-arrow"));

            tsacOptionsCategory.AddButton($"{RichText("TSAC")} Source",
                "Opens TSAC github page for sourcing the original author",
                () =>
                {
                    QuickMenuEx.Instance.ShowConfirmDialog("TSAC Source",
                        "Do you really wish to visit the TSAC source-code repository?",
                        () => { Process.Start("https://github.com/knah/VRCMods/tree/master/TrueShaderAntiCrash"); });
                }, ResourceManager.GetSprite("github-logo"));

            #endregion
        }

        if (!BundleVerifierMod.shouldLoad) return;

        var bundleVerifierCategory = ugcSanityCategoryPage.AddCategory($"{RichText("Bundle")} Verifier");

        #region bundleVerifierCategory

        bundleVerifierCategory.AddToggle(BundleVerifierMod.EnabledSetting.Name,
            "Verifies asset-bundles before you try to load them and prevents loading bad ones",
            BundleVerifierMod.EnabledSetting);

        bundleVerifierCategory.AddToggle(BundleVerifierMod.OnlyPublics.Name,
            "Verify asset-bundles only in public worlds", BundleVerifierMod.OnlyPublics);

        _timeLimitButton = bundleVerifierCategory.AddButton(
            $"Time limit: {BundleVerifierMod.TimeLimit.Value}",
            "CPU Time limit (seconds), adjust this if you're on a weaker PC",
            () =>
            {
                VRCUiPopupManager.prop_VRCUiPopupManager_0.ShowInputPopup("Time Limit:", BundleVerifierMod.TimeLimit,
                    _timeLimitButton);
            }, ResourceManager.GetSprite("gear"));

        _memoryLimitButton = bundleVerifierCategory.AddButton(
            $"Memory limit: {BundleVerifierMod.MemoryLimit.Value}",
            "RAM Memory limit (megabytes) for avatars loaded into memory",
            () =>
            {
                VRCUiPopupManager.prop_VRCUiPopupManager_0.ShowInputPopup("Memory limit:",
                    BundleVerifierMod.MemoryLimit, _memoryLimitButton);
            }, ResourceManager.GetSprite("gear"));

        _componentLimitButton = bundleVerifierCategory.AddButton(
            $"Component limit: {BundleVerifierMod.ComponentLimit.Value}",
            "Component limit (0=unlimited) as amount of components on the avatar",
            () =>
            {
                VRCUiPopupManager.prop_VRCUiPopupManager_0.ShowInputPopup("Component limit:",
                    BundleVerifierMod.ComponentLimit, _componentLimitButton);
            }, ResourceManager.GetSprite("gear"));

        bundleVerifierCategory.AddButton("Reset Cache", "Resets the corrupted bundle cache", () =>
            BundleVerifierMod.BadBundleCache.Clear(), ResourceManager.GetSprite("update-arrow"));

        bundleVerifierCategory.AddButton($"{RichText("BV")} Source",
            "Opens Bundle Verifier github page for sourcing the original author",
            () =>
            {
                QuickMenuEx.Instance.ShowConfirmDialog("BV Source",
                    "Do you really wish to visit the BV source-code repository?",
                    () =>
                    {
                        Process.Start("https://github.com/knah/VRCMods/tree/master/AdvancedSafety/BundleVerifier");
                    });
            }, ResourceManager.GetSprite("github-logo"));

        #endregion
    }

    public override void OnSceneLoaded()
    {
        PickupUtils.FetchPickups();
        PickupUtils.SetActive(Config.Pickups);
    }

    #region BundleVerifierButtons

    private static MenuButton _timeLimitButton;
    private static MenuButton _memoryLimitButton;
    private static MenuButton _componentLimitButton;

    #endregion
}