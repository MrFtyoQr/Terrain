using UnityEngine;
using System;
using System.Collections;

[RequireComponent(typeof(SphereCollider))]
public class FloatingSphere : MonoBehaviour
{
    [Header("Health")]
    [SerializeField] int maxHealth = 5;

    [Header("Floating")]
    [SerializeField] float bobAmplitude = 0.8f;
    [SerializeField] float bobSpeed = 1.5f;
    [SerializeField] float driftRadius = 4f;
    [SerializeField] float driftSpeed = 0.4f;
    [SerializeField] float spinSpeed = 45f;

    public event Action<FloatingSphere> OnDeath;

    int currentHealth;
    Vector3 anchorPosition;
    float bobOffset;
    float driftAngle;
    Renderer sphereRenderer;
    MaterialPropertyBlock propBlock;
    bool isDying;
    Color baseColor;
    Light glowLight;

    void Awake()
    {
        sphereRenderer = GetComponent<Renderer>();
        propBlock = new MaterialPropertyBlock();
        bobOffset = UnityEngine.Random.Range(0f, Mathf.PI * 2f);
        driftAngle = UnityEngine.Random.Range(0f, Mathf.PI * 2f);
    }

    void Start()
    {
        currentHealth = maxHealth;
        anchorPosition = transform.position;

        baseColor = new Color(
            UnityEngine.Random.Range(0.2f, 1f),
            UnityEngine.Random.Range(0.2f, 1f),
            UnityEngine.Random.Range(0.5f, 1f)
        );
        SetColor(baseColor);

        var lightGo = new GameObject("Glow");
        lightGo.transform.SetParent(transform);
        lightGo.transform.localPosition = Vector3.zero;
        glowLight = lightGo.AddComponent<Light>();
        glowLight.type = LightType.Point;
        glowLight.color = baseColor;
        glowLight.intensity = 2f;
        glowLight.range = 10f;
    }

    void Update()
    {
        if (isDying) return;

        driftAngle += driftSpeed * Time.deltaTime;
        float bobY = Mathf.Sin((Time.time + bobOffset) * bobSpeed) * bobAmplitude;
        float driftX = Mathf.Cos(driftAngle) * driftRadius;
        float driftZ = Mathf.Sin(driftAngle) * driftRadius;

        transform.position = anchorPosition + new Vector3(driftX, bobY, driftZ);
        transform.Rotate(Vector3.up, spinSpeed * Time.deltaTime);
    }

    public void TakeDamage(int amount)
    {
        if (isDying) return;

        currentHealth -= amount;

        var hud = FindAnyObjectByType<GameHUD>();
        if (hud != null) hud.ShowHitMarker();

        if (currentHealth <= 0)
        {
            isDying = true;
            StartCoroutine(DeathSequence());
        }
        else
        {
            StartCoroutine(HitFlash());
        }
    }

    IEnumerator HitFlash()
    {
        SetColor(Color.red);
        float originalScale = transform.localScale.x;
        transform.localScale = Vector3.one * (originalScale * 1.2f);
        if (glowLight != null) { glowLight.color = Color.red; glowLight.intensity = 5f; }
        yield return new WaitForSeconds(0.12f);
        transform.localScale = Vector3.one * originalScale;
        SetColor(baseColor);
        if (glowLight != null) { glowLight.color = baseColor; glowLight.intensity = 2f; }
    }

    IEnumerator DeathSequence()
    {
        SetColor(Color.white);
        if (glowLight != null) { glowLight.color = Color.white; glowLight.intensity = 8f; }

        float duration = 0.4f;
        float elapsed = 0f;
        Vector3 startScale = transform.localScale;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            transform.localScale = Vector3.Lerp(startScale, startScale * 1.5f, t);
            if (sphereRenderer != null)
            {
                Color c = Color.Lerp(Color.white, new Color(1, 1, 1, 0), t);
                SetColor(c);
            }
            if (glowLight != null) glowLight.intensity = Mathf.Lerp(8f, 0f, t);
            yield return null;
        }

        OnDeath?.Invoke(this);
        Destroy(gameObject);
    }

    void SetColor(Color color)
    {
        if (sphereRenderer == null) return;
        propBlock.SetColor("_BaseColor", color);
        propBlock.SetColor("_EmissionColor", color * 4f);
        sphereRenderer.SetPropertyBlock(propBlock);
    }
}
