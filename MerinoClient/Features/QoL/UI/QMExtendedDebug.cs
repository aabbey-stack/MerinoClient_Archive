using System.Reflection;
using ExitGames.Client.Photon;
using MelonLoader;
using MerinoClient.Core.Managers;
using MerinoClient.Core.VRChat;
using TMPro;
using UnityEngine;

// ReSharper disable InconsistentNaming

namespace MerinoClient.Features.QoL.UI;

[FeatureDisabled]
internal class QMExtendedDebug : FeatureComponent
{
    private const string _bytesInText = "BYTES IN:\n";
    private const string _bytesOutText = "BYTES OUT:\n";

    private static GameObject _pingTextGameObject;
    private static GameObject _bytesInTextGameObject;
    private static GameObject _bytesOutTextGameObject;

    public QMExtendedDebug()
    {
        Main.MerinoHarmony.Patch(typeof(PhotonPeer).GetProperty(nameof(PhotonPeer.BytesIn))?.GetGetMethod(),
            postfix: typeof(QMExtendedDebug).GetMethod(nameof(BytesInPatch),
                BindingFlags.NonPublic | BindingFlags.Static).ToNewHarmonyMethod());

        Main.MerinoHarmony.Patch(typeof(PhotonPeer).GetProperty(nameof(PhotonPeer.BytesOut))?.GetGetMethod(),
            postfix: typeof(QMExtendedDebug).GetMethod(nameof(BytesOutPatch),
                BindingFlags.NonPublic | BindingFlags.Static).ToNewHarmonyMethod());
    }

    public override string FeatureName => GetType().Name;
    public override string OriginalAuthor => "abbey";

    private static void BytesInPatch(long __result)
    {
        var ksIn = __result / Time.realtimeSinceStartup / 1024;
        if (_bytesInTextGameObject != null)
            _bytesInTextGameObject.GetComponent<TextMeshProUGUI>().text =
                _bytesInText + ksIn.ToString("0.00") + "k/s";
    }

    private static void BytesOutPatch(long __result)
    {
        var ksOut = __result / Time.realtimeSinceStartup / 1024;
        if (_bytesOutTextGameObject != null)
            _bytesOutTextGameObject.GetComponent<TextMeshProUGUI>().text =
                _bytesOutText + ksOut.ToString("0.00") + "k/s";
    }

    public override void OnQuickMenuInited(UiManager uiManager)
    {
        var parent = QuickMenuEx.Instance.field_Public_Transform_0
            .Find("Window/QMNotificationsArea/DebugInfoPanel/Panel").transform;

        _pingTextGameObject = QuickMenuEx.Instance.field_Public_Transform_0
            .Find("Window/QMNotificationsArea/DebugInfoPanel/Panel/Text_Ping").gameObject;

        _bytesInTextGameObject = CreateText(_pingTextGameObject, parent, "Text_BytesIn", 320, _bytesInText);

        _bytesOutTextGameObject = CreateText(_bytesInTextGameObject, parent, "Text_BytesOut", 600, _bytesOutText);
    }

    private static GameObject CreateText(GameObject original, Transform parent, string name, float xPosition,
        string text)
    {
        var textGameObject = Object.Instantiate(original, parent);
        textGameObject.name = name;
        textGameObject.GetComponent<RectTransform>().localPosition += new Vector3(xPosition, 0);
        textGameObject.GetComponent<TextMeshProUGUI>().text = text;
        textGameObject.GetComponent<TextMeshProUGUI>().autoSizeTextContainer = true;
        return textGameObject;
    }
}