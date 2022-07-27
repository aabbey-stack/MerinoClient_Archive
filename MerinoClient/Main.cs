using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using MelonLoader;
using MerinoClient.Core;
using MerinoClient.Core.Managers;
using MerinoClient.Core.Unity;
using MerinoClient.Core.VRChat;
using MerinoClient.DetourHooks;
using MerinoClient.HarmonyPatches;
using UnityEngine;
using VRC.Core;
using VRC.UI.Core;
using ConfigManager = MerinoClient.Core.Managers.ConfigManager;
using Logger = VRC.Core.Logger;
using Object = UnityEngine.Object;
using Resources = MerinoClient.Properties.Resources;
#if DEPLOY
#endif

// NOT a modified client. It's a game >> MOD << for educational purposes 

namespace MerinoClient
{
    internal static class ModInfo
    {
        public const string Version = "1.0.0.2";
#if DEBUG
        public const string Name = "Merino[DEV]";
        public const string Author = "abbey";
#endif
    }

#if !DEBUG
    public static class Main
#else
    public class Main : MelonMod
#endif
    {
        private static readonly List<FeatureComponent> FeatureComponents = new();
        private static readonly List<MenuComponent> MenuComponents = new();

        private static UiManager _uiManager;

        public static readonly HarmonyLib.Harmony MerinoHarmony = new("MerinoClient");

        public static ConfigValue<bool> streamerMode;


#if !DEBUG
        public static void OnApplicationStart()
#else
        public override void OnApplicationStart()
#endif
        {
            try
            {
                var buildDate = Resources.BuildDate.Trim();
                MerinoLogger.Msg(ConsoleColor.DarkGray, $"Build date: {buildDate}");
#if DEBUG
                var watch = new Stopwatch();
                watch.Start();
#endif
                Directory.CreateDirectory(FeatureComponent.ClientDirectory);

                //https://stackoverflow.com/a/18216908 may be faster
                ResourceManager.LoadResources(typeof(Main).Assembly);

                EnableDisableListener.RegisterSafe();

                ConfigManager.Create(nameof(MerinoClient));

                streamerMode = new ConfigValue<bool>(nameof(streamerMode), false);

                InstanceCreator.InitializeMenus(MenuComponents);
                InstanceCreator.InitializeFeatures(FeatureComponents);

                DetourHookManager.Initialize();
                HarmonyPatcherManager.Initialize();

                //DumpIdentifiers();
#if DEBUG
                watch.Stop();
                MerinoLogger.Msg($"Finished main initialization in {watch.ElapsedMilliseconds} ms");
#else
                MerinoLogger.Msg("Finished initialization");
#endif
            }
            catch (Exception e)
            {
                MerinoLogger.Error($"Error occurred on OnApplicationStart:\n{e}");
            }
        }


#if !DEBUG
        public static void OnApplicationLateStart()
#else
        public override void OnApplicationLateStart()
#endif
        {
            try
            {
                if (streamerMode) return;
                MelonCoroutines.Start(WaitForUiManager());
            }
            catch (Exception e)
            {
                MerinoLogger.Error($"Error occurred on OnApplicationLateStart:\n{e}");
            }
        }

#if !DEBUG
        public static void OnPreferencesLoaded()
#else
        public override void OnPreferencesLoaded()
#endif
        {
        }


        private static IEnumerator WaitForUiManager()
        {
            while (VRCUiManager.field_Private_Static_VRCUiManager_0 == null) yield return null;
            OnUiManagerInitEarly();

            while (UIManager.field_Private_Static_UIManager_0 == null) yield return null;
            while (GameObject.Find("UserInterface").GetComponentInChildren<VRC.UI.Elements.QuickMenu>(true) == null)
                yield return null;
            yield return null;

            OnUiManagerInit();
        }

        private static void OnUiManagerInitEarly()
        {
            try
            {
                foreach (var f in FeatureComponents) f.OnVRCUiManagerInited();
                var flowManager = VRCFlowCommandLine.field_Internal_Static_VRCFlowCommandLine_0;
                for (var level = DebugLevel.Always; level <= DebugLevel.All; ++level) Logger.AddDebugLevel(level);
                flowManager.field_Public_Boolean_8 = true;
            }
            catch (Exception e)
            {
                MerinoLogger.Error($"Error occurred on VRCUiManagerInit:\n{e}");
            }
        }

        private static void OnUiManagerInit()
        {
            try
            {
                _uiManager = new UiManager($"Merino{MenuComponent.RichText("Client")}",
                    ResourceManager.GetSprite("sweat-droplets-white"));

                _uiManager.MainMenu.AddCategoryPage($"{MenuComponent.RichText("Logging")}",
                    "Logging features including photon events, player joins/leaves etc",
                    ResourceManager.GetSprite("eye"));

                _uiManager.MainMenu.AddCategoryPage($"{MenuComponent.RichText("Privacy")}",
                    "Determine what players and VRChat knows about you",
                    ResourceManager.GetSprite("hidden"));

                _uiManager.MainMenu.AddMenuPage($"{MenuComponent.RichText("Restrictions")}",
                    "Limit certain actions from affecting you",
                    ResourceManager.GetSprite("restricted-area"));

                _uiManager.MainMenu.AddMenuPage($"{MenuComponent.RichText("Visuals")}",
                    "Customize some aspects of the game's visuals, including post processing",
                    ResourceManager.GetSprite("ui"));

                _uiManager.MainMenu.AddMenuPage($"{MenuComponent.RichText("General")}",
                    "More generalized features that don't fit in any category in particular",
                    ResourceManager.GetSprite("gear"));

                _uiManager.MainMenu.AddButton("Reset Config",
                    "Resets your config to a default one",
                    () =>
                    {
                        QuickMenuEx.Instance.ShowConfirmDialog("Reset Config",
                            "Do you really wish to reset your config?", Config.ResetConfig);
                    });

                _uiManager.MainMenu.AddButton($"User: {MenuComponent.RichText(Authentication.Authentication.username)}",
                    "Displays your website tied account username", () => { },
                    ResourceManager.GetSprite("user")).Interactable = false;

                foreach (var m in MenuComponents) m.OnQuickMenuInited(_uiManager);
                foreach (var f in FeatureComponents) f.OnQuickMenuInited(_uiManager);
            }
            catch (Exception e)
            {
                MerinoLogger.Error($"Error occurred while creating a menu:\n{e}");
            }
        }


#if !DEBUG
        public static void OnSceneWasLoaded(int buildIndex, string sceneName)
#else
        public override void OnSceneWasLoaded(int buildIndex, string sceneName)
#endif
        {
            foreach (var f in FeatureComponents) f.OnSceneWasLoaded(buildIndex, sceneName);

            switch (buildIndex)
            {
                case -1:
                    foreach (var m in MenuComponents) m.OnSceneLoaded();
                    break;
            }
        }

#if !DEBUG
        public static void OnSceneWasInitialized(int buildIndex, string sceneName)
#else
        public override void OnSceneWasInitialized(int buildIndex, string sceneName)
#endif
        {
        }

#if !DEBUG
        public static void OnUpdate()
#else
        public override void OnUpdate()
#endif
        {
#if !DEBUG
            Authentication.Authentication.Initialize();
#endif
            foreach (var f in FeatureComponents) f.OnUpdated();
        }

#if !DEBUG
        public static void OnFixedUpdate()
#else
        public override void OnFixedUpdate()
#endif
        {
        }

#if !DEBUG
        public static void OnLateUpdate()
#else
        public override void OnLateUpdate()
#endif
        {
        }

#if !DEBUG
        public static void OnGUI()
#else
        public override void OnGUI()
#endif
        {
        }

#if !DEBUG
        public static void OnPreferencesSaved()
#else
        public override void OnPreferencesSaved()
#endif
        {
        }

#if !DEBUG
        public static void OnApplicationQuit()
#else
        public override void OnApplicationQuit()
#endif
        {
            MelonPreferences.Save();
            foreach (var f in FeatureComponents) f.OnApplicationQuit();
            Object.FindObjectOfType<VRCApplication>().OnApplicationQuit();
            Process.GetCurrentProcess().Kill();
        }


        /*private static void DumpIdentifiers()
        {
            MerinoLogger.Msg("Device name: " + SystemInfo.deviceName);
            MerinoLogger.Msg("Graphics Device ID: " + SystemInfo.graphicsDeviceID);
            MerinoLogger.Msg("Graphics Device Name: " + SystemInfo.graphicsDeviceName);
            MerinoLogger.Msg("Graphics Device Vendor: " + SystemInfo.graphicsDeviceVendor);
            MerinoLogger.Msg("Graphics Device VendorID: " + SystemInfo.graphicsDeviceVendorID);
            MerinoLogger.Msg("Graphics Memory Size: " + SystemInfo.graphicsMemorySize);
            MerinoLogger.Msg("Processor Count: " + SystemInfo.processorCount);
            MerinoLogger.Msg("Processor Frequency: " + SystemInfo.processorFrequency);
            MerinoLogger.Msg("Processor Type: " + SystemInfo.processorType);
        }*/
    }
}