using MelonLoader;
using MerinoClient.Core;
using MerinoClient.Core.Managers;

namespace MerinoClient;

internal static class Config
{
    private static MelonPreferences_Category _category;

    public static void ResetConfig()
    {
        _category = MelonPreferences.GetCategory(ConfigManager.Instance.CategoryName);

        if (_category == null) return;

        foreach (var entry in _category.Entries) entry.ResetToDefault();
    }

    #region LOGGING

    public static ConfigValue<bool> LogObjectInstantiate { get; set; }
    public static ConfigValue<bool> LogUdonEvents { get; set; }
    public static ConfigValue<bool> OnlyGlobalUdonEvents { get; set; }
    public static ConfigValue<bool> LogPlayerEntries { get; set; }
    public static ConfigValue<bool> LogOnlyPhotonEntries { get; set; }
    public static ConfigValue<bool> LogRPCs { get; set; }
    public static ConfigValue<bool> AdvancedPhotonLogging { get; set; }
    public static ConfigValue<bool> FullPunLogLogLevel { get; set; }
    public static ConfigValue<bool> LogAPIRequests { get; set; }
    public static ConfigValue<bool> LogUnityErrors { get; set; }

    #endregion

    #region PRIVACY

    public static ConfigValue<bool> SendModeration { get; set; }
    public static ConfigValue<bool> FakeOffline { get; set; }
    public static ConfigValue<bool> SteamAPI { get; set; }
    public static ConfigValue<bool> SpoofPing { get; set; }
    public static ConfigValue<int> PingValue { get; set; }
    public static ConfigValue<bool> SpoofFPS { get; set; }

    #endregion

    #region RESTRICTIONS

    public static ConfigValue<bool> VRCStations { get; set; }
    public static ConfigValue<bool> MuteEveryone { get; set; }
    public static ConfigValue<bool> FreezeEveryone { get; set; }
    public static ConfigValue<bool> UdonEvents { get; set; }
    public static ConfigValue<bool> VideoPlayers { get; set; }
    public static ConfigValue<bool> Portals { get; set; }
    public static ConfigValue<bool> AllPortals { get; set; }
    public static ConfigValue<bool> NonFriendsPortals { get; set; }
    public static ConfigValue<bool> AskToPortal { get; set; }
    public static ConfigValue<bool> Pickups { get; set; }

    #endregion

    #region Visuals

    public static ConfigValue<bool> PerformanceStats { get; set; }
    public static ConfigValue<bool> PostProcessing { get; set; }
    public static ConfigValue<bool> XSIntegration { get; set; }
    public static ConfigValue<bool> XSIntegrationSound { get; set; }
    public static ConfigValue<bool> SocialInformation { get; set; }
    public static ConfigValue<bool> Notifications { get; set; }
    public static ConfigValue<string> ThemeColor { get; set; }

    #endregion

    #region GENERAL

    public static ConfigValue<bool> AppearFrozen { get; set; }
    public static ConfigValue<bool> ReModAPI { get; set; }
    public static ConfigValue<bool> SavePlayerPrefs { get; set; }
    public static ConfigValue<bool> Emojis { get; set; }

    #endregion

    #region WINGS

    public static ConfigValue<bool> Highlights { get; set; }
    public static ConfigValue<bool> ThirdPerson { get; set; }
    public static ConfigValue<bool> ThirdPersonHotkeys { get; set; }
    public static ConfigValue<bool> LocalClone { get; set; }

    #endregion
}