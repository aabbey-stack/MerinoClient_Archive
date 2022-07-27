using System.Linq;
using MerinoClient.Core.Managers;
using MerinoClient.Core.VRChat;
using MerinoClient.Utilities;
using UnityEngine;

namespace MerinoClient.MenuComponents;

internal class TargetRemoteMenuComponents : MenuComponent
{
    public override void OnQuickMenuInited(UiManager uiManager)
    {
        uiManager.TargetRemoteMenu.AddButton("Portal To", "Drops a portal to the target player's instance", () =>
        {
            var remoteUser = QuickMenuEx.SelectedUserRemote.GetIUser();
            var location = remoteUser.GetLocation().Split(':');

            if (location.Contains("private") || location.Length == 0)
            {
                QuickMenuEx.Instance.GetModalAlert()
                    .ShowModalAlert($"\'{remoteUser.GetDisplayName()}\' is in a private world");
                return;
            }

            PortalUtils.DropPortal(location[0], location[1]);
        }, ResourceManager.GetSprite("portal"));

        uiManager.TargetRemoteMenu.AddButton("Copy Instance",
            "Copies target player's instance link to your clipboard",
            () =>
            {
                var remoteUser = QuickMenuEx.SelectedUserRemote.GetIUser();
                var location = remoteUser.GetLocation().Split(':');

                if (location.Contains("private") || location.Length == 0)
                {
                    QuickMenuEx.Instance.GetModalAlert()
                        .ShowModalAlert($"\'{remoteUser.GetDisplayName()}\' is in a private world");
                    return;
                }

                GUIUtility.systemCopyBuffer =
                    $"https://vrchat.com/home/launch?worldId={location[0]}&instanceId={location[1]}";
            }, ResourceManager.GetSprite("copy-content"));

        uiManager.TargetRemoteMenu.AddButton("Copy User ID", "Copies target player's user ID", () =>
        {
            var selectedUser = QuickMenuEx.SelectedUserRemote.GetIUser();
            if (selectedUser == null) return;

            GUIUtility.systemCopyBuffer = selectedUser.GetUserId();
        }, ResourceManager.GetSprite("copy-content"));
    }
}