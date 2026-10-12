using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public static class TerrainCleaner
{
    [MenuItem("Tools/Terrain Game/4. Fix Terrain (clean white objects + add trees)")]
    public static void FixTerrain()
    {
        var terrains = Object.FindObjectsByType<Terrain>(FindObjectsSortMode.None);
        if (terrains.Length == 0)
        {
            Debug.LogError("[TerrainCleaner] No terrains found in scene.");
            return;
        }

        ClearDetails(terrains);
        ClearBrokenTrees(terrains);
        AddURPTrees(terrains);
        AddRocks(terrains);

        AssetDatabase.SaveAssets();
        Debug.Log("[TerrainCleaner] Terrain fixed! Details cleared, URP trees and rocks added.");
    }

    static void ClearDetails(Terrain[] terrains)
    {
        int totalCleared = 0;
        foreach (var terrain in terrains)
        {
            var data = terrain.terrainData;
            if (data == null) continue;

            int detailCount = data.detailPrototypes.Length;
            if (detailCount == 0) continue;

            int res = data.detailResolution;
            for (int layer = 0; layer < detailCount; layer++)
            {
                int[,] empty = new int[res, res];
                data.SetDetailLayer(0, 0, layer, empty);
            }

            data.detailPrototypes = new DetailPrototype[0];
            EditorUtility.SetDirty(data);
            totalCleared += detailCount;
        }
        Debug.Log($"[TerrainCleaner] Cleared {totalCleared} detail layers (white objects removed).");
    }

    static void ClearBrokenTrees(Terrain[] terrains)
    {
        int total = 0;
        foreach (var terrain in terrains)
        {
            var data = terrain.terrainData;
            if (data == null) continue;
            total += data.treeInstanceCount;
            data.SetTreeInstances(new TreeInstance[0], true);
            data.treePrototypes = new TreePrototype[0];
            EditorUtility.SetDirty(data);
        }
        Debug.Log($"[TerrainCleaner] Cleared {total} old tree instances.");
    }

    static void AddURPTrees(Terrain[] terrains)
    {
        string[] treePaths = {
            "Assets/TerrainDemoScene_HDRP/Prefabs/Trees/Pines/Pine_A/Pine_A.prefab",
            "Assets/TerrainDemoScene_HDRP/Prefabs/Trees/Pines/Pine_B/Pine_B.prefab",
            "Assets/TerrainDemoScene_HDRP/Prefabs/Trees/Pines/Pine_C/Pine_C.prefab",
            "Assets/TerrainDemoScene_HDRP/Prefabs/Trees/Pines/Pine_D/Pine_D.prefab",
            "Assets/TerrainDemoScene_HDRP/Prefabs/Trees/Conifer/Conifer.prefab",
            "Assets/TerrainDemoScene_HDRP/Prefabs/Trees/Cypress/Cypress.prefab",
        };

        var validPrefabs = new List<GameObject>();
        Shader urpLit = Shader.Find("Universal Render Pipeline/Lit");

        foreach (var path in treePaths)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null) continue;

            var renderers = prefab.GetComponentsInChildren<Renderer>();
            foreach (var r in renderers)
            {
                var mats = r.sharedMaterials;
                for (int i = 0; i < mats.Length; i++)
                {
                    if (mats[i] == null) continue;
                    string sn = mats[i].shader.name;
                    if (!sn.StartsWith("Universal Render Pipeline") && urpLit != null)
                    {
                        Texture tex = null;
                        if (mats[i].HasProperty("_BaseColorMap")) tex = mats[i].GetTexture("_BaseColorMap");
                        if (tex == null && mats[i].HasProperty("_MainTex")) tex = mats[i].GetTexture("_MainTex");
                        if (tex == null && mats[i].HasProperty("_TrunkBaseColorMap")) tex = mats[i].GetTexture("_TrunkBaseColorMap");

                        Color col = Color.white;
                        if (mats[i].HasProperty("_BaseColor")) col = mats[i].GetColor("_BaseColor");
                        if (mats[i].HasProperty("_Color")) col = mats[i].GetColor("_Color");
                        if (mats[i].HasProperty("_TrunkBaseColor")) col = mats[i].GetColor("_TrunkBaseColor");

                        mats[i].shader = urpLit;
                        if (tex != null)
                            mats[i].SetTexture("_BaseMap", tex);
                        else
                            mats[i].SetColor("_BaseColor", new Color(0.25f, 0.35f, 0.15f));
                        mats[i].SetFloat("_Smoothness", 0.2f);
                        mats[i].SetFloat("_Cutoff", 0.5f);
                        mats[i].EnableKeyword("_ALPHATEST_ON");
                        mats[i].SetFloat("_AlphaClip", 1f);
                        mats[i].renderQueue = 2450;
                        EditorUtility.SetDirty(mats[i]);
                    }
                }
            }
            validPrefabs.Add(prefab);
            Debug.Log($"[TerrainCleaner] Tree prefab ready: {prefab.name}");
        }

        if (validPrefabs.Count == 0)
        {
            Debug.LogWarning("[TerrainCleaner] No valid tree prefabs found. Adding basic capsule trees.");
            return;
        }

        var protos = new TreePrototype[validPrefabs.Count];
        for (int i = 0; i < validPrefabs.Count; i++)
        {
            protos[i] = new TreePrototype { prefab = validPrefabs[i] };
        }

        int treesPerTile = 80;
        foreach (var terrain in terrains)
        {
            var data = terrain.terrainData;
            if (data == null) continue;

            data.treePrototypes = protos;

            var trees = new List<TreeInstance>();
            var size = data.size;

            for (int i = 0; i < treesPerTile; i++)
            {
                float nx = Random.Range(0.05f, 0.95f);
                float nz = Random.Range(0.05f, 0.95f);
                float height = data.GetInterpolatedHeight(nx, nz) / size.y;
                float steepness = data.GetSteepness(nx, nz);

                if (steepness > 30f) continue;
                if (height < 0.05f) continue;

                trees.Add(new TreeInstance
                {
                    position = new Vector3(nx, height, nz),
                    prototypeIndex = Random.Range(0, validPrefabs.Count),
                    widthScale = Random.Range(0.7f, 1.3f),
                    heightScale = Random.Range(0.8f, 1.2f),
                    color = Color.white,
                    lightmapColor = Color.white,
                    rotation = Random.Range(0f, Mathf.PI * 2f)
                });
            }

            data.SetTreeInstances(trees.ToArray(), true);
            EditorUtility.SetDirty(data);
            Debug.Log($"[TerrainCleaner] {terrain.name}: placed {trees.Count} trees");
        }

        Debug.Log($"[TerrainCleaner] Added trees using {validPrefabs.Count} URP-ready prefabs across {terrains.Length} tiles.");
    }

    static void AddRocks(Terrain[] terrains)
    {
        string[] rockPaths = {
            "Assets/TerrainDemoScene_HDRP/Prefabs/Rocks/Rock_A_01.prefab",
            "Assets/TerrainDemoScene_HDRP/Prefabs/Rocks/Rock_B_01.prefab",
            "Assets/TerrainDemoScene_HDRP/Prefabs/Rocks/Rock_C_01.prefab",
            "Assets/TerrainDemoScene_HDRP/Prefabs/Rocks/Rock_D.prefab",
        };

        Shader urpLit = Shader.Find("Universal Render Pipeline/Lit");
        var validRocks = new List<GameObject>();

        foreach (var path in rockPaths)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null) continue;

            foreach (var r in prefab.GetComponentsInChildren<Renderer>())
            {
                var mats = r.sharedMaterials;
                for (int i = 0; i < mats.Length; i++)
                {
                    if (mats[i] == null || urpLit == null) continue;
                    if (mats[i].shader.name.StartsWith("Universal Render Pipeline")) continue;

                    Texture tex = null;
                    if (mats[i].HasProperty("_BaseColorMap")) tex = mats[i].GetTexture("_BaseColorMap");
                    if (tex == null && mats[i].HasProperty("_MainTex")) tex = mats[i].GetTexture("_MainTex");

                    mats[i].shader = urpLit;
                    if (tex != null)
                        mats[i].SetTexture("_BaseMap", tex);
                    else
                        mats[i].SetColor("_BaseColor", new Color(0.4f, 0.38f, 0.33f));
                    mats[i].SetFloat("_Smoothness", 0.15f);
                    EditorUtility.SetDirty(mats[i]);
                }
            }
            validRocks.Add(prefab);
        }

        if (validRocks.Count == 0) return;

        int rocksPerTile = 15;
        int totalPlaced = 0;
        foreach (var terrain in terrains)
        {
            var data = terrain.terrainData;
            if (data == null) continue;
            var size = data.size;
            var terrainPos = terrain.transform.position;

            for (int i = 0; i < rocksPerTile; i++)
            {
                float nx = Random.Range(0.05f, 0.95f);
                float nz = Random.Range(0.05f, 0.95f);
                float steepness = data.GetSteepness(nx, nz);
                if (steepness > 45f) continue;

                float worldX = terrainPos.x + nx * size.x;
                float worldZ = terrainPos.z + nz * size.z;
                float worldY = terrain.SampleHeight(new Vector3(worldX, 0, worldZ)) + terrainPos.y;

                var rockPrefab = validRocks[Random.Range(0, validRocks.Count)];
                var rock = (GameObject)PrefabUtility.InstantiatePrefab(rockPrefab);
                rock.transform.position = new Vector3(worldX, worldY - 0.3f, worldZ);
                rock.transform.rotation = Quaternion.Euler(0, Random.Range(0f, 360f), 0);
                float s = Random.Range(0.8f, 2.5f);
                rock.transform.localScale = Vector3.one * s;
                rock.isStatic = true;
                totalPlaced++;
            }
        }
        Debug.Log($"[TerrainCleaner] Placed {totalPlaced} rocks across {terrains.Length} tiles.");
    }
}
