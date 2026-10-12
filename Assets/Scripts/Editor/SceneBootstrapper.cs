using UnityEngine;
using UnityEditor;

public static class SceneBootstrapper
{
    [MenuItem("Tools/Terrain Game/3. Bootstrap Game Objects")]
    public static void Bootstrap()
    {
        RemoveDefaultCamera();
        CreatePlayer();
        CreateGameManager();
        CreateHUD();
        CreateMaterialFixer();
        Debug.Log("[SceneBootstrapper] Game objects created. Save the scene (Ctrl+S) and press Play!");
    }

    static void RemoveDefaultCamera()
    {
        var existingCam = Camera.main;
        if (existingCam != null && existingCam.GetComponentInParent<FirstPersonController>() == null)
        {
            Object.DestroyImmediate(existingCam.gameObject);
            Debug.Log("[SceneBootstrapper] Removed default Main Camera.");
        }
    }

    static void CreatePlayer()
    {
        if (Object.FindAnyObjectByType<FirstPersonController>() != null)
        {
            Debug.Log("[SceneBootstrapper] Player already exists, skipping.");
            return;
        }

        var player = new GameObject("Player");
        var cc = player.AddComponent<CharacterController>();
        cc.height = 1.8f;
        cc.radius = 0.35f;
        cc.center = new Vector3(0f, 0.9f, 0f);
        cc.slopeLimit = 45f;
        cc.stepOffset = 0.4f;
        cc.skinWidth = 0.08f;
        player.AddComponent<FirstPersonController>();

        var camGo = new GameObject("MainCamera");
        camGo.tag = "MainCamera";
        camGo.transform.SetParent(player.transform);
        camGo.transform.localPosition = new Vector3(0f, 1.6f, 0f);
        camGo.transform.localRotation = Quaternion.identity;

        var cam = camGo.AddComponent<Camera>();
        cam.nearClipPlane = 0.1f;
        cam.farClipPlane = 3000f;
        cam.fieldOfView = 70f;
        camGo.AddComponent<AudioListener>();

        var weapon = camGo.AddComponent<WeaponController>();

        var gunPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
            "Assets/Sci Fi Gun Light/Prefabs/SciFiGunLight/SciFiGunLight_Black.prefab");
        if (gunPrefab != null)
        {
            var so = new SerializedObject(weapon);
            so.FindProperty("weaponPrefab").objectReferenceValue = gunPrefab;
            so.ApplyModifiedProperties();
            Debug.Log("[SceneBootstrapper] Gun prefab assigned: SciFiGunLight_Black");
        }
        else
        {
            Debug.LogWarning("[SceneBootstrapper] Gun prefab not found at expected path!");
        }

        Vector3 spawnPos = GetTerrainGridCenter();
        player.transform.position = spawnPos;

        Debug.Log($"[SceneBootstrapper] Player created at {spawnPos}");
    }

    static void CreateGameManager()
    {
        if (Object.FindAnyObjectByType<SphereManager>() != null)
        {
            Debug.Log("[SceneBootstrapper] SphereManager already exists, skipping.");
            return;
        }

        var go = new GameObject("GameManager");
        go.AddComponent<SphereManager>();
        Debug.Log("[SceneBootstrapper] GameManager created.");
    }

    static void CreateHUD()
    {
        if (Object.FindAnyObjectByType<GameHUD>() != null)
        {
            Debug.Log("[SceneBootstrapper] HUD already exists, skipping.");
            return;
        }

        var go = new GameObject("HUD");
        go.AddComponent<GameHUD>();
        Debug.Log("[SceneBootstrapper] HUD created.");
    }

    static void CreateMaterialFixer()
    {
        if (Object.FindAnyObjectByType<TerrainMaterialFixer>() != null) return;
        var go = new GameObject("TerrainMaterialFixer");
        go.AddComponent<TerrainMaterialFixer>();
        Debug.Log("[SceneBootstrapper] TerrainMaterialFixer created.");
    }

    static Vector3 GetTerrainGridCenter()
    {
        var terrains = Terrain.activeTerrains;
        if (terrains == null || terrains.Length == 0)
            return new Vector3(0f, 50f, 0f);

        float minX = float.MaxValue, minZ = float.MaxValue;
        float maxX = float.MinValue, maxZ = float.MinValue;
        foreach (var t in terrains)
        {
            var pos = t.transform.position;
            var size = t.terrainData.size;
            if (pos.x < minX) minX = pos.x;
            if (pos.z < minZ) minZ = pos.z;
            if (pos.x + size.x > maxX) maxX = pos.x + size.x;
            if (pos.z + size.z > maxZ) maxZ = pos.z + size.z;
        }

        float cx = (minX + maxX) * 0.5f;
        float cz = (minZ + maxZ) * 0.5f;

        float height = 0f;
        foreach (var t in terrains)
        {
            var pos = t.transform.position;
            var size = t.terrainData.size;
            if (cx >= pos.x && cx <= pos.x + size.x && cz >= pos.z && cz <= pos.z + size.z)
            {
                height = t.SampleHeight(new Vector3(cx, 0, cz)) + pos.y;
                break;
            }
        }

        return new Vector3(cx, height + 3f, cz);
    }
}
