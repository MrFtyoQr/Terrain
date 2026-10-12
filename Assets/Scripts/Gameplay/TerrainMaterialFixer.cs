using UnityEngine;

public class TerrainMaterialFixer : MonoBehaviour
{
    void Start()
    {
        FixBrokenRenderers();
    }

    void FixBrokenRenderers()
    {
        Shader urpLit = Shader.Find("Universal Render Pipeline/Lit");
        if (urpLit == null) return;

        int fixed_ = 0;
        foreach (var renderer in FindObjectsByType<Renderer>(FindObjectsSortMode.None))
        {
            if (renderer == null) continue;
            var mats = renderer.sharedMaterials;
            bool changed = false;
            for (int i = 0; i < mats.Length; i++)
            {
                var mat = mats[i];
                if (mat == null) continue;
                string sn = mat.shader.name;
                if (sn == "Hidden/InternalErrorShader" ||
                    sn.Contains("HDRP") ||
                    sn.Contains("HDRenderPipeline") ||
                    sn.Contains("Shader Graphs/"))
                {
                    var newMat = new Material(urpLit);
                    Texture tex = null;
                    if (mat.HasProperty("_BaseColorMap")) tex = mat.GetTexture("_BaseColorMap");
                    if (tex == null && mat.HasProperty("_MainTex")) tex = mat.GetTexture("_MainTex");
                    if (tex != null)
                        newMat.SetTexture("_BaseMap", tex);
                    else
                        newMat.SetColor("_BaseColor", new Color(0.4f, 0.38f, 0.33f));
                    newMat.SetFloat("_Smoothness", 0.25f);
                    mats[i] = newMat;
                    changed = true;
                    fixed_++;
                }
            }
            if (changed) renderer.sharedMaterials = mats;
        }
        if (fixed_ > 0)
            Debug.Log($"[TerrainMaterialFixer] Fixed {fixed_} broken materials at runtime.");
    }
}
