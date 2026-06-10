using UnityEngine;
/// <summary>
/// Mine lanceable : reste au sol après l'atterrissage et explose au contact
/// d'un collider présent dans le damageMask.
/// Hérite de ThrowableProjectile pour être compatible avec Bomb_Launch.
/// </summary>
public class MineProjectile : ThrowableProjectile
{
    [SerializeField] float radius = 50f;
    [SerializeField] int maxDamage = 80;
    [SerializeField] LayerMask damageMask;

    [Header("Explosion")]
    [SerializeField] GameObject explosionFxPrefab;
    [SerializeField] float exploseForce = 10f;
    [SerializeField] bool applyForce = false;

    [Header("Mine Settings")]
    [SerializeField] float armingDelay = 0.5f;

    bool pendingArm; // Arm() appelé, on attend le lâcher physique
    bool armed;      // Mine posée et active sur la surface

    // ── IThrowableProjectile ──────────────────────────────────────────────────

    public override void InitializeStats(int _attackerID, float _radius, int _damage, float _fuse, LayerMask _mask)
    {
        this.throwerID = _attackerID;
        radius      = _radius;
        maxDamage   = _damage;
        armingDelay = _fuse;
        damageMask  = _mask;
    }

    /// <summary>
    /// Appelé par Bomb_Launch juste avant le lancer.
    /// On pose un flag et on attend que le Rigidbody soit réellement en vol
    /// (isKinematic == false) avant de démarrer le timer d'armement.
    /// Ainsi la mine ne s'arme JAMAIS pendant qu'elle est encore dans la main.
    /// </summary>
    public override void Arm()
    {
        if (armed || pendingArm) return;
        pendingArm = true;
    }

    public override void Disarm()
    {
        armed      = false;
        pendingArm = false;
        CancelInvoke(nameof(ActivateTrigger));
    }

    // ── Cycle de vie ──────────────────────────────────────────────────────────

    void Update()
    {
        if (!pendingArm) return;

        // FIX : on détecte le lâcher via le Rigidbody, pas via transform.parent.
        // Quand la mine est en main : rb.isKinematic = true  → on attend.
        // Quand elle est lancée     : rb.isKinematic = false → on arme.
        // Quand elle est collée     : rb.isKinematic = true  → pendingArm est déjà false.
        Rigidbody rb = GetComponentInParent<Rigidbody>();
        if (rb != null && !rb.isKinematic)
        {
            pendingArm = false;
            Debug.Log("<color=orange>[Mine]</color> En vol — lancement du timer d'armement.");
            Invoke(nameof(ActivateTrigger), armingDelay);
        }
    }

    void ActivateTrigger()
    {
        armed = true;
        Debug.Log("<color=red>[Mine] ACTIVÉE !</color>");
    }

    // ── Trigger du capteur enfant ─────────────────────────────────────────────

    public void OnSensorTriggered(Collider other)
    {
        if (!armed || IsExploded) return;

        int layer = other.gameObject.layer;
        int playerLayer = LayerMask.NameToLayer("Player_Damageable");
        int enemyLayer = LayerMask.NameToLayer("Enemy_Damageable");

        if (layer != playerLayer && layer != enemyLayer) return;


        Debug.Log($"<color=red>[Mine]</color> Déclenchée par : {other.name}");
        Explode();
    }

    // ── Explosion ─────────────────────────────────────────────────────────────

    void Explode()
    {
        if (IsExploded) return;
        IsExploded = true;

        if (explosionFxPrefab != null)
            explosionFxPrefab.GetComponent<Dying_Explosion_Effect>().Explode(transform.position);

        Collider[] hits = Physics.OverlapSphere(transform.position, radius, damageMask);
        foreach (var col in hits)
        {
            if (col.TryGetComponent<IDamageable>(out var dmg) && !col.CompareTag("Bomb"))
            {
                float dist   = Vector3.Distance(transform.position, col.ClosestPoint(transform.position));
                float t      = Mathf.InverseLerp(radius, 0f, dist);
                int   damage = Mathf.RoundToInt(maxDamage * t);
                if (damage > 0) dmg.TakeDamage(damage, (col.transform.position - transform.position).normalized, this.throwerID, false);
            }

            if (applyForce && col.attachedRigidbody != null && !col.CompareTag("Player"))
                col.attachedRigidbody.AddExplosionForce(exploseForce, transform.position, radius, 1f, ForceMode.Impulse);
        }
        AudioManager.instance?.PlaySoundAtLocation("Explosion", transform.position);
        Destroy(gameObject, 0.1f);
    }

    // ── Collision / Collage ───────────────────────────────────────────────────

    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Player") ||
                collision.gameObject.CompareTag("Bomb")) return;

        Rigidbody rb = GetComponentInParent<Rigidbody>();
        if (rb != null && rb.isKinematic) return;

        StickToSurface(collision);
    }

    /// <summary>
    /// Colle la mine à n'importe quelle surface correctement :
    ///
    ///  1. Normale MOYENNE de tous les contacts → stable sur coins et murs courbes.
    ///  2. Demi-taille AABB correcte : Σ |extents[i] * normal[i]|
    ///     (au lieu de Vector3.Dot qui peut être négatif et sous-estime les diagonales).
    ///  3. On déplace la RACINE PHYSIQUE (Rigidbody.transform), pas l'enfant
    ///     MineProjectile, sinon seule la partie visuelle se déplace.
    ///  4. isKinematic = true  → retiré du pipeline physique, aucune simulation.
    /// </summary>
    void StickToSurface(Collision collision)
    {
        // ── 1. Normale moyenne ────────────────────────────────────────────────
        Vector3 avgNormal = Vector3.zero;
        foreach (ContactPoint cp in collision.contacts)
            avgNormal += cp.normal;
        avgNormal = (avgNormal / collision.contactCount).normalized;

        // ── 2. Racine physique ────────────────────────────────────────────────
        Rigidbody rb       = GetComponentInParent<Rigidbody>();
        Transform physRoot = rb != null ? rb.transform : transform;

        // ── 3. Demi-taille correcte le long de la normale ─────────────────────
        Collider col = physRoot.GetComponent<Collider>();
        if (col == null) col = physRoot.GetComponentInChildren<Collider>();

        const float epsilon = 0.015f; 
        float halfExtent    = epsilon;

        if (col != null)
        {
            // bounds.extents est en world-space AABB.
            // Formule correcte pour la projection d'une AABB sur une direction :
            //   reach = |ex*nx| + |ey*ny| + |ez*nz|
            // Vector3.Dot seul est FAUX (peut être négatif, sous-estime les diagonales)
            Vector3 e = col.bounds.extents;
            halfExtent += Mathf.Abs(e.x * avgNormal.x)
                        + Mathf.Abs(e.y * avgNormal.y)
                        + Mathf.Abs(e.z * avgNormal.z);
        }

        // ── 4. Repositionner et orienter la racine ────────────────────────────
        Vector3 contactPoint = collision.contacts[0].point;
        physRoot.position    = contactPoint + avgNormal * halfExtent;
        physRoot.rotation    = Quaternion.FromToRotation(Vector3.up, avgNormal);

        // ── 5. Geler la physique ──────────────────────────────────────────────
        if (rb != null)
        {
            rb.linearVelocity  = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic     = true;
        }

        AudioManager.instance?.PlaySound("Mine_Activation", gameObject, true);

        Debug.Log($"<color=yellow>[Mine]</color> Collée — normale:{avgNormal} offset:{halfExtent:F3}");
    }

    // ── Gizmos ────────────────────────────────────────────────────────────────

#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        Gizmos.color = new Color(1f, 0.8f, 0f, 0.25f);
        Gizmos.DrawSphere(transform.position, radius);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, radius);
    }
#endif
}