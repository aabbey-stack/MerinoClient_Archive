using MerinoClient.Core.Managers;
using MerinoClient.Core.UI.QuickMenu;
using MerinoClient.Core.Unity;
using MerinoClient.Core.VRChat;
using MerinoClient.DetourHooks.Events;
using MerinoClient.Features.Protection.BundleVerifier;
using UnityEngine;

namespace MerinoClient.MenuComponents;

internal class TargetComponents : MenuComponent
{
    public static MenuButton _forceAllowBundleButton;

    public override void OnQuickMenuInited(UiManager uiManager)
    {
        uiManager.TargetMenu.AddButton("Teleport To", "Teleports to you to a selected player",
            () =>
            {
                var selectedUser = QuickMenuEx.SelectedUserLocal.GetIUser();
                if (selectedUser == null) return;

                if (selectedUser.IsSelf())
                {
                    QuickMenuEx.Instance.GetModalAlert().ShowModalAlert("You can't teleport to yourself");
                    return;
                }

                var remotePlayerTransform = PlayerEx.PlayerManager.GetPlayer(selectedUser.GetUserId())
                    .transform;

                if (!remotePlayerTransform.position.IsSafe() ||
                    !remotePlayerTransform.rotation.eulerAngles.IsSafe())
                {
                    QuickMenuEx.Instance.ShowAlertDialog("Teleport To Error:",
                        $"Couldn't teleport to \"{selectedUser.GetDisplayName()}\" because their transform contains invalid position or rotation");
                    return;
                }

                PlayerEx.VRCPlayer.transform.position = remotePlayerTransform.position;
            });

        uiManager.TargetMenu.AddButton("Copy User ID", "Copies selected player's user ID", () =>
        {
            var selectedUser = QuickMenuEx.SelectedUserLocal.GetIUser();
            if (selectedUser == null) return;

            GUIUtility.systemCopyBuffer = selectedUser.GetUserId();
        }, ResourceManager.GetSprite("copy-content"));

        var avatarUtilitiesSubMenu = uiManager.TargetMenu.AddMenuPage($"{RichText("Avatar")} Utilities",
            "Advanced utilities for handling targeted player's avatar");

        #region avatarUtilitiesSubMenu

        avatarUtilitiesSubMenu.AddButton("Copy Avatar ID", "Copies selected player's avatar's ID",
            () =>
            {
                var selectedUser = QuickMenuEx.SelectedUserLocal.field_Private_IUser_0;
                if (selectedUser == null) return;

                var apiAvatar = PlayerEx.PlayerManager.GetPlayer(selectedUser.GetUserId())
                    .GetApiAvatar();

                if (apiAvatar.releaseStatus == "private")
                    QuickMenuEx.Instance.ShowConfirmDialog("Copy Avatar ID",
                        $"Avatar \"{apiAvatar.name}\" is <color=red>private and not yours</color>, do you still wish to copy its avatarId?",
                        () => { GUIUtility.systemCopyBuffer = $"https://vrchat.com/home/avatar/{apiAvatar.id}"; });
                else
                    GUIUtility.systemCopyBuffer = $"https://vrchat.com/home/avatar/{apiAvatar.id}";
            }, ResourceManager.GetSprite("copy-content"));

        avatarUtilitiesSubMenu.AddButton("Copy Asset URL",
            "Copies the selected player's avatar's Asset URL",
            () =>
            {
                var selectedUser = QuickMenuEx.SelectedUserLocal.GetIUser();
                if (selectedUser == null) return;

                var apiAvatar = PlayerEx.PlayerManager.GetPlayer(selectedUser.GetUserId())
                    .GetApiAvatar();

                switch (apiAvatar.releaseStatus)
                {
                    case "private" when !apiAvatar.IsOwn():
                        QuickMenuEx.Instance.ShowAlertDialog("Copy Asset URL",
                            $"Avatar \"{apiAvatar.name}\" is <color=red>private and not yours</color>, you can't copy its assetUrl");
                        return;
                    case "public" when !apiAvatar.IsOwn():
                        QuickMenuEx.Instance.ShowConfirmDialog("Copy Asset URL",
                            $"Avatar \"{apiAvatar.name}\" is <color=red>not yours</color>, do you still wish to copy its assetUrl?",
                            () => { GUIUtility.systemCopyBuffer = apiAvatar.assetUrl; });
                        break;
                    default:
                        GUIUtility.systemCopyBuffer = apiAvatar.assetUrl;
                        break;
                }
            }, ResourceManager.GetSprite("copy-content"));

        avatarUtilitiesSubMenu.AddButton("Local Clone",
            "Clone other people's avatars locally (including private avatars)",
            () =>
            {
                var selectedUser = QuickMenuEx.SelectedUserLocal.GetIUser();
                if (selectedUser == null) return;

                if (selectedUser.IsSelf())
                {
                    QuickMenuEx.Instance.GetModalAlert().ShowModalAlert("You can't local clone yourself");
                    return;
                }

                if (!Config.LocalClone.Value)
                {
                    QuickMenuEx.Instance.GetModalAlert().ShowModalAlert("You don't have local clone enabled");
                    return;
                }

                LoadBalancingClientHook.AvatarDictCache = PlayerEx.PlayerManager.GetPlayer(selectedUser.GetUserId())
                    .GetPlayer().field_Private_Hashtable_0["avatarDict"];
                PlayerEx.VRCPlayer.ReloadAvatar();
            });

        #endregion

        _forceAllowBundleButton = uiManager.TargetMenu.AddButton("Force Allow Bundle",
            "Forces the asset-bundle to load even if it has been rejected",
            () =>
            {
                var selectedUser = QuickMenuEx.SelectedUserLocal.GetIUser();
                if (selectedUser == null) return;

                var player = PlayerEx.PlayerManager.GetPlayer(selectedUser.GetUserId());
                if (player == null)
                    return;

                var apiUser = player.GetAPIUser();
                if (apiUser == null)
                    return;

                var apiAvatar = player.GetApiAvatar();
                if (apiAvatar == null)
                    return;

                if (!BundleVerifierMod.BadBundleCache.Contains(apiAvatar.assetUrl) ||
                    BundleVerifierMod.ForceAllowedCache.Contains(apiAvatar.assetUrl))
                {
                    QuickMenuEx.Instance.GetModalAlert()
                        .ShowModalAlert("This asset is already allowed and has passed bundle verifier");
                    return;
                }

                BundleVerifierMod.ForceAllowedCache.Add(apiAvatar.assetUrl);
                PlayerEx.VRCPlayer.ReloadAllAvatars(true);
            }, ResourceManager.GetSprite("unity"));
    }
}