using UnityEngine;
using UnityEditor;
using System.IO;

public static class MaterialConverter
{
    [MenuItem("Tools/Terrain Game/1. Convert Materials to URP")]
    public static void ConvertAllMaterials()
    {
        int converted = 0;
        converted += ConvertAllHDRPMaterials();
        converted += ConvertGunMaterials();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[MaterialConverter] Done! {converted} materials converted to URP.");
    }

    static int ConvertAllHDRPMaterials()
    {
        int count = 0;
        string basePath = "Assets/TerrainDemoScene_HDRP";
        if (!AssetDatabase.IsValidFolder(basePath)) return 0;

        Shader urpLit = Shader.Find("Universal Render Pipeline/Lit");
        Shader urpSpeedTree = Shader.Find("Universal Render Pipeline/Nature/SpeedTree8");
        if (urpLit == null)
        {
            Debug.LogError("[MaterialConverter] URP/Lit shader not found!");
            return 0;
        }

        var guids = AssetDatabase.FindAssets("t:Material", new[] { basePath });
        Debug.Log($"[MaterialConverter] Found {guids.Length} materials in {basePath}");

        foreach (var guid in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null) continue;

            string shaderName = mat.shader.name;
            if (shaderName.StartsWith("Universal Render Pipeline")) continue;

            var mainTex = ExtractTexture(mat, "_BaseColorMap", "_MainTex", "_BaseMap");
            var normalTex = ExtractTexture(mat, "_NormalMap", "_BumpMap");
            var maskTex = ExtractTexture(mat, "_MaskMap");
            var color = ExtractColor(mat, "_BaseColor", "_Color");
            float metallic = ExtractFloat(mat, "_Metallic", 0f);
            float smoothness = ExtractFloat(mat, "_Smoothness", "_Glossiness", 0.5f);

            bool isSpeedTree = shaderName.Contains("SpeedTree") ||
                               path.Contains("/Trees/");

            if (isSpeedTree && urpSpeedTree != null)
            {
                mat.shader = urpSpeedTree;
                if (mainTex != null) mat.SetTexture("_MainTex", mainTex);
                if (normalTex != null) mat.SetTexture("_BumpMap", normalTex);
                mat.SetColor("_Color", color);
            }
            else
            {
                mat.shader = urpLit;
                if (mainTex != null) mat.SetTexture("_BaseMap", mainTex);
                if (normalTex != null)
                {
                    mat.SetTexture("_BumpMap", normalTex);
                    mat.EnableKeyword("_NORMALMAP");
                }
                if (maskTex != null)
                    mat.SetTexture("_MetallicGlossMap", maskTex);
                mat.SetColor("_BaseColor", color);
                mat.SetFloat("_Metallic", metallic);
                mat.SetFloat("_Smoothness", smoothness);
            }

            EditorUtility.SetDirty(mat);
            count++;
            Debug.Log($"[MaterialConverter] {Path.GetFileName(path)}: {shaderName} -> {mat.shader.name}");
        }
        return count;
    }

    static int ConvertGunMaterials()
    {
        int count = 0;
        Shader urpLit = Shader.Find("Universal Render Pipeline/Lit");
        if (urpLit == null) return 0;

        string[] gunPaths = {
            "Assets/Low Poly Guns/Models",
            "Assets/Low Poly Guns/Materials",
            "Assets/Sci Fi Gun Light/Materials"
        };
        foreach (var basePath in gunPaths)
        {
            if (!AssetDatabase.IsValidFolder(basePath)) continue;
            var guids = AssetDatabase.FindAssets("t:Material", new[] { basePath });
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (mat == null) continue;

                string shaderName = mat.shader.name;
                if (shaderName.StartsWith("Universal Render Pipeline")) continue;

                var mainTex = ExtractTexture(mat, "_MainTex", "_BaseMap", "_BaseColorMap");
                var normalTex = ExtractTexture(mat, "_BumpMap", "_NormalMap");
                var color = ExtractColor(mat, "_Color", "_BaseColor");
                float metallic = ExtractFloat(mat, "_Metallic", 0f);
                float smoothness = ExtractFloat(mat, "_Glossiness", "_Smoothness", 0.5f);

                mat.shader = urpLit;
                if (mainTex != null) mat.SetTexture("_BaseMap", mainTex);
                if (normalTex != null)
                {
                    mat.SetTexture("_BumpMap", normalTex);
                    mat.EnableKeyword("_NORMALMAP");
                }
                mat.SetColor("_BaseColor", color);
                mat.SetFloat("_Metallic", metallic);
                mat.SetFloat("_Smoothness", smoothness);
                EditorUtility.SetDirty(mat);
                count++;
                Debug.Log($"[MaterialConverter] Gun: {Path.GetFileName(path)} -> URP/Lit");
            }
        }
        return count;
    }

    static Texture ExtractTexture(Material mat, params string[] props)
    {
        foreach (var p in props)
            if (mat.HasProperty(p))
            {
                var tex = mat.GetTexture(p);
                if (tex != null) return tex;
            }
        return null;
    }

    static Color ExtractColor(Material mat, params string[] props)
    {
        foreach (var p in props)
            if (mat.HasProperty(p)) return mat.GetColor(p);
        return Color.white;
    }

    static float ExtractFloat(Material mat, string prop, float fallback)
    {
        return mat.HasProperty(prop) ? mat.GetFloat(prop) : fallback;
    }

    static float ExtractFloat(Material mat, string prop1, string prop2, float fallback)
    {
        if (mat.HasProperty(prop1)) return mat.GetFloat(prop1);
        if (mat.HasProperty(prop2)) return mat.GetFloat(prop2);
        return fallback;
    }
}
