using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class Melee_Attack_Online : TeamColorObserver, IWeapon, IWeaponFeedback
{
    [Header("Melee Settings")]
    [SerializeField] private Transform player;
    [Space]
    [SerializeField] private float meleeDamage = 50f;
    [SerializeField] private float meleeRange = 2f;
    [SerializeField] private float meleeRadius = 0.5f;
    [SerializeField] private float meleeRate = 1.0f;
    [SerializeField] private LayerMask hitMask;
    [SerializeField] private string meleeAnimationTrigger = "Cut";

    [Header("References")]
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private float timeCutVisible = 1f;
    [SerializeField] GameObject weaponPrefab;
    private SkinnedMeshRenderer weaponRenderer;
    Material instancedWeaponMaterial;
    int powerPropertyID;
    string emissivePropertyName = "_Emissive";
    int emissivePropertyID;

    private int attackerID = -1;

    private WeaponAnimation currentWeaponAnim;
    private float nextMeleeTime;
    public float MeleeRange => meleeRange;
    public float MeleeRadius => meleeRadius;

    

    [Header("Player 3rd Person Anim")]
    [SerializeField] bool isOtherPlayer;
    [SerializeField] Animator animator;

    public void Init()
    {
        if (cameraTransform == null && Camera.main != null)
            cameraTransform = Camera.main.transform;

        currentWeaponAnim = FindAnyObjectByType<WeaponAnimation>();

        powerPropertyID = Shader.PropertyToID("_Power");
        emissivePropertyID = Shader.PropertyToID(emissivePropertyName);

        SkinnedMeshRenderer[] allRenderers = weaponPrefab.GetComponentsInChildren<SkinnedMeshRenderer>(true);
        foreach (SkinnedMeshRenderer smr in allRenderers)
        {
            if (smr.CompareTag("Weapon") && smr.name.Contains("knife"))
            {
                weaponRenderer = smr;
                break;
            }
        }

        if (weaponRenderer != null)
        {
            for (int i = 0; i < weaponRenderer.materials.Length; i++)
            {
                if (weaponRenderer.materials[i].name.Contains("Tank"))
                {
                    instancedWeaponMaterial = weaponRenderer.materials[i];
                }
            }

            instancedWeaponMaterial.SetFloat(powerPropertyID, 0.5f);
            instancedWeaponMaterial.SetColor(emissivePropertyID, colorTeam);
        }

        if (this.player != null && this.player.TryGetComponent<Player_Health>(out Player_Health ph))
        {
            this.attackerID = ph.DamageableID;
        }
    }

    public bool CanShoot()
    {
        bool ready = Time.time >= nextMeleeTime;
        if (!ready) Debug.Log($"[Melee Debug] Attaque en cooldown. Prêt dans : {nextMeleeTime - Time.time:F2}s");
        return ready;
    }

    public void Shoot()
    {
        if (!CanShoot()) return;

        nextMeleeTime = Time.time + meleeRate;
        PlayAttackFeedback();

        Ray ray = new Ray(cameraTransform.position, cameraTransform.forward);

        if (Physics.SphereCast(ray, meleeRadius, out RaycastHit hit, meleeRange, hitMask))
        {
            if (player != null && hit.collider.transform.IsChildOf(player))
            {
                Debug.Log("[Melee Debug] Self-hit ignoré (slide/dash).");
                return;
            }

            ApplyDamage(hit, true, out int _, out float _);
        }
    }

    public bool ApplyDamage(RaycastHit _hit, bool _doDamages, out int _hitID, out float _damages)
    {
        if (_hit.collider.TryGetComponent<IDamageable>(out var damageable))
        {
            damageable.TakeDamage(meleeDamage, transform.position - _hit.point, this.attackerID, false);

            _hitID = damageable.DamageableID;
            _damages = meleeDamage;
            return true;
        }
        else
        {
            Debug.LogWarning($"[Melee Debug] {_hit.collider.name} n'a pas de composant IDamageable !");
            _hitID = -1;
            _damages = -1.0f;
            return false;
        }
    }

    public void PlayAttackFeedback()
    {
        WeaponController_Online controller = GetComponentInParent<WeaponController_Online>();
        if (controller == null) return;

        Weapon_Visual_Manager visualManager = controller.GetComponentInChildren<Weapon_Visual_Manager>();

        if (visualManager != null)
        {
            StartCoroutine(VisualCutSequence(visualManager));
        }

        if (currentWeaponAnim != null)
        {
            currentWeaponAnim.PlayMeleeAnimation(meleeAnimationTrigger);
        }

        if (isOtherPlayer)
        {
            animator.SetTrigger("OnStab");
        }

        if (AudioManager.instance != null)
            AudioManager.instance.PlaySound("Melee_Swoosh", gameObject, true);
    }

    private IEnumerator VisualCutSequence(Weapon_Visual_Manager visualManager)
    {
        GameObject currentWeaponModel = visualManager.GetActiveModel();
        Transform knifeTransform = visualManager.FindDeepChild(visualManager.transform, "GRP_KNIFE");

        if (currentWeaponModel != null && knifeTransform != null)
        {
            currentWeaponModel.SetActive(false);
            knifeTransform.gameObject.SetActive(true);

            yield return new WaitForSeconds(timeCutVisible);

            knifeTransform.gameObject.SetActive(false);
            currentWeaponModel.SetActive(true);
        }
        else
        {
            Debug.LogWarning("[Melee] Impossible de faire le switch visuel : " +
                             (currentWeaponModel == null ? "Arme active nulle" : "GRP_KNIFE introuvable"));
        }
    }

    protected override void OnColorUpdated(Color newColor)
    {
        if (instancedWeaponMaterial != null)
            instancedWeaponMaterial.SetColor(emissivePropertyID, newColor);
    }
}