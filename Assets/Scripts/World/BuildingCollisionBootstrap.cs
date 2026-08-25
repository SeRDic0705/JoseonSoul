using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Adds static collision to imported building meshes that were authored without
/// colliders. Collision objects are hidden from the runtime hierarchy and use
/// the Wall layer expected by the player movement code.
/// </summary>
public static class BuildingCollisionBootstrap
{
    private const string ColliderObjectName = "_Runtime Building Collider";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Initialize()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
        AddBuildingColliders();
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        AddBuildingColliders();
    }

    private static void AddBuildingColliders()
    {
        if (GameObject.Find("Gaia Terrain_0_0 - FirstVillageTemple") == null &&
            GameObject.Find("FIRST_VILLAGE_TO_TEMPLE_LAYOUT") == null)
        {
            return;
        }

        int wallLayer = LayerMask.NameToLayer("Wall");
        if (wallLayer < 0)
            wallLayer = 0;

        int meshColliderCount = 0;
        int fallbackBoxCount = 0;
        MeshFilter[] meshFilters = Object.FindObjectsByType<MeshFilter>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (MeshFilter meshFilter in meshFilters)
        {
            if (meshFilter.sharedMesh == null || !IsPartOfBuilding(meshFilter.transform))
                continue;
            if (meshFilter.transform.Find(ColliderObjectName) != null)
                continue;
            if (HasBlockingCollider(meshFilter.gameObject))
                continue;

            var colliderObject = new GameObject(ColliderObjectName);
            colliderObject.hideFlags = HideFlags.HideInHierarchy;
            colliderObject.layer = wallLayer;
            colliderObject.transform.SetParent(meshFilter.transform, false);
            colliderObject.transform.localPosition = Vector3.zero;
            colliderObject.transform.localRotation = Quaternion.identity;
            colliderObject.transform.localScale = Vector3.one;

            var meshCollider = colliderObject.AddComponent<MeshCollider>();
            meshCollider.convex = false;
            meshCollider.isTrigger = false;
            meshCollider.sharedMesh = meshFilter.sharedMesh;

            if (meshCollider.sharedMesh != null)
            {
                meshColliderCount++;
            }
            else
            {
                Object.DestroyImmediate(meshCollider);
                var boxCollider = colliderObject.AddComponent<BoxCollider>();
                boxCollider.isTrigger = false;
                boxCollider.center = meshFilter.sharedMesh.bounds.center;
                boxCollider.size = meshFilter.sharedMesh.bounds.size;
                fallbackBoxCount++;
            }
        }

        Physics.SyncTransforms();
        Debug.Log($"Building collision: added {meshColliderCount} mesh colliders and {fallbackBoxCount} fallback box colliders.");
    }

    private static bool HasBlockingCollider(GameObject target)
    {
        Collider[] colliders = target.GetComponents<Collider>();
        foreach (Collider collider in colliders)
        {
            if (collider.enabled && !collider.isTrigger)
                return true;
        }
        return false;
    }

    private static bool IsPartOfBuilding(Transform target)
    {
        for (Transform current = target; current != null; current = current.parent)
        {
            string name = current.name.ToLowerInvariant();
            if (ContainsAny(name,
                    "house", "storehouse", "building", "temple", "shrine", "palace",
                    "gate", "tavern", "smithy", "mill", "tower", "pagoda", "stupa",
                    "wall", "roof", "pillar", "column", "pavilion", "courtyard",
                    "건물", "집", "사찰", "절", "문", "성벽", "담장", "탑"))
            {
                return true;
            }
        }
        return false;
    }

    private static bool ContainsAny(string source, params string[] values)
    {
        foreach (string value in values)
        {
            if (source.Contains(value))
                return true;
        }
        return false;
    }
}
