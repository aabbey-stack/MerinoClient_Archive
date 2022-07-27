using System;
using MerinoClient.Core.Managers;

namespace MerinoClient;

public class MenuDisabled : Attribute
{
}

internal class MenuComponent
{
    public static string RichText(string richTextText)
    {
        return $"<color={Config.ThemeColor.Value}>{richTextText}</color>";
    }

    public virtual void OnQuickMenuInited(UiManager uiManager)
    {
    }

    public virtual void OnSceneLoaded()
    {
    }
}