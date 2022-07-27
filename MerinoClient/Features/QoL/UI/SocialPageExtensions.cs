using System;
using System.Collections;
using MelonLoader;
using MerinoClient.Core.UI;
using MerinoClient.Core.Unity;
using MerinoClient.Core.VRChat;
using MerinoClient.Utilities;
using UnityEngine;
using VRC.Core;
using VRC.DataModel;
using VRC.UI;

namespace MerinoClient.Features.QoL.UI;

internal class SocialPageExtensions : FeatureComponent
{
    private static GameObject _viewOnVRChatGameObject;

    private static GameObject _nameTextGameObject;

    private static UiButton _teleportToButton;
    private static UiButton _portalToButton;
    private static UiButton _copyInstanceButton;

    private static UiText _dateJoinedText;
    private static UiText _usernameText;

    private static PageUserInfo _pageUserInfo;
    private static APIUser _apiUser;
    private static IUser _iUser;
    private GameObject _socialMenu;

    public override string FeatureName => GetType().Name;
    public override string OriginalAuthor => "abbey";

    public override void OnVRCUiManagerInited()
    {
        _pageUserInfo = PlayerEx.PageUserInfo;

        _socialMenu = GameObject.Find("UserInterface/MenuContent/Screens/Social");
        _viewOnVRChatGameObject =
            GameObject.Find("UserInterface/MenuContent/Screens/UserInfo/ViewUserOnVRChatWebsiteButton");

        var viewOnVRChatGameObjectTransform = _viewOnVRChatGameObject.GetComponent<RectTransform>();
        viewOnVRChatGameObjectTransform.localScale = new Vector3(.84f, .84f, .84f);
        viewOnVRChatGameObjectTransform.anchoredPosition = new Vector2(84, 278);

        var buttonParent = _viewOnVRChatGameObject.transform.GetParent();

        static void Teleport()
        {
            _apiUser = _pageUserInfo.GetAPIUser();

            var remotePlayerTransform = PlayerEx.PlayerManager.GetPlayer(_apiUser.id)
                .GetVRCPlayer().transform;

            if (!remotePlayerTransform.position.IsSafe() ||
                !remotePlayerTransform.rotation.eulerAngles.IsSafe())
            {
                VRCUiPopupManager.prop_VRCUiPopupManager_0.ShowAlert("Teleport To Error:",
                    $"Couldn't teleport to \"{_apiUser.displayName}\" because their transform contains invalid position or rotation");
                return;
            }

            PlayerEx.VRCPlayer.transform.position = remotePlayerTransform.transform.position;
            VRCUiManagerEx.Instance.CloseUi();
        }

        _teleportToButton = new UiButton("Teleport To", new Vector2(-528, -234), new Vector2(1, 1), Teleport,
            buttonParent);

        _portalToButton = new UiButton("Portal To", new Vector2(-679, -167), new Vector2(1, 1), () =>
        {
            _iUser = _pageUserInfo.GetIUser();
            var iUserLocation = _iUser.GetLocation().Split(':');
            PortalUtils.DropPortal(iUserLocation[0], iUserLocation[1]);
            VRCUiManagerEx.Instance.CloseUi();
        }, buttonParent);

        _copyInstanceButton = new UiButton("Copy Instance", new Vector2(-528, -167), new Vector2(1, 1), () =>
        {
            _iUser = _pageUserInfo.GetIUser();
            var iUserLocation = _iUser.GetLocation().Split(':');
            GUIUtility.systemCopyBuffer =
                $"https://vrchat.com/home/launch?worldId={iUserLocation[0]}&instanceId={iUserLocation[1]}";
        }, buttonParent);

        var unused = new UiButton("Copy User ID", new Vector2(-377, -167), new Vector2(1f, 1f), () =>
        {
            _iUser = _pageUserInfo.GetIUser();
            var apiUserId = _iUser.GetUserId();
            GUIUtility.systemCopyBuffer = apiUserId;
        }, buttonParent);

        //https://github.com/markviews/Invite-fix/blob/47781d154d7e411f156a232287a53b34b5fec43f/Invite%2B%20fix/Invite%2BFix.cs
        buttonParent.gameObject.AddComponent<EnableDisableListener>().OnEnableEvent += () =>
        {
            MelonCoroutines.Start(DelayRun(UpdateInteractable));
        };

        _nameTextGameObject = GameObject.Find("UserInterface/MenuContent/Screens/UserInfo/User Panel/NameText");
        var textParent = _nameTextGameObject.GetComponent<RectTransform>().parent;

        //please be nice I was autistic back then....
        _dateJoinedText = new UiText("date_joined: 2017-11-04", new Vector2(146, 23), new Vector2(1, 1), null,
            textParent)
        {
            GameObject =
            {
                name = "DateJoinedText"
            }
        };

        _usernameText = new UiText("username: qqqqqqqqqq", new Vector2(550, 23), new Vector2(1, 1), null, textParent)
        {
            GameObject =
            {
                name = "UsernameText"
            }
        };

        //https://github.com/markviews/Invite-fix/blob/47781d154d7e411f156a232287a53b34b5fec43f/Invite%2B%20fix/Invite%2BFix.cs
        textParent.gameObject.AddComponent<EnableDisableListener>().OnEnableEvent += () =>
        {
            MelonCoroutines.Start(DelayRun(UpdateText));
        };

        if (Config.SocialInformation.Value) return;

        _dateJoinedText.Active = false;
        _usernameText.Active = false;
    }

    private IEnumerator DelayRun(Action action)
    {
        yield return new WaitForSeconds(0.1f);

        while (_socialMenu.active) yield return null;

        action.Invoke();
    }

    private static void UpdateText()
    {
        _apiUser = _pageUserInfo.GetAPIUser();

        if (_apiUser == null) return;

        if (string.IsNullOrEmpty(_apiUser.date_joined) || string.IsNullOrEmpty(_apiUser.username))
            APIUser.FetchUser(_apiUser.id, new Action<APIUser>(user =>
                {
                    _dateJoinedText.Text = $"date_joined: {user.date_joined}";
                    _usernameText.Text = $"username: {user.username}";
                }),
                new Action<string>(error =>
                {
                    MerinoLogger.Error($"Could not fetch APIUser object of {_apiUser.id}, {error}");
                }));

        _dateJoinedText.Text = $"date_joined: {_apiUser.date_joined}";
        _usernameText.Text = $"username: {_apiUser.username}";
    }

    public static void ChangeSocialInformation(bool enabled)
    {
        _dateJoinedText.Active = enabled;
        _usernameText.Active = enabled;
    }

    private static void UpdateInteractable()
    {
        static void SetInteractable(bool interactable)
        {
            _portalToButton.Interactable = interactable;
            _copyInstanceButton.Interactable = interactable;
        }

        _pageUserInfo = PlayerEx.PageUserInfo;

        if (_pageUserInfo == null)
        {
            SetInteractable(false);
            return;
        }

        _apiUser = _pageUserInfo.GetAPIUser();

        if (_apiUser == null)
        {
            SetInteractable(false);
            return;
        }

        if (_apiUser.IsSelf || !_apiUser.IsUserInRoom()) _teleportToButton.Interactable = false;
        else _teleportToButton.Interactable = true;

        if (_apiUser.IsSelf)
        {
            SetInteractable(false);
        }
        else if ((!_apiUser.IsSelf && string.IsNullOrEmpty(_apiUser.location)) ||
                 _apiUser.location.Contains("private") ||
                 _apiUser.location.Contains("offline"))
        {
            SetInteractable(false);
            return;
        }

        SetInteractable(true);

        if (_apiUser.IsUserInRoom()) _portalToButton.Interactable = false;
    }
}