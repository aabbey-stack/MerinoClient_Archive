using System.Linq;
using Il2CppSystem;
using UnhollowerBaseLib;
using UnhollowerRuntimeLib;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace MerinoClient.Features.QoL.UI;

/*
 * Original source code goes to old loukylor's mod, can't find any links so yea
 */

internal class PreviewScroller : FeatureComponent
{
    public override string FeatureName => GetType().Name;
    public override string OriginalAuthor => "loukylor and Potato";

    public override void OnVRCUiManagerInited()
    {
        if (GameObject.Find("UserInterface/MenuContent/Screens/Avatar") == null) return;

        var scrollerContainer = new GameObject("ScrollerContainer",
            new Il2CppReferenceArray<Type>(new[] { Il2CppType.Of<RectMask2D>(), Il2CppType.Of<RectTransform>() }));
        var scrollerContainerRect = scrollerContainer.GetComponent<RectTransform>();
        scrollerContainerRect.SetParent(GameObject.Find("UserInterface/MenuContent/Screens/Avatar").transform);
        scrollerContainerRect.anchoredPosition3D = new Vector3(-565, 20, 1);
        scrollerContainerRect.localScale = Vector3.one;
        scrollerContainerRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 400);
        scrollerContainerRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 650);
        scrollerContainerRect.SetAsFirstSibling();

        var scrollerContent = new GameObject("ScrollerContent",
            new Il2CppReferenceArray<Type>(new[] { Il2CppType.Of<Image>(), Il2CppType.Of<RectTransform>() }));
        var scrollerContentRect = scrollerContent.GetComponent<RectTransform>();
        scrollerContentRect.SetParent(scrollerContainerRect);

        scrollerContentRect.anchoredPosition3D = Vector3.zero;
        scrollerContentRect.localScale = Vector3.one;
        scrollerContentRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 800);
        scrollerContentRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 1300);
        scrollerContentRect.GetComponent<Image>().color = new Color(0, 0, 0, 0);

        var scrollRect = scrollerContainer.AddComponent<ScrollRect>();
        scrollRect.content = scrollerContentRect;
        scrollRect.vertical = false;
        scrollRect.movementType = ScrollRect.MovementType.Unrestricted;
        scrollRect.decelerationRate = 0.03f;
        scrollRect.scrollSensitivity = 6;
        scrollRect.onValueChanged = new ScrollRect.ScrollRectEvent();
        var pedestal =
            GameObject.Find("UserInterface/MenuContent/Screens/Avatar/AvatarPreviewBase/MainRoot/MainModel");
        var autoTurn = pedestal.GetComponents<MonoBehaviour>().First(c =>
            c.GetIl2CppType().FullName == "UnityStandardAssets.Utility.AutoMoveAndRotate");
        Object.DestroyImmediate(autoTurn);

        var lastPos = Vector2.zero;

        scrollRect.onValueChanged.AddListener(new System.Action<Vector2>(pos =>
        {
            var velocity = pos - lastPos;

            lastPos = pos;
            var scrollRectVelocity = scrollRect.velocity;
            switch (scrollRect.horizontalNormalizedPosition)
            {
                case > 0.01f when velocity.x > 0:
                    scrollRect.horizontalNormalizedPosition = 0f;
                    lastPos.x = -1;
                    break;
                case < -0.01f when velocity.x < 0:
                    scrollRect.horizontalNormalizedPosition = 0f;
                    lastPos.x = 1;
                    break;
            }

            pedestal.transform.Rotate(new Vector2(0, velocity.normalized.x),
                velocity.magnitude * 375 * Time.deltaTime);
            scrollRect.velocity = scrollRectVelocity;
        }));
    }
}