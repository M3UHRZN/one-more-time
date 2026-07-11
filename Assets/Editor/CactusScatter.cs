using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public static class CactusScatter
{
    const int Count = 40;
    const float MinDistance = 3f;
    const float MinScale = 0.8f;
    const float MaxScale = 1.2f;
    const float RayStartHeight = 50f;
    const float RayExtraDistance = 200f;
    const int AttemptsPerCactus = 30;

    static readonly string[] PrefabPaths =
    {
        "Assets/Prefabs/kaktus1.prefab",
        "Assets/Prefabs/kaktus2.prefab",
        "Assets/Prefabs/kaktus3.prefab",
        "Assets/Prefabs/kaktus4.prefab",
    };

    [MenuItem("Tools/Scatter Cacti")]
    static void Scatter()
    {
        if (!TryGetTerrain(out var collider, out var bounds)) return;
        if (!TryLoadPrefabs(out var prefabs)) return;

        var parent = new GameObject("Cacti").transform;
        Undo.RegisterCreatedObjectUndo(parent.gameObject, "Scatter Cacti");

        var placed = new List<Vector3>();
        int attempts = 0;
        int maxAttempts = Count * AttemptsPerCactus;

        while (placed.Count < Count && attempts < maxAttempts)
        {
            attempts++;
            if (TryFindPlacementPoint(collider, bounds, placed, out var hit))
            {
                SpawnCactus(prefabs, parent, hit);
                placed.Add(hit.point);
            }
        }

        Debug.Log($"CactusScatter: placed {placed.Count}/{Count} cacti ({attempts} attempts).");
    }

    static bool TryGetTerrain(out MeshCollider collider, out Bounds bounds)
    {
        collider = null;
        bounds = default;

        var terrainGO = GameObject.Find("terrain");
        if (terrainGO == null)
        {
            Debug.LogError("CactusScatter: 'terrain' GameObject not found.");
            return false;
        }

        var foundCollider = terrainGO.GetComponent<MeshCollider>();
        var foundRenderer = terrainGO.GetComponent<MeshRenderer>();
        if (foundCollider == null || foundRenderer == null)
        {
            Debug.LogError("CactusScatter: 'terrain' needs a MeshCollider and MeshRenderer.");
            return false;
        }

        collider = foundCollider;
        bounds = foundRenderer.bounds;
        return true;
    }

    static bool TryLoadPrefabs(out List<GameObject> prefabs)
    {
        prefabs = new List<GameObject>();
        foreach (var path in PrefabPaths)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null)
            {
                Debug.LogError($"CactusScatter: prefab not found at {path}");
                return false;
            }
            prefabs.Add(prefab);
        }
        return true;
    }

    static bool TryFindPlacementPoint(MeshCollider collider, Bounds bounds, List<Vector3> placed, out RaycastHit hit)
    {
        float x = Random.Range(bounds.min.x, bounds.max.x);
        float z = Random.Range(bounds.min.z, bounds.max.z);
        var rayOrigin = new Vector3(x, bounds.max.y + RayStartHeight, z);
        float rayDistance = bounds.size.y + RayExtraDistance;

        if (!collider.Raycast(new Ray(rayOrigin, Vector3.down), out hit, rayDistance))
            return false;

        foreach (var p in placed)
        {
            if (Vector3.Distance(p, hit.point) < MinDistance)
                return false;
        }

        return true;
    }

    static void SpawnCactus(List<GameObject> prefabs, Transform parent, RaycastHit hit)
    {
        var prefab = prefabs[Random.Range(0, prefabs.Count)];
        var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
        instance.transform.position = hit.point;
        instance.transform.rotation = Quaternion.FromToRotation(Vector3.up, hit.normal)
            * Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
        instance.transform.localScale = Vector3.one * Random.Range(MinScale, MaxScale);
        Undo.RegisterCreatedObjectUndo(instance, "Scatter Cacti");
    }
}
