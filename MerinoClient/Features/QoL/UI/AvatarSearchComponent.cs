using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using MelonLoader;
using MerinoClient.Core.UI;
using MerinoClient.Core.VRChat;
using Newtonsoft.Json;
using UnhollowerRuntimeLib;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using UnityEngine.XR;
using VRC.Core;
using VRC.UI;
using AvatarList = Il2CppSystem.Collections.Generic.List<VRC.Core.ApiAvatar>;

namespace MerinoClient.Features.QoL.UI;

/*
 * Original source code al though stripped of favorites and rather heavily edited, only used to search for avatars
 * (I've asked Requi for a permission if I could use his ReModCE avatars API):
 * https://github.com/RequiDev/ReModCE/blob/master/ReModCE/Components/AvatarFavoritesComponent.cs
 */

internal class AvatarSearchComponent : FeatureComponent, IAvatarListOwner
{
    private const string ApiUrl = "https://remod-ce.requi.dev/api";

    //TODO: fix this path thing
    public const string ReModAPIPinFile = "\\ReModAPI_Pin.json";
    public static Dictionary<string, object> SavedReModAPIPin;

    private readonly AvatarList _searchedAvatars;

    private GameObject _avatarScreen;

    private Button.ButtonClickedEvent _changeButtonEvent;
    private HttpClient _httpClient;
    private HttpClientHandler _httpClientHandler;

    private int _loginRetries;
    private UnityAction<string> _searchAvatarsAction;
    private UiInputField _searchBox;
    private CAvatarList _searchedAvatarList;
    private string _userAgent = "";

    public AvatarSearchComponent()
    {
        if (!Config.ReModAPI.Value) return;

        if (File.Exists(ClientDirectory + ReModAPIPinFile))
        {
            SavedReModAPIPin =
                JsonConvert.DeserializeObject<Dictionary<string, object>>(
                    File.ReadAllText(ClientDirectory + ReModAPIPinFile));
        }
        else
        {
            SavedReModAPIPin = new Dictionary<string, object>();
            File.WriteAllText(ClientDirectory + ReModAPIPinFile, JsonConvert.SerializeObject(SavedReModAPIPin));
        }

        _searchedAvatars = new AvatarList();
    }

    public override string FeatureName => GetType().Name;
    public override string OriginalAuthor => "Requi";

    public AvatarList GetAvatars(CAvatarList avatarList)
    {
        return avatarList == _searchedAvatarList ? _searchedAvatars : null;
    }

    public void Clear(CAvatarList avatarList)
    {
        if (avatarList != _searchedAvatarList) return;
        _searchedAvatars.Clear();
        avatarList.RefreshAvatars();
    }

    public override void OnVRCUiManagerInited()
    {
        if (!Config.ReModAPI.Value) return;

        InitializeNetworkClient();

        _searchedAvatarList = new CAvatarList("ReMod API Search", this);

        var changeButton = GameObject.Find("UserInterface/MenuContent/Screens/Avatar/Change Button");
        if (changeButton != null)
        {
            var button = changeButton.GetComponent<Button>();
            _changeButtonEvent = button.onClick;

            button.onClick = new Button.ButtonClickedEvent();
            button.onClick.AddListener(new Action(ChangeAvatarChecked));
        }


        _searchAvatarsAction = DelegateSupport.ConvertDelegate<UnityAction<string>>(SearchAvatars);

        _avatarScreen = GameObject.Find("UserInterface/MenuContent/Screens/Avatar");
        _searchBox = GameObject
            .Find("UserInterface/MenuContent/Backdrop/Header/Tabs/ViewPort/Content/Search/InputField")
            .GetComponent<UiInputField>();
    }

    private void InitializeNetworkClient()
    {
        _httpClientHandler = new HttpClientHandler
        {
            UseCookies = true,
            CookieContainer = new CookieContainer()
        };
        _httpClient = new HttpClient(_httpClientHandler);

        var vrHeadset = XRDevice.isPresent ? XRDevice.model : "Desktop";
        vrHeadset = vrHeadset.Replace(' ', '_');

        _userAgent = $"MerinoClient/{vrHeadset}.{Application.version} ({SystemInfo.operatingSystem})";

        _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(_userAgent);
    }

    public override void OnUpdated()
    {
        if (!Config.ReModAPI.Value) return;

        if (_searchBox == null)
            return;

        if (!_avatarScreen.active) return;

        if (_searchBox.field_Public_Button_0.interactable) return;
        _searchBox.field_Public_Button_0.interactable = true;
        _searchBox.field_Public_UnityAction_1_String_0 = _searchAvatarsAction;
    }

    private void SearchAvatars(string searchTerm)
    {
        try
        {
            if (string.IsNullOrEmpty(searchTerm) || searchTerm.Length < 3)
            {
                MelonCoroutines.Start(
                    ShowAlertDelayed("That search term is too short, the search term has to be at least 3 characters",
                        0.2f));
                return;
            }

            if (searchTerm.Contains("crasher") || searchTerm.Contains("lagger") || searchTerm.Contains("crash") ||
                searchTerm.Contains("lag"))
            {
                MelonCoroutines.Start(
                    ShowAlertDelayed("You can't search for such avatars",
                        0.2f));
                return;
            }

            var request = new HttpRequestMessage(HttpMethod.Get, $"{ApiUrl}/search.php?searchTerm={searchTerm}");

            _httpClient.SendAsync(request).ContinueWith(rsp =>
            {
                var searchResponse = rsp.Result;
                if (!searchResponse.IsSuccessStatusCode)
                {
                    if (searchResponse.StatusCode == HttpStatusCode.Unauthorized)
                    {
                        LoginToAPI(APIUser.CurrentUser, () => SearchAvatars(searchTerm));
                        return;
                    }

                    searchResponse.Content.ReadAsStringAsync().ContinueWith(errorData =>
                    {
                        var errorMessage = JsonConvert.DeserializeObject<ApiError>(errorData.Result)?.Error;

                        MerinoLogger.Error($"Could not search for avatars: \"{errorMessage}\"");
                        if (searchResponse.StatusCode == HttpStatusCode.Forbidden)
                            MelonCoroutines.Start(
                                ShowAlertDelayed($"Could not search for avatars\nReason: \"{errorMessage}\""));
                    });
                }
                else
                {
                    searchResponse.Content.ReadAsStringAsync().ContinueWith(t =>
                    {
                        var avatars = JsonConvert.DeserializeObject<List<CAvatar>>(t.Result) ?? new List<CAvatar>();

                        if (!avatars.Any())
                        {
                            MelonCoroutines.Start(
                                ShowAlertDelayed($"No public avatars found from your search query: \"{searchTerm}\""));
                            return;
                        }

                        MelonCoroutines.Start(RefreshSearchedAvatars(avatars));
                    });
                }
            });
        }
        catch (Exception e)
        {
            MerinoLogger.Error("An exception occurred while trying to search an avatar in the ReMod API database\n" +
                               e);
        }
    }

    private IEnumerator RefreshSearchedAvatars(IEnumerable<CAvatar> results)
    {
        yield return new WaitForEndOfFrame();

        _searchedAvatars.Clear();

        foreach (var avi in results.Select(x => x.AsApiAvatar())) _searchedAvatars.Add(avi);

        _searchedAvatarList.RefreshAvatars();

        if (QuickMenuEx.Instance.IsActive()) UIManagerImpl.prop_UIManagerImpl_0.CloseQuickMenu();

        VRCUiManagerEx.Instance.ShowUi();
        VRCUiManagerEx.Instance.ShowScreen(QuickMenu.MainMenuScreenIndex.AvatarMenu);
    }

    private void ChangeAvatarChecked()
    {
        var currentAvatar = _searchedAvatarList.AvatarPedestal.field_Internal_ApiAvatar_0;
        if (currentAvatar.IsLocal) _changeButtonEvent.Invoke();
        else
            new ApiAvatar { id = currentAvatar.id }.Fetch(new Action<ApiContainer>(ac =>
                {
                    var updatedAvatar = ac.Model.Cast<ApiAvatar>();
                    switch (updatedAvatar.releaseStatus)
                    {
                        case "private" when updatedAvatar.authorId != APIUser.CurrentUser.id:
                            VRCUiPopupManager.prop_VRCUiPopupManager_0.ShowAlert("ReMod API",
                                "This avatar is private and you don't own it, you can't switch into it");
                            break;
                        case "unavailable":
                            VRCUiPopupManager.prop_VRCUiPopupManager_0.ShowAlert("ReMod API",
                                "This avatar has been deleted, you can't switch into it");
                            break;
                        default:
                            _changeButtonEvent.Invoke();
                            break;
                    }
                }),
                new Action<ApiContainer>(error =>
                {
                    VRCUiPopupManager.prop_VRCUiPopupManager_0.ShowAlert("ReMod API",
                        "Failed to get fetch an avatar: " + error.GetErrorMessage());
                }));
    }

    private void LoginToAPI(ApiModel user, Action onLogin)
    {
        if (_loginRetries >= 3)
        {
            MerinoLogger.Error(
                "Could not login to ReModCE API: Exceeded retries. Please restart your game and make sure your pin is correct!");
            return;
        }

        var request = new HttpRequestMessage(HttpMethod.Post, $"{ApiUrl}/login.php")
        {
            Content = new FormUrlEncodedContent(new List<KeyValuePair<string, string>>
            {
                new("user_id", user.id),
                new("pin", SavedReModAPIPin["PIN"].ToString())
            })
        };

        ++_loginRetries;
        _httpClient.SendAsync(request).ContinueWith(t =>
        {
            var loginResponse = t.Result;
            if (!loginResponse.IsSuccessStatusCode)
                loginResponse.Content.ReadAsStringAsync().ContinueWith(tsk =>
                {
                    var errorMessage = JsonConvert.DeserializeObject<ApiError>(tsk.Result)?.Error;

                    MerinoLogger.Error($"Could not login to ReMod API: \"{errorMessage}\"");
                    MelonCoroutines.Start(ShowAlertDelayed(
                        $"Could not login to ReMod API\nReason: \"{errorMessage}\" statusCode: {loginResponse.StatusCode}"));

                    // ReSharper disable once SwitchStatementHandlesSomeKnownEnumValuesWithDefault
                    switch (loginResponse.StatusCode)
                    {
                        case HttpStatusCode.Forbidden:
                            File.Delete(ClientDirectory + ReModAPIPinFile);
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                });
            else
                onLogin();
        });
    }

    private static IEnumerator ShowAlertDelayed(string message, float seconds = 0.5f)
    {
        if (VRCUiPopupManager.prop_VRCUiPopupManager_0 == null) yield break;

        yield return new WaitForSeconds(seconds);

        VRCUiPopupManager.prop_VRCUiPopupManager_0.ShowAlert("ReMod API", message);
    }
}

internal class ApiError
{
    [JsonProperty("error")] public string Error { get; set; }

    [JsonProperty("status_code")] public int StatusCode { get; set; }
}

[Serializable]
internal class CAvatar
{
    public ApiModel.SupportedPlatforms SupportedPlatforms = ApiModel.SupportedPlatforms.StandaloneWindows;

    public CAvatar()
    {
    }

    public CAvatar(ApiAvatar apiAvatar)
    {
        Id = apiAvatar.id;
        AvatarName = apiAvatar.name;
        AuthorId = apiAvatar.authorId;
        AuthorName = apiAvatar.authorName;
        Description = apiAvatar.description;
        AssetUrl = apiAvatar.assetUrl;
        ThumbnailUrl = apiAvatar.thumbnailImageUrl;
        SupportedPlatforms = apiAvatar.supportedPlatforms;
    }

    public string Id { get; set; }
    public string AvatarName { get; set; }
    public string AuthorId { get; set; }
    public string AuthorName { get; set; }
    public string Description { get; set; }
    public string AssetUrl { get; set; }
    public string ImageUrl { get; set; }
    public string ThumbnailUrl { get; set; }

    public ApiAvatar AsApiAvatar()
    {
        return new ApiAvatar
        {
            id = Id,
            name = AvatarName,
            authorId = AuthorId,
            authorName = AuthorName,
            description = Description,
            assetUrl = AssetUrl,
            thumbnailImageUrl = string.IsNullOrEmpty(ThumbnailUrl)
                ? string.IsNullOrEmpty(ImageUrl) ? "https://assets.vrchat.com/system/defaultAvatar.png" : ImageUrl
                : ThumbnailUrl,
            releaseStatus = "public",
            unityVersion = "2019.4.29f1",
            version = 1,
            apiVersion = 1,
            Endpoint = "avatars",
            Populated = false,
            assetVersion = new AssetVersion("2019.4.29f1", 0),
            tags = new Il2CppSystem.Collections.Generic.List<string>(0),
            supportedPlatforms = SupportedPlatforms
        };
    }

    public string ToJson()
    {
        return JsonConvert.SerializeObject(this);
    }
}