#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using Gaia;

[InitializeOnLoad]
public static class JoseonFirstVillageGrayboxBuilder
{
    private const string BuildRequestPath = "Temp/JoseonGrayboxBuild.request";
    private const string RootFolder = "Assets/JoseonSoul";
    private const string OutputFolder = RootFolder + "/FirstVillageTempleGraybox";
    private const string ScenePath = "Assets/Scenes/FirstVillageTemple_Graybox.unity";
    private const int HeightmapResolution = 1025;
    private const int TerrainSize = 2000;
    private const float TerrainHeight = 160f;
    private const float TerrainBaseY = -10f;
    private const float RoadWidth = 3.75f;

    private struct RouteNode
    {
        public string Name;
        public Vector3 Position;
        public float Radius;
        public Color Color;

        public RouteNode(string name, Vector3 position, float radius, Color color)
        {
            Name = name;
            Position = position;
            Radius = radius;
            Color = color;
        }
    }

    private static readonly RouteNode[] Route =
    {
        new RouteNode("00_Start_Village (0m)", new Vector3(0f, 0f, -195f), 38f, new Color(0.20f, 0.48f, 0.83f)),
        new RouteNode("01_Practice_BasicAttack (+5m)", new Vector3(-8f, 5f, -135f), 28f, new Color(0.25f, 0.65f, 0.34f)),
        new RouteNode("02_Practice_HeavyAttack (+12m)", new Vector3(10f, 12f, -52f), 30f, new Color(0.86f, 0.58f, 0.16f)),
        new RouteNode("03_Practice_Dodge (+20m)", new Vector3(-12f, 20f, 35f), 32f, new Color(0.56f, 0.31f, 0.72f)),
        new RouteNode("04_Boss_Approach (+30m)", new Vector3(15f, 30f, 132f), 25f, new Color(0.75f, 0.18f, 0.15f)),
        new RouteNode("05_Temple_Courtyard (+30m)", new Vector3(5f, 30f, 187f), 38f, new Color(0.75f, 0.18f, 0.15f)),
    };

    static JoseonFirstVillageGrayboxBuilder()
    {
        EditorApplication.delayCall += TryRunPendingBuild;
    }

    private static void TryRunPendingBuild()
    {
        if (!System.IO.File.Exists(BuildRequestPath) || EditorApplication.isCompiling || EditorApplication.isPlayingOrWillChangePlaymode) return;
        System.IO.File.Delete(BuildRequestPath);
        try
        {
            Build();
            Debug.Log("JOSEON_GRAYBOX_AUTO_BUILD_SUCCESS: " + ScenePath);
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
        }
    }

    [MenuItem("JoseonSoul/Build First Village - Temple Graybox")]
    public static void BuildFromMenu()
    {
        Build();
    }

    public static void BuildFromCommandLine()
    {
        try
        {
            Build();
            Debug.Log("JOSEON_GRAYBOX_BUILD_SUCCESS: " + ScenePath);
            EditorApplication.Exit(0);
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            EditorApplication.Exit(1);
        }
    }

    private static void Build()
    {
        EnsureFolder("Assets", "JoseonSoul");
        EnsureFolder(RootFolder, "FirstVillageTempleGraybox");

        ResetGaiaLoaderSingleton();
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        scene.name = "FirstVillageTemple_Graybox";

        Material ground = CreateMaterial("M_Ground", new Color(0.35f, 0.33f, 0.24f), 0f);
        Material road = CreateMaterial("M_Road", new Color(0.43f, 0.32f, 0.20f), 0f);
        Material timber = CreateMaterial("M_Timber", new Color(0.22f, 0.13f, 0.075f), 0f);
        Material roof = CreateMaterial("M_Roof", new Color(0.09f, 0.11f, 0.11f), 0.08f);
        Material stone = CreateMaterial("M_Stone", new Color(0.29f, 0.31f, 0.30f), 0f);
        Material enemy = CreateMaterial("M_Enemy", new Color(0.50f, 0.11f, 0.09f), 0f);
        Material dummy = CreateMaterial("M_Dummy", new Color(0.55f, 0.39f, 0.18f), 0f);
        Material boss = CreateMaterial("M_Boss", new Color(0.17f, 0.035f, 0.03f), 0.12f);

        Terrain terrain = CreateTerrain(ground);
        ConfigureRoadMaterial(road);
        CreateSafeGaiaTerrainLoader();
        GameObject layoutRoot = new GameObject("FIRST_VILLAGE_TO_TEMPLE_LAYOUT");
        layoutRoot.transform.position = Vector3.zero;

        CreateDesignInfo(layoutRoot.transform);
        CreateRoad(layoutRoot.transform, road);
        CreateStartVillage(layoutRoot.transform, timber, roof, dummy);
        CreatePracticeOne(layoutRoot.transform, timber, dummy);
        CreatePracticeTwo(layoutRoot.transform, timber, enemy);
        CreatePracticeThree(layoutRoot.transform, timber, enemy);
        CreateTempleApproach(layoutRoot.transform, timber, roof, stone);
        CreateBossArena(layoutRoot.transform, timber, stone, boss);
        CreateRouteMarkers(layoutRoot.transform);
        CreateGaiaNature(terrain, layoutRoot.transform);
        CreateLightingAndCamera();

        terrain.Flush();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, ScenePath);
        CaptureForestPreview();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Selection.activeGameObject = layoutRoot;
        Debug.Log("Created Gaia-compatible 2 km terrain graybox with 390 m route at " + ScenePath);
    }

    private static void ResetGaiaLoaderSingleton()
    {
        System.Reflection.FieldInfo instanceField = typeof(TerrainLoaderManager).GetField("instance", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
        if (instanceField != null) instanceField.SetValue(null, null);
    }

    private static void CreateSafeGaiaTerrainLoader()
    {
        GameObject existing = GameObject.Find(GaiaConstants.gaiaTerrainLoaderManagerObjects);
        if (existing != null) return;
        GameObject loaderObject = new GameObject(GaiaConstants.gaiaTerrainLoaderManagerObjects);
        loaderObject.SetActive(false);
        loaderObject.transform.SetParent(GaiaUtils.GetRuntimeSceneObject().transform);
        TerrainLoaderManager loader = loaderObject.AddComponent<TerrainLoaderManager>();
        loader.m_assumeGridLayout = false;
        loaderObject.SetActive(true);
    }

    private static Terrain CreateTerrain(Material groundMaterial)
    {
        string dataPath = OutputFolder + "/FirstVillageTemple_Terrain.asset";
        TerrainData data = AssetDatabase.LoadAssetAtPath<TerrainData>(dataPath);
        if (data == null)
        {
            data = new TerrainData();
            AssetDatabase.CreateAsset(data, dataPath);
        }

        data.heightmapResolution = HeightmapResolution;
        data.size = new Vector3(TerrainSize, TerrainHeight, TerrainSize);
        data.SetHeights(0, 0, GenerateHeights());
        data.name = "FirstVillageTemple_Terrain";
        ConfigureTerrainLayers(data, groundMaterial.color);

        GameObject terrainObject = Terrain.CreateTerrainGameObject(data);
        terrainObject.name = "Gaia Terrain_0_0 - FirstVillageTemple";
        terrainObject.transform.position = new Vector3(-TerrainSize * 0.5f, TerrainBaseY, -TerrainSize * 0.5f);
        Terrain terrain = terrainObject.GetComponent<Terrain>();
        terrain.materialTemplate = CreateTerrainMaterial();
        terrain.drawInstanced = true;
        terrain.heightmapPixelError = 5f;
        terrain.basemapDistance = 1500f;
        return terrain;
    }

    private static void ConfigureTerrainLayers(TerrainData data, Color color)
    {
        string texturePath = OutputFolder + "/T_GroundColor.asset";
        Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
        if (texture == null)
        {
            texture = new Texture2D(8, 8, TextureFormat.RGBA32, true) { name = "T_GroundColor", wrapMode = TextureWrapMode.Repeat };
            Color[] pixels = new Color[64];
            for (int i = 0; i < pixels.Length; i++)
            {
                float variation = ((i * 17) % 7 - 3) * 0.012f;
                pixels[i] = color + new Color(variation, variation, variation, 0f);
            }
            texture.SetPixels(pixels);
            texture.Apply(true, false);
            AssetDatabase.CreateAsset(texture, texturePath);
        }

        Texture2D grassDiffuse = CreateProceduralGroundTexture("T_ForestGrass", new Color(0.19f, 0.27f, 0.105f), new Color(0.34f, 0.40f, 0.18f), 11.3f);
        Texture2D grassNormal = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Procedural Worlds/Packages - Install/Asset Samples/NatureManufacture/T_ground_grass_01_N.png");
        Texture2D earthDiffuse = CreateProceduralGroundTexture("T_ForestEarth", new Color(0.16f, 0.105f, 0.055f), new Color(0.33f, 0.235f, 0.12f), 27.8f);
        Texture2D rockDiffuse = CreateProceduralGroundTexture("T_MountainRock", new Color(0.20f, 0.19f, 0.16f), new Color(0.39f, 0.38f, 0.32f), 42.4f);
        Texture2D rockNormal = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Procedural Worlds/Packages - Install/Asset Samples/NatureManufacture/T_ground_rock_01_N.png");

        TerrainLayer grassLayer = CreateOrUpdateTerrainLayer("TL_TemperateGrass", grassDiffuse != null ? grassDiffuse : texture, grassNormal, 12f);
        TerrainLayer dirtLayer = CreateOrUpdateTerrainLayer("TL_EarthPath", earthDiffuse, null, 8f);
        TerrainLayer rockLayer = CreateOrUpdateTerrainLayer("TL_MountainRock", rockDiffuse != null ? rockDiffuse : texture, rockNormal, 15f);
        data.terrainLayers = new[] { grassLayer, dirtLayer, rockLayer };
        PaintTerrain(data);
    }

    private static Texture2D CreateProceduralGroundTexture(string name, Color dark, Color light, float seed)
    {
        string path = OutputFolder + "/" + name + ".asset";
        Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        if (texture == null)
        {
            texture = new Texture2D(64, 64, TextureFormat.RGB24, true) { name = name, wrapMode = TextureWrapMode.Repeat, filterMode = FilterMode.Bilinear };
            AssetDatabase.CreateAsset(texture, path);
        }
        Color[] pixels = new Color[64 * 64];
        for (int y = 0; y < 64; y++)
        {
            for (int x = 0; x < 64; x++)
            {
                float large = Mathf.PerlinNoise(x * 0.095f + seed, y * 0.095f + seed * 0.7f);
                float small = Mathf.PerlinNoise(x * 0.31f + seed * 1.3f, y * 0.31f + seed * 0.4f);
                pixels[y * 64 + x] = Color.Lerp(dark, light, Mathf.Clamp01(large * 0.74f + small * 0.26f));
            }
        }
        texture.SetPixels(pixels);
        texture.Apply(true, false);
        EditorUtility.SetDirty(texture);
        return texture;
    }

    private static Material CreateTerrainMaterial()
    {
        string path = OutputFolder + "/M_Terrain_URP.mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        Shader shader = Shader.Find("Universal Render Pipeline/Terrain/Lit");
        if (shader == null) shader = Shader.Find("Nature/Terrain/Standard");
        if (material == null)
        {
            material = new Material(shader) { name = "M_Terrain_URP" };
            AssetDatabase.CreateAsset(material, path);
        }
        else if (material.shader != shader)
        {
            material.shader = shader;
        }
        EditorUtility.SetDirty(material);
        return material;
    }

    private static TerrainLayer CreateOrUpdateTerrainLayer(string name, Texture2D diffuse, Texture2D normal, float tileSize)
    {
        string path = OutputFolder + "/" + name + ".terrainlayer";
        TerrainLayer layer = AssetDatabase.LoadAssetAtPath<TerrainLayer>(path);
        if (layer == null)
        {
            layer = new TerrainLayer { name = name };
            AssetDatabase.CreateAsset(layer, path);
        }
        layer.diffuseTexture = diffuse;
        layer.normalMapTexture = normal;
        layer.tileSize = new Vector2(tileSize, tileSize);
        layer.normalScale = normal != null ? 0.8f : 0f;
        layer.smoothness = 0f;
        layer.metallic = 0f;
        EditorUtility.SetDirty(layer);
        return layer;
    }

    private static void PaintTerrain(TerrainData data)
    {
        int width = data.alphamapWidth;
        int height = data.alphamapHeight;
        float[,,] alpha = new float[height, width, 3];
        for (int z = 0; z < height; z++)
        {
            float normalizedZ = (float)z / (height - 1);
            float worldZ = normalizedZ * TerrainSize - TerrainSize * 0.5f;
            for (int x = 0; x < width; x++)
            {
                float normalizedX = (float)x / (width - 1);
                float worldX = normalizedX * TerrainSize - TerrainSize * 0.5f;
                float routeHeight;
                float routeDistance = DistanceToRoute(new Vector2(worldX, worldZ), out routeHeight);
                float padWeight = GetPadWeight(new Vector2(worldX, worldZ), 5f);
                float trailEdgeNoise = (Mathf.PerlinNoise(worldX * 0.10f + 5.2f, worldZ * 0.10f + 8.7f) - 0.5f) * 1.8f;
                float irregularRouteDistance = routeDistance + trailEdgeNoise;
                float roadWeight = Mathf.Max(1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(2.1f, 5.3f, irregularRouteDistance)), padWeight * 0.78f);
                float slope = data.GetSteepness(normalizedX, normalizedZ);
                float rockNoise = Mathf.PerlinNoise(worldX * 0.025f + 13.7f, worldZ * 0.025f + 8.2f);
                float rockWeight = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(20f, 42f, slope)) * Mathf.Lerp(0.65f, 1f, rockNoise);
                rockWeight *= 1f - roadWeight;
                float grassWeight = Mathf.Max(0.04f, 1f - roadWeight - rockWeight);
                float total = grassWeight + roadWeight + rockWeight;
                alpha[z, x, 0] = grassWeight / total;
                alpha[z, x, 1] = roadWeight / total;
                alpha[z, x, 2] = rockWeight / total;
            }
        }
        data.SetAlphamaps(0, 0, alpha);
    }

    private static float[,] GenerateHeights()
    {
        float[,] heights = new float[HeightmapResolution, HeightmapResolution];
        for (int zIndex = 0; zIndex < HeightmapResolution; zIndex++)
        {
            float worldZ = ((float)zIndex / (HeightmapResolution - 1) - 0.5f) * TerrainSize;
            for (int xIndex = 0; xIndex < HeightmapResolution; xIndex++)
            {
                float worldX = ((float)xIndex / (HeightmapResolution - 1) - 0.5f) * TerrainSize;
                float northSlope = Mathf.Clamp01((worldZ + 235f) / 470f) * 31f;
                float noise = (Mathf.PerlinNoise((worldX + 700f) * 0.008f, (worldZ + 900f) * 0.008f) - 0.5f) * 7f;
                float broadNoise = (Mathf.PerlinNoise((worldX + 1800f) * 0.0018f, (worldZ + 1400f) * 0.0018f) - 0.5f) * 18f;
                float worldHeight = northSlope + noise + broadNoise;

                // From the third practice area onward, lift both sides of the route into a mountain pass.
                float upperMountainMask = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(-15f, 115f, worldZ));
                float preliminaryRouteHeight;
                float preliminaryRouteDistance = DistanceToRoute(new Vector2(worldX, worldZ), out preliminaryRouteHeight);
                float sideRise = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(28f, 145f, preliminaryRouteDistance));
                float ridgeNoise = Mathf.PerlinNoise(worldX * 0.012f + 31.2f, worldZ * 0.012f + 17.4f) * 13f;
                float ridgeBreakup = (Mathf.PerlinNoise(worldX * 0.032f + 8.1f, worldZ * 0.032f + 3.6f) - 0.5f) * 7f;
                worldHeight += upperMountainMask * sideRise * (17f + ridgeNoise + ridgeBreakup);

                float routeHeight;
                float routeDistance = DistanceToRoute(new Vector2(worldX, worldZ), out routeHeight);
                float corridorBlend = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(6f, 24f, routeDistance));
                worldHeight = Mathf.Lerp(worldHeight, routeHeight, corridorBlend);

                for (int i = 0; i < Route.Length; i++)
                {
                    Vector2 center = new Vector2(Route[i].Position.x, Route[i].Position.z);
                    float distance = Vector2.Distance(new Vector2(worldX, worldZ), center);
                    float padBlend = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(Route[i].Radius * 0.72f, Route[i].Radius + 12f, distance));
                    worldHeight = Mathf.Lerp(worldHeight, Route[i].Position.y, padBlend);
                }

                Vector2 bossCenter = new Vector2(95f, 187f);
                float bossDistance = Vector2.Distance(new Vector2(worldX, worldZ), bossCenter);
                float bossBlend = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(34f, 50f, bossDistance));
                worldHeight = Mathf.Lerp(worldHeight, 30f, bossBlend);

                heights[zIndex, xIndex] = Mathf.Clamp01((worldHeight - TerrainBaseY) / TerrainHeight);
            }
        }
        return heights;
    }

    private static float DistanceToRoute(Vector2 point, out float routeHeight)
    {
        float bestDistance = float.MaxValue;
        routeHeight = 0f;
        for (int i = 0; i < Route.Length - 1; i++)
        {
            Vector2 a = new Vector2(Route[i].Position.x, Route[i].Position.z);
            Vector2 b = new Vector2(Route[i + 1].Position.x, Route[i + 1].Position.z);
            Vector2 ab = b - a;
            float t = Mathf.Clamp01(Vector2.Dot(point - a, ab) / Mathf.Max(ab.sqrMagnitude, 0.001f));
            float distance = Vector2.Distance(point, a + ab * t);
            if (distance < bestDistance)
            {
                bestDistance = distance;
                routeHeight = Mathf.Lerp(Route[i].Position.y, Route[i + 1].Position.y, t);
            }
        }
        return bestDistance;
    }

    private static void CreateRoad(Transform parent, Material material)
    {
        GameObject roads = new GameObject("Main Route - 390m x 3.75m");
        roads.transform.SetParent(parent);
        float[] declaredLengths = { 60f, 85f, 90f, 100f, 55f };
        for (int i = 0; i < Route.Length - 1; i++)
        {
            CreateMeanderingRoadSegment("Route " + i + " - " + declaredLengths[i].ToString("0") + "m", Route[i].Position, Route[i + 1].Position, material, roads.transform, i);
            CreateStairsAtTransition(i, roads.transform, material);
        }

        Vector3 portalA = Route[Route.Length - 1].Position + new Vector3(35f, 0.1f, 0f);
        Vector3 portalB = new Vector3(61f, 30.1f, 187f);
        CreateSegment("Boss Room Link - Separate Space", portalA, portalB, 3f, 0.12f, material, roads.transform);
    }

    private static void CreateMeanderingRoadSegment(string name, Vector3 a, Vector3 b, Material material, Transform parent, int segmentIndex)
    {
        GameObject routeRoot = new GameObject(name);
        routeRoot.transform.SetParent(parent);
        Vector3 horizontal = new Vector3(b.x - a.x, 0f, b.z - a.z);
        Vector3 side = Vector3.Cross(Vector3.up, horizontal.normalized);
        int pieces = Mathf.Max(10, Mathf.CeilToInt(horizontal.magnitude / 6f));
        Vector3 previous = a;
        for (int i = 1; i <= pieces; i++)
        {
            float t = (float)i / pieces;
            float previousT = (float)(i - 1) / pieces;
            float amplitude = segmentIndex < 2 ? 1.25f : 2.6f;
            float currentOffset = Mathf.Sin(t * Mathf.PI) * Mathf.Sin((t * 2.2f + segmentIndex * 0.37f) * Mathf.PI) * amplitude;
            float previousOffset = Mathf.Sin(previousT * Mathf.PI) * Mathf.Sin((previousT * 2.2f + segmentIndex * 0.37f) * Mathf.PI) * amplitude;
            Vector3 current = Vector3.Lerp(a, b, t) + side * currentOffset;
            previous = Vector3.Lerp(a, b, previousT) + side * previousOffset;
            float width = RoadWidth * (0.90f + Mathf.PerlinNoise(segmentIndex * 3.1f, t * 5.7f) * 0.18f);
            GameObject trailGuide = CreateSegment("Dirt Trail Guide " + i.ToString("00"), previous, current, width, 0.025f, material, routeRoot.transform);
            trailGuide.GetComponent<Renderer>().enabled = false;
            Collider trailCollider = trailGuide.GetComponent<Collider>();
            if (trailCollider != null) trailCollider.enabled = false;
        }
    }

    private static void CreateStairsAtTransition(int index, Transform parent, Material material)
    {
        Vector3 a = Route[index].Position;
        Vector3 b = Route[index + 1].Position;
        float rise = b.y - a.y;
        if (rise < 1f) return;
        Vector3 direction = new Vector3(b.x - a.x, 0f, b.z - a.z).normalized;
        int count = Mathf.Clamp(Mathf.RoundToInt(rise / 0.5f), 6, 20);
        Vector3 center = Vector3.Lerp(a, b, 0.84f);
        for (int i = 0; i < count; i++)
        {
            float t = (i + 0.5f) / count;
            Vector3 position = center + direction * ((t - 0.5f) * count * 0.9f);
            position.y = Mathf.Lerp(a.y, b.y, 0.72f + t * 0.24f);
            GameObject step = CreateCube("Step", position, new Vector3(RoadWidth + 0.5f, 0.35f, 1.05f), material, parent);
            step.transform.rotation = Quaternion.LookRotation(direction) * Quaternion.Euler(0f, 90f, 0f);
        }
    }

    private static void CreateStartVillage(Transform root, Material timber, Material roof, Material dummy)
    {
        Transform zone = CreateZoneRoot(Route[0], root);
        CreateCircularFence(zone, Route[0].Position, 40f, timber, 24, 1.6f, 70f);
        CreateGate("Village Entrance Gate", Route[0].Position + new Vector3(0f, 0f, -39f), Vector3.forward, timber, roof, zone);
        CreateHouse("House A", Route[0].Position + new Vector3(-22f, 0f, 4f), 18f, 11f, timber, roof, zone);
        CreateHouse("House B", Route[0].Position + new Vector3(23f, 0f, 9f), 15f, 10f, timber, roof, zone);
        CreateHouse("Storehouse", Route[0].Position + new Vector3(17f, 0f, -15f), 11f, 8f, timber, roof, zone);
        CreateDummyLine(zone, Route[0].Position + new Vector3(-10f, 0f, -5f), 3, 5f, dummy);
    }

    private static void CreatePracticeOne(Transform root, Material timber, Material dummy)
    {
        Transform zone = CreateZoneRoot(Route[1], root);
        CreateCircularFence(zone, Route[1].Position, 30f, timber, 18, 1.35f, 50f);
        CreateDummyLine(zone, Route[1].Position + new Vector3(-10f, 0f, 2f), 5, 5f, dummy);
    }

    private static void CreatePracticeTwo(Transform root, Material timber, Material enemy)
    {
        Transform zone = CreateZoneRoot(Route[2], root);
        CreateCircularFence(zone, Route[2].Position, 32f, timber, 20, 1.35f, 50f);
        CreateEnemy("Heavy Enemy A", Route[2].Position + new Vector3(-11f, 0f, 4f), enemy, zone, 1.25f);
        CreateEnemy("Heavy Enemy B", Route[2].Position + new Vector3(11f, 0f, 6f), enemy, zone, 1.25f);
    }

    private static void CreatePracticeThree(Transform root, Material timber, Material enemy)
    {
        Transform zone = CreateZoneRoot(Route[3], root);
        CreateCircularFence(zone, Route[3].Position, 34f, timber, 22, 1.35f, 50f);
        CreateEnemy("Dodge Enemy A", Route[3].Position + new Vector3(-15f, 0f, 5f), enemy, zone, 1f);
        CreateEnemy("Dodge Enemy B", Route[3].Position + new Vector3(0f, 0f, 11f), enemy, zone, 1f);
        CreateEnemy("Dodge Enemy C", Route[3].Position + new Vector3(15f, 0f, 3f), enemy, zone, 1f);
    }

    private static void CreateTempleApproach(Transform root, Material timber, Material roof, Material stone)
    {
        Transform approach = CreateZoneRoot(Route[4], root);
        CreateCircularFence(approach, Route[4].Position, 27f, timber, 18, 1.45f, 60f);
        CreateGate("Boss Approach Gate", Route[4].Position + new Vector3(0f, 0f, 18f), Vector3.forward, timber, roof, approach);

        Transform temple = CreateZoneRoot(Route[5], root);
        CreateCircularFence(temple, Route[5].Position, 41f, timber, 26, 1.55f, 55f);
        CreateCube("Temple Stone Platform", Route[5].Position + new Vector3(0f, 0.6f, 10f), new Vector3(26f, 1.2f, 17f), stone, temple);
        CreateHouse("Temple Placeholder", Route[5].Position + new Vector3(0f, 1.2f, 10f), 24f, 14f, timber, roof, temple);
        CreateGate("Temple Courtyard Gate", Route[5].Position + new Vector3(0f, 0f, -28f), Vector3.forward, timber, roof, temple);
    }

    private static void CreateBossArena(Transform root, Material timber, Material stone, Material boss)
    {
        Vector3 center = new Vector3(95f, 30f, 187f);
        GameObject zoneObject = new GameObject("BOSS_ROOM - Separate Space - 4min Target");
        zoneObject.transform.SetParent(root);
        Transform zone = zoneObject.transform;
        CreateCylinder("Boss Arena Floor", center + Vector3.up * 0.12f, 33f, 0.24f, stone, zone);
        CreateCircularFence(zone, center, 35f, timber, 26, 1.75f, 110f);
        CreateEnemy("FIRST BOSS", center, boss, zone, 2.2f);
        CreateTorch(center + new Vector3(-22f, 0f, 0f), zone);
        CreateTorch(center + new Vector3(22f, 0f, 0f), zone);
        CreateGate("Boss Arena Gate", center + new Vector3(-34f, 0f, 0f), Vector3.right, timber, stone, zone);
    }

    private static void CreateGaiaNature(Terrain terrain, Transform root)
    {
        GameObject natureRoot = new GameObject("GAIA NATURE - Joseon Temperate Mountain");
        natureRoot.transform.SetParent(root);

        GameObject sourceInfo = new GameObject("Gaia Sources - PW Spruce 05, PW Stone 01, PW Wild Grass");
        sourceInfo.transform.SetParent(natureRoot.transform);
        new GameObject("Clearance Mask - Route 14m / Combat Pads 8m").transform.SetParent(sourceInfo.transform);
        new GameObject("Deterministic Seed - 1592").transform.SetParent(sourceInfo.transform);

        AddGaiaTrees(terrain, natureRoot.transform);
        AddGaiaGrass(terrain);
        AddGaiaUnderstoryClusters(terrain, natureRoot.transform);
        AddGaiaRocks(terrain, natureRoot.transform);

        terrain.treeDistance = 650f;
        terrain.treeBillboardDistance = 180f;
        terrain.treeCrossFadeLength = 35f;
        terrain.detailObjectDistance = 140f;
        terrain.detailObjectDensity = 0.72f;
    }

    private static void AddGaiaTrees(Terrain terrain, Transform parent)
    {
        GameObject treePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Procedural Worlds/Packages - Install/Asset Samples/Procedural Worlds/Prefabs/PW_Tree_Spruce_05.prefab");
        if (treePrefab == null)
        {
            Debug.LogWarning("Gaia sample tree PW_Tree_Spruce_05 was not found; tree pass skipped.");
            return;
        }

        TerrainData data = terrain.terrainData;
        data.treePrototypes = new[]
        {
            new TreePrototype { prefab = treePrefab, bendFactor = 0.22f }
        };

        System.Random random = new System.Random(1592);
        List<TreeInstance> instances = new List<TreeInstance>();
        int villageAndZoneOneCount = 0;
        int transitionZoneTwoCount = 0;
        int mountainZoneThreeCount = 0;
        int attempts = 14500;
        for (int i = 0; i < attempts && instances.Count < 2850; i++)
        {
            float worldX = Mathf.Lerp(-310f, 345f, (float)random.NextDouble());
            float worldZ = Mathf.Lerp(-330f, 390f, (float)random.NextDouble());
            Vector2 point = new Vector2(worldX, worldZ);
            float routeHeight;
            float routeDistance = DistanceToRoute(point, out routeHeight);
            float routeClearance = worldZ >= 5f ? 7.2f : 13f;
            float padWeight = worldZ >= 5f ? GetPadWeight(point, 2f) : GetPadWeight(point, 8f);
            if (routeDistance < routeClearance || padWeight > (worldZ >= 5f ? 0.32f : 0.04f)) continue;
            if (Vector2.Distance(point, new Vector2(95f, 187f)) < 45f) continue;

            float nx = Mathf.InverseLerp(-TerrainSize * 0.5f, TerrainSize * 0.5f, worldX);
            float nz = Mathf.InverseLerp(-TerrainSize * 0.5f, TerrainSize * 0.5f, worldZ);
            float slope = data.GetSteepness(nx, nz);
            if (slope > 43f) continue;
            float forestNoise = Mathf.PerlinNoise(worldX * 0.0105f + 4.2f, worldZ * 0.0105f + 7.7f);
            float groveNoise = Mathf.PerlinNoise(worldX * 0.026f + 19.3f, worldZ * 0.026f + 2.8f);
            float regionDensity;
            if (worldZ < -105f)
            {
                // Village and practice area 1 stay open and readable.
                regionDensity = 0.10f;
            }
            else if (worldZ < 5f)
            {
                // Practice area 2 is the visual transition into the mountain.
                regionDensity = Mathf.Lerp(0.16f, 0.42f, Mathf.InverseLerp(-105f, 5f, worldZ));
            }
            else
            {
                // Practice area 3 and the temple approach form the dense mountain forest.
                regionDensity = Mathf.Lerp(0.74f, 0.98f, Mathf.InverseLerp(5f, 155f, worldZ));
            }
            float clusterFitness = forestNoise * 0.72f + groveNoise * 0.28f;
            if ((float)random.NextDouble() > regionDensity || clusterFitness < (worldZ >= 5f ? 0.30f : 0.49f)) continue;

            float scale = Mathf.Lerp(0.60f, worldZ >= 5f ? 1.45f : 1.18f, (float)random.NextDouble());
            Color tint = Color.Lerp(new Color(0.72f, 0.82f, 0.68f), Color.white, (float)random.NextDouble() * 0.65f);
            instances.Add(new TreeInstance
            {
                position = new Vector3(nx, 0f, nz),
                prototypeIndex = 0,
                widthScale = scale * Mathf.Lerp(0.82f, 1.02f, (float)random.NextDouble()),
                heightScale = scale,
                rotation = (float)random.NextDouble() * Mathf.PI * 2f,
                color = tint,
                lightmapColor = Color.white
            });
            if (worldZ < -105f) villageAndZoneOneCount++;
            else if (worldZ < 5f) transitionZoneTwoCount++;
            else mountainZoneThreeCount++;
        }
        data.SetTreeInstances(instances.ToArray(), true);
        GameObject treeSummary = new GameObject("Gaia Terrain Trees - " + instances.Count + " instances");
        treeSummary.transform.SetParent(parent);
        new GameObject("Sparse Village + Zone 1 - " + villageAndZoneOneCount).transform.SetParent(treeSummary.transform);
        new GameObject("Transition Zone 2 - " + transitionZoneTwoCount).transform.SetParent(treeSummary.transform);
        new GameObject("Dense Mountain Zone 3+ - " + mountainZoneThreeCount).transform.SetParent(treeSummary.transform);
    }

    private static void AddGaiaGrass(Terrain terrain)
    {
        Texture2D grassTexture = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Procedural Worlds/Packages - Install/Asset Samples/Procedural Worlds/Content Resources/Terrain Details/PW_WildGrass_00_D Sample.png");
        Texture2D lowGrassTexture = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Procedural Worlds/Packages - Install/Asset Samples/Procedural Worlds/Content Resources/Terrain Details/PW_LawnGrass_00_D Sample.png");
        if (grassTexture == null)
        {
            Debug.LogWarning("Gaia sample wild grass texture was not found; detail pass skipped.");
            return;
        }

        TerrainData data = terrain.terrainData;
        DetailPrototype detail = new DetailPrototype
        {
            prototypeTexture = grassTexture,
            renderMode = DetailRenderMode.GrassBillboard,
            minWidth = 0.55f,
            maxWidth = 1.15f,
            minHeight = 0.65f,
            maxHeight = 1.35f,
            noiseSeed = 1592,
            noiseSpread = 0.34f,
            healthyColor = new Color(0.52f, 0.64f, 0.38f),
            dryColor = new Color(0.57f, 0.49f, 0.30f)
        };
        DetailPrototype lowDetail = new DetailPrototype
        {
            prototypeTexture = lowGrassTexture != null ? lowGrassTexture : grassTexture,
            renderMode = DetailRenderMode.GrassBillboard,
            minWidth = 0.38f,
            maxWidth = 0.82f,
            minHeight = 0.30f,
            maxHeight = 0.72f,
            noiseSeed = 1593,
            noiseSpread = 0.27f,
            healthyColor = new Color(0.34f, 0.54f, 0.25f),
            dryColor = new Color(0.49f, 0.42f, 0.25f)
        };
        data.SetDetailResolution(512, 16);
        data.detailPrototypes = new[] { detail, lowDetail };
        int[,] density = new int[data.detailHeight, data.detailWidth];
        int[,] lowDensity = new int[data.detailHeight, data.detailWidth];
        for (int z = 0; z < data.detailHeight; z++)
        {
            float normalizedZ = (float)z / (data.detailHeight - 1);
            float worldZ = normalizedZ * TerrainSize - TerrainSize * 0.5f;
            for (int x = 0; x < data.detailWidth; x++)
            {
                float normalizedX = (float)x / (data.detailWidth - 1);
                float worldX = normalizedX * TerrainSize - TerrainSize * 0.5f;
                if (worldX < -320f || worldX > 360f || worldZ < -350f || worldZ > 400f) continue;
                Vector2 point = new Vector2(worldX, worldZ);
                float routeHeight;
                if (DistanceToRoute(point, out routeHeight) < 9.5f || GetPadWeight(point, 7f) > 0.08f) continue;
                if (Vector2.Distance(point, new Vector2(95f, 187f)) < 43f) continue;
                float slope = data.GetSteepness(normalizedX, normalizedZ);
                if (slope > 31f) continue;
                float noise = Mathf.PerlinNoise(worldX * 0.045f + 2.5f, worldZ * 0.045f + 9.1f);
                float upperDensity = Mathf.Lerp(0.55f, 1.35f, Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(-50f, 120f, worldZ)));
                density[z, x] = noise > (worldZ >= 5f ? 0.34f : 0.48f) ? Mathf.RoundToInt(Mathf.Lerp(1f, 10f, noise) * upperDensity) : 0;
                float lowNoise = Mathf.PerlinNoise(worldX * 0.072f + 14.1f, worldZ * 0.072f + 1.8f);
                lowDensity[z, x] = lowNoise > (worldZ >= 5f ? 0.29f : 0.53f) ? Mathf.RoundToInt(Mathf.Lerp(1f, 8f, lowNoise) * upperDensity) : 0;
            }
        }
        data.SetDetailLayer(0, 0, 0, density);
        data.SetDetailLayer(0, 0, 1, lowDensity);
    }

    private static void AddGaiaUnderstoryClusters(Terrain terrain, Transform parent)
    {
        GameObject wildGrass = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Procedural Worlds/Packages - Install/Asset Samples/Procedural Worlds/Content Resources/Terrain Details/PW_WildGrass_General_Lod1 Sample.fbx");
        GameObject cloverGrass = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Procedural Worlds/Packages - Install/Asset Samples/Procedural Worlds/Content Resources/Terrain Details/PW_LawnGrass_CloverFlower Sample.fbx");
        if (wildGrass == null && cloverGrass == null) return;

        GameObject clusterRoot = new GameObject("Gaia Understory Clusters - Zone 3+");
        clusterRoot.transform.SetParent(parent);
        TerrainData data = terrain.terrainData;
        System.Random random = new System.Random(1594);
        int count = 0;
        for (int attempt = 0; attempt < 3200 && count < 360; attempt++)
        {
            float worldZ = Mathf.Lerp(3f, 235f, (float)random.NextDouble());
            float worldX = Mathf.Lerp(-115f, 145f, (float)random.NextDouble());
            Vector2 point = new Vector2(worldX, worldZ);
            float routeHeight;
            float routeDistance = DistanceToRoute(point, out routeHeight);
            if (routeDistance < 4.8f || routeDistance > 31f) continue;
            if (GetPadWeight(point, 0.5f) > 0.58f) continue;
            if (Vector2.Distance(point, new Vector2(95f, 187f)) < 40f) continue;

            float nx = Mathf.InverseLerp(-TerrainSize * 0.5f, TerrainSize * 0.5f, worldX);
            float nz = Mathf.InverseLerp(-TerrainSize * 0.5f, TerrainSize * 0.5f, worldZ);
            if (data.GetSteepness(nx, nz) > 31f) continue;
            float clusterNoise = Mathf.PerlinNoise(worldX * 0.075f + 4.7f, worldZ * 0.075f + 12.9f);
            if (clusterNoise < 0.43f) continue;

            GameObject source = ((float)random.NextDouble() < 0.72f || cloverGrass == null) ? wildGrass : cloverGrass;
            if (source == null) source = cloverGrass;
            GameObject cluster = PrefabUtility.InstantiatePrefab(source) as GameObject;
            if (cluster == null) continue;
            cluster.name = "Understory " + count.ToString("000");
            cluster.transform.SetParent(clusterRoot.transform);
            float groundY = terrain.SampleHeight(new Vector3(worldX, 0f, worldZ)) + terrain.transform.position.y;
            cluster.transform.position = new Vector3(worldX, groundY, worldZ);
            cluster.transform.rotation = Quaternion.Euler(0f, (float)random.NextDouble() * 360f, 0f);
            float scale = Mathf.Lerp(0.75f, 1.55f, (float)random.NextDouble());
            cluster.transform.localScale = new Vector3(scale, Mathf.Lerp(0.8f, 1.35f, (float)random.NextDouble()), scale);
            foreach (Collider collider in cluster.GetComponentsInChildren<Collider>()) collider.enabled = false;
            count++;
        }
        new GameObject("Understory Instance Count - " + count).transform.SetParent(clusterRoot.transform);
    }

    private static void AddGaiaRocks(Terrain terrain, Transform parent)
    {
        GameObject rockPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Procedural Worlds/Packages - Install/Asset Samples/Procedural Worlds/Prefabs/PW_Stone_01.prefab");
        if (rockPrefab == null)
        {
            Debug.LogWarning("Gaia sample rock PW_Stone_01 was not found; rock pass skipped.");
            return;
        }

        GameObject rocksRoot = new GameObject("Gaia Rocks - PW Stone 01");
        rocksRoot.transform.SetParent(parent);
        TerrainData data = terrain.terrainData;
        System.Random random = new System.Random(1593);
        int count = 0;
        for (int attempt = 0; attempt < 2800 && count < 180; attempt++)
        {
            float worldX = Mathf.Lerp(-285f, 325f, (float)random.NextDouble());
            float worldZ = Mathf.Lerp(-300f, 370f, (float)random.NextDouble());
            Vector2 point = new Vector2(worldX, worldZ);
            float routeHeight;
            float routeDistance = DistanceToRoute(point, out routeHeight);
            if (routeDistance < 9f || routeDistance > (worldZ >= 5f ? 145f : 80f) || GetPadWeight(point, 3f) > 0.15f) continue;
            if (Vector2.Distance(point, new Vector2(95f, 187f)) < 40f) continue;

            float nx = Mathf.InverseLerp(-TerrainSize * 0.5f, TerrainSize * 0.5f, worldX);
            float nz = Mathf.InverseLerp(-TerrainSize * 0.5f, TerrainSize * 0.5f, worldZ);
            float slope = data.GetSteepness(nx, nz);
            float noise = Mathf.PerlinNoise(worldX * 0.037f + 11f, worldZ * 0.037f + 6f);
            if (slope < (worldZ >= 5f ? 6f : 11f) && noise < (worldZ >= 5f ? 0.56f : 0.72f)) continue;

            GameObject rock = PrefabUtility.InstantiatePrefab(rockPrefab) as GameObject;
            if (rock == null) continue;
            rock.name = "PW Stone " + count.ToString("000");
            rock.transform.SetParent(rocksRoot.transform);
            float groundY = terrain.SampleHeight(new Vector3(worldX, 0f, worldZ)) + terrain.transform.position.y;
            rock.transform.position = new Vector3(worldX, groundY - 0.25f, worldZ);
            Vector3 normal = data.GetInterpolatedNormal(nx, nz);
            rock.transform.rotation = Quaternion.FromToRotation(Vector3.up, normal) * Quaternion.Euler(0f, (float)random.NextDouble() * 360f, 0f);
            float scale = Mathf.Lerp(0.72f, worldZ >= 5f ? 2.85f : 1.9f, (float)random.NextDouble());
            rock.transform.localScale = Vector3.one * scale;
            count++;
        }
    }

    private static float GetPadWeight(Vector2 point, float extraRadius)
    {
        float weight = 0f;
        for (int i = 0; i < Route.Length; i++)
        {
            float inner = Mathf.Max(0f, Route[i].Radius * 0.7f);
            float outer = Route[i].Radius + extraRadius;
            float distance = Vector2.Distance(point, new Vector2(Route[i].Position.x, Route[i].Position.z));
            weight = Mathf.Max(weight, 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(inner, outer, distance)));
        }
        float bossDistance = Vector2.Distance(point, new Vector2(95f, 187f));
        weight = Mathf.Max(weight, 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(30f, 38f + extraRadius, bossDistance)));
        return weight;
    }

    private static Transform CreateZoneRoot(RouteNode routeNode, Transform parent)
    {
        GameObject zone = new GameObject(routeNode.Name);
        zone.transform.SetParent(parent);
        GameObject floor = CreateCylinder("Playable Area", routeNode.Position + Vector3.up * 0.1f, routeNode.Radius * 0.9f, 0.2f, CreateMaterial("M_Zone_" + Sanitize(routeNode.Name), routeNode.Color * 0.5f + Color.gray * 0.18f, 0f), zone.transform);
        floor.GetComponent<Renderer>().sharedMaterial.color = routeNode.Color * 0.45f + new Color(0.18f, 0.16f, 0.12f);
        floor.GetComponent<Renderer>().enabled = false;
        Collider floorCollider = floor.GetComponent<Collider>();
        if (floorCollider != null) floorCollider.enabled = false;
        return zone.transform;
    }

    private static void CreateDesignInfo(Transform parent)
    {
        GameObject info = new GameObject("DESIGN_SPECS - Gaia6_URP_2km_Heightmap1025");
        info.transform.SetParent(parent);
        new GameObject("1 Unity Unit = 1 meter").transform.SetParent(info.transform);
        new GameObject("Main Route = 390m / Width = 3.75m").transform.SetParent(info.transform);
        new GameObject("Elevation = 0m, 5m, 12m, 20m, 30m").transform.SetParent(info.transform);
        new GameObject("Target Timing = 8min route + 4min boss").transform.SetParent(info.transform);
        new GameObject("Terrain is standard Unity Terrain and Gaia-editable").transform.SetParent(info.transform);
    }

    private static void CreateRouteMarkers(Transform parent)
    {
        GameObject markers = new GameObject("Route Checkpoints & Distances");
        markers.transform.SetParent(parent);
        float[] lengths = { 60f, 85f, 90f, 100f, 55f };
        for (int i = 0; i < Route.Length; i++)
        {
            GameObject marker = new GameObject(Route[i].Name);
            marker.transform.SetParent(markers.transform);
            marker.transform.position = Route[i].Position + Vector3.up * 2f;
            if (i < lengths.Length)
            {
                GameObject distance = new GameObject("Next segment: " + lengths[i].ToString("0") + "m");
                distance.transform.SetParent(marker.transform);
            }
        }
    }

    private static void CreateLightingAndCamera()
    {
        GameObject lightObject = new GameObject("Directional Light - Warm Forest Morning");
        Light light = lightObject.AddComponent<Light>();
        light.type = LightType.Directional;
        light.color = new Color(1f, 0.78f, 0.56f);
        light.intensity = 2.35f;
        light.shadows = LightShadows.Soft;
        light.shadowStrength = 0.92f;
        lightObject.transform.rotation = Quaternion.Euler(43f, -32f, 0f);

        GameObject cameraObject = new GameObject("Forest Walk Camera - Zone 3 (Main)");
        Camera camera = cameraObject.AddComponent<Camera>();
        cameraObject.tag = "MainCamera";
        cameraObject.transform.position = new Vector3(-4f, 25.2f, 71f);
        cameraObject.transform.rotation = Quaternion.LookRotation(new Vector3(7f, 27.5f, 101f) - cameraObject.transform.position);
        camera.fieldOfView = 58f;
        camera.farClipPlane = 900f;
        camera.clearFlags = CameraClearFlags.Skybox;
        camera.GetUniversalAdditionalCameraData().renderPostProcessing = true;

        GameObject overviewObject = new GameObject("Overview Camera (Disabled)");
        Camera overview = overviewObject.AddComponent<Camera>();
        overviewObject.transform.position = new Vector3(265f, 310f, -235f);
        overviewObject.transform.rotation = Quaternion.LookRotation(new Vector3(10f, 15f, 10f) - overviewObject.transform.position);
        overview.fieldOfView = 43f;
        overview.farClipPlane = 2500f;
        overview.enabled = false;

        GameObject postPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Procedural Worlds/Packages - Install/Sky & Lighting Presets/Presets/URP/Morning Preset GlobalPostProcessing.prefab");
        if (postPrefab != null)
        {
            GameObject post = PrefabUtility.InstantiatePrefab(postPrefab) as GameObject;
            if (post != null) post.name = "Gaia URP Morning Forest Post Processing";
        }

        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogDensity = 0.0028f;
        RenderSettings.fogColor = new Color(0.50f, 0.54f, 0.49f);
        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientIntensity = 1.25f;
        RenderSettings.ambientSkyColor = new Color(0.45f, 0.52f, 0.53f);
        RenderSettings.ambientEquatorColor = new Color(0.37f, 0.40f, 0.29f);
        RenderSettings.ambientGroundColor = new Color(0.18f, 0.19f, 0.13f);
    }

    private static void CaptureForestPreview()
    {
        GameObject cameraObject = GameObject.Find("Forest Walk Camera - Zone 3 (Main)");
        if (cameraObject == null) return;
        Camera camera = cameraObject.GetComponent<Camera>();
        if (camera == null) return;

        const int width = 1280;
        const int height = 720;
        RenderTexture target = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
        Texture2D image = new Texture2D(width, height, TextureFormat.RGB24, false);
        RenderTexture previous = RenderTexture.active;
        camera.targetTexture = target;
        camera.Render();
        RenderTexture.active = target;
        image.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        image.Apply();
        string previewAssetPath = OutputFolder + "/ForestPreview.png";
        string previewAbsolutePath = System.IO.Path.GetFullPath(previewAssetPath);
        System.IO.File.WriteAllBytes(previewAbsolutePath, image.EncodeToPNG());
        camera.targetTexture = null;
        RenderTexture.active = previous;
        UnityEngine.Object.DestroyImmediate(target);
        UnityEngine.Object.DestroyImmediate(image);
        AssetDatabase.ImportAsset(previewAssetPath, ImportAssetOptions.ForceUpdate);
    }

    private static void CreateHouse(string name, Vector3 groundPosition, float width, float depth, Material timber, Material roof, Transform parent)
    {
        GameObject house = new GameObject(name);
        house.transform.SetParent(parent);
        CreateCube("Body", groundPosition + Vector3.up * 2.1f, new Vector3(width, 4.2f, depth), timber, house.transform);
        GameObject roofObject = CreateCube("Tiled Roof Placeholder", groundPosition + Vector3.up * 4.9f, new Vector3(width + 3f, 0.75f, depth + 3f), roof, house.transform);
        roofObject.transform.rotation = Quaternion.Euler(0f, 0f, 3f);
        CreateCube("Roof Ridge", groundPosition + Vector3.up * 5.5f, new Vector3(0.55f, 0.45f, depth + 3.5f), roof, house.transform);
    }

    private static void CreateGate(string name, Vector3 position, Vector3 facing, Material timber, Material topMaterial, Transform parent)
    {
        GameObject gate = new GameObject(name);
        gate.transform.SetParent(parent);
        gate.transform.position = position;
        gate.transform.rotation = Quaternion.LookRotation(facing);
        CreateCube("Left Post", position + gate.transform.right * -3.2f + Vector3.up * 2.6f, new Vector3(0.55f, 5.2f, 0.55f), timber, gate.transform);
        CreateCube("Right Post", position + gate.transform.right * 3.2f + Vector3.up * 2.6f, new Vector3(0.55f, 5.2f, 0.55f), timber, gate.transform);
        CreateCube("Lintel", position + Vector3.up * 5.0f, new Vector3(7.6f, 0.65f, 0.8f), timber, gate.transform);
        CreateCube("Gate Roof", position + Vector3.up * 5.65f, new Vector3(9f, 0.6f, 2.6f), topMaterial, gate.transform);
    }

    private static void CreateCircularFence(Transform parent, Vector3 center, float radius, Material material, int segments, float height, float gapDegrees)
    {
        GameObject fence = new GameObject("Low Timber Boundary Fence");
        fence.transform.SetParent(parent);
        for (int i = 0; i < segments; i++)
        {
            float angle = i * 360f / segments;
            if (Mathf.Abs(Mathf.DeltaAngle(angle, 270f)) < gapDegrees * 0.5f) continue;
            float radians = angle * Mathf.Deg2Rad;
            Vector3 position = center + new Vector3(Mathf.Cos(radians) * radius, 0f, Mathf.Sin(radians) * radius);
            CreateCube("Fence Post", position + Vector3.up * height * 0.5f, new Vector3(0.28f, height, 0.28f), material, fence.transform);
        }
    }

    private static void CreateDummyLine(Transform parent, Vector3 start, int count, float spacing, Material material)
    {
        for (int i = 0; i < count; i++)
        {
            Vector3 position = start + Vector3.right * spacing * i;
            GameObject dummy = new GameObject("Training Dummy " + (i + 1));
            dummy.transform.SetParent(parent);
            CreateCylinder("Post", position + Vector3.up * 1.15f, 0.22f, 2.3f, material, dummy.transform);
            CreateCube("Arms", position + Vector3.up * 1.65f, new Vector3(1.55f, 0.18f, 0.18f), material, dummy.transform);
        }
    }

    private static void CreateEnemy(string name, Vector3 position, Material material, Transform parent, float scale)
    {
        GameObject enemyObject = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        enemyObject.name = name;
        enemyObject.transform.SetParent(parent);
        enemyObject.transform.position = position + Vector3.up * scale;
        enemyObject.transform.localScale = new Vector3(scale, scale, scale);
        enemyObject.GetComponent<Renderer>().sharedMaterial = material;
    }

    private static void CreateTorch(Vector3 position, Transform parent)
    {
        GameObject torch = new GameObject("Boss Brazier Placeholder");
        torch.transform.SetParent(parent);
        torch.transform.position = position;
        Light light = torch.AddComponent<Light>();
        light.type = LightType.Point;
        light.range = 13f;
        light.intensity = 4f;
        light.color = new Color(1f, 0.35f, 0.08f);
    }

    private static GameObject CreateSegment(string name, Vector3 a, Vector3 b, float width, float height, Material material, Transform parent)
    {
        Vector3 delta = b - a;
        Vector3 center = (a + b) * 0.5f + Vector3.up * 0.08f;
        GameObject segment = CreateCube(name, center, new Vector3(width, height, delta.magnitude), material, parent);
        segment.transform.rotation = Quaternion.LookRotation(delta.normalized);
        return segment;
    }

    private static GameObject CreateCube(string name, Vector3 position, Vector3 scale, Material material, Transform parent)
    {
        GameObject gameObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
        gameObject.name = name;
        gameObject.transform.SetParent(parent, true);
        gameObject.transform.position = position;
        gameObject.transform.localScale = scale;
        gameObject.GetComponent<Renderer>().sharedMaterial = material;
        return gameObject;
    }

    private static GameObject CreateCylinder(string name, Vector3 position, float radius, float height, Material material, Transform parent)
    {
        GameObject gameObject = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        gameObject.name = name;
        gameObject.transform.SetParent(parent, true);
        gameObject.transform.position = position;
        gameObject.transform.localScale = new Vector3(radius, height * 0.5f, radius);
        gameObject.GetComponent<Renderer>().sharedMaterial = material;
        return gameObject;
    }

    private static Material CreateMaterial(string name, Color color, float smoothness)
    {
        string path = OutputFolder + "/" + name + ".mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");
        if (material == null)
        {
            material = new Material(shader) { name = name };
            AssetDatabase.CreateAsset(material, path);
        }
        else if (material.shader != shader)
        {
            material.shader = shader;
        }
        material.color = color;
        if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", smoothness);
        EditorUtility.SetDirty(material);
        return material;
    }

    private static void ConfigureRoadMaterial(Material material)
    {
        Texture2D dirtTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(OutputFolder + "/T_ForestEarth.asset");
        if (dirtTexture == null) return;
        if (material.HasProperty("_BaseMap")) material.SetTexture("_BaseMap", dirtTexture);
        if (material.HasProperty("_MainTex")) material.SetTexture("_MainTex", dirtTexture);
        material.mainTextureScale = new Vector2(0.55f, 2.2f);
        EditorUtility.SetDirty(material);
    }

    private static void EnsureFolder(string parent, string child)
    {
        string combined = parent + "/" + child;
        if (!AssetDatabase.IsValidFolder(combined)) AssetDatabase.CreateFolder(parent, child);
    }

    private static string Sanitize(string value)
    {
        foreach (char character in System.IO.Path.GetInvalidFileNameChars()) value = value.Replace(character, '_');
        return value.Replace(' ', '_').Replace('(', '_').Replace(')', '_').Replace('+', '_');
    }
}
#endif
