using System;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace MerinoClient.Features.QoL.UI;

/*
 * Original source code: https://github.com/RequiDev/ReModCE/blob/master/ReModCE/Components/PopupExComponent.cs
 */

internal class PopupExtensions : FeatureComponent
{
    public override string FeatureName => GetType().Name;
    public override string OriginalAuthor => "Requi";

    public override void OnVRCUiManagerInited()
    {
        var inputPopup = GameObject.Find("UserInterface/MenuContent/Popups/InputPopup").transform;
        var pasteButton = Object.Instantiate(inputPopup.Find("ButtonRight").gameObject, inputPopup);
        pasteButton.name = "PasteButton";
        pasteButton.GetComponentInChildren<RectTransform>().anchoredPosition += new Vector2(220, 0);
        pasteButton.GetComponentInChildren<Text>().text = "Paste";

        var inputField = inputPopup.GetComponentInChildren<InputField>();

        var button = pasteButton.GetComponentInChildren<Button>();
        button.onClick = new Button.ButtonClickedEvent();
        button.onClick.AddListener(new Action(() => { inputField.text = GUIUtility.systemCopyBuffer; }));
    }
}