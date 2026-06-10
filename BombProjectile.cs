using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.VFX;

public class BombProjectile : ThrowableProjectile
{
    public enum TypeBomb { Smoke, Explode }

    [SerializeField] float fuseTime = 2.0f;
    [SerializeField] bool explodeOnImpact = false;

    [SerializeField] float radius = 50f;
    [SerializeField] int maxDamage = 80;
    [SerializeField] LayerMask damageMask;

    [Header("Type of Bomb")]
    [SerializeField] TypeBomb type;
    [Header("Explosion")]
    [SerializeField] GameObject explosionFxPrefab;
    [SerializeField] float exploseForce = 10f;
    [SerializeField] bool applyForce;
    [Header("Smoke")]
    [SerializeField] VisualEffectAsset smokeFxAsset;

    bool armed;

    protected override void Awake()
    {
        base.Awake();
    }

    private void Start()
    {
        Renderer rend = GetComponentInChildren<Renderer>();

        if (rend != null)
        {
            Color targetColor = (type == TypeBomb.Explode) ? Color.white : Color.black;

            MaterialPropertyBlock propBlock = new MaterialPropertyBlock();

            rend.GetPropertyBlock(propBlock);

            propBlock.SetColor("_Color", targetColor);

            rend.SetPropertyBlock(propBlock);
        }
        else
        {
            Debug.LogError($"[BombProjectile] Aucun Renderer trouvé sur {gameObject.name} ou ses enfants !");
        }
    }

    public override void InitializeStats(int _attackerID, float _radius, int _damage, float _fuse, LayerMask _mask)
    {
        this.throwerID = _attackerID;
        radius = _radius;
        maxDamage = _damage;
        fuseTime = _fuse;
        damageMask = _mask;
    }

    public override void Arm()
    {
        if (armed) return;
        armed = true;
        HideLine();
        CancelInvoke(nameof(Explode));
        Invoke(nameof(Explode), fuseTime);
    }

    public override void Disarm()
    {
        armed = false;
        CancelInvoke(nameof(Explode));
    }

    void OnCollisionEnter(Collision collision)
    {
        if (!armed || IsExploded) return;
        if (explodeOnImpact) Explode();

        AudioManager.instance?.PlaySound("Rebound_Grenade", gameObject, true);
    }

    void Explode()
    {
        if (!armed || IsExploded) return;
        IsExploded = true;
        HideLine();

        if (type == TypeBomb.Explode)
        {
            if (explosionFxPrefab != null)
            {
                var rb = GetComponent<Rigidbody>();
                rb.useGravity = false;
                rb.isKinematic = true;
                rb.freezeRotation = true;
                explosionFxPrefab.GetComponent<Dying_Explosion_Effect>().Explode(transform.position);
            }

            Collider[] hits = Physics.OverlapSphere(transform.position, radius, damageMask);
            foreach (var col in hits)
            {
                if (col.TryGetComponent<IDamageable>(out var dmg) && !col.CompareTag("Bomb"))
                {
                    float dist = Vector3.Distance(transform.position, col.ClosestPoint(transform.position));
                    float t = Mathf.InverseLerp(radius, 0f, dist);
                    int damage = Mathf.RoundToInt(maxDamage * t);
                    if (damage > 0) dmg.TakeDamage(damage, col.transform.position - transform.position, this.throwerID, false);
                }

                if (col.attachedRigidbody != null && applyForce
                    && !col.CompareTag("Player") && !col.CompareTag("Bomb"))
                    col.attachedRigidbody.AddExplosionForce(exploseForce, transform.position, radius, 1f, ForceMode.Impulse);
            }
            AudioManager.instance?.PlaySoundAtLocation("Explosion", transform.localPosition);

        }
        else
        {
            explosionFxPrefab.gameObject.SetActive(false);
            SpawnSmoke();
        }

        GetComponent<MeshRenderer>().enabled = false;
        GetComponent<Collider>().enabled = false;
        GetComponent<TrailRenderer>().enabled = false;

        Destroy(gameObject, 0.5f);
    }

    void SpawnSmoke()
    {
        if (smokeFxAsset == null) return;
        var go = new GameObject("SmokeVFX");
        go.transform.position = transform.position;
        var vfx = go.AddComponent<VisualEffect>();
        vfx.visualEffectAsset = smokeFxAsset;
        vfx.Play();
        Destroy(go, 10f);
    }

#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        Gizmos.color = new Color(1f, 0.3f, 0f, 0.35f);
        Gizmos.DrawSphere(transform.position, radius);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, radius);
    }
#endif
}