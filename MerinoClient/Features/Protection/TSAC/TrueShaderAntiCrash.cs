using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using MelonLoader;
using MerinoClient.Core;
using UnityEngine.SceneManagement;

namespace MerinoClient.Features.Protection.TSAC;

/*
 * Original project source (slightly edited compared to the original): https://github.com/knah/VRCMods/tree/master/TrueShaderAntiCrash
 */

internal class TrueShaderAntiCrash : FeatureComponent
{
    private static readonly Dictionary<string, int> OurOffsets = new()
    {
        {
            "aCEmIwSIcjYriBQDFjQlpTNNW1/kA8Wlbkqelmt1USOMB09cnKwK7QWyOulz9d7DEYJh4+vO0Ldv8gdH+dZCrg==", 0x819130
        }, // U2018.4.20 non-dev
        {
            "5dkhl/dWeTREXhHCIkZK17mzZkbjhTKlxb+IUSk+YaWzZrrV+G+M0ekTOEGjZ4dJuB4O3nU/oE3dycXWeJq9uA==", 0x79B3F0
        }, // U2019.4.28 non-dev
        {
            "MV6xP7theydao4ENbGi6BbiBxdZsgGOBo/WrPSeIqh6A/E00NImjUNZn+gL+ZxzpVbJms7nUb6zluLL3+aIcfg==", 0x79C060
        }, // U2019.4.29 non-dev
        {
            "ccZ4F7iE7a78kWdXdMekJzP7/ktzS5jOOS8IOITxa1C5Jg2TKxC0/ywY8F0o9I1vZHsxAO4eh7G2sOGzsR/+uQ==", 0x79CEE0
        }, // U2019.4.30 non-dev
        {
            "sgZUlX3+LSHKnTiTC+nXNcdtLOTrAB1fNjBLOwDdKzCyndlFLAdL0udR4S1szTC/q5pnFhG3Kdspsj5jvwLY1A==", 0x79F070
        } // U2019.4.31 non-dev
    };

    private static MelonPreferences_Category _category;

    public static ConfigValue<bool> LoopsEnabled;
    public static ConfigValue<bool> GeometryEnabled;
    public static ConfigValue<bool> TessEnabled;

    public static bool shouldLoad = true;

    private readonly ShaderFilterApi _filterApi;

    public TrueShaderAntiCrash()
    {
        if (IsModAlreadyPresent("True Shader Anticrash", "knah"))
        {
            shouldLoad = false;
            return;
        }

        string unityPlayerHash;
        {
            using var sha = SHA512.Create();
            using var unityPlayerStream = File.OpenRead("UnityPlayer.dll");
            unityPlayerHash = Convert.ToBase64String(sha.ComputeHash(unityPlayerStream));
        }

        if (!OurOffsets.TryGetValue(unityPlayerHash, out var offset))
        {
            MerinoLogger.Error($"TSAC: Unknown UnityPlayer hash: {unityPlayerHash}, mod will not work");
            return;
        }

        var pluginsPath = MelonUtils.GetGameDataDirectory() + "/Plugins";
        var deeperPluginsPath = Path.Combine(pluginsPath, "x86_64");
        if (Directory.Exists(deeperPluginsPath)) pluginsPath = deeperPluginsPath;

        const string dllName = ShaderFilterApi.DllName + ".dll";

        try
        {
            using var resourceStream = Assembly.GetExecutingAssembly()
                .GetManifestResourceStream(typeof(TrueShaderAntiCrash), dllName);
            using var fileStream = File.Open(pluginsPath + "/" + dllName, FileMode.Create, FileAccess.Write);

            resourceStream?.CopyTo(fileStream);
        }
        catch (IOException ex)
        {
            MerinoLogger.Msg(ex.ToString());
        }

        var process = Process.GetCurrentProcess();
        foreach (ProcessModule module in process.Modules)
        {
            if (!module.FileName.Contains("UnityPlayer"))
                continue;

            var loadLibraryAddress = module.BaseAddress + offset;
            var findAndLoadUnityPlugin =
                Marshal.GetDelegateForFunctionPointer<FindAndLoadUnityPlugin>(loadLibraryAddress);

            var strPtr = Marshal.StringToHGlobalAnsi(ShaderFilterApi.DllName);

            findAndLoadUnityPlugin(strPtr, out var loaded, 1);

            if (loaded == IntPtr.Zero)
            {
                MerinoLogger.Error("TSAC: Module load failed");
                return;
            }

            _filterApi = new ShaderFilterApi(loaded);

            Marshal.FreeHGlobal(strPtr);

            break;
        }

        _category = MelonPreferences.CreateCategory("True Shader Anticrash");

        LoopsEnabled = new ConfigValue<bool>("LimitLoops", true, "Limit loops", _category);
        GeometryEnabled = new ConfigValue<bool>("LimitGeometry", true, "Limit geometry shaders", _category);
        TessEnabled = new ConfigValue<bool>("LimitTesselation", true, "Limit tesselation", _category);

        IEnumerator WaitForRoomManagerAndUpdate()
        {
            while (RoomManager.field_Internal_Static_ApiWorldInstance_0 == null)
                yield return null;
            UpdateLimiters();
        }

        void UpdateLimiters()
        {
            var room = RoomManager.field_Internal_Static_ApiWorldInstance_0;
            if (room == null)
            {
                MelonCoroutines.Start(WaitForRoomManagerAndUpdate());
                return;
            }

            _filterApi.SetFilteringState(LoopsEnabled.Value, GeometryEnabled.Value, TessEnabled.Value);
        }

        LoopsEnabled.OnValueChanged += UpdateLimiters;
        GeometryEnabled.OnValueChanged += UpdateLimiters;
        TessEnabled.OnValueChanged += UpdateLimiters;

        var maxLoopIteration = new ConfigValue<int>("MaxLoopIterations", 128, "Max loop iterations", _category);
        maxLoopIteration.OnValueChanged += () => _filterApi.SetLoopLimit(maxLoopIteration);

        var maxGeometry = new ConfigValue<int>("MaxGeometryOutputs", 60, "Max geometry shader outputs", _category);
        maxGeometry.OnValueChanged += () => _filterApi.SetGeometryLimit(maxGeometry);

        var maxTess = new ConfigValue<float>("MaxTesselation", 5f, "Max tesselation power", _category);
        maxTess.OnValueChanged += () => _filterApi.SetMaxTesselationPower(maxTess);

        SceneManager.add_sceneLoaded(new Action<Scene, LoadSceneMode>((sc, _) =>
        {
            if (sc.buildIndex == -1) UpdateLimiters();
        }));

        SceneManager.add_sceneUnloaded(new Action<Scene>(_ => { _filterApi.SetFilteringState(false, false, false); }));

        UpdateLimiters();

        _filterApi!.SetLoopLimit(maxLoopIteration.Value);
        _filterApi.SetGeometryLimit(maxGeometry.Value);
        _filterApi!.SetMaxTesselationPower(maxTess.Value);
    }

    public override string FeatureName => GetType().Name;
    public override string OriginalAuthor => "knah";

    public static void ResetLimiterValues()
    {
        var tsacCategory = MelonPreferences.GetCategory(_category.Identifier);
        foreach (var entry in tsacCategory.Entries) entry.ResetToDefault();
    }

    [UnmanagedFunctionPointer(CallingConvention.FastCall)]
    private delegate void FindAndLoadUnityPlugin(IntPtr name, out IntPtr loadedModule, byte bEnableSomeDebug);
}