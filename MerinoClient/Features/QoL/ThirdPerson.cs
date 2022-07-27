using System;
using HarmonyLib;
using MerinoClient.Core.VRChat;
using UnityEngine;
using UnityEngine.XR;
using CameraTakePhotoEnumerator = VRC.UserCamera.CameraUtil._TakeScreenShot_d__5;

namespace MerinoClient.Features.QoL;

/*
 * Original project source: https://github.com/RequiDev/ReModCE/blob/master/ReModCE/Components/ThirdPersonComponent.cs
 */

internal class ThirdPerson : FeatureComponent
{
    private static readonly string[] ThirdPersonAlternatives = { "Insight Camera System", "ThirdPersonPlayerCamera" };
    private static Camera _cameraBack;
    private static Camera _cameraFront;

    private static ThirdPersonMode _cameraSetup;
    private ThirdPersonMode _lastPersonMode;
    private Camera _photoCamera;
    private Camera _referenceCamera;

    private bool _shouldTp;

    public ThirdPerson()
    {
        if (Main.streamerMode) return;

        Main.MerinoHarmony.Patch(typeof(CameraTakePhotoEnumerator).GetMethod("MoveNext"),
            new HarmonyMethod(typeof(ThirdPerson), nameof(CameraEnumeratorMoveNextPatch)));
    }


    public override string FeatureName => GetType().Name;
    public override string OriginalAuthor => "Requi";

    // ReSharper disable once InconsistentNaming
    private static void CameraEnumeratorMoveNextPatch(ref CameraTakePhotoEnumerator __instance)
    {
        if (_cameraSetup == ThirdPersonMode.Off)
            return;

        __instance.field_Public_Camera_0 = _cameraSetup == ThirdPersonMode.Back ? _cameraBack : _cameraFront;
    }

    public override void OnVRCUiManagerInited()
    {
        _shouldTp = !XRDevice.isPresent;

        if (!_shouldTp || Main.streamerMode) return;

        var cameraObject = GameObject.Find("Camera (eye)");
        _photoCamera = GameObject.Find("UserCamera")?.transform.Find("PhotoCamera")?.GetComponent<Camera>();

        if (cameraObject == null)
        {
            cameraObject = GameObject.Find("CenterEyeAnchor");

            if (cameraObject == null) return;
        }

        _referenceCamera = cameraObject.GetComponent<Camera>();
        if (_referenceCamera == null)
            return;

        _cameraBack = CreateCamera(ThirdPersonMode.Back, Vector3.zero, 75f);
        _cameraFront = CreateCamera(ThirdPersonMode.Front, new Vector3(0f, 180f, 0f), 75f);
    }

    public override void OnUpdated()
    {
        if (!_shouldTp || Main.streamerMode) return;

        if (!Config.ThirdPerson.Value)
        {
            SetThirdPersonMode(ThirdPersonMode.Off);
            return;
        }

        if (_cameraBack == null || _cameraFront == null) return;

        HandleHotkeys();
        HandleThirdPerson();
    }

    public override void OnSceneWasLoaded(int buildIndex, string name)
    {
        switch (buildIndex)
        {
            case -1:
                foreach (var thirdPerson in ThirdPersonAlternatives)
                {
                    if (GameObject.Find(thirdPerson))
                    {
                        _shouldTp = false;
                        break;
                    }

                    _shouldTp = true;
                }

                break;
        }
    }

    private Camera CreateCamera(ThirdPersonMode cameraType, Vector3 rotation, float fieldOfView)
    {
        var cameraObject = new GameObject($"{cameraType}Camera")
        {
            transform =
            {
                localScale = _referenceCamera.transform.localScale,
                parent = _referenceCamera.transform,
                rotation = _referenceCamera.transform.rotation
            }
        };
        cameraObject.transform.Rotate(rotation);
        cameraObject.transform.position =
            _referenceCamera.transform.position + -cameraObject.transform.forward * 2f;

        var camera = cameraObject.AddComponent<Camera>();
        camera.enabled = false;
        camera.fieldOfView = fieldOfView;
        camera.nearClipPlane /= 4f;

        if (_photoCamera != null)
            camera.cullingMask = _photoCamera.cullingMask;

        return camera;
    }

    private void HandleHotkeys()
    {
        if (Config.ThirdPersonHotkeys.Value)
            if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.T))
            {
                var mode = _cameraSetup;
                if (++mode > ThirdPersonMode.Front) mode = ThirdPersonMode.Off;

                _lastPersonMode = mode;
                SetThirdPersonMode(mode);
            }

        if (QuickMenuEx.Instance == null) return;

        if (QuickMenuEx.Instance.isActiveAndEnabled && _lastPersonMode != ThirdPersonMode.Off)
            SetThirdPersonMode(ThirdPersonMode.Off);
        else SetThirdPersonMode(_lastPersonMode);
    }

    private void SetThirdPersonMode(ThirdPersonMode mode)
    {
        _cameraSetup = mode;
        switch (mode)
        {
            case ThirdPersonMode.Off:
                _cameraBack.enabled = false;
                _cameraFront.enabled = false;
                break;
            case ThirdPersonMode.Back:
                _cameraBack.enabled = true;
                _cameraFront.enabled = false;
                break;
            case ThirdPersonMode.Front:
                _cameraBack.enabled = false;
                _cameraFront.enabled = true;
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private void HandleThirdPerson()
    {
        if (_cameraSetup == ThirdPersonMode.Off) return;

        var scrollWheel = Input.GetAxis("Mouse ScrollWheel");
        switch (scrollWheel)
        {
            case > 0f:
                _cameraBack.transform.position += _cameraBack.transform.forward * 0.1f;
                _cameraFront.transform.position -= _cameraBack.transform.forward * 0.1f;
                break;
            case < 0f:
                _cameraBack.transform.position -= _cameraBack.transform.forward * 0.1f;
                _cameraFront.transform.position += _cameraBack.transform.forward * 0.1f;
                break;
        }
    }

    private enum ThirdPersonMode
    {
        Off = 0,
        Back,
        Front
    }
}