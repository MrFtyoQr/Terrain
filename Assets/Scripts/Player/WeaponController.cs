using UnityEngine;
using System.Collections;

public class WeaponController : MonoBehaviour
{
    [Header("Weapon")]
    [SerializeField] GameObject weaponPrefab;
    [SerializeField] Vector3 weaponOffset = new Vector3(0.15f, -0.1f, 0.25f);
    [SerializeField] Vector3 weaponRotation = new Vector3(0f, 0f, 0f);
    [SerializeField] float weaponScale = 0.5f;

    [Header("Shooting")]
    [SerializeField] float fireRate = 0.08f;
    [SerializeField] float range = 300f;
    [SerializeField] int damage = 1;

    [Header("Ammo")]
    [SerializeField] int magazineSize = 30;
    [SerializeField] int maxReserveAmmo = 120;
    [SerializeField] float reloadTime = 1.5f;

    [Header("Muzzle Flash")]
    [SerializeField] float flashIntensity = 4f;

    public int CurrentAmmo { get; private set; }
    public int ReserveAmmo { get; private set; }
    public bool IsReloading { get; private set; }

    float nextFireTime;
    Light muzzleFlash;
    float flashTimer;
    Transform weaponTransform;
    Vector3 weaponBasePos;
    GameHUD hud;

    void Start()
    {
        CurrentAmmo = magazineSize;
        ReserveAmmo = maxReserveAmmo;
        SpawnWeapon();
        CreateMuzzleFlash();
        hud = FindAnyObjectByType<GameHUD>();
        UpdateHUD();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.R) && !IsReloading && CurrentAmmo < magazineSize && ReserveAmmo > 0)
            StartCoroutine(Reload());

        if (Input.GetMouseButton(0) && Time.time >= nextFireTime && !IsReloading)
        {
            if (CurrentAmmo > 0)
            {
                nextFireTime = Time.time + fireRate;
                Shoot();
            }
            else if (ReserveAmmo > 0)
            {
                StartCoroutine(Reload());
            }
        }

        if (flashTimer > 0f)
        {
            flashTimer -= Time.deltaTime;
            if (flashTimer <= 0f && muzzleFlash != null)
                muzzleFlash.enabled = false;
        }
    }

    void SpawnWeapon()
    {
        if (weaponPrefab == null) return;

        var instance = Instantiate(weaponPrefab, transform);
        instance.name = "Weapon";
        instance.transform.localPosition = weaponOffset;
        instance.transform.localRotation = Quaternion.Euler(weaponRotation);
        instance.transform.localScale = Vector3.one * weaponScale;

        foreach (var col in instance.GetComponentsInChildren<Collider>())
            col.enabled = false;

        weaponTransform = instance.transform;
        weaponBasePos = weaponOffset;
    }

    void CreateMuzzleFlash()
    {
        var flashGo = new GameObject("MuzzleFlash");
        flashGo.transform.SetParent(transform);
        flashGo.transform.localPosition = weaponOffset + new Vector3(0f, 0.08f, 0.3f);
        muzzleFlash = flashGo.AddComponent<Light>();
        muzzleFlash.type = LightType.Point;
        muzzleFlash.color = new Color(1f, 0.7f, 0.2f);
        muzzleFlash.intensity = flashIntensity;
        muzzleFlash.range = 8f;
        muzzleFlash.enabled = false;
    }

    void Shoot()
    {
        CurrentAmmo--;
        UpdateHUD();

        if (muzzleFlash != null)
        {
            muzzleFlash.enabled = true;
            flashTimer = 0.04f;
        }

        if (weaponTransform != null)
            StartCoroutine(RecoilKick());

        var cam = Camera.main;
        if (cam == null) return;

        var ray = new Ray(cam.transform.position, cam.transform.forward);
        if (Physics.Raycast(ray, out var hit, range))
        {
            var sphere = hit.collider.GetComponent<FloatingSphere>();
            if (sphere != null)
                sphere.TakeDamage(damage);
        }
    }

    IEnumerator RecoilKick()
    {
        Vector3 recoilPos = weaponBasePos + new Vector3(0f, 0.015f, -0.03f);
        weaponTransform.localPosition = recoilPos;
        float t = 0f;
        while (t < 0.06f)
        {
            t += Time.deltaTime;
            weaponTransform.localPosition = Vector3.Lerp(recoilPos, weaponBasePos, t / 0.06f);
            yield return null;
        }
        weaponTransform.localPosition = weaponBasePos;
    }

    IEnumerator Reload()
    {
        if (IsReloading || ReserveAmmo <= 0) yield break;
        IsReloading = true;
        UpdateHUD();

        if (weaponTransform != null)
        {
            Vector3 reloadPos = weaponBasePos + new Vector3(0f, -0.2f, 0f);
            float half = reloadTime * 0.5f;
            float t = 0f;
            while (t < half)
            {
                t += Time.deltaTime;
                weaponTransform.localPosition = Vector3.Lerp(weaponBasePos, reloadPos, t / half);
                yield return null;
            }
            t = 0f;
            while (t < half)
            {
                t += Time.deltaTime;
                weaponTransform.localPosition = Vector3.Lerp(reloadPos, weaponBasePos, t / half);
                yield return null;
            }
            weaponTransform.localPosition = weaponBasePos;
        }
        else
        {
            yield return new WaitForSeconds(reloadTime);
        }

        int needed = magazineSize - CurrentAmmo;
        int toLoad = Mathf.Min(needed, ReserveAmmo);
        CurrentAmmo += toLoad;
        ReserveAmmo -= toLoad;
        IsReloading = false;
        UpdateHUD();
    }

    void UpdateHUD()
    {
        if (hud == null) hud = FindAnyObjectByType<GameHUD>();
        if (hud != null) hud.UpdateAmmo(CurrentAmmo, ReserveAmmo, IsReloading);
    }
}
