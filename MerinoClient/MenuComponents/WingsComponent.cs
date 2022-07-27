using MerinoClient.Core;
using MerinoClient.Core.Managers;
using MerinoClient.Core.UI.Wings;
using MerinoClient.Core.VRChat;
using MerinoClient.Features.QoL.UI;
using MerinoClient.Utilities;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR;
using VRC.Core;
using VRC.SDKBase;

namespace MerinoClient.MenuComponents;

internal class WingsComponent : MenuComponent
{
    public WingsComponent()
    {
        Config.Highlights = new ConfigValue<bool>(nameof(Config.Highlights), false, "Highlights");
        Config.Highlights.OnValueChanged += () => Highlights.ToggleESP(Config.Highlights);

        Config.ThirdPerson = new ConfigValue<bool>(nameof(Config.ThirdPerson), false, "ThirdPerson");
        Config.ThirdPersonHotkeys = new ConfigValue<bool>(nameof(Config.ThirdPersonHotkeys), true, "Hotkeys");

        Config.LocalClone = new ConfigValue<bool>(nameof(Config.LocalClone), false, "LocalClone");
    }

    public override void OnQuickMenuInited(UiManager uiManager)
    {
        var wingMenu = MirroredWingMenu.Create($"Merino{RichText("Client")}",
            "Open the MerinoClient wing menu", ResourceManager.GetSprite("sweat-droplets-white"));

        wingMenu.AddToggle(Config.Highlights.DisplayName,
            "Makes you able to see player's through walls with a capsule + name esp", Config.Highlights);

        wingMenu.AddToggle(Config.LocalClone.DisplayName,
            "Enables you to clone other people's models including private ones locally", Config.LocalClone);

        var utilitiesMenu = wingMenu.AddSubMenu($"{RichText("Util")}ities",
            "Useful utilities such as Copy Instance, Join instance and Avatar By Id");

        #region utilitiesMenu

        utilitiesMenu.AddButton("Avatar By ID", "Switches to an avatar by either an Id or a link", () =>
        {
            VRCUiPopupManager.prop_VRCUiPopupManager_0.ShowInputPopupWithCancel("Switch avatar",
                null, InputField.InputType.Standard, false, "Change",
                (s, _, _) =>
                {
                    var avatarID = ParserUtils.ParseAvatarId(s);

                    PlayerEx.VRCPlayer.ChangeToAvatar(avatarID);
                }, null, "Enter avatarId....");
        });

        utilitiesMenu.AddButton("Portal By Id",
            "Drops a portal to an instance by either an Id or a link",
            () =>
            {
                VRCUiPopupManager.prop_VRCUiPopupManager_0.ShowInputPopupWithCancel("Drop Portal",
                    null,
                    InputField.InputType.Standard,
                    false, "Drop", (s, _, _) =>
                    {
                        var instance = ParserUtils.ParseInstanceId(s);
                        PortalUtils.DropPortal(instance[0], instance[1]);
                    }, null, "Enter instance link....");
            });

        utilitiesMenu.AddButton("Join Instance",
            "Joins a world/instance by either an Id or a link",
            () =>
            {
                VRCUiPopupManager.prop_VRCUiPopupManager_0.ShowInputPopupWithCancel("Join world",
                    null,
                    InputField.InputType.Standard,
                    false, "Join", (s, _, _) =>
                    {
                        var instance = ParserUtils.ParseInstanceId(s);
                        Networking.GoToRoom($"{instance[0]}:{instance[1]}".Trim().TrimEnd('\r', '\n'));
                    }, null,
                    "Enter instance link....");
            });

        utilitiesMenu.AddButton("Copy Instance",
            "Copies your current instance link that you are in to your clipboard",
            () =>
            {
                var location = APIUser.CurrentUser.location.Split(':');
                GUIUtility.systemCopyBuffer =
                    $"https://vrchat.com/home/launch?worldId={location[0]}&instanceId={location[1]}";
            });

        #endregion

        #region thirdPersonSubMenu

        if (XRDevice.isPresent) return;

        var thirdPersonSubMenu =
            wingMenu.AddSubMenu($"{RichText("Third")}Person", "Third person options and toggles");

        thirdPersonSubMenu.AddToggle(Config.ThirdPerson.DisplayName,
            "Makes you able to change your person view to third person view (Default key-bind is Left Control + T)",
            Config.ThirdPerson);

        thirdPersonSubMenu.AddToggle(Config.ThirdPersonHotkeys.DisplayName,
            "Makes you able to use hotkeys when third person is activated", Config.ThirdPersonHotkeys);

        #endregion
    }
}