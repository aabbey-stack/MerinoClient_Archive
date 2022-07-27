using System;
using System.IO;
using System.Linq;
using MelonLoader;
using MerinoClient.Core.Managers;

namespace MerinoClient;

public class FeatureDisabled : Attribute
{
}

internal class FeatureComponent
{
    public static readonly string ClientDirectory = Path.Combine(MelonUtils.UserDataDirectory, "Merino");
    public virtual string FeatureName => "FeatureName";
    public virtual string OriginalAuthor => "OriginalAuthor";

    public virtual void OnVRCUiManagerInited()
    {
    }

    public virtual void OnQuickMenuInited(UiManager uiManager)
    {
    }

    public virtual void OnUpdated()
    {
    }

    public virtual void OnSceneWasLoaded(int buildIndex, string name)
    {
    }

    public virtual void OnApplicationQuit()
    {
    }

    protected static bool IsModAlreadyPresent(string modName, string authorName, string version = null,
        string message = null)
    {
        void Warning(string customMessage)
        {
            MerinoLogger.Warning(string.IsNullOrEmpty(customMessage)
                ? $"Found \"{modName}\" Mod by {authorName}, not loading standalone"
                : customMessage);
        }

        if (!string.IsNullOrEmpty(version))
        {
            if (!MelonHandler.Mods.Any(i =>
                    i.Info.Name == modName && i.Info.Author == authorName && i.Info.Version == version)) return false;
            Warning(message);
            return true;
        }

        if (!MelonHandler.Mods.Any(i => i.Info.Name == modName && i.Info.Author == authorName)) return false;
        Warning(message);
        return true;
    }
}