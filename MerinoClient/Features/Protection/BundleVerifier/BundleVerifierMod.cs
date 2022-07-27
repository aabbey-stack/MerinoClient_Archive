using System.Collections;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Text.RegularExpressions;
using MelonLoader;
using MerinoClient.Core;
using MerinoClient.MenuComponents;
using VRC.Core;
#if !DEBUG
#endif

namespace MerinoClient.Features.Protection.BundleVerifier;

/*
 * Original project source: https://github.com/knah/VRCMods/tree/master/AdvancedSafety/BundleVerifier
 */

internal class BundleVerifierMod : FeatureComponent
{
    private const string VerifierVersion = "1.4-2019.4.31";
    private const string SettingsCategory = "ASBundleVerifier";

    public static ConfigValue<int> TimeLimit;
    public static ConfigValue<int> MemoryLimit;
    public static ConfigValue<int> ComponentLimit;

    public static ConfigValue<bool> OnlyPublics;
    public static ConfigValue<bool> EnabledSetting;

    internal static BundleHashCache BadBundleCache;
    internal static BundleHashCache ForceAllowedCache;

    internal static string BundleVerifierPath;

    public static bool shouldLoad = true;

    private static readonly Regex OurUrlRegex = new("file_([^/]+)/([^/]+)");

    public BundleVerifierMod()
    {
        if (IsModAlreadyPresent("Advanced Safety", "knah, Requi, Ben", "1.6.2",
                "BundleVerifier mod is disabled due to it already being present in the latest Advanced Safety"))
        {
            shouldLoad = false;
            return;
        }

        var category = MelonPreferences.CreateCategory(SettingsCategory, "Advanced Safety - Bundles");

        TimeLimit = new ConfigValue<int>(nameof(TimeLimit), 15, "Time limit (seconds)", category);
        MemoryLimit = new ConfigValue<int>("MemLimit", 2048, "Memory limit (megabytes)", category);
        ComponentLimit =
            new ConfigValue<int>(nameof(ComponentLimit), 10_000, "Component limit (0=unlimited)", category);

        EnabledSetting = new ConfigValue<bool>("Enabled", true, "Check for corrupted bundles", category);
        EnabledSetting.OnValueChanged += () => { TargetComponents._forceAllowBundleButton.SetActive(EnabledSetting); };
        OnlyPublics = new ConfigValue<bool>("OnlyPublics", true, "Only check bundles in public worlds", category);

        BadBundleCache = new BundleHashCache(Path.Combine(MelonUtils.UserDataDirectory, "BadBundleHashes.bin"));
        ForceAllowedCache = new BundleHashCache(null);

        var initSuccess = BundleDownloadMethods.Init();
        if (!initSuccess) return;

        try
        {
            PrepareVerifierDir();
        }
        catch (IOException ex)
        {
            MerinoLogger.Error("BV: Unable to extract bundle verifier app, the mod will not work", ex);
            return;
        }

        EnabledSetting.OnValueChanged += () => MelonCoroutines.Start(CheckInstanceType());
        OnlyPublics.OnValueChanged += () => MelonCoroutines.Start(CheckInstanceType());
    }

    public override string FeatureName => GetType().Name;
    public override string OriginalAuthor => "knah, Requi, Ben";

    public override void OnApplicationQuit()
    {
        BadBundleCache?.Dispose();
    }

    public static void OnLeftRoom()
    {
        BundleDlInterceptor.ShouldIntercept = false;
    }

    public static void OnJoinedRoom()
    {
        if (!shouldLoad) return;
        MelonCoroutines.Start(CheckInstanceType());
    }

    private static IEnumerator CheckInstanceType()
    {
        while (RoomManager.field_Internal_Static_ApiWorldInstance_0 == null)
            yield return null;

        if (!EnabledSetting.Value)
        {
            BundleDlInterceptor.ShouldIntercept = false;
            yield break;
        }

        var currentInstance = RoomManager.field_Internal_Static_ApiWorldInstance_0;
        BundleDlInterceptor.ShouldIntercept = !OnlyPublics.Value || currentInstance.type == InstanceAccessType.Public;
    }

    private static void PrepareVerifierDir()
    {
        var baseDir = Path.Combine(MelonUtils.UserDataDirectory, "BundleVerifier");
        Directory.CreateDirectory(baseDir);
        BundleVerifierPath = Path.Combine(baseDir, "BundleVerifier.exe");
        var versionFile = Path.Combine(baseDir, "version.txt");
        if (File.Exists(versionFile))
        {
            var existingVersion = File.ReadAllText(versionFile);
            if (existingVersion == VerifierVersion) return;
        }

        BadBundleCache.Clear();

        File.Copy(Path.Combine(MelonUtils.GameDirectory, "UnityPlayer.dll"), Path.Combine(baseDir, "UnityPlayer.dll"),
            true);
        //https://github.com/knah/VRCMods/blob/72bd9f0e8b139d37be7bfe09a7d93294312f8d5b/TrueShaderAntiCrash/TrueShaderAntiCrashMod.cs#L59 way better imo
        using var zipFile =
            new ZipArchive(
                Assembly.GetExecutingAssembly()
                    .GetManifestResourceStream(typeof(BundleVerifierMod), "BundleVerifier.zip")!, ZipArchiveMode.Read,
                false);
        foreach (var zipArchiveEntry in zipFile.Entries)
        {
            var targetFile = Path.Combine(baseDir, zipArchiveEntry.FullName);
            var looksLikeDir = Path.GetFileName(targetFile).Length == 0;
            Directory.CreateDirectory(looksLikeDir
                ? targetFile
                : Path.GetDirectoryName(targetFile)!);
            if (!looksLikeDir)
                zipArchiveEntry.ExtractToFile(targetFile, true);
        }

        File.WriteAllText(versionFile, VerifierVersion);
    }

    internal static (string, string) SanitizeUrl(string url)
    {
        var matches = OurUrlRegex.Match(url);
        return !matches.Success ? ("", url) : (matches.Groups[1].Value, matches.Groups[2].Value);
    }
}