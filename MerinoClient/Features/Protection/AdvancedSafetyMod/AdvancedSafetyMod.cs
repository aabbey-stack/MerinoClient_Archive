using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using UnhollowerBaseLib;
using UnhollowerRuntimeLib.XrefScans;
using UnityEngine;
using UnityEngine.SceneManagement;
using VRC.Core;
using Object = UnityEngine.Object;

namespace MerinoClient.Features.Protection.AdvancedSafetyMod;

/*
 * FinalIkPatches source: https://github.com/knah/VRCMods/blob/master/AdvancedSafety/FinalIkPatches.cs
 * UnityPlayer patches source: https://github.com/knah/VRCMods/blob/master/AdvancedSafety/ReaderPatches.cs
 * Original project source (al though most of it scraped): https://github.com/knah/VRCMods/tree/master/AdvancedSafety
 */

internal class AdvancedSafetyMod : FeatureComponent
{
    public const int MaxPolygons = 2_000_000;
    public const int MaxMaterialSlotsOverSubmeshCount = 2;
    public const int MaxMaterialSlots = 150;

    internal static bool CanReadAudioMixers = true;
    internal static bool CanReadBadFloats = true;


    private static readonly PriorityQueue<GameObjectWithPriorityData> OurBfsQueue =
        new(GameObjectWithPriorityData.IsActiveDepthNumChildrenComparer);

    public AdvancedSafetyMod()
    {
        if (IsModAlreadyPresent("Advanced Safety", "knah, Requi, Ben")) return;

        var matchingMethods = typeof(AssetManagement)
            .GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly).Where(it =>
                it.Name.StartsWith("Method_Public_Static_Object_Object_Vector3_Quaternion_Boolean_Boolean_Boolean_") &&
                it.GetParameters().Length == 6).ToList();

        foreach (var matchingMethod in matchingMethods)
        {
            ObjectInstantiateDelegate originalInstantiateDelegate = null;

            ObjectInstantiateDelegate replacement =
                (assetPtr, pos, rot, allowCustomShaders, isUI, validate, nativeMethodPointer) =>
                    // ReSharper disable once AccessToModifiedClosure
                    ObjectInstantiatePatch(assetPtr, pos, rot, allowCustomShaders, isUI, validate, nativeMethodPointer,
                        originalInstantiateDelegate);

            NativePatchUtils.NativePatch(matchingMethod, out originalInstantiateDelegate, replacement);
        }

        foreach (var nestedType in typeof(VRCAvatarManager).GetNestedTypes())
        {
            var moveNext = nestedType.GetMethod("MoveNext");
            if (moveNext == null) continue;
            var avatarManagerField = nestedType.GetProperties()
                .SingleOrDefault(it => it.PropertyType == typeof(VRCAvatarManager));
            if (avatarManagerField == null) continue;

            var fieldOffset = (int)IL2CPP.il2cpp_field_get_offset((IntPtr)UnhollowerUtils
                .GetIl2CppFieldInfoPointerFieldForGeneratedFieldAccessor(avatarManagerField.GetMethod)
                .GetValue(null));

            unsafe
            {
                var originalMethodPointer = *(IntPtr*)(IntPtr)UnhollowerUtils
                    .GetIl2CppMethodInfoPointerFieldForGeneratedMethod(moveNext).GetValue(null);

                originalMethodPointer = XrefScannerLowLevel.JumpTargets(originalMethodPointer).First();

                VoidDelegate originalDelegate = null;

                void TaskMoveNextPatch(IntPtr taskPtr, IntPtr nativeMethodInfo)
                {
                    var avatarManager = *(IntPtr*)(taskPtr + fieldOffset - 16);
                    using (new AvatarManagerCookie(new VRCAvatarManager(avatarManager)))
                        // ReSharper disable once AccessToModifiedClosure
                        // ReSharper disable once PossibleNullReferenceException
                    {
                        originalDelegate(taskPtr, nativeMethodInfo);
                    }
                }

                var patchDelegate = new VoidDelegate(TaskMoveNextPatch);

                NativePatchUtils.NativePatch(originalMethodPointer, out originalDelegate, patchDelegate);
            }
        }

        ReaderPatches.ApplyPatches();

        FinalIkPatches.ApplyPatches(Main.MerinoHarmony);

        SceneManager.add_sceneLoaded(new Action<Scene, LoadSceneMode>((s, _) =>
        {
            if (s.buildIndex != -1) return;
            CanReadAudioMixers = false;
            CanReadBadFloats = false;
        }));

        SceneManager.add_sceneUnloaded(new Action<Scene>(s =>
        {
            if (s.buildIndex != -1) return;
            CanReadAudioMixers = true;
            CanReadBadFloats = true;
        }));
    }

    public override string FeatureName => GetType().Name;
    public override string OriginalAuthor => "knah, Requi, Ben";

    private static void CleanAvatar(GameObject go)
    {
        var scannedObjects = 0;
        var destroyedObjects = 0;
        var seenPolys = 0;
        var seenMaterials = 0;
        var componentList = new Il2CppSystem.Collections.Generic.List<Component>();
        var skinnedRendererListList = new List<SkinnedMeshRenderer>();

        void Bfs(GameObjectWithPriorityData objWithPriority)
        {
            var obj = objWithPriority.GameObject;

            if (obj == null) return;
            scannedObjects++;

            if (obj.layer is 12 or 5) obj.layer = 9;

            obj.GetComponents(componentList);
            foreach (var component in componentList)
            {
                if (component == null) continue;

                component.TryCast<Renderer>()?.VisitRenderer(ref scannedObjects, ref destroyedObjects, ref seenPolys,
                    ref seenMaterials, obj, skinnedRendererListList);
            }

            foreach (var child in obj.transform)
                OurBfsQueue.Enqueue(new GameObjectWithPriorityData(child.Cast<Transform>().gameObject,
                    objWithPriority.Depth + 1, objWithPriority.IsActiveInHierarchy));
        }

        Bfs(new GameObjectWithPriorityData(go, 0, true, true));
        while (OurBfsQueue.Count > 0)
            Bfs(OurBfsQueue.Dequeue());

        ComponentAdjustment.PostprocessSkinnedRenderers(skinnedRendererListList);
    }

    private static IntPtr ObjectInstantiatePatch(IntPtr assetPtr, Vector3 pos, Quaternion rot,
        byte allowCustomShaders, byte isUI, byte validate, IntPtr nativeMethodPointer,
        ObjectInstantiateDelegate originalInstantiateDelegate)
    {
        if (AvatarManagerCookie.CurrentManager == null || assetPtr == IntPtr.Zero)
            return originalInstantiateDelegate(assetPtr, pos, rot, allowCustomShaders, isUI, validate,
                nativeMethodPointer);

        var avatarManager = AvatarManagerCookie.CurrentManager;
        var vrcPlayer = avatarManager.field_Private_VRCPlayer_0;
        if (vrcPlayer == null)
            return originalInstantiateDelegate(assetPtr, pos, rot, allowCustomShaders, isUI, validate,
                nativeMethodPointer);

        var go = new Object(assetPtr).Cast<GameObject>();
        if (go == null)
            return originalInstantiateDelegate(assetPtr, pos, rot, allowCustomShaders, isUI, validate,
                nativeMethodPointer);

        var wasActive = go.activeSelf;
        go.SetActive(false);
        var result =
            originalInstantiateDelegate(assetPtr, pos, rot, allowCustomShaders, isUI, validate, nativeMethodPointer);
        go.SetActive(wasActive);
        if (result == IntPtr.Zero) return result;
        var instantiated = new GameObject(result);
        try
        {
            CleanAvatar(instantiated);
        }
        catch (Exception ex)
        {
            MerinoLogger.Error($"Exception when cleaning avatar: {ex}");
        }

        return result;
    }

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void VoidDelegate(IntPtr thisPtr, IntPtr nativeMethodInfo);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate IntPtr ObjectInstantiateDelegate(IntPtr assetPtr, Vector3 pos, Quaternion rot,
        byte allowCustomShaders, byte isUI, byte validate, IntPtr nativeMethodPointer);

    private readonly struct AvatarManagerCookie : IDisposable
    {
        internal static VRCAvatarManager CurrentManager;
        private readonly VRCAvatarManager _myLastManager;

        public AvatarManagerCookie(VRCAvatarManager avatarManager)
        {
            _myLastManager = CurrentManager;
            CurrentManager = avatarManager;
        }

        public void Dispose()
        {
            CurrentManager = _myLastManager;
        }
    }

    private readonly struct GameObjectWithPriorityData
    {
        public readonly GameObject GameObject;
        private readonly bool _isActive;
        public readonly bool IsActiveInHierarchy;
        private readonly int _numChildren;
        public readonly int Depth;

        public GameObjectWithPriorityData(GameObject go, int depth, bool parentActive, bool enforceActive = false)
        {
            GameObject = go;
            Depth = depth;
            _isActive = go.activeSelf || enforceActive;
            IsActiveInHierarchy = _isActive && parentActive;
            _numChildren = go.transform.childCount;
        }

        private int Priority => Depth + _numChildren;

        private sealed class IsActiveDepthNumChildrenRelationalComparer : IComparer<GameObjectWithPriorityData>
        {
            public int Compare(GameObjectWithPriorityData x, GameObjectWithPriorityData y)
            {
                var isActiveComparison = -x._isActive.CompareTo(y._isActive);
                if (isActiveComparison != 0) return isActiveComparison;
                return x.Priority.CompareTo(y.Priority);
            }
        }

        public static IComparer<GameObjectWithPriorityData> IsActiveDepthNumChildrenComparer { get; } =
            new IsActiveDepthNumChildrenRelationalComparer();
    }
}