using System;
using System.Diagnostics;
using MerinoClient.Core.Managers;
using MerinoClient.Core.VRChat;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace MerinoClient.Features.QoL.UI;

internal class QMRestart : FeatureComponent
{
    private static GameObject _exitButtonGameObject;
    private static Sprite _restartSprite;
    private static string _processPath;

    public QMRestart()
    {
        var processModule = Process.GetCurrentProcess().MainModule;

        if (processModule != null) _processPath = processModule.FileName;
    }

    public override string FeatureName => GetType().Name;
    public override string OriginalAuthor => "abbey";

    public override void OnQuickMenuInited(UiManager uiManager)
    {
        _restartSprite = ResourceManager.GetSprite("update-arrow");

        _exitButtonGameObject = QuickMenuEx.Instance.field_Public_Transform_0
            .Find("Window/QMParent/Menu_Settings/QMHeader_H1/RightItemContainer/Button_QM_Exit").gameObject;

        var parent = QuickMenuEx.Instance.field_Public_Transform_0
            .Find("Window/QMParent/Menu_Settings/QMHeader_H1/RightItemContainer").transform;

        var restartVRChatButton = Object.Instantiate(_exitButtonGameObject, parent);
        restartVRChatButton.name = "Button_QM_Restart";
        restartVRChatButton.GetComponent<VRC.UI.Elements.Tooltips.UiTooltip>().field_Public_String_0 =
            "Restart VRChat";
        restartVRChatButton.transform.GetChild(0).GetComponent<Image>().sprite = _restartSprite;

        static void Restart()
        {
            QuickMenuEx.Instance.ShowConfirmDialog("Restart", "Really restart VRChat?", () =>
            {
                Application.Quit();
                Process.Start(_processPath, Environment.CommandLine);
            });
        }

        restartVRChatButton.GetComponent<Button>().onClick.AddListener((UnityAction)Restart);
    }
}