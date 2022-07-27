using System.Diagnostics;
using System.IO;
using MerinoClient.Core;
using MerinoClient.Core.Managers;
using MerinoClient.Core.UI.QuickMenu;
using MerinoClient.Core.VRChat;
using MerinoClient.Features.QoL.UI;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;

namespace MerinoClient.MenuComponents.MainMenu;

internal class GeneralComponent : MenuComponent
{
    private static MenuButton _remodAPIPinButton;
    private static MenuButton _remodAPIPinResetButton;

    public GeneralComponent()
    {
        Config.AppearFrozen = new ConfigValue<bool>(nameof(Config.AppearFrozen), false, "Appear Frozen");

        Config.SavePlayerPrefs = new ConfigValue<bool>(nameof(Config.SavePlayerPrefs), false, "Save PlayerPrefs");

        Config.Emojis = new ConfigValue<bool>(nameof(Config.Emojis), true, "Emojis");

        Config.ThemeColor = new ConfigValue<string>(nameof(Config.ThemeColor), "#ffc540", "Theme color");

        Config.ReModAPI = new ConfigValue<bool>(nameof(Config.ReModAPI), false, "ReModAPI");
        Config.ReModAPI.OnValueChanged += () =>
        {
            _remodAPIPinButton.SetActive(Config.ReModAPI);
            _remodAPIPinResetButton.SetActive(Config.ReModAPI);
        };
    }

    public override void OnQuickMenuInited(UiManager uiManager)
    {
        var generalMenuPage = uiManager.MainMenu.GetMenuPage("General");

        generalMenuPage.AddButton("Optimize Mirrors", "Changes reflect layers of a mirror to an optimized one",
            () =>
            {
                if (Object.FindObjectsOfType<VRC_MirrorReflection>() == null)
                {
                    QuickMenuEx.Instance.GetModalAlert().ShowModalAlert("There are no mirrors in this world");
                    return;
                }

                foreach (var vrcMirrorReflection in Object.FindObjectsOfType<VRC_MirrorReflection>())
                {
                    if (!vrcMirrorReflection.isActiveAndEnabled)
                    {
                        QuickMenuEx.Instance.GetModalAlert().ShowModalAlert("There is no active mirror in the scene");
                        return;
                    }

                    vrcMirrorReflection.m_ReflectLayers = 262656;
                }
            });

        generalMenuPage.AddButton("Normalize Mirrors", "Normalizes reflect layers of a mirror to a high quality one",
            () =>
            {
                if (Object.FindObjectsOfType<VRC_MirrorReflection>() == null)
                {
                    QuickMenuEx.Instance.GetModalAlert().ShowModalAlert("There are no mirrors in this world");
                    return;
                }

                foreach (var vrcMirrorReflection in Object.FindObjectsOfType<VRC_MirrorReflection>())
                {
                    if (!vrcMirrorReflection.isActiveAndEnabled)
                    {
                        QuickMenuEx.Instance.GetModalAlert().ShowModalAlert("There is no active mirror in the scene");
                        return;
                    }

                    vrcMirrorReflection.m_ReflectLayers = 262657;
                }
            });

        generalMenuPage.AddToggle(Config.AppearFrozen.DisplayName, "Makes you appear frozen in place for remote users",
            Config.AppearFrozen);

        generalMenuPage.AddToggle(Config.SavePlayerPrefs.DisplayName,
            "Moves game settings storage from Windows registry to UserData folder, useful when using multiple accounts (<color=#ffcc00>Requires game restart</color>)",
            Config.SavePlayerPrefs);

        generalMenuPage.AddToggle(Config.Emojis.DisplayName,
            "Disables emojis from remote players, useful against people spamming them in big instances",
            Config.Emojis);

        generalMenuPage.AddButton(
            RichText($"Hex theme: {Config.ThemeColor.Value}"),
            "Client's accent color that's in Hex format, <color=yellow>requires game restart for changes to take an effect</color>",
            () =>
            {
                VRCUiPopupManager.prop_VRCUiPopupManager_0.ShowInputPopup("Hex theme:",
                    Config.ThemeColor);
            }, ResourceManager.GetSprite("gear"));

        var remodSearchMenuPage = generalMenuPage.AddMenuPage($"{RichText("ReMod API")}",
            "ReMod API options for utilizing avatar search function", ResourceManager.GetSprite("remod"));

        #region remodSearchMenuPage

        remodSearchMenuPage.AddToggle(Config.ReModAPI.DisplayName,
            "Makes you able to access ReMod avatar search API, requires game restart to take full effect <color=yellow>Exposes your userId to the ReMod API every time you make a search query</color>",
            Config.ReModAPI);

        _remodAPIPinButton = remodSearchMenuPage.AddButton("API Pin",
            "Your ReModAPI pin that's used to tie userIds, used against people spamming the api with too many requests",
            () =>
            {
                VRCUiPopupManager.field_Private_Static_VRCUiPopupManager_0.ShowInputPopupWithCancel("ReModAPI Pin",
                    "", InputField.InputType.Password, true, "Set",
                    (s, _, _) =>
                    {
                        if (string.IsNullOrEmpty(s))
                        {
                            MerinoLogger.Error("Pin cannot be null or empty");
                            return;
                        }

                        if (s.Length < 6)
                        {
                            MerinoLogger.Error("Pin needs to be at least 6 characters long");
                            return;
                        }

                        if (s is "123456" or "654321")
                        {
                            MerinoLogger.Error("Pin should be more complex than this");
                            return;
                        }

                        AvatarSearchComponent.SavedReModAPIPin.Add("PIN", int.Parse(s));
                        File.WriteAllText(FeatureComponent.ClientDirectory + AvatarSearchComponent.ReModAPIPinFile,
                            JsonConvert.SerializeObject(AvatarSearchComponent.SavedReModAPIPin));
                    }, null);
            });

        _remodAPIPinResetButton = remodSearchMenuPage.AddButton("Reset Pin",
            "Click this if you've forgotten your RemodAPI Pin and needs to reset it",
            () => { Process.Start("https://remod-ce.requi.dev/api/pin.php"); });

        #endregion
    }
}