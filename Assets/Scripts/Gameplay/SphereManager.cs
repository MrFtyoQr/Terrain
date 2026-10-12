using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class SphereManager : MonoBehaviour
{
    [Header("Spawning")]
    [SerializeField] int targetCount = 3;
    [SerializeField] float spawnHeight = 6f;
    [SerializeField] float minDistanceFromPlayer = 15f;
    [SerializeField] float maxDistanceFromPlayer = 60f;
    [SerializeField] float minDistanceBetweenSpheres = 10f;
    [SerializeField] float respawnDelay = 1.5f;

    [Header("Sphere Settings")]
    [SerializeField] float sphereRadius = 1.2f;

    public int Score { get; private set; }

    List<FloatingSphere> activeSpheres = new List<FloatingSphere>();
    Transform playerTransform;
    Material sphereMaterial;
    Terrain[] allTerrains;

    void Start()
    {
        var player = FindAnyObjectByType<FirstPersonController>();
        if (player != null)
            playerTransform = player.transform;

        allTerrains = FindObjectsByType<Terrain>(FindObjectsSortMode.None);
        CreateDefaultMaterial();

        for (int i = 0; i < targetCount; i++)
            SpawnSphere();
    }

    void CreateDefaultMaterial()
    {
        sphereMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        sphereMaterial.SetColor("_BaseColor", new Color(0.3f, 0.6f, 1f));
        sphereMaterial.SetColor("_EmissionColor", new Color(0.3f, 0.6f, 1f) * 4f);
        sphereMaterial.EnableKeyword("_EMISSION");
        sphereMaterial.SetFloat("_Smoothness", 0.95f);
        sphereMaterial.SetFloat("_Metallic", 0.1f);
    }

    void SpawnSphere()
    {
        Vector3 spawnPos = FindSpawnPosition();

        var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        go.name = "FloatingSphere";
        go.transform.position = spawnPos;
        go.transform.localScale = Vector3.one * (sphereRadius * 2f);

        go.GetComponent<Renderer>().material = sphereMaterial;

        var sphere = go.AddComponent<FloatingSphere>();
        sphere.OnDeath += OnSphereDeath;
        activeSpheres.Add(sphere);
    }

    float SampleTerrainHeight(float x, float z)
    {
        if (allTerrains == null || allTerrains.Length == 0) return -1f;

        foreach (var t in allTerrains)
        {
            if (t == null) continue;
            var pos = t.transform.position;
            var size = t.terrainData.size;
            if (x >= pos.x && x <= pos.x + size.x && z >= pos.z && z <= pos.z + size.z)
                return t.SampleHeight(new Vector3(x, 0, z)) + pos.y;
        }
        return -1f;
    }

    Vector3 FindSpawnPosition()
    {
        Vector3 playerPos = playerTransform != null ? playerTransform.position : GetTerrainGridCenter();

        for (int attempt = 0; attempt < 100; attempt++)
        {
            float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
            float dist = Random.Range(minDistanceFromPlayer, maxDistanceFromPlayer);
            float x = playerPos.x + Mathf.Cos(angle) * dist;
            float z = playerPos.z + Mathf.Sin(angle) * dist;

            float terrainHeight = SampleTerrainHeight(x, z);
            if (terrainHeight < 0f) continue;

            Vector3 candidate = new Vector3(x, terrainHeight + spawnHeight, z);

            bool tooClose = false;
            foreach (var s in activeSpheres)
            {
                if (s == null) continue;
                if (Vector3.Distance(s.transform.position, candidate) < minDistanceBetweenSpheres)
                {
                    tooClose = true;
                    break;
                }
            }
            if (tooClose) continue;

            return candidate;
        }

        return playerPos + new Vector3(Random.Range(-20f, 20f), spawnHeight + 5f, Random.Range(-20f, 20f));
    }

    Vector3 GetTerrainGridCenter()
    {
        if (allTerrains == null || allTerrains.Length == 0) return Vector3.zero;

        float minX = float.MaxValue, minZ = float.MaxValue;
        float maxX = float.MinValue, maxZ = float.MinValue;
        foreach (var t in allTerrains)
        {
            if (t == null) continue;
            var pos = t.transform.position;
            var size = t.terrainData.size;
            if (pos.x < minX) minX = pos.x;
            if (pos.z < minZ) minZ = pos.z;
            if (pos.x + size.x > maxX) maxX = pos.x + size.x;
            if (pos.z + size.z > maxZ) maxZ = pos.z + size.z;
        }

        float cx = (minX + maxX) * 0.5f;
        float cz = (minZ + maxZ) * 0.5f;
        float cy = SampleTerrainHeight(cx, cz);
        return new Vector3(cx, Mathf.Max(cy, 0f), cz);
    }

    void OnSphereDeath(FloatingSphere sphere)
    {
        Score++;
        activeSpheres.Remove(sphere);

        var hud = FindAnyObjectByType<GameHUD>();
        if (hud != null)
            hud.UpdateScore(Score);

        StartCoroutine(RespawnAfterDelay());
    }

    IEnumerator RespawnAfterDelay()
    {
        yield return new WaitForSeconds(respawnDelay);
        activeSpheres.RemoveAll(s => s == null);
        while (activeSpheres.Count < targetCount)
            SpawnSphere();
    }
}
