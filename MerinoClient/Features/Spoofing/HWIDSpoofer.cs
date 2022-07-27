using System;
using System.Linq;
using System.Reflection;
using MelonLoader;
using UnhollowerBaseLib;
using UnityEngine;
using Exception = System.Exception;
using IntPtr = System.IntPtr;
using Object = Il2CppSystem.Object;
using Random = System.Random;

namespace MerinoClient.Features.Spoofing;

/*
 * Original source code: https://github.com/knah/ML-UniversalMods/tree/main/HWIDPatch
 */

internal class HWIDSpoofer : FeatureComponent
{
    private static Object _ourGeneratedHwidString;
    private static Object _ourGeneratedDeviceName;

    public HWIDSpoofer()
    {
        if (IsModAlreadyPresent("HWIDPatch", "knah")) return;

        try
        {
            var category = MelonPreferences.CreateCategory("HWIDPatch", "HWID Patch");
            var hwidEntry = category.CreateEntry("HWID", "", is_hidden: true);
            var deviceNameEntry = category.CreateEntry("DeviceName", "", is_hidden: true);

            var newId = hwidEntry.Value;
            var newDeviceName = deviceNameEntry.Value;

            SpoofDeviceUniqueIdentifier(newId, hwidEntry, category);
            SpoofDeviceName(newDeviceName, deviceNameEntry, category);
        }
        catch (Exception ex)
        {
            MerinoLogger.Error($"Error occurred while trying to patch identifiers:\n{ex}");
        }
    }

    public override string FeatureName => GetType().Name;
    public override string OriginalAuthor => "knah";

    private static IntPtr GetDeviceUniqueIdentifierPatch()
    {
        return _ourGeneratedHwidString.Pointer;
    }

    private static IntPtr GetDeviceNamePatch()
    {
        return _ourGeneratedDeviceName.Pointer;
    }

    private static void SpoofDeviceUniqueIdentifier(string newId, MelonPreferences_Entry<string> hwidEntry,
        MelonPreferences_Category category)
    {
        try
        {
            if (newId.Length != SystemInfo.deviceUniqueIdentifier.Length)
            {
                var random = new Random(Environment.TickCount);
                var bytes = new byte[SystemInfo.deviceUniqueIdentifier.Length / 2];
                random.NextBytes(bytes);
                newId = string.Join("", bytes.Select(it => it.ToString("x2")));
                MerinoLogger.Msg("Generated and saved a new HWID");
                hwidEntry.Value = newId;
                category.SaveToFile(false);
            }

            _ourGeneratedHwidString = new Object(IL2CPP.ManagedStringToIl2Cpp(newId));

            const string icallName = "UnityEngine.SystemInfo::GetDeviceUniqueIdentifier";

            unsafe
            {
                var icallAddress = IL2CPP.il2cpp_resolve_icall(icallName);
                if (icallAddress == IntPtr.Zero)
                {
                    MerinoLogger.Error("Can't resolve the icall, not patching");
                    return;
                }

                MelonUtils.NativeHookAttach((IntPtr)(&icallAddress),
                    typeof(HWIDSpoofer).GetMethod(nameof(GetDeviceUniqueIdentifierPatch),
                        BindingFlags.Static | BindingFlags.NonPublic)!.MethodHandle.GetFunctionPointer());
            }

            if (SystemInfo.deviceUniqueIdentifier == newId)
                MerinoLogger.Msg($"Spoofed HWID to: {newId}");
            else
                MerinoLogger.Warning("HWIDs don't match, please refer to previous exception in the console");
        }
        catch (Exception e)
        {
            MerinoLogger.Error("An exception procured while trying to spoof deviceUniqueIdentifier:\n" + e);
        }
    }

    private static void SpoofDeviceName(string newDeviceName, MelonPreferences_Entry<string> deviceNameEntry,
        MelonPreferences_Category category)
    {
        try
        {
            if (newDeviceName.Length != SystemInfo.deviceName.Length)
            {
                var random = new Random(Environment.TickCount);
                const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
                var randomDeviceName = new string(Enumerable.Repeat(chars, 7)
                    .Select(s => s[random.Next(s.Length)]).ToArray());
                var originalDeviceName = SystemInfo.deviceName;
                newDeviceName = originalDeviceName.Replace(SystemInfo.deviceName, $"DESKTOP-{randomDeviceName}");
                deviceNameEntry.Value = newDeviceName;
                category.SaveToFile(false);
            }

            _ourGeneratedDeviceName = new Object(IL2CPP.ManagedStringToIl2Cpp(newDeviceName));

            const string icallName = "UnityEngine.SystemInfo::GetDeviceName";

            unsafe
            {
                var icallAddress = IL2CPP.il2cpp_resolve_icall(icallName);
                if (icallAddress == IntPtr.Zero)
                {
                    MerinoLogger.Error("Can't resolve the icall, not patching");
                    return;
                }

                MelonUtils.NativeHookAttach((IntPtr)(&icallAddress),
                    typeof(HWIDSpoofer).GetMethod(nameof(GetDeviceNamePatch),
                        BindingFlags.Static | BindingFlags.NonPublic)!.MethodHandle.GetFunctionPointer());
            }

            if (SystemInfo.deviceName != newDeviceName)
                MerinoLogger.Warning("DeviceNames don't match, please refer to previous exception in the console");
        }
        catch (Exception e)
        {
            MerinoLogger.Error("An exception procured while trying to spoof deviceName:\n" + e);
        }
    }
}