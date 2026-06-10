using UnityEngine;
using UnityEngine.InputSystem;

public class Bomb_Launch : MonoBehaviour, IWeapon, IWeaponFeedback
{
    [SerializeField] private Transform player;

    [Header("Held projectile (in hand)")]
    [SerializeField] ThrowableProjectile heldProjectile;
    [SerializeField] Transform handSocket;

    [Header("Throw")]
    [SerializeField] Transform throwOrigin;
    private float range;
    private float arcHeight;
    private float cooldown;
    private float power;
    private float flatSpeed;
    private LayerMask aimMask;
    [SerializeField] Camera cam;

    LineRenderer lineRenderer;

    [SerializeField]
    [Range(10, 100)]
    int linePOints = 25;

    public Weapon_Config weaponConfig;
    float nextShootTime;


    // ── Config ────────────────────────────────────────────────────────────────

    public void SetAndApplyConfig(Weapon_Config config)
    {
        weaponConfig = config;
        ApplyConfig();
    }

    private void ApplyConfig()
    {
        if (weaponConfig == null) return;
        range = weaponConfig.range;
        cooldown = weaponConfig.fireRate;
        aimMask = weaponConfig.hitMask;
        arcHeight = weaponConfig.arcHeight;
        power = weaponConfig.power;
        flatSpeed = weaponConfig.flatSpeed;
    }

    // ── IWeapon ───────────────────────────────────────────────────────────────

    public bool ApplyDamage(RaycastHit _hit, bool _doDamages, out int _hitID, out float _damages) => throw new System.NotImplementedException();
    public bool CanShoot()
    {
        if (Time.time < nextShootTime || heldProjectile == null) return false;
        if (ExplosiveInventory.Instance == null) return true; 
        return ExplosiveInventory.Instance.HasAny(GetTypeOfExplosive());
    }

    public void PlayAttackFeedback() => throw new System.NotImplementedException();

    public void Shoot()
    {
        HideLine();
        if (!CanShoot()) return;
        nextShootTime = Time.time + cooldown;

        ExplosiveInventory.Instance?.TryConsume(GetTypeOfExplosive());
        
        Ray ray = Camera.main.ViewportPointToRay(new Vector3(0.5f, 0.5f));
        Vector3 targetPoint = Physics.Raycast(ray, out RaycastHit hit, range, aimMask, QueryTriggerInteraction.Ignore)
            ? hit.point : ray.origin + ray.direction * range;

        Debug.LogWarning(heldProjectile.name);
        GameObject projectileRoot = heldProjectile.GetComponentInParent<Rigidbody>().gameObject;

        projectileRoot.transform.SetParent(null, true);

        int ignoreLayer = 2;
        projectileRoot.layer = ignoreLayer;
        foreach (Transform child in projectileRoot.GetComponentsInChildren<Transform>())
            child.gameObject.layer = ignoreLayer;

        Rigidbody rb = heldProjectile.GetComponentInParent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = false;
            rb.useGravity = true;

            Vector3 v0 = CalculateLaunchVelocityGrenade(
                throwOrigin.position, targetPoint, cam.transform,
                flatSpeed, arcHeight, power, out _);

            if (Vector3.Dot(v0, cam.transform.forward) < 0)
                v0 = cam.transform.forward * flatSpeed;

            rb.linearVelocity = v0;
            Debug.Log($"<color=cyan>[Launch]</color> Lancé vers : {targetPoint}");
        }
        else
        {
            Debug.LogError($"<color=red>[Launch]</color> Rigidbody introuvable sur {projectileRoot.name}");
        }

        Collider col = projectileRoot.GetComponent<Collider>();
        if (col == null) col = projectileRoot.GetComponentInChildren<Collider>();
        if (col != null) col.enabled = true;

        heldProjectile.Arm();
        heldProjectile = null;
    }

    // ── Trajectoire ───────────────────────────────────────────────────────────

    Vector3 CalculateLaunchVelocityGrenade(
        Vector3 start, Vector3 end, Transform camTransform,
        float flatSpeed, float height, float power,
        out float flightTime)
    {
        Vector3 gravity = Physics.gravity;
        Vector3 toEnd = end - start;
        Vector3 toEndXZ = Vector3.ProjectOnPlane(toEnd, Vector3.up);
        float distXZ = toEndXZ.magnitude;
        Vector3 dirXZ = (distXZ > 0.001f)
                                ? toEndXZ / distXZ
                                : Vector3.ProjectOnPlane(camTransform.forward, Vector3.up).normalized;

        float pitch = Mathf.Asin(camTransform.forward.normalized.y) * Mathf.Rad2Deg;
        float blend = Mathf.InverseLerp(5f, 45f, pitch);
        blend = Mathf.Clamp01(blend);
        blend = blend * blend * (3f - 2f * blend);

        float speedFlat = Mathf.Max(0.1f, flatSpeed);
        float tFlat = Mathf.Clamp(distXZ / speedFlat, 0.15f, 0.60f);
        float vyFlat = Mathf.Clamp((toEnd.y - 0.5f * gravity.y * tFlat * tFlat) / tFlat, -2f, 6f);
        Vector3 vFlat = dirXZ * speedFlat + Vector3.up * vyFlat;

        float speedLob = Mathf.Max(0.1f, power);
        float tLob = Mathf.Clamp(distXZ / speedLob, 0.35f, 1.25f);
        tLob += Mathf.Clamp(height, 0f, 4f) * 0.10f;
        Vector3 vLob = (toEnd - 0.5f * gravity * tLob * tLob) / tLob;

        flightTime = Mathf.Lerp(tFlat, tLob, blend);
        return Vector3.Lerp(vFlat, vLob, blend);
    }

    // ── Gestion de la main ────────────────────────────────────────────────────

    void SnapProjectileToHand()
    {
        if (heldProjectile == null || handSocket == null) return;
        SetProjectileTrail(heldProjectile, false);

        Rigidbody rb = heldProjectile.GetComponentInParent<Rigidbody>();
        if (rb == null)
        {
            Debug.LogError("[Bomb_Launch] Rigidbody introuvable sur la hiérarchie du projectile !");
            return;
        }
        GameObject rootObject = rb.gameObject;

        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.useGravity = false;

        Collider col = rootObject.GetComponent<Collider>();
        if (col == null) col = rootObject.GetComponentInChildren<Collider>();
        if (col != null) col.enabled = false;

        rootObject.transform.SetParent(handSocket, false);
        rootObject.transform.localPosition = Vector3.zero;
        rootObject.transform.localRotation = Quaternion.identity;

        heldProjectile.Disarm();
    }

    public void Init() => SnapProjectileToHand();

    // ── Update ────────────────────────────────────────────────────────────────

    void Update()
    {
        if (heldProjectile != null && heldProjectile.IsExploded)
        {
            HideLine();
            heldProjectile = null;
        }
    }

    // ── Actions publiques ─────────────────────────────────────────────────────

    public void PrepareQuickThrow()
    {
        if (heldProjectile == null) RespawnProjectile();

        if (CanShoot())
        {
            heldProjectile.Arm();
            Debug.Log("Projectile armé (lancer rapide) !");
        }
    }

    public void HoldAim()
    {
        if (heldProjectile != null)
            UpdateTrajectory();
    }

    // ── Spawn ─────────────────────────────────────────────────────────────────

    public void RespawnProjectile()
    {
        GameObject prefab = GetPrefabFromConfig();
        if (prefab == null || handSocket == null)
        {
            Debug.LogWarning("Aucun prefab configuré dans WeaponConfig !");
            return;
        }

        GameObject newObj = Instantiate(prefab, handSocket.position, handSocket.rotation);
        heldProjectile = newObj.GetComponent<ThrowableProjectile>();
        if (heldProjectile == null)
            heldProjectile = newObj.GetComponentInChildren<ThrowableProjectile>();

        if (heldProjectile == null)
        {
            Debug.LogError("Le prefab n'a pas de composant ThrowableProjectile !");
            return;
        }

        int throwerID = -1;
        if(this.player != null && this.player.TryGetComponent<Player_Health>(out Player_Health ph))
        {
            throwerID = ph.DamageableID;
        }

        heldProjectile.InitializeStats(
            throwerID,
            weaponConfig.explosionRadius,
            (int)weaponConfig.explosionDamage,
            weaponConfig.fuseTime,
            weaponConfig.hitMask
        );

        lineRenderer = newObj.GetComponent<LineRenderer>();
        if (lineRenderer == null) lineRenderer = newObj.GetComponentInChildren<LineRenderer>();

        SnapProjectileToHand();
    }

    GameObject GetPrefabFromConfig()
    {
        Debug.Log("nom : " + weaponConfig.name + " throwable type : " + weaponConfig.throwableType);
        if (weaponConfig == null) return null;
        return weaponConfig.ActiveThrowablePrefab;
    }

    // ── Trail ─────────────────────────────────────────────────────────────────

    void SetProjectileTrail(ThrowableProjectile proj, bool on)
    {
        TrailRenderer tr = proj.GetComponentInChildren<TrailRenderer>(true);
        if (tr == null) return;
        tr.Clear();
        tr.emitting = on;
    }

    // ── Line Renderer ─────────────────────────────────────────────────────────

    public void ShowLine(Vector3 startPoint, Vector3 velocity, float totalFlightTime)
    {
        if (lineRenderer == null) return;
        lineRenderer.enabled = true;
        lineRenderer.positionCount = linePOints;

        float timeStep = totalFlightTime / (linePOints - 1);
        for (int i = 0; i < linePOints; i++)
        {
            float t = i * timeStep;
            Vector3 point = startPoint + velocity * t + 0.5f * Physics.gravity * t * t;
            lineRenderer.SetPosition(i, point);
        }
    }

    public void HideLine()
    {
        if (lineRenderer != null)
            lineRenderer.enabled = false;
    }

    void UpdateTrajectory()
    {
        Ray ray = Camera.main.ViewportPointToRay(new Vector3(0.5f, 0.5f));
        float effectiveRange = (range <= 0) ? 50f : range;
        Vector3 targetPoint;

        if (Physics.Raycast(ray, out RaycastHit hit, effectiveRange, aimMask, QueryTriggerInteraction.Ignore))
        {
            targetPoint = hit.point;
            if (Vector3.Distance(ray.origin, targetPoint) < 1.0f)
                targetPoint = ray.origin + ray.direction * effectiveRange;
        }
        else
            targetPoint = ray.origin + ray.direction * effectiveRange;

        Vector3 velocity = CalculateLaunchVelocityGrenade(
            throwOrigin.position, targetPoint, cam.transform,
            flatSpeed, arcHeight, power, out float flightTime);

        ShowLine(throwOrigin.position, velocity, flightTime);
    }

    public ThrowableType GetTypeOfExplosive()
    {
        if (weaponConfig == null)
        {
            Debug.LogError("[Bomb_Launch] weaponConfig est null ! Assigne-le dans l'Inspector sur le composant Bomb_Launch.");
            return default;
        }
        return weaponConfig.throwableType;
    }


    // ── Gizmos ────────────────────────────────────────────────────────────────

#if UNITY_EDITOR
    /*
        void OnDrawGizmos()
        {
            if (!drawThrowArc || throwOrigin == null || !Application.isPlaying) return;

            Ray ray = Camera.main.ViewportPointToRay(new Vector3(0.5f, 0.5f));
            Vector3 target = Physics.Raycast(ray, out RaycastHit hit, range, aimMask)
                                ? hit.point
                                : ray.origin + ray.direction * range;

            Vector3 velocity = CalculateLaunchVelocityGrenade(
                throwOrigin.position, target, cam.transform,
                flatSpeed, arcHeight, power, out _);

            Gizmos.color = arcColor;
            Vector3 prevPoint = throwOrigin.position;
            for (int i = 1; i <= arcResolution; i++)
            {
                float t = i * 0.1f;
                Vector3 point = throwOrigin.position + velocity * t + 0.5f * Physics.gravity * t * t;
                Gizmos.DrawLine(prevPoint, point);
                prevPoint = point;
            }
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(target, 0.15f);
        }
    */
#endif
}