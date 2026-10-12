using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.Rendering;
using System.Collections.Generic;
using System.Linq;

public static class TerrainSetup
{
    const string TerrainDataPath = "Assets/TerrainDemoScene_HDRP/Terrain/Data";
    const string TerrainLayersPath = "Assets/TerrainDemoScene_HDRP/Terrain/Layers";
    const string ScenePath = "Assets/Scenes/TerrainGame.unity";

    [MenuItem("Tools/Terrain Game/2. Setup Terrain Scene")]
    public static void SetupTerrainScene()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        var terrainDatas = LoadOriginalTerrainData();
        if (terrainDatas.Count == 0)
        {
            Debug.LogError("[TerrainSetup] No terrain data found at " + TerrainDataPath);
            return;
        }

        var terrainLayers = LoadTerrainLayers();
        Debug.Log($"[TerrainSetup] Loaded {terrainDatas.Count} terrain tiles and {terrainLayers.Length} terrain layers.");

        var terrainMat = CreateURPTerrainMaterial();

        var parent = new GameObject("Terrain");
        var terrains = new Dictionary<Vector2Int, Terrain>();

        foreach (var kvp in terrainDatas)
        {
            var coord = kvp.Key;
            var data = kvp.Value;

            var go = Terrain.CreateTerrainGameObject(data);
            go.name = $"Terrain_{coord.x}_{coord.y}";
            go.transform.SetParent(parent.transform);
            go.transform.position = new Vector3(
                coord.x * data.size.x,
                0f,
                coord.y * data.size.z
            );

            var terrain = go.GetComponent<Terrain>();
            terrain.materialTemplate = terrainMat;
            terrain.drawTreesAndFoliage = true;
            terrain.treeBillboardDistance = 200f;
            terrain.treeDistance = 2000f;
            terrain.detailObjectDistance = 150f;
            terrain.heightmapPixelError = 5f;

            if (terrainLayers.Length > 0)
                data.terrainLayers = terrainLayers;

            var collider = go.GetComponent<TerrainCollider>();
            if (collider != null)
                collider.terrainData = data;

            terrains[coord] = terrain;
        }

        SetNeighbors(terrains);
        SetupLighting();
        SetupPostProcessing();

        if (!AssetDatabase.IsValidFolder("Assets/Scenes"))
            AssetDatabase.CreateFolder("Assets", "Scenes");

        EditorSceneManager.SaveScene(scene, ScenePath);
        AssetDatabase.Refresh();
        Debug.Log($"[TerrainSetup] Scene saved to {ScenePath} with {terrains.Count} terrain tiles.");
    }

    static Dictionary<Vector2Int, TerrainData> LoadOriginalTerrainData()
    {
        var result = new Dictionary<Vector2Int, TerrainData>();
        var guids = AssetDatabase.FindAssets("t:TerrainData", new[] { TerrainDataPath });
        foreach (var guid in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var fileName = System.IO.Path.GetFileNameWithoutExtension(path);

            if (fileName.Contains(" 1")) continue;

            if (!TryParseCoord(fileName, out var coord)) continue;

            var data = AssetDatabase.LoadAssetAtPath<TerrainData>(path);
            if (data != null && !result.ContainsKey(coord))
                result[coord] = data;
        }
        return result;
    }

    static bool TryParseCoord(string fileName, out Vector2Int coord)
    {
        coord = Vector2Int.zero;
        var parts = fileName.Split('_');
        if (parts.Length < 3) return false;
        if (!int.TryParse(parts[1], out int x)) return false;
        if (!int.TryParse(parts[2], out int y)) return false;
        coord = new Vector2Int(x, y);
        return true;
    }

    static TerrainLayer[] LoadTerrainLayers()
    {
        var guids = AssetDatabase.FindAssets("t:TerrainLayer", new[] { TerrainLayersPath });
        return guids
            .Select(g => AssetDatabase.LoadAssetAtPath<TerrainLayer>(AssetDatabase.GUIDToAssetPath(g)))
            .Where(l => l != null)
            .ToArray();
    }

    static void SetNeighbors(Dictionary<Vector2Int, Terrain> terrains)
    {
        foreach (var kvp in terrains)
        {
            var c = kvp.Key;
            terrains.TryGetValue(c + Vector2Int.up, out var top);
            terrains.TryGetValue(c + Vector2Int.down, out var bottom);
            terrains.TryGetValue(c + Vector2Int.left, out var left);
            terrains.TryGetValue(c + Vector2Int.right, out var right);
            kvp.Value.SetNeighbors(left, top, right, bottom);
        }
    }

    static void SetupLighting()
    {
        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.53f, 0.71f, 0.85f);
        RenderSettings.ambientEquatorColor = new Color(0.65f, 0.65f, 0.55f);
        RenderSettings.ambientGroundColor = new Color(0.25f, 0.2f, 0.15f);
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogColor = new Color(0.7f, 0.8f, 0.9f);
        RenderSettings.fogStartDistance = 200f;
        RenderSettings.fogEndDistance = 2000f;

        var lightGo = new GameObject("Directional Light");
        var light = lightGo.AddComponent<Light>();
        light.type = LightType.Directional;
        light.color = new Color(1f, 0.95f, 0.84f);
        light.intensity = 1.5f;
        light.shadows = LightShadows.Soft;
        light.shadowStrength = 0.8f;
        lightGo.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
    }

    static void SetupPostProcessing()
    {
        var volumeGo = new GameObject("Global Volume");
        var volume = volumeGo.AddComponent<Volume>();
        volume.isGlobal = true;
        var profile = ScriptableObject.CreateInstance<VolumeProfile>();

        if (!AssetDatabase.IsValidFolder("Assets/Settings"))
            AssetDatabase.CreateFolder("Assets", "Settings");
        AssetDatabase.CreateAsset(profile, "Assets/Settings/TerrainGameVolumeProfile.asset");
        volume.profile = profile;
    }

    static Material CreateURPTerrainMaterial()
    {
        string matPath = "Assets/Settings/URPTerrainMaterial.mat";
        var existing = AssetDatabase.LoadAssetAtPath<Material>(matPath);
        if (existing != null) return existing;

        Shader terrainShader = Shader.Find("Universal Render Pipeline/Terrain/Lit");
        if (terrainShader == null)
        {
            Debug.LogWarning("[TerrainSetup] URP Terrain/Lit shader not found, trying fallback.");
            terrainShader = Shader.Find("Nature/Terrain/Standard");
        }
        if (terrainShader == null)
        {
            Debug.LogError("[TerrainSetup] No terrain shader found!");
            return null;
        }

        var mat = new Material(terrainShader);
        mat.name = "URPTerrainMaterial";

        if (!AssetDatabase.IsValidFolder("Assets/Settings"))
            AssetDatabase.CreateFolder("Assets", "Settings");
        AssetDatabase.CreateAsset(mat, matPath);
        Debug.Log($"[TerrainSetup] Created URP terrain material at {matPath}");
        return mat;
    }
}
