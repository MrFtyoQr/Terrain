using UnityEngine;
using UnityEngine.UI;

public class GameHUD : MonoBehaviour
{
    Text scoreText;
    Text ammoText;
    Text reloadText;
    Image hitMarker;
    float hitMarkerTimer;

    Font uiFont;

    void Start()
    {
        var canvas = GetComponent<Canvas>();
        if (canvas == null)
        {
            canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
        }

        var scaler = GetComponent<CanvasScaler>();
        if (scaler == null)
            scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        if (GetComponent<GraphicRaycaster>() == null)
            gameObject.AddComponent<GraphicRaycaster>();

        uiFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (uiFont == null) uiFont = Resources.GetBuiltinResource<Font>("Arial.ttf");
        if (uiFont == null) uiFont = Font.CreateDynamicFontFromOSFont("Arial", 1);

        CreateCrosshair();
        CreateHitMarker();
        CreateScoreText();
        CreateAmmoText();
        CreateReloadText();
    }

    void Update()
    {
        if (hitMarkerTimer > 0f)
        {
            hitMarkerTimer -= Time.deltaTime;
            if (hitMarkerTimer <= 0f && hitMarker != null)
                hitMarker.enabled = false;
        }
    }

    void CreateCrosshair()
    {
        var root = new GameObject("Crosshair");
        root.transform.SetParent(transform, false);

        CreateUIImage(root.transform, Vector2.zero, new Vector2(4, 4), Color.white);
        CreateUIImage(root.transform, new Vector2(0, 12), new Vector2(3, 16), new Color(1f, 1f, 1f, 0.9f));
        CreateUIImage(root.transform, new Vector2(0, -12), new Vector2(3, 16), new Color(1f, 1f, 1f, 0.9f));
        CreateUIImage(root.transform, new Vector2(12, 0), new Vector2(16, 3), new Color(1f, 1f, 1f, 0.9f));
        CreateUIImage(root.transform, new Vector2(-12, 0), new Vector2(16, 3), new Color(1f, 1f, 1f, 0.9f));
    }

    void CreateHitMarker()
    {
        var go = new GameObject("HitMarker");
        go.transform.SetParent(transform, false);

        var root = go.AddComponent<RectTransform>();
        root.anchorMin = new Vector2(0.5f, 0.5f);
        root.anchorMax = new Vector2(0.5f, 0.5f);
        root.anchoredPosition = Vector2.zero;
        root.sizeDelta = new Vector2(40f, 40f);

        hitMarker = CreateUIImage(go.transform, new Vector2(8, 8), new Vector2(14, 3), new Color(1f, 0.2f, 0.2f, 0.9f));
        CreateUIImage(go.transform, new Vector2(-8, -8), new Vector2(14, 3), new Color(1f, 0.2f, 0.2f, 0.9f));
        CreateUIImage(go.transform, new Vector2(-8, 8), new Vector2(3, 14), new Color(1f, 0.2f, 0.2f, 0.9f));
        CreateUIImage(go.transform, new Vector2(8, -8), new Vector2(3, 14), new Color(1f, 0.2f, 0.2f, 0.9f));

        go.SetActive(false);
    }

    Image CreateUIImage(Transform parent, Vector2 position, Vector2 size, Color color)
    {
        var go = new GameObject("Img");
        go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>();
        img.color = color;
        var rect = img.rectTransform;
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        return img;
    }

    void CreateScoreText()
    {
        scoreText = CreateText("ScoreText",
            new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f),
            new Vector2(-25f, -15f), new Vector2(350f, 60f),
            "Score: 0", 42, TextAnchor.UpperRight);
    }

    void CreateAmmoText()
    {
        ammoText = CreateText("AmmoText",
            new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f),
            new Vector2(-30f, 40f), new Vector2(400f, 70f),
            "30 / 120", 52, TextAnchor.LowerRight);
    }

    void CreateReloadText()
    {
        reloadText = CreateText("ReloadText",
            new Vector2(0.5f, 0.35f), new Vector2(0.5f, 0.35f), new Vector2(0.5f, 0.5f),
            Vector2.zero, new Vector2(400f, 55f),
            "RECARGANDO...", 36, TextAnchor.MiddleCenter);
        reloadText.color = new Color(1f, 0.85f, 0.1f);
        reloadText.gameObject.SetActive(false);
    }

    Text CreateText(string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot,
        Vector2 position, Vector2 size, string content, int fontSize, TextAnchor alignment)
    {
        var go = new GameObject(name);
        go.transform.SetParent(transform, false);
        var text = go.AddComponent<Text>();
        text.text = content;
        text.font = uiFont;
        text.fontSize = fontSize;
        text.color = Color.white;
        text.alignment = alignment;
        text.fontStyle = FontStyle.Bold;

        var outline = go.AddComponent<Outline>();
        outline.effectColor = new Color(0f, 0f, 0f, 0.9f);
        outline.effectDistance = new Vector2(2f, -2f);

        var shadow = go.AddComponent<Shadow>();
        shadow.effectColor = new Color(0f, 0f, 0f, 0.5f);
        shadow.effectDistance = new Vector2(3f, -3f);

        var rect = text.rectTransform;
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        return text;
    }

    public void UpdateScore(int score)
    {
        if (scoreText != null)
            scoreText.text = $"Score: {score}";
    }

    public void UpdateAmmo(int current, int reserve, bool isReloading)
    {
        if (ammoText != null)
        {
            ammoText.text = $"{current} / {reserve}";
            ammoText.color = current <= 5 ? new Color(1f, 0.3f, 0.3f) : Color.white;
        }

        if (reloadText != null)
            reloadText.gameObject.SetActive(isReloading);
    }

    public void ShowHitMarker()
    {
        if (hitMarker != null)
        {
            hitMarker.transform.parent.gameObject.SetActive(true);
            hitMarkerTimer = 0.2f;
        }
    }
}
