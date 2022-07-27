using MerinoClient.Core;
using MerinoClient.Core.Managers;
using MerinoClient.Core.UI.QuickMenu;
using MerinoClient.Core.VRChat;
using UnityEngine.UI;

namespace MerinoClient.MenuComponents.MainMenu;

internal class PrivacyComponent : MenuComponent
{
    private static MenuButton _pingValueButton;

    public PrivacyComponent()
    {
        Config.SendModeration = new ConfigValue<bool>(nameof(Config.SendModeration), true, "Send Moderation");

        Config.FakeOffline = new ConfigValue<bool>(nameof(Config.FakeOffline), false, "Fake Offline");

        Config.SteamAPI = new ConfigValue<bool>(nameof(Config.SteamAPI), true, "Steam API");

        Config.SpoofPing = new ConfigValue<bool>(nameof(Config.SpoofPing), false, "Spoof Ping");
        Config.SpoofPing.OnValueChanged += () => { _pingValueButton.SetActive(Config.SpoofPing); };
        Config.PingValue = new ConfigValue<int>(nameof(Config.PingValue), 20, "Ping Value");

        Config.SpoofFPS = new ConfigValue<bool>(nameof(Config.SpoofFPS), false, "Spoof FPS");
    }

    public override void OnQuickMenuInited(UiManager uiManager)
    {
        var privacyCategoryPage = uiManager.MainMenu.GetCategoryPage("Privacy");

        var basicOptionsCategory = privacyCategoryPage.AddCategory($"{RichText("Basic")} Options", false);

        basicOptionsCategory.AddToggle(Config.SendModeration.DisplayName,
            "Blocks send moderation, useful when you want to clear your moderations every time you restart the game",
            Config.SendModeration);

        basicOptionsCategory.AddToggle(Config.FakeOffline.DisplayName,
            "Makes you appear offline in-game and \"active\" on the website",
            Config.FakeOffline);

        basicOptionsCategory.AddToggle(Config.SteamAPI.DisplayName,
            "Enables steam API, useful when you want to buy VRC+",
            Config.SteamAPI);

        #region nicheOptionsCategory

        var nicheOptionsCategory = privacyCategoryPage.AddCategory($"{RichText("Niche")} Options");

        nicheOptionsCategory.AddToggle(Config.SpoofPing.DisplayName, "Spoofs your ping to remote players",
            Config.SpoofPing);

        _pingValueButton = nicheOptionsCategory.AddButton(Config.PingValue.DisplayName,
            "Enter custom value that you wish your ping to be spoofed to",
            () =>
            {
                VRCUiPopupManager.prop_VRCUiPopupManager_0.ShowInputPopupWithCancel("Set Ping Value",
                    Config.PingValue.Value.ToString(), InputField.InputType.Standard, false, "Set",
                    (s, _, _) =>
                    {
                        if (string.IsNullOrEmpty(s))
                            return;

                        if (!int.TryParse(s, out var pingValue))
                            return;

                        Config.PingValue.SetValue(pingValue);
                    }, null);
            });

        _pingValueButton.SetActive(Config.SpoofPing);

        nicheOptionsCategory.AddToggle(Config.SpoofFPS.DisplayName,
            "Spoofs your fps to random number in random time intervals, useful against client users",
            Config.SpoofFPS);

        #endregion
    }
}