using System;
using Il2CppSystem.Collections.Generic;
using UnhollowerBaseLib;
using UnityEngine;
using Object = UnityEngine.Object;

namespace MerinoClient.Features.Protection.AdvancedSafetyMod;

public static class ComponentAdjustment
{
    private static readonly List<Material> OurMaterialsList = new();

    public static void VisitRenderer(this Renderer renderer, ref int totalCount, ref int deletedCount,
        ref int polyCount, ref int materialCount, GameObject obj,
        System.Collections.Generic.List<SkinnedMeshRenderer> skinnedRendererList)
    {
        totalCount++;

        var skinnedMeshRenderer = renderer.TryCast<SkinnedMeshRenderer>();
        var meshFilter = obj.GetComponent<MeshFilter>();

        if (skinnedMeshRenderer != null)
            skinnedRendererList.Add(skinnedMeshRenderer);

        renderer.sortingLayerID = 0;
        renderer.sortingOrder = 0;

        renderer.GetSharedMaterials(OurMaterialsList);
        if (OurMaterialsList.Count == 0) return;

        var mesh = skinnedMeshRenderer?.sharedMesh ?? meshFilter?.sharedMesh;
        var submeshCount = 0;
        if (mesh != null)
        {
            submeshCount = mesh.subMeshCount;
            var (meshPolyCount, firstBadSubmesh) =
                CountMeshPolygons(mesh, AdvancedSafetyMod.MaxPolygons - polyCount);

            if (firstBadSubmesh != -1)
            {
                OurMaterialsList.RemoveRange(firstBadSubmesh, OurMaterialsList.Count - firstBadSubmesh);
                renderer.SetMaterialArray((Il2CppReferenceArray<Material>)OurMaterialsList.ToArray());
            }

            polyCount += meshPolyCount;

            if (meshFilter != null && OurMaterialsList.Count > 0 && (OurMaterialsList[0]?.renderQueue ?? 0) >= 2500)
                if ((OurMaterialsList[0]?.renderQueue ?? 0) >= 7530)
                {
                    deletedCount++;
                    renderer.SetMaterialArray(new Il2CppReferenceArray<Material>(0));
                    return;
                }
        }

        var allowedCountBasedOnSubmeshes = submeshCount + AdvancedSafetyMod.MaxMaterialSlotsOverSubmeshCount;
        if (allowedCountBasedOnSubmeshes < renderer.GetMaterialCount())
            Object.Destroy(renderer.gameObject);

        var allowedMaterialCount =
            Math.Min(AdvancedSafetyMod.MaxMaterialSlots - materialCount, allowedCountBasedOnSubmeshes);
        if (allowedMaterialCount < renderer.GetMaterialCount())
        {
            renderer.GetSharedMaterials(OurMaterialsList);

            deletedCount += OurMaterialsList.Count - allowedMaterialCount;

            OurMaterialsList.RemoveRange(allowedMaterialCount, OurMaterialsList.Count - allowedMaterialCount);
            renderer.materials = (Il2CppReferenceArray<Material>)OurMaterialsList.ToArray();
        }

        materialCount += renderer.GetMaterialCount();
    }

    private static (int TotalPolys, int FirstSubmeshOverLimit) CountMeshPolygons(Mesh mesh, int remainingLimit)
    {
        var polyCount = 0;
        var firstSubmeshOverLimit = -1;
        var submeshCount = mesh.subMeshCount;
        for (var i = 0; i < submeshCount; i++)
        {
            var polysInSubmesh = mesh.GetIndexCount(i);
            switch (mesh.GetTopology(i))
            {
                case MeshTopology.Triangles:
                    polysInSubmesh /= 3;
                    break;
                case MeshTopology.Quads:
                    polysInSubmesh /= 4;
                    break;
                case MeshTopology.Lines:
                    polysInSubmesh /= 2;
                    break;
                // keep LinesStrip/Points as-is
            }

            if (polyCount + polysInSubmesh >= remainingLimit)
            {
                firstSubmeshOverLimit = i;
                break;
            }

            polyCount += (int)polysInSubmesh;
        }

        return (polyCount, firstSubmeshOverLimit);
    }

    public static void PostprocessSkinnedRenderers(System.Collections.Generic.List<SkinnedMeshRenderer> renderers)
    {
        foreach (var skinnedMeshRenderer in renderers)
        {
            if (skinnedMeshRenderer == null) continue;

            Transform zeroScaleRoot = null;

            var bones = skinnedMeshRenderer.bones;
            for (var i = 0; i < bones.Count; i++)
            {
                if (bones[i] != null) continue;

                // this prevents stretch-to-zero uglies
                if (ReferenceEquals(zeroScaleRoot, null))
                {
                    var newGo = new GameObject("zero-scale");
                    zeroScaleRoot = newGo.transform;
                    zeroScaleRoot.SetParent(skinnedMeshRenderer.rootBone, false);
                    zeroScaleRoot.localScale = Vector3.zero;
                }

                bones[i] = zeroScaleRoot;
            }

            if (!ReferenceEquals(zeroScaleRoot, null))
                skinnedMeshRenderer.bones = bones;
        }
    }
}