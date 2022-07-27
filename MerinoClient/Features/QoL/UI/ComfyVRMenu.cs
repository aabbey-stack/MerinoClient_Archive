using System.Linq;
using System.Reflection;
using HarmonyLib;
using MerinoClient.Core.VRChat;
using UnhollowerRuntimeLib;
using UnhollowerRuntimeLib.XrefScans;
using UnityEngine;
using UnityEngine.XR;

namespace MerinoClient.Features.QoL.UI;

internal class ComfyVRMenu : FeatureComponent
{
    private static MethodInfo _placeUi;



    public ComfyVRMenu()
    {
        if (IsModAlreadyPresent("ComfyVRMenu", "Moons")) return;

        var method = PlaceUiMethod;
        if (method == null)
        {
            MerinoLogger.Error("[ComfyVRMenu] Couldn't find VRCUiManager PlaceUi method to patch");
            return;
        }

        Main.MerinoHarmony.Patch(typeof(VRCUiManager).GetMethod(method.Name),
            new HarmonyMethod(AccessTools.Method(typeof(ComfyVRMenu), nameof(PlaceUiPatch))));
    }

    public override string FeatureName => GetType().Name;
    public override string OriginalAuthor => "Moons";

    private static MethodInfo PlaceUiMethod
    {
        get
        {
            if (_placeUi != null) return _placeUi;
            var xrefs = XrefScanner.XrefScan(typeof(VRCUiManager).GetMethod(nameof(VRCUiManager.LateUpdate)));
            foreach (var x in xrefs)
            {
                if (x.Type != XrefType.Method || x.TryResolve() == null || x.TryResolve().GetParameters().Length != 2 ||
                    x.TryResolve().GetParameters().Any(a => a.ParameterType != typeof(bool))) continue;
                _placeUi = (MethodInfo)x.TryResolve();
                break;
            }

            return _placeUi;
        }
    }

    // ReSharper disable once InconsistentNaming
    private static bool PlaceUiPatch(VRCUiManager __instance, bool __0)
    {
        if (!XRDevice.isPresent) return true;
        var vrcTrackingManager = VRCTrackingManager.field_Private_Static_VRCTrackingManager_0;
        var num = vrcTrackingManager != null ? vrcTrackingManager.transform.localScale.x : 1f;
        if (num <= 0f) num = 1f;
        var playerTrackingDisplay = __instance.transform;
        var unscaledUIRoot = __instance.transform.Find("UnscaledUI");
        playerTrackingDisplay.position = GetWorldCameraPosition();
        var rotation = GameObject.Find("Camera (eye)").transform.rotation.eulerAngles;
        var euler = new Vector3(rotation.x - 30f, rotation.y, 0f);
        //if (rotation.x > 0f && rotation.x < 300f) rotation.x = 0f;
        if (PlayerEx.VRCPlayer == null) euler.x = euler.z = 0f;
        if (!__0)
        {
            playerTrackingDisplay.rotation = Quaternion.Euler(euler);
        }
        else
        {
            var quaternion = Quaternion.Euler(euler);
            if (!(Quaternion.Angle(playerTrackingDisplay.rotation, quaternion) < 15f))
                playerTrackingDisplay.rotation = Quaternion.RotateTowards(playerTrackingDisplay.rotation, quaternion,
                    !(Quaternion.Angle(playerTrackingDisplay.rotation, quaternion) < 25f) ? 5f : 1f);
        }

        if (num >= 0f)
            playerTrackingDisplay.localScale = num * Vector3.one;
        else
            playerTrackingDisplay.localScale = Vector3.one;
        if (num > float.Epsilon)
            unscaledUIRoot.localScale = 1f / num * Vector3.one;
        else
            unscaledUIRoot.localScale = Vector3.one;
        return false;
    }

    private static Vector3 GetWorldCameraPosition()
    {
        var camera = VRCVrCamera.field_Private_Static_VRCVrCamera_0;
        var type = camera.GetIl2CppType();
        if (type == Il2CppType.Of<VRCVrCameraSteam>())
        {
            var steam = camera.Cast<VRCVrCameraSteam>();
            var transform1 = steam.field_Private_Transform_0;
            var transform2 = steam.field_Private_Transform_1;
            if (transform1.name == "Camera (eye)")
                return transform1.position;
            if (transform2.name == "Camera (eye)") return transform2.position;
        }
        else if (type == Il2CppType.Of<VRCVrCameraUnity>())
        {
            var unity = camera.Cast<VRCVrCameraUnity>();
            return unity.field_Public_Camera_0.transform.position;
        }
        else if (type == Il2CppType.Of<VRCVrCameraWave>())
        {
            var wave = camera.Cast<VRCVrCameraWave>();
            return wave.transform.position;
        }

        return camera.transform.parent.TransformPoint(GetLocalCameraPosition());
    }

    private static Vector3 GetLocalCameraPosition()
    {
        var camera = VRCVrCamera.field_Private_Static_VRCVrCamera_0;
        var type = camera.GetIl2CppType();
        if (type == Il2CppType.Of<VRCVrCameraSteam>())
        {
            var steam = camera.Cast<VRCVrCameraSteam>();
            var transform1 = steam.field_Private_Transform_0;
            var transform2 = steam.field_Private_Transform_1;
            if (transform1.name == "Camera (eye)")
                return camera.transform.parent.InverseTransformPoint(transform1.position);
            if (transform2.name == "Camera (eye)")
                return camera.transform.parent.InverseTransformPoint(transform2.position);
            return Vector3.zero;
        }

        if (type == Il2CppType.Of<VRCVrCameraUnity>())
        {
            if (XRDevice.isPresent)
                return camera.transform.localPosition + InputTracking.GetLocalPosition(XRNode.CenterEye);
            var unity = camera.Cast<VRCVrCameraUnity>();
            return camera.transform.parent.InverseTransformPoint(unity.field_Public_Camera_0.transform.position);
        }

        if (type == Il2CppType.Of<VRCVrCameraWave>())
        {
            var wave = camera.Cast<VRCVrCameraWave>();
            return wave.field_Public_Transform_0.InverseTransformPoint(camera.transform.position);
        }

        return camera.transform.localPosition;
    }
}