#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class JoseonHierarchyOrganizer
{
    private const string ScenePath = "Assets/Scenes/FirstVillageTemple_Graybox.unity";
    private const string RequestPath = "Temp/JoseonHierarchyOrganizer.request";
    private const string ContentRootName = "FIRST_VILLAGE_TO_TEMPLE_LAYOUT";

    private static readonly string[] FolderNames =
    {
        "00_REGION - Start Village",
        "01_REGION - Practice Basic Attack",
        "02_REGION - Practice Heavy Attack",
        "03_REGION - Practice Dodge",
        "04_REGION - Temple Approach",
        "05_REGION - Temple Courtyard",
        "06_REGION - Boss Arena",
        "80_LEVEL - Route Guides & Markers",
        "90_ENVIRONMENT - Terrain & Nature",
        "99_MISC - Unsorted Level Objects",
    };

    private static readonly Vector3[] RegionCenters =
    {
        new Vector3(0f, 0f, -195f),
        new Vector3(-8f, 5f, -135f),
        new Vector3(10f, 12f, -52f),
        new Vector3(-12f, 20f, 35f),
        new Vector3(15f, 30f, 132f),
        new Vector3(5f, 30f, 187f),
        new Vector3(95f, 30f, 187f),
    };

    static JoseonHierarchyOrganizer()
    {
        EditorApplication.delayCall += TryRunRequestedOrganization;
    }

    [MenuItem("JoseonSoul/Organize First Village Hierarchy")]
    public static void OrganizeFromMenu()
    {
        Organize();
    }

    private static void TryRunRequestedOrganization()
    {
        if (!File.Exists(RequestPath) || EditorApplication.isCompiling || EditorApplication.isPlayingOrWillChangePlaymode)
            return;

        File.Delete(RequestPath);
        Organize();
    }

    private static void Organize()
    {
        Scene scene = SceneManager.GetSceneByPath(ScenePath);
        if (!scene.IsValid() || !scene.isLoaded)
        {
            Debug.LogError($"Open {ScenePath} before organizing its hierarchy.");
            return;
        }

        GameObject contentRoot = scene.GetRootGameObjects().FirstOrDefault(root => root.name == ContentRootName);
        if (contentRoot == null)
        {
            contentRoot = new GameObject(ContentRootName);
            SceneManager.MoveGameObjectToScene(contentRoot, scene);
        }

        contentRoot.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
        contentRoot.transform.localScale = Vector3.one;

        var folders = new Dictionary<string, Transform>(StringComparer.Ordinal);
        foreach (string folderName in FolderNames)
        {
            Transform folder = contentRoot.transform.Find(folderName);
            if (folder == null)
            {
                var folderObject = new GameObject(folderName);
                folderObject.transform.SetParent(contentRoot.transform, false);
                folder = folderObject.transform;
            }
            folders.Add(folderName, folder);
        }

        var movedCounts = FolderNames.ToDictionary(name => name, _ => 0, StringComparer.Ordinal);
        var preservedAtRoot = new List<string>();

        // Organize existing direct children first. Player/control objects are explicitly
        // pulled out to the scene root instead of being placed into a folder.
        Transform[] contentChildren = contentRoot.transform.Cast<Transform>().ToArray();
        foreach (Transform child in contentChildren)
        {
            if (folders.ContainsKey(child.name))
                continue;

            if (IsPlayerOrControl(child.name) || IsCoreRuntimeSystem(child.name))
            {
                child.SetParent(null, true);
                SceneManager.MoveGameObjectToScene(child.gameObject, scene);
                preservedAtRoot.Add(child.name);
                continue;
            }

            string folderName = Classify(child);
            child.SetParent(folders[folderName], true);
            movedCounts[folderName]++;
        }

        // Fold other loose scene roots into the level content root. Player/control and
        // Gaia runtime manager roots remain untouched for reliable object discovery.
        GameObject[] looseRoots = scene.GetRootGameObjects();
        foreach (GameObject root in looseRoots)
        {
            if (root == contentRoot)
                continue;
            if (IsPlayerOrControl(root.name) || IsCoreRuntimeSystem(root.name))
            {
                preservedAtRoot.Add(root.name);
                continue;
            }

            string folderName = Classify(root.transform);
            root.transform.SetParent(folders[folderName], true);
            movedCounts[folderName]++;
        }

        for (int index = 0; index < FolderNames.Length; index++)
            folders[FolderNames[index]].SetSiblingIndex(index);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, ScenePath);

        WriteResult(movedCounts, preservedAtRoot);
        Selection.activeGameObject = contentRoot;
        EditorGUIUtility.PingObject(contentRoot);
        Debug.Log("First Village hierarchy organized by gameplay region. Player and control objects remain at scene root.");
    }

    private static string Classify(Transform target)
    {
        string lower = target.name.ToLowerInvariant();

        if (ContainsAny(lower, "boss", "first boss"))
            return FolderNames[6];
        if (ContainsAny(lower, "courtyard", "temple placeholder"))
            return FolderNames[5];
        if (ContainsAny(lower, "boss approach", "temple approach"))
            return FolderNames[4];
        if (ContainsAny(lower, "practice_dodge", "practice dodge", "zone 3"))
            return FolderNames[3];
        if (ContainsAny(lower, "practice_heavy", "heavy attack", "zone 2"))
            return FolderNames[2];
        if (ContainsAny(lower, "practice_basic", "basic attack", "zone 1"))
            return FolderNames[1];
        if (ContainsAny(lower, "start village", "start_village", "village"))
            return FolderNames[0];
        if (ContainsAny(lower, "route", "guide", "marker", "design info", "clearance mask", "deterministic seed"))
            return FolderNames[7];
        if (ContainsAny(lower, "terrain", "gaia nature", "understory", "grass", "tree", "rock", "forest", "lighting", "directional light", "volume", "post processing", "sky"))
            return FolderNames[8];

        int regionIndex = FindNearestRegion(target.position);
        return regionIndex >= 0 && regionIndex <= 6 ? FolderNames[regionIndex] : FolderNames[9];
    }

    private static int FindNearestRegion(Vector3 position)
    {
        int nearest = -1;
        float nearestDistance = float.PositiveInfinity;
        var point = new Vector2(position.x, position.z);
        for (int index = 0; index < RegionCenters.Length; index++)
        {
            float distance = Vector2.SqrMagnitude(point - new Vector2(RegionCenters[index].x, RegionCenters[index].z));
            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                nearest = index;
            }
        }
        return nearest;
    }

    private static bool IsPlayerOrControl(string objectName)
    {
        string lower = objectName.ToLowerInvariant();
        return ContainsAny(lower, "player", "controller", "input", "eventsystem", "cinemachine", "camera bridge") ||
               lower == "main camera";
    }

    private static bool IsCoreRuntimeSystem(string objectName)
    {
        string lower = objectName.ToLowerInvariant();
        return ContainsAny(lower, "gaia runtime", "terrain loader manager", "game manager", "scene manager");
    }

    private static bool ContainsAny(string source, params string[] values)
    {
        return values.Any(value => source.Contains(value));
    }

    private static void WriteResult(Dictionary<string, int> movedCounts, List<string> preservedAtRoot)
    {
        string projectRoot = Directory.GetParent(Application.dataPath)?.FullName;
        if (string.IsNullOrEmpty(projectRoot))
            return;

        string outputDirectory = Path.Combine(projectRoot, "RecoveryBackups", "HierarchyOrganization");
        Directory.CreateDirectory(outputDirectory);
        var result = new StringBuilder();
        result.AppendLine($"Completed={DateTime.Now:yyyy-MM-dd HH:mm:ss zzz}");
        foreach (string folderName in FolderNames)
            result.AppendLine($"{folderName}={movedCounts[folderName]}");
        result.AppendLine("PreservedRootObjects:");
        foreach (string objectName in preservedAtRoot.Distinct().OrderBy(name => name))
            result.AppendLine(objectName);
        File.WriteAllText(Path.Combine(outputDirectory, "latest.txt"), result.ToString(), new UTF8Encoding(false));
    }
}
#endif
