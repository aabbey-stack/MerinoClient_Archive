using MerinoClient.Core;
using MerinoClient.Core.Managers;
using MerinoClient.Core.UI.QuickMenu;
using Photon.Pun;

namespace MerinoClient.MenuComponents.MainMenu;

internal class LoggingComponent : MenuComponent
{
    private static MenuToggle _debugOnlyPhotonEntriesToggle;

    public LoggingComponent()
    {
        Config.LogObjectInstantiate =
            new ConfigValue<bool>(nameof(Config.LogObjectInstantiate), false, "Object Instantiate");

        Config.LogUdonEvents = new ConfigValue<bool>(nameof(Config.LogUdonEvents), false, "Udon Events");
        Config.OnlyGlobalUdonEvents = new ConfigValue<bool>(nameof(Config.OnlyGlobalUdonEvents), false, "Global Only");

        Config.LogPlayerEntries = new ConfigValue<bool>(nameof(Config.LogPlayerEntries), true, "Player Entries");
        Config.LogPlayerEntries.OnValueChanged += () =>
        {
            _debugOnlyPhotonEntriesToggle.SetActive(Config.LogPlayerEntries);
        };
        Config.LogOnlyPhotonEntries = new ConfigValue<bool>(nameof(Config.LogOnlyPhotonEntries), false, "Photon Only");

        Config.LogRPCs = new ConfigValue<bool>(nameof(Config.LogRPCs), false, "RPCs");

        Config.AdvancedPhotonLogging =
            new ConfigValue<bool>(nameof(Config.AdvancedPhotonLogging), false, "Advanced Log");

        Config.FullPunLogLogLevel = new ConfigValue<bool>(nameof(Config.FullPunLogLogLevel), false, "FullPunLog");
        Config.FullPunLogLogLevel.OnValueChanged += () =>
        {
            PhotonNetwork.field_Public_Static_PunLogLevel_0 =
                Config.FullPunLogLogLevel ? PunLogLevel.Full : PunLogLevel.ErrorsOnly;
        };

        Config.LogAPIRequests = new ConfigValue<bool>(nameof(Config.LogAPIRequests), false, "API Requests");

        Config.LogUnityErrors = new ConfigValue<bool>(nameof(Config.LogUnityErrors), false, "Unity Errors");
    }

    public override void OnQuickMenuInited(UiManager uiManager)
    {
        var loggingCategoryPage = uiManager.MainMenu.GetCategoryPage("Logging");

        #region handyLoggingCategory

        var handyLoggingCategory = loggingCategoryPage.AddCategory($"{RichText("Handy")} Options");
        handyLoggingCategory.AddToggle(Config.LogPlayerEntries.DisplayName,
            "Logs OnPlayerJoined and OnPlayerLeft events",
            Config.LogPlayerEntries);

        _debugOnlyPhotonEntriesToggle = handyLoggingCategory.AddToggle(Config.LogOnlyPhotonEntries.DisplayName,
            "Logs only photon alternatives of OnPlayerJoined and OnPlayerLeft events",
            Config.LogOnlyPhotonEntries);

        handyLoggingCategory.AddToggle(Config.AdvancedPhotonLogging.DisplayName,
            "Enables advanced logging of NetworkData, can be viewed by using --enable-debug-gui or output_log.txt",
            Config.AdvancedPhotonLogging);

        #endregion

        #region nicheLoggingCategory

        var nicheLoggingCategory = loggingCategoryPage.AddCategory($"{RichText("Niche")} Options");
        nicheLoggingCategory.AddToggle(Config.LogObjectInstantiate.DisplayName,
            "Logs ObjectInstantiate events, useful for debugging spawned objects",
            Config.LogObjectInstantiate);

        nicheLoggingCategory.AddToggle(Config.LogRPCs.DisplayName,
            "Logs all valid RPCs sent by you and other players through the network",
            Config.LogRPCs);

        nicheLoggingCategory.AddToggle(Config.LogAPIRequests.DisplayName, "Logs all API calls you send to VRChat's api",
            Config.LogAPIRequests);

        #endregion

        #region udonLoggingCategory

        var udonLoggingCategory = loggingCategoryPage.AddCategory($"{RichText("Udon")} Options");
        udonLoggingCategory.AddToggle(Config.LogUdonEvents.DisplayName, "Logs all type of udon events you send",
            Config.LogUdonEvents);

        udonLoggingCategory.AddToggle(Config.OnlyGlobalUdonEvents.DisplayName,
            "Logs only networked udon events you send",
            Config.OnlyGlobalUdonEvents);

        #endregion

        #region advancedLoggingCategory

        var advancedLoggingCategory = loggingCategoryPage.AddCategory($"{RichText("Advanced")} Options");

        advancedLoggingCategory.AddToggle(Config.FullPunLogLogLevel.DisplayName,
            "Enables full debug level of built in photon networking logging, only useful in particular cases",
            Config.FullPunLogLogLevel);

        advancedLoggingCategory.AddToggle(Config.LogUnityErrors.DisplayName,
            "Logs all unity il2cpp errors callbacks, useful against corrupted assets", Config.LogUnityErrors);

        #endregion
    }
}