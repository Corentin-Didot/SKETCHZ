using Unity.Cinemachine;
using UnityEngine;

public enum WeaponType
{
    Ranged,
    Bomb,
    Melee,
    Heal
}

public enum ThrowableType
{
    Bomb,
    Mine
}

[CreateAssetMenu(fileName = "WeaponConfig", menuName = "Weapons/Weapon Config", order = 1)]
public class Weapon_Config : ScriptableObject
{
    [Header("Visual")]
    public WeaponType weaponType;
    public string weaponID;
    public RuntimeAnimatorController animatorController;
    public GameObject armsPrefab;
    [Header("UI")]
    public Sprite weaponIcon;

    [Header("Fire")]
    public WeaponController_Online.FireMode fireMode;
    public float fireRate;
    public float range;
    public int damage;

    [Header("Spread")]
    public bool useSpread;
    public float spreadAngle;
    public float spreadMultADS;

    [Header("Dynamic Spread - Distance")]
    public bool spreadWithDistance = true;
    public float distanceStart = 10f;
    public float distanceEnd = 80f;
    public float maxExtraSpreadByDistance = 3f;
    public AnimationCurve distanceSpreadCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Dynamic Spread - Consecutive shots (Bloom)")]
    public bool spreadWithBloom = true;
    public float bloomPerShot = 0.20f;
    public float bloomMax = 2.5f;
    public float bloomRecovery = 8f;
    public float chainResetDelay = 0.25f;

    [Header("Burst")]
    public int burstCount;
    public float burstInterval;
    public float burstCooldown;

    [Header("Ammunition")]
    public int maxAmmo;
    public float reloadRate;
    public float reloadDelay;

    [Header("Damage per distance")]
    public float damageReducerMinDistance = 5f;
    public float damageReducerPerMeter = 0.5f;

    [Range(0f, 1f)]
    public float damageReducerCap = 0.5f;


    [Header("Sound Name")]
    public string soundNameShoot = "Shoot";

    // Bomb

    [Header("Explosion (Bomb / Mine)")]
    public ThrowableType throwableType = ThrowableType.Bomb;
    public GameObject bombPrefab;
    public GameObject minePrefab;
    public float explosionRadius;
    public float explosionDamage;
    public float fuseTime;      // Bombe : délai avant explosion | Mine : délai d'armement

    [Header("Launch Settings (Bomb / Mine)")]
    public float arcHeight = 3f;
    public float power = 50f;
    public float flatSpeed = 50f;

    [Header("Mask")]
    public LayerMask hitMask;

    [Header("Animation 3rd person")]
    public float stopShootDelay = 1f;


    //Heal
    
    [Header("Heal")]
    public int healAmount;       
    public int healCharges = 1;
    public Sprite bigHealSprite;



    // ── ID ───────────────────────────────────────────────────────────────

    /// <summary>Renvoie le prefab actif selon le throwableType sélectionné.</summary>
    public GameObject ActiveThrowablePrefab =>
        throwableType == ThrowableType.Mine ? minePrefab : bombPrefab;
}