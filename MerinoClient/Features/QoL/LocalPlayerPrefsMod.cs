using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using MelonLoader;
using MelonLoader.TinyJSON;
using UnhollowerBaseLib;
using UnityEngine;

namespace MerinoClient.Features.QoL;

/*
 * Original source code: https://github.com/knah/ML-UniversalMods/blob/main/LocalPlayerPrefs/LocalPlayerPrefsMod.cs
 */

internal class LocalPlayerPrefsMod : FeatureComponent
{
    private const string LocalPlayerPrefsFile = "\\playerPrefs.json";

    // ReSharper disable once CollectionNeverQueried.Local
    private readonly List<Delegate> _myPinnedDelegates = new();
    private readonly ConcurrentDictionary<string, object> _myPrefs = new();

    private readonly object _mySaveLock = new();

    private bool _myHadChanges;

    public LocalPlayerPrefsMod()
    {
        if (IsModAlreadyPresent("LocalPlayerPrefs", "knah")) return;

        if (!Config.SavePlayerPrefs.Value) return;

        try
        {
            if (File.Exists(ClientDirectory + LocalPlayerPrefsFile))
            {
                var dict = (ProxyObject)JSON.Load(File.ReadAllText(LocalPlayerPrefsFile));
                foreach (var keyValuePair in dict)
                    _myPrefs[keyValuePair.Key] = ToObject(keyValuePair.Key, keyValuePair.Value);
            }
        }
        catch (Exception ex)
        {
            MerinoLogger.Error($"Unable to load PlayerPrefs.json: {ex}");
        }

        HookICall<TrySetFloatDelegate>(nameof(PlayerPrefs.TrySetFloat), TrySetFloat);
        HookICall<TrySetIntDelegate>(nameof(PlayerPrefs.TrySetInt), TrySetInt);
        HookICall<TrySetStringDelegate>(nameof(PlayerPrefs.TrySetSetString), TrySetString);

        HookICall<GetFloatDelegate>(nameof(PlayerPrefs.GetFloat), GetFloat);
        HookICall<GetIntDelegate>(nameof(PlayerPrefs.GetInt), GetInt);
        HookICall<GetStringDelegate>(nameof(PlayerPrefs.GetString), GetString);

        HookICall<HasKeyDelegate>(nameof(PlayerPrefs.HasKey), HasKey);
        HookICall<DeleteKeyDelegate>(nameof(PlayerPrefs.DeleteKey), DeleteKey);

        HookICall<VoidDelegate>(nameof(PlayerPrefs.DeleteAll), DeleteAll);
        HookICall<VoidDelegate>(nameof(PlayerPrefs.Save), Save);
    }

    private static object ToObject(string key, Variant value)
    {
        if (value is null) return null;

        if (value is ProxyString proxyString)
            return proxyString.ToString(CultureInfo.InvariantCulture);

        if (value is ProxyNumber number)
        {
            var numDouble = number.ToDouble(NumberFormatInfo.InvariantInfo);
            // ReSharper disable once RedundantCast
            // ReSharper disable once CompareOfFloatsByEqualityOperator
            if ((double)(int)numDouble == numDouble)
                return (int)numDouble;

            return (float)numDouble;
        }

        throw new ArgumentException($"Unknown value in prefs: {key} = {value.GetType()} / {value}");
    }

    public override void OnSceneWasLoaded(int buildIndex, string name)
    {
        switch (buildIndex)
        {
            default:
                Save();
                break;
        }
    }

    public override void OnApplicationQuit()
    {
        Save();
    }

    private bool HasKey(IntPtr keyPtr)
    {
        var key = IL2CPP.Il2CppStringToManaged(keyPtr);
        return _myPrefs.ContainsKey(key);
    }

    private void DeleteKey(IntPtr keyPtr)
    {
        var key = IL2CPP.Il2CppStringToManaged(keyPtr);
        _myPrefs.TryRemove(key, out _);
    }

    private void DeleteAll()
    {
        _myPrefs.Clear();
    }

    private void Save()
    {
        if (!_myHadChanges)
            return;

        _myHadChanges = false;

        try
        {
            lock (_mySaveLock)
            {
                File.WriteAllText(LocalPlayerPrefsFile, JSON.Dump(_myPrefs, EncodeOptions.PrettyPrint));
            }
        }
        catch (IOException ex)
        {
            MerinoLogger.Warning($"Exception while saving PlayerPrefs: {ex}");
        }
    }

    private float GetFloat(IntPtr keyPtr, float defaultValue)
    {
        var key = IL2CPP.Il2CppStringToManaged(keyPtr);
        if (_myPrefs.TryGetValue(key, out var result))
            switch (result)
            {
                case float resultFloat:
                    return resultFloat;
                case int resultInt:
                    return resultInt;
            }

        return defaultValue;
    }

    private int GetInt(IntPtr keyPtr, int defaultValue)
    {
        var key = IL2CPP.Il2CppStringToManaged(keyPtr);
        if (_myPrefs.TryGetValue(key, out var result))
            switch (result)
            {
                case float resultFloat:
                    return (int)resultFloat;
                case int resultInt:
                    return resultInt;
            }

        return defaultValue;
    }

    private IntPtr GetString(IntPtr keyPtr, IntPtr defaultValuePtr)
    {
        var key = IL2CPP.Il2CppStringToManaged(keyPtr);
        if (_myPrefs.TryGetValue(key, out var result))
            if (result is string resultString)
                return IL2CPP.ManagedStringToIl2Cpp(resultString);

        return defaultValuePtr;
    }

    private bool TrySetFloat(IntPtr keyPtr, float value)
    {
        _myHadChanges = true;

        var key = IL2CPP.Il2CppStringToManaged(keyPtr);
        _myPrefs[key] = value;
        return true;
    }

    private bool TrySetInt(IntPtr keyPtr, int value)
    {
        _myHadChanges = true;

        var key = IL2CPP.Il2CppStringToManaged(keyPtr);
        _myPrefs[key] = value;
        return true;
    }

    private bool TrySetString(IntPtr keyPtr, IntPtr valuePtr)
    {
        _myHadChanges = true;

        var key = IL2CPP.Il2CppStringToManaged(keyPtr);
        _myPrefs[key] = IL2CPP.Il2CppStringToManaged(valuePtr);
        return true;
    }

    private unsafe void HookICall<T>(string name, T target) where T : Delegate
    {
        var originalPointer = IL2CPP.il2cpp_resolve_icall("UnityEngine.PlayerPrefs::" + name);
        if (originalPointer == IntPtr.Zero)
        {
            MerinoLogger.Warning($"ICall {name} was not found, not patching");
            return;
        }

        _myPinnedDelegates.Add(target);
        MelonUtils.NativeHookAttach((IntPtr)(&originalPointer), Marshal.GetFunctionPointerForDelegate(target));
    }

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate bool TrySetFloatDelegate(IntPtr keyPtr, float value);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate bool TrySetIntDelegate(IntPtr keyPtr, int value);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate bool TrySetStringDelegate(IntPtr keyPtr, IntPtr valuePtr);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate int GetIntDelegate(IntPtr keyPtr, int defaultValue);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate float GetFloatDelegate(IntPtr keyPtr, float defaultValue);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate IntPtr GetStringDelegate(IntPtr keyPtr, IntPtr defaultValuePtr);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate bool HasKeyDelegate(IntPtr keyPtr);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void DeleteKeyDelegate(IntPtr keyPtr);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void VoidDelegate();
}