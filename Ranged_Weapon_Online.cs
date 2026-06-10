using Online.Shared.Utils;
using System.Collections;
using Unity.Cinemachine;
using Unity.VisualScripting;
using Unity.XR.Oculus.Input;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.Universal;
using UnityEngine.VFX;
using static UnityEngine.UI.Image;

public class Ranged_Weapon_Online : TeamColorObserver, IWeapon, IWeaponFeedback
{
    #region Variables
    #region Variables WeaponConfig 
    [Header("References")]

    /// <summary>
    /// GameObect "barel"
    /// </summary>
    [Tooltip("Mettre le GameObject 'barel' : Weapon_Prefab/Shoot_Weapon/Visual/WeaponsHolder/WeaponRecoil/VisualRoot/barrel")]
    [SerializeField] Transform shootOrigin;
    [Tooltip("Mettre le GameObject 'barel' : Weapon_Prefab/Shoot_Weapon/Visual/WeaponsHolder/WeaponRecoil/VisualRoot/barrel")]
    [SerializeField] Transform shootOriginTrail;
    /// <summary>
    /// GameObject "player" 
    /// </summary>

    [Tooltip("Mettre le GameObject 'Player' : Player_Prefab/Player")]
    [SerializeField] Transform player;
    [Header("--- DATA FROM SCRIPTABLE OBJECT ---")]
    [Space(10)]

    [Header("Weapon settings")]
    [Space(10)]
    [SerializeField] float fireRate = 0.25f;
    [SerializeField] float range = 100f;
    [SerializeField] float damage = 10.0f;
    [SerializeField] LayerMask hitMask;
    [Space(10)]

    [Header("Dynamic Spread - Distance")]
    [Space(10)]
    [SerializeField] bool spreadWithDistance = true;
    [SerializeField] float distanceStart = 10f;
    [SerializeField] float distanceEnd = 80f;
    [SerializeField] float maxExtraSpreadByDistance = 3f;
    [SerializeField] AnimationCurve distanceSpreadCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Space(10)]
    [Header("Dynamic Spread - Consecutive shots (Bloom)")]
    [Space(10)]
    [SerializeField] bool spreadWithBloom = true;
    [SerializeField] float bloomPerShot = 0.20f;
    [SerializeField] float bloomMax = 2.5f;
    [SerializeField] float bloomRecovery = 8f;
    [SerializeField] float chainResetDelay = 0.25f;

    [Space(10)]
    [Header("Spread")]
    [Space(10)]
    [SerializeField] float spreadAngle = 1.5f;
    [SerializeField] bool useSpread = true;

    [Space(10)]
    [Header("Ammunition")]
    [Space(10)]
    [SerializeField] int maxAmmo = 30;

    [Space(10)]
    [Header("Audio")]
    [Space(10)]
    [SerializeField] string shootSoundName = "Shoot";

    [Space(10)]
    [Header("Reaload setting")]
    [Space(10)]
    [SerializeField] float reloadRate = 10f;
    [SerializeField] float reloadDelay = 1.5f;

    [Space(10)]
    [Header("Aim Settings")]
    [Space(10)]
    [SerializeField] bool useAimPrecision = true;
    [SerializeField, Range(0f, 1f)] float aimSpreadMultiplier = 0.4f;
    [SerializeField, Range(0f, 1f)] float aimSensibilityMultiplier = 0.8f;
    [SerializeField] ST_Local_Camera_Rig localCamRig;

    [SerializeField] float damageReducerMinDistance = 5f;
    [SerializeField] float damageReducerPerMeter = 0.5f;
    [SerializeField] float damageReducerCap = 0.5f;  // 0f -> 1f percentage

    #endregion

    [Header("--- OTHER VARIABLES ---")]

    [Space(10)]
    [Header("Bullet Trail")]
    [Space(10)]
    [Tooltip("Mettre le Prefab 'bulletsTrail': Assets\\_Project\\Prefabs\\Weapon\\bulletsTrail.prefab")]
    [SerializeField] GameObject trailPrefab;
    [SerializeField] float trailSpeed = 120f;
    [SerializeField] float trailLifeTime = 1.5f;

    [Space(10)]
    [Header("Impact FX")]
    [Space(10)]
    [Tooltip("Mettre le Prefab 'DecalPrefab': Assets\\_Project\\Prefabs\\Weapon\\DecalPrefab.prefab")]
    [SerializeField] GameObject impactDecalPrefab;

    [Space(10)]
    [Header("Splash Shoot")]
    [Space(10)]
    [Tooltip("Mettre le GameObject 'VFX_MuzzleFlash' : Weapon_Prefab/Shoot_Weapon/Visual/WeaponsHolder/WeaponRecoil/VisualRoot/barrel/VFX_MuzzleFlash")]
    [SerializeField] private VisualEffect slashVFXPrefab;
    [SerializeField] private string vfxColorParameter = "Main_Color";

    [Space(10)]
    [Header("CameraReferences")]
    [Space(10)]
    [Tooltip("Mettre le GameObject 'WeaponCamera' : Player_Prefab/Camera_Holder/Camera_Yaw/Camera_Pitch/Camera_Tilt/WeaponCamera")]
    [SerializeField] Camera weaponCamera;
    [SerializeField] Camera mainCamera;
    [Tooltip("Mettre le GameObject 'barel' : Weapon_Prefab/Shoot_Weapon/Visual/WeaponsHolder/WeaponRecoil/VisualRoot/barrel")]
    [SerializeField] CinemachineImpulseSource cinemachineImpulseSource;
    [Tooltip("Mettre le GameObject 'Camera_Recoil' : Player_Prefab/Camera_Holder/Camera_Yaw/Camera_Pitch/Camera_Tilt/Camera_Recoil")]
    [SerializeField] ProceduralRecoil cameraRecoil;
    [Tooltip("Mettre le GameObject 'WeaponRecoil' : Weapon_Prefab/Shoot_Weapon/Visual/WeaponsHolder/WeaponRecoil")]
    [SerializeField] ProceduralRecoil weaponRecoil;

    [Space(10)]
    [Header("Weapon Heat FX")]
    [Space(10)]
    [SerializeField] float powerAddedPerShot = 0.25f; // Quantité ajoutée par tir
    [SerializeField] float powerCooldownRate = 3.0f;  // Vitesse de descente (plus c'est haut, plus ça descend vite)
    [SerializeField] float minPower = 0.05f;          // Valeur de repos
    [SerializeField] float maxPower = 1.0f;
    [SerializeField] string powerPropertyName = "_Power";
    [SerializeField] GameObject weaponPrefab;

    [Space(10)]
    [Header("StatContainer")]
    [Space(10)]
    [Tooltip("Mettre le GameObject 'Statistics' : Player_Prefab/Player/Statistics")]
    [SerializeField] PlayerStats stats;

    [Space(10)]
    [Header("Hit Marker")]
    [Space(10)]
    [Tooltip("Mettre le GameObject 'Reticle' : Player_Prefab/Player_UI/Reticle")]
    [SerializeField] private DynamicHitmarker hitmarkerScript;

    [Space(10)]
    [Header("Paint Splash Impact VFX")]
    [Space(10)]
    [Tooltip("Mettre le Prefab 'PaintSplashBurst': Assets\\_Project\\Prefabs\\VFX\\PaintSplashBurst.prefab")]
    [SerializeField] private GameObject impactSplashVFXPrefab;


    [Space(10)]
    [Header("Animator 3rd person things")]
    [Space(10)]
    [SerializeField] Animator animator_3rdPerson;
    [SerializeField] bool isOtherPlayer;
    [SerializeField] ProceduralRecoil hipsRecoil;
    [SerializeField] float hipsForce;

    Coroutine shootDelayCo;

    // --- ACCESSEURS POUR LE SCRIPT DE DEBUG ---
    public Transform ShootOrigin => shootOrigin;
    public float Range => range;
    public bool UseSpread => useSpread;
    public bool Debug_InfiniteAmmo { get; set; } = false;
    public float FireRate => fireRate;
    public float CurrentAmmo => currentAmmo;
    public int MaxAmmo => maxAmmo;
    public float CurrentBloom => currentBloom;
    public float BloomMax => bloomMax;
    public bool SpreadWithBloom => spreadWithBloom;
    public bool SpreadWithDistance => spreadWithDistance;
    public float NextShootTime => nextShootTime;
    public Camera WeaponCamera => weaponCamera;
    public Vector3 GetCorrectedShootOriginPublic() => GetCorrectedShootOrigin();    // --- VARIABLES PRIVEES ---
    // Input Action 
    InputAction aimAction;
    InputAction shootAction;
    // Spread
    float currentBloom;
    float lastBloomShotTime = -999f;
    // Reference a d'autre script lier au weapon
    Weapon_Config weaponConfig;
    WeaponController_Online controller;
    private WeaponAnimation weaponAnim;
    private Quaternion prevCameraRotation;
    private Vector3 cameraAngularVelocity;
    bool maxAmmoSoundPlayed = true;
    // Shoot / ammo 
    float currentAmmo;
    float lastShootTime;
    public float nextShootTime;
    public System.Action<float, int> OnAmmoChanged;
    // Test pour new decal 
    //public Material brushMaterial;
    //public RenderTexture paintRT;
    // permet de faire l'effect visuel de l'arme 
    private SkinnedMeshRenderer weaponRenderer;
    Material instancedWeaponMaterial;
    float currentPower;
    int powerPropertyID;
    string emissivePropertyName = "_Emissive";
    int emissivePropertyID;

    private int shooterID;
    private float oldSensibility;

    private int totalShootCounter = 0;
    private int hitShootCounter = 0;
    private int surfacePainted = 0;
    #endregion



    public void Init()
    {
        controller = GetComponentInParent<WeaponController_Online>();


        // permet d'enlever la souris 
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // permet de récup le chargeur de l'arme pour le faire tournée par la suite 
        powerPropertyID = Shader.PropertyToID(powerPropertyName);
        emissivePropertyID = Shader.PropertyToID(emissivePropertyName);
        currentPower = minPower;

        SkinnedMeshRenderer[] allRenderers = weaponPrefab.GetComponentsInChildren<SkinnedMeshRenderer>(true);
        foreach (SkinnedMeshRenderer smr in allRenderers)
        {
            if (smr.CompareTag("Weapon"))
            {
                weaponRenderer = smr;
                break;
            }
        }

        // Met la bonne couleur au chargeur de l'arme 
        if (weaponRenderer != null)
        {

            for (global::System.Int32 i = 0; i < weaponRenderer.materials.Length; i++)
            {
                if (weaponRenderer.materials[i].name.Contains("Tank"))
                {
                    instancedWeaponMaterial = weaponRenderer.materials[i];
                }
            }

            if (instancedWeaponMaterial != null)
            {
                instancedWeaponMaterial.SetFloat(powerPropertyID, currentPower);
                instancedWeaponMaterial.SetColor(emissivePropertyID, colorTeam);
            }

        }
        else
        {
            //Debug.LogError("Impossible de trouver un SkinnedMeshRenderer avec le tag 'Weapon' dans les enfants de weaponPrefab.");
        }

        // met la bonne couleur au splash de peinture au bout du canon 
        if (slashVFXPrefab != null)
        {
            slashVFXPrefab.SetVector4(vfxColorParameter, colorTeam);
        }

        // Input 
        aimAction = InputSystem.actions.FindAction("Aim");
        shootAction = InputSystem.actions.FindAction("Attack");

        // Permet de mettre tout les paramètres de l'armes 
        ApplyConfig();

        if (weaponPrefab != null)
        {
            weaponAnim = weaponPrefab.GetComponent<WeaponAnimation>();
            if (weaponAnim == null) weaponAnim = weaponPrefab.GetComponentInChildren<WeaponAnimation>();
        }

        this.shooterID = (this.player != null && this.player.TryGetComponent<Player_Health>(out Player_Health ph)) ? ph.DamageableID : this.shooterID = -1;
    }


    void Update()
    {
        // Recharge l'arme 
        HandleAmmoRecharge();

        if (instancedWeaponMaterial != null)
        {
            currentPower = Mathf.Lerp(currentPower, minPower, Time.deltaTime * powerCooldownRate);
            instancedWeaponMaterial.SetFloat(powerPropertyID, currentPower);
        }

        // Bloom , Aim et Animation existants
        if (spreadWithBloom && currentBloom > 0f)
        {
            currentBloom = Mathf.MoveTowards(currentBloom, 0f, bloomRecovery * Time.deltaTime);
        }

        if (aimAction != null && aimAction.WasPressedThisFrame())
        {
            AimWeapon(true);
            if(localCamRig)
            {
                oldSensibility = localCamRig.sensitivity;
                localCamRig.sensitivity = localCamRig.sensitivity * aimSensibilityMultiplier;
            }
            weaponAnim?.SetAiming(true);
        }

        if (aimAction != null && aimAction.WasReleasedThisFrame())
        {
            AimWeapon(false);
            if(localCamRig) localCamRig.sensitivity = oldSensibility;
            weaponAnim?.SetAiming(false);
        }

        if (mainCamera != null)
        {
            Quaternion deltaRot = mainCamera.transform.rotation * Quaternion.Inverse(prevCameraRotation);
            deltaRot.ToAngleAxis(out float angle, out Vector3 axis);

            cameraAngularVelocity = axis * (angle * Mathf.Deg2Rad) / Time.deltaTime;
            prevCameraRotation = mainCamera.transform.rotation;
        }
    }

    /// <summary>
    /// Permet de recharger les munitions de l'armes en fonction du temps 
    /// et du nombre de balle actuelle et maximum
    /// </summary>
    public void HandleAmmoRecharge()
    {
        if (currentAmmo >= maxAmmo)
        {
            if (!maxAmmoSoundPlayed)
            {
                if (AudioManager.instance != null)
                {
                    AudioManager.instance.PlaySound("MaxAmmunition", gameObject, true);
                }
                maxAmmoSoundPlayed = true;
            }
            return;
        }

        if (Time.time - lastShootTime < reloadDelay) return;

        maxAmmoSoundPlayed = false;

        currentAmmo += reloadRate * Time.deltaTime;
        currentAmmo = Mathf.Min(currentAmmo, maxAmmo);
        NotifyAmmoChange();
    }

    public bool CanShoot()
    {
        Logs.WriteLog(Logs.IMPORTANCE_LEVEL.LOW, "WEAPON",
            "CanShoot() ? (Time.time >= nextShootTime)={0}, Debug_InfiniteAmmo={1}, (currentAmmo > 0)={2}",
            Time.time >= nextShootTime, Debug_InfiniteAmmo, currentAmmo > 0
        );

        return Time.time >= nextShootTime
            && (Debug_InfiniteAmmo || currentAmmo > 0);
    }

    public bool LocalClientShoot(bool _doesDamages, out Vector3 _origin, out Vector3 _direction, out int _hitID, bool playFeedback = true, bool ignoreCooldown = false)
    {
        bool hasHit = false;
        _hitID = -1;
        _origin = Vector3.zero;
        _direction = Vector3.zero;

        if (currentAmmo <= 0 && !Debug_InfiniteAmmo)
        {
            if (Time.time >= nextShootTime)
            {
                AudioManager.instance?.PlaySound("EmptyWeapon", gameObject);
                nextShootTime = Time.time + fireRate;
            }
            return false;
        }

        if (ignoreCooldown || CanShoot())
        {
            lastShootTime = Time.time;
            nextShootTime = Time.time + fireRate;


            hasHit = true;

            // update du bloom en fonction de combien de fois on tire 
            UpdateBloomOnShot();

            _origin = shootOrigin.position;
            _direction = GetSpreadDirection(GetBaseShootDirection(), range);

            Vector3 visualOrigin = GetCorrectedShootOrigin();
            SpawnTrail(_origin, _direction, colorTeam);

            if (playFeedback)
            {
                PlayAttackFeedback();
            }

            Ray visualRay = new Ray(_origin, _direction);
            totalShootCounter++;
            if (Physics.Raycast(visualRay, out RaycastHit hit, range, hitMask, QueryTriggerInteraction.Ignore))
            {
                if (!hit.collider.CompareTag("BulletsTrail"))
                {
                    if (!hit.collider.CompareTag("Player") && !hit.collider.CompareTag("Weapon"))
                    {
                        SpawnImpact(hit, _direction, colorTeam);
                    }

                    ApplyDamage(hit, _doesDamages, out int _id, out float _);
                    _hitID = _id;

                    GetComponentInParent<Ranged_Weapon_Debug>()?.RegisterImpact(hit.point, hit.normal);
                    GetComponentInParent<Ranged_Weapon_Debug>()?.RegisterRay(_origin, _direction, true, hit.point);
                }
            }
            else
            {
                GetComponentInParent<Ranged_Weapon_Debug>()?.RegisterRay(_origin, _direction, false);
            }

            if (!Debug_InfiniteAmmo)
            {
                currentAmmo--;
                NotifyAmmoChange();
            }
            Player_Perf_Tracker.UpdatePerf(hitShootCounter * 100 / totalShootCounter, Player_Perf_Tracker.Perf.ACC);
        }

        return hasHit;
    }

    public void ServerPerformShoot(Vector3 _clientOrigin, Vector3 _clientDir, out Vector3 _hitPos, out int _hitID, out float _damages)
    {
        _hitPos = Vector3.zero;
        _hitID = -1;
        _damages = -1.0f;

        if (CanShoot())
        {
            lastShootTime = Time.time;
            nextShootTime = Time.time + fireRate;

            if (!Debug_InfiniteAmmo)
            {
                currentAmmo--;
            }

            Logs.WriteLog(Logs.IMPORTANCE_LEVEL.LOW, "WEAPON", "Shooting a ray from {0} in direction {1}.", _clientOrigin, _clientDir);
            if (Physics.Raycast(_clientOrigin, _clientDir, out RaycastHit hit, range, hitMask, QueryTriggerInteraction.Ignore))
            {
                if (!hit.collider.CompareTag("BulletsTrail"))
                {
                    ApplyDamage(hit, true, out _hitID, out _damages);
                    _hitPos = hit.point;
                }
            }
        }
    }

    /// <summary>
    /// Appeler par le serveur pour les autre joueurs
    /// </summary>
    /// <param name="origin"></param>
    /// <param name="direction"></param>
    public void RemoteClientShoot(Vector3 origin, Vector3 direction, Color _colour)
    {
        if (!isOtherPlayer) return;

        SpawnTrail(origin, direction, _colour);

        PlayAttackFeedbackRemote(_colour);

        Ray visualRay = new Ray(origin, direction);
        if (Physics.Raycast(visualRay, out RaycastHit hit, range, hitMask, QueryTriggerInteraction.Ignore))
        {
            if (!hit.collider.CompareTag("BulletsTrail"))
            {
                SpawnImpact(hit, direction, _colour);
            }
        }
    }

    /// <summary>
    /// Feedback visuel du tir pour les clients distants (sans recoil ni son local).
    /// Joue uniquement le VFX muzzle flash avec la couleur de l'équipe distante.
    /// </summary>
    void PlayAttackFeedbackRemote(Color _colour)
    {
        if (slashVFXPrefab != null)
        {
            slashVFXPrefab.SetVector4(vfxColorParameter, _colour);
            slashVFXPrefab.Play();
        }

        if (instancedWeaponMaterial != null)
        {
            currentPower += powerAddedPerShot;
            currentPower = Mathf.Min(currentPower, maxPower);
            instancedWeaponMaterial.SetFloat(powerPropertyID, currentPower);
        }

        if (isOtherPlayer)
        {
            hipsRecoil.ApplyRecoil(hipsForce);
            animator_3rdPerson.SetBool("isFiring", true);
            if (shootDelayCo != null)
            {
                StopCoroutine(shootDelayCo);
            }
            shootDelayCo = StartCoroutine(ShootDelayAnim(weaponConfig.stopShootDelay));
        }
    }

    /// <summary>
    /// Permet de tirer avec gestion du spread , des fonction de débug ,
    /// diminutions des balles placement corect de l'instanciation de la balle
    /// Lancement des feedBack , application des damage , spawn des impacts .
    /// </summary>
    public void Shoot()
    {
        if (!CanShoot()) return;

        // Gestion munitions
        if (!Debug_InfiniteAmmo)
        {
            currentAmmo--;
            lastShootTime = Time.time;
            NotifyAmmoChange();
        }
        // gestion du fire rate 
        nextShootTime = Time.time + fireRate;

        // Replacement du point de sortie de la balle au niveau du barel 
        Ray cameraRay = mainCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f));
        Vector3 targetPoint;

        if (Physics.Raycast(cameraRay, out RaycastHit camHit, range, hitMask, QueryTriggerInteraction.Ignore))
        {
            targetPoint = camHit.point;
        }
        else
        {
            targetPoint = cameraRay.origin + cameraRay.direction * range;
        }
        Vector3 visualOrigin = GetCorrectedShootOrigin();
        Vector3 visualDir = (targetPoint - visualOrigin).normalized;

        Vector3 forward = (targetPoint - shootOrigin.position).normalized;

        UpdateBloomOnShot();  // application du 1er spread 

        float distanceToTarget = Vector3.Distance(shootOrigin.position, targetPoint);

        // permet de faire en sorte que le spread soit en cerle et réduisse en fonction du aim 
        Vector3 shootDir = GetSpreadDirection(forward, distanceToTarget);
        if (aimAction.IsPressed())
        {
            shootDir = Vector3.Lerp(forward, shootDir, weaponConfig.spreadMultADS);
        }
        visualDir = GetSpreadDirection(visualDir, Vector3.Distance(visualOrigin, targetPoint));

        SpawnTrail(visualOrigin, shootDir, colorTeam);
        //PlayAttackFeedback();

        Ray shootRay = new Ray(shootOrigin.position, shootDir);
        if (Physics.Raycast(shootRay, out RaycastHit hit, range, hitMask, QueryTriggerInteraction.Ignore))
        {
            if (hit.collider.CompareTag("BulletsTrail")) return;
            GetComponent<Ranged_Weapon_Debug>()?.RegisterImpact(hit.point, hit.normal);
            ApplyDamage(hit, false, out int _, out float _);
            SpawnImpact(hit, shootDir, colorTeam);
        }
    }

    void SpawnTrail(Vector3 origin, Vector3 spreadDirection, Color _colour)
    {
        if (!trailPrefab) return;

        Vector3 visualOrigin = origin;
        Vector3 visualDirection = spreadDirection;

        GameObject trail = Instantiate(trailPrefab, visualOrigin, Quaternion.LookRotation(visualDirection));
        //trail.transform.SetParent(shootOriginTrail);

        TrailRenderer tr = trail.GetComponent<TrailRenderer>();
        if (tr != null)
        {
            tr.Clear();

            tr.material = new Material(tr.sharedMaterial);
            tr.material.SetColor("_Color", _colour);

            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new GradientColorKey[] {
                    new GradientColorKey(_colour, 0f),
                    new GradientColorKey(_colour, 1f)
                },
                new GradientAlphaKey[] {
                    new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(0f, 1f)
                }
            );
            tr.colorGradient = gradient;
        }

        BulletTrail bt = trail.GetComponent<BulletTrail>();
        if (bt != null)
            bt.Init(visualOrigin, visualDirection, trailSpeed, trailLifeTime);
    }

    /// <summary>
    ///  Permet quand on tire plusieur balle d'augmenter le spread de l'arme 
    /// </summary>
    void UpdateBloomOnShot()
    {
        if (!spreadWithBloom) return;

        float timeSinceLastShot = Time.time - lastBloomShotTime;
        float resetDelay = Mathf.Max(chainResetDelay, fireRate * 1.1f);

        if (timeSinceLastShot > resetDelay)
        {
            currentBloom = 0f;
        }
        else
        {
            currentBloom = Mathf.Min(currentBloom + bloomPerShot, bloomMax);
        }

        lastBloomShotTime = Time.time;
    }


    /// <summary>
    /// Cette fonction permet de calculer l'impression en fonction de la distance 
    /// </summary>
    float GetDistanceExtraSpread(float distance)
    {
        if (!spreadWithDistance) return 0f;

        float t = Mathf.InverseLerp(distanceStart, distanceEnd, distance);
        float curve = distanceSpreadCurve.Evaluate(t);
        return maxExtraSpreadByDistance * curve;
    }
    /// <summary>
    /// Calcule l'ange du spread 
    /// </summary>
    /// <returns></returns>
    public float GetCurrentTotalAngle() 
    {
        float baseAngle = spreadAngle + GetDistanceExtraSpread(range) + (spreadWithBloom ? currentBloom : 0f);
        if (useAimPrecision && aimAction.IsPressed())
        {
            baseAngle *= aimSpreadMultiplier;
        }
        float finalAngle = Mathf.Max(0f, baseAngle);


        return finalAngle;
    }

    Vector3 GetSpreadDirection(Vector3 forward, float distance)
    {
        if (!useSpread)
        {
            Debug.Log("No spread");
            return forward;
        }
        float totalAngle = GetCurrentTotalAngle();

        if (totalAngle <= 0f)
        {
            Debug.Log("Angle too low");
            return forward;
        }

        Vector2 r = Random.insideUnitCircle * totalAngle;
        Quaternion spreadRotation = Quaternion.Euler(r.x, r.y, 0f);
        return spreadRotation * forward;
    }

    /// <summary>
    /// Calcule la direction de base du tir depuis le centre de l'écran.
    /// Lance un raycast depuis la caméra principale pour trouver le point visé,
    /// puis retourne la direction normalisée entre le canon et ce point.
    /// </summary>
    public Vector3 GetBaseShootDirection()
    {
        Ray cameraRay;
        if (mainCamera != null)
        {
            cameraRay = mainCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        }
        else
        {
            cameraRay = new Ray(transform.position, transform.forward);
        }
        Vector3 targetPoint;
        RaycastHit camHit;
        if (Physics.Raycast(cameraRay, out camHit, range, hitMask, QueryTriggerInteraction.Ignore))
        {
            targetPoint = camHit.point;
        }
        else
        {
            targetPoint = cameraRay.origin + cameraRay.direction * range;
        }
        return (targetPoint - GetCorrectedShootOrigin()).normalized;
    }

    public bool ApplyDamage(RaycastHit hit, bool _doDamages, out int _hitID, out float _damages)
    {
        float multiplacateurRarety = 1f + (0.3f * ((float)controller.GetRaretyItemSelected()));


        float damageReduction = 0f;
        // cap the damage reduction to half of the damage amount
        float maxReduction = damage * damageReducerCap;
        float newDamages = damage;
        float dist = Vector3.Distance(transform.position, hit.point);

        if (dist > damageReducerMinDistance)
        { // take back the minimum distance out of the 
            float extraDistance = dist - damageReducerMinDistance;
            // compute the reducted damages
            damageReduction = extraDistance * damageReducerPerMeter;
            // cap the reduction of damages applyable to the enemy
            damageReduction = Mathf.Min(damageReduction, maxReduction);

            // apply these damages
            newDamages = damage - damageReduction;
        }


        float finalDamage = newDamages * multiplacateurRarety /**  stats.stats[StatType.DAMAGE].GetValue()*/;
        bool hasHitTarget = false;
        Color hitColor = Color.white;
        Vector3 damagesDir = (transform.position - hit.point).normalized;

        _hitID = -1;
        _damages = finalDamage;
        if (this.shooterID == -1 || this.shooterID == 0) this.shooterID = (this.player != null && this.player.TryGetComponent<Player_Health>(out Player_Health ph)) ? ph.DamageableID : -1;

        if (hit.collider.TryGetComponent<Hitbox>(out var hitbox))
        {
            _hitID = hitbox.GetDamageableID();
            if (_doDamages) hitbox.OnHit(finalDamage, damagesDir, this.shooterID, true, out _damages);
            hasHitTarget = true;

            if (hitbox.damageMultiplier > 1.5f) hitColor = Color.red;
        }
        else if (hit.collider.TryGetComponent<IDamageable>(out var damageable))
        {
            _hitID = damageable.DamageableID;
            if (_doDamages) damageable.TakeDamage(finalDamage, damagesDir, this.shooterID, true, false);
            hasHitTarget = true;
        }
        else if (hit.collider.TryGetComponent<DamageTransmitter>(out DamageTransmitter dt))
        {
            _hitID = dt.GetDamageableID();
            if (_doDamages) dt.TakeDamage(finalDamage, damagesDir, this.shooterID, true);
            hasHitTarget = true;
        }

        if (hasHitTarget && hitmarkerScript != null)
        {
            hitShootCounter++;
            hitmarkerScript.PlayHit(hitColor);
        }

        Logs.WriteLog((_hitID != -1) ? Logs.IMPORTANCE_LEVEL.HIGH : Logs.IMPORTANCE_LEVEL.LOW, "WEAPON", "Trying to deal {0} damages to id {1}.", _damages, _hitID);

        return hasHitTarget;
    }

    public void SpawnImpact(RaycastHit hit, Vector3 shootDir, Color _color)
    {
        float impactAngle = Vector3.Angle(-shootDir, hit.normal);
        if (impactAngle > 80f) return;

        Quaternion baseRotation = Quaternion.LookRotation(-hit.normal);

        if (IsWallFloorCorner(hit, hitMask))
        {
            float cornerAngle = Random.Range(40f, 45f);
            baseRotation *= Quaternion.AngleAxis(cornerAngle, Vector3.right);
        }

        GameObject decal = Instantiate(impactDecalPrefab, hit.point, baseRotation);

        if (hit.collider.TryGetComponent<IDamageable>(out _))
            decal.transform.SetParent(hit.transform, true);

        decal.GetComponent<ImpactDecalRandomizer>().playerOwner = player;

        if (decal.TryGetComponent<DecalProjector>(out var projector))
        {
            decal.transform.position = hit.point + hit.normal * 0.02f;

            float scale = Random.Range(1f, 3f);
            surfacePainted += (int)(scale * scale);
            projector.size = new Vector3(scale, scale, projector.size.z);

            if (projector.material != null && projector.material.HasProperty("_BaseColor"))
                projector.material.SetColor("_BaseColor", _color);
        }

        Player_Perf_Tracker.UpdatePerf(surfacePainted, Player_Perf_Tracker.Perf.AREA);
        //SpawnSplashShoot(hit, shootDir, _color);
    }

    /// <summary>
    /// Vérifie la présence d'une surface proche via un Raycast dans une direction donnée.
    /// </summary>
    bool IsNearSurface(Vector3 point, Vector3 direction, float distance, LayerMask mask)
    {
        // Petit offset dans la direction pour éviter l'auto-intersection au point de départ
        Vector3 origin = point + direction * 0.02f;
        return Physics.Raycast(origin, direction, distance, mask);
    }

    /// <summary>
    /// Détermine si une surface est un mur (normale quasi-horizontale).
    /// </summary>
    bool IsWall(Vector3 normal) => Vector3.Dot(normal, Vector3.up) < 0.2f;

    /// <summary>
    /// Détermine si une surface est un sol (normale quasi-verticale vers le haut).
    /// </summary>
    bool IsFloor(Vector3 normal) => Vector3.Dot(normal, Vector3.up) > 0.8f;

    /// <summary>
    /// Détecte si le point d'impact se situe dans l'angle entre un mur et un sol.
    /// </summary>
    bool IsWallFloorCorner(RaycastHit hit, LayerMask mask)
    {
        Vector3 normal = hit.normal.normalized;
        Vector3 p = hit.point;

        bool hitWall = IsWall(normal);
        bool hitFloor = IsFloor(normal);

        if (!hitWall && !hitFloor) return false;

        bool nearFloor = IsNearSurface(p, Vector3.down, 0.25f, mask);
        bool nearWall = IsNearSurface(p, Vector3.forward, 0.25f, mask)
                      || IsNearSurface(p, Vector3.back, 0.25f, mask)
                      || IsNearSurface(p, Vector3.left, 0.25f, mask)
                      || IsNearSurface(p, Vector3.right, 0.25f, mask);

        return (hitWall && nearFloor) || (hitFloor && nearWall);
    }

    void SpawnSplashShoot(RaycastHit hit, Vector3 shootDir, Color _color)
    {
        if (impactSplashVFXPrefab == null) return;

        // Le splash joue sur les joueurs ET sur les surfaces
        bool isPlayerHit = hit.collider.TryGetComponent<IDamageable>(out IDamageable _);

        Quaternion splashRotation = Quaternion.LookRotation(hit.normal, -shootDir);
        Vector3 euler = splashRotation.eulerAngles;
        euler.z = 0f;
        splashRotation = Quaternion.Euler(euler);

        GameObject splashObj = Instantiate(impactSplashVFXPrefab, hit.point, splashRotation);
        VisualEffect splashVFX = splashObj.GetComponentInChildren<VisualEffect>();

        if (splashVFX != null)
        {
            splashVFX.SetVector4("TeamColor", _color);

            float distanceToFloor = 0f;

            if (!isPlayerHit)
            {
                Vector3 rayOrigin = hit.point + (hit.normal * 0.2f) + (Vector3.up * 0.1f);
                if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit floorHit, 20f, hitMask))
                {
                    distanceToFloor = hit.point.y - floorHit.point.y;
                }
                else
                {
                    Debug.LogWarning("<color=red>[VFX Debug]</color> Aucun sol détecté sous l'impact ! Vérifie ton HitMask.");
                }
            }

            if (splashVFX.HasVector3("BoxPosition"))
            {
                splashVFX.SetVector3("BoxPosition", new Vector3(0, -distanceToFloor, 0));
            }
            else
            {
                Debug.LogError("<color=red>[VFX Debug]</color> Le paramètre 'BoxPosition' n'existe pas dans le VFX Graph ! Vérifie l'orthographe.");
            }

            splashVFX.Play();
        }

        Destroy(splashObj, 2f);
    }
    public void PlayAttackFeedbackLocal()
    {


    }

    public void PlayAttackFeedback()
    {
        // Splash weapon shoot
        if (slashVFXPrefab != null)
        {
            slashVFXPrefab.SetVector4(vfxColorParameter, colorTeam);
            slashVFXPrefab.Play();
        }

        // Chargeur power 
        if (instancedWeaponMaterial != null)
        {
            currentPower += powerAddedPerShot;
            currentPower = Mathf.Min(currentPower, maxPower);
            instancedWeaponMaterial.SetFloat(powerPropertyID, currentPower);
        }

        if (!isOtherPlayer)
        {
            ST_Local_Camera_Rig localCam = ST_Local_Camera_Rig.Instance;
            if (localCam != null)
            {
                localCam.AddRecoil(new Vector2(0f, -0.05f));
            }

            if (cameraRecoil != null) cameraRecoil.ApplyRecoil(1f);
            if (weaponRecoil != null) weaponRecoil.ApplyRecoil(1f);

            if (weaponAnim != null)
            {
                weaponAnim.SetShooting();
            }

            // Audio local
            if (AudioManager.instance != null && weaponConfig.fireMode != WeaponController_Online.FireMode.Burst)
            {
                AudioManager.instance.PlaySound(shootSoundName, gameObject, true);
                AudioManager.instance.SetRandomPitch(shootSoundName, gameObject, 0.8f, 1.2f);
            }
        }
        else
        {
            // Animation et recoil hanches pour le joueur distant (vue 3eme personne)
            hipsRecoil.ApplyRecoil(hipsForce);
            animator_3rdPerson.SetBool("isFiring", true);
            if (shootDelayCo != null)
            {
                StopCoroutine(shootDelayCo);
            }
            shootDelayCo = StartCoroutine(ShootDelayAnim(weaponConfig.stopShootDelay));
        }
    }

    IEnumerator ShootDelayAnim(float _delay)
    {
        yield return new WaitForSeconds(_delay);
        animator_3rdPerson.SetBool("isFiring", false);
        shootDelayCo = null;
    }

    public void AimWeapon(bool _aiming)
    {
        ST_Local_Camera_Rig localCam = ST_Local_Camera_Rig.Instance;
        if (localCam != null)
        {
            if (_aiming)
            {
                localCam.SetFOV(40f);

                if (isOtherPlayer)
                {
                    animator_3rdPerson.SetBool("isAiming", true);
                }
            }
            else
            {
                localCam.ResetFOV();
                if (isOtherPlayer)
                {
                    animator_3rdPerson.SetBool("isAiming", false);
                }
            }
        }
    }

    /// <summary>
    /// Corrige la position du barrel en tenant compte du FOV différent
    /// entre la weapon camera et Camera.main.
    /// 
    /// Principe :
    /// 1. On trouve où le barrel apparaît à l'écran VU PAR la weapon camera
    /// 2. On reconstruit ce point dans le monde VU PAR Camera.main
    /// Résultat : le trail part exactement là où le joueur voit le canon.
    /// </summary>
    Vector3 GetCorrectedShootOrigin()
    {
        if (shootOrigin == null)
            return transform.position;

        if (weaponCamera == null || mainCamera == null)
            return shootOrigin.position;

        Vector3 screenPos = weaponCamera.WorldToScreenPoint(shootOrigin.position);

        if (screenPos.z <= 0f)
            return shootOrigin.position;

        float depthFromMain = Vector3.Distance(mainCamera.transform.position, shootOrigin.position);

        Vector3 corrected = mainCamera.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, depthFromMain));

        //Debug.Log($"[CorrectedOrigin] raw: {shootOrigin.position} | corrected: {corrected} | offset: {(corrected - shootOrigin.position).magnitude:F4}m");
        //Debug.Log($"[CorrectedOrigin] weaponCam FOV: {weaponCamera.fieldOfView} | mainCam FOV: {mainCamera.fieldOfView}");

        return corrected;
    }

    /// <summary>
    /// Notifie du changement de quantité de ammo pour ui player 
    /// </summary>
    void NotifyAmmoChange()
    {
        OnAmmoChanged?.Invoke(currentAmmo, maxAmmo);

        if (controller != null)
        {
            controller.onAmmoChanged?.Invoke(currentAmmo, (int)maxAmmo);
        }
    }

    void ApplyConfig()
    {
        controller.fireMode = weaponConfig.fireMode;
        controller.burstCount = weaponConfig.burstCount;
        controller.burstInterval = weaponConfig.burstInterval;
        fireRate = weaponConfig.fireRate;
        range = weaponConfig.range;
        damage = weaponConfig.damage;
        useSpread = weaponConfig.useSpread;
        spreadAngle = weaponConfig.spreadAngle;
        spreadWithDistance = weaponConfig.spreadWithDistance;
        distanceEnd = weaponConfig.distanceEnd;
        distanceStart = weaponConfig.distanceStart;
        maxExtraSpreadByDistance = weaponConfig.maxExtraSpreadByDistance;
        distanceSpreadCurve = weaponConfig.distanceSpreadCurve;
        spreadWithBloom = weaponConfig.spreadWithBloom;
        bloomPerShot = weaponConfig.bloomPerShot;
        bloomMax = weaponConfig.bloomMax;
        bloomRecovery = weaponConfig.bloomRecovery;
        chainResetDelay = weaponConfig.chainResetDelay;
        maxAmmo = weaponConfig.maxAmmo;
        reloadRate = weaponConfig.reloadRate;
        reloadDelay = weaponConfig.reloadDelay;
        hitMask = weaponConfig.hitMask;
        shootSoundName = weaponConfig.soundNameShoot;
        damageReducerMinDistance = weaponConfig.damageReducerMinDistance;
        damageReducerPerMeter = weaponConfig.damageReducerPerMeter;
        damageReducerCap = weaponConfig.damageReducerCap;

        currentAmmo = maxAmmo;
        NotifyAmmoChange();
    }

    public void SetWeaponConfig(Weapon_Config _weapon_Config)
    {
        weaponConfig = _weapon_Config;
    }

    public void SetAndApplyWeaponConfig(Weapon_Config _weapon_Config)
    {
        weaponConfig = _weapon_Config;
        ApplyConfig();

        float multiplacateurRarety = 1f + (0.5f * ((float)controller.GetRaretyItemSelected()));
        Debug.Log("[Ranged weapon online ]  multiplacateur rarety : " + multiplacateurRarety);
        reloadRate = reloadRate * multiplacateurRarety;
        Debug.Log("[Ranged weapon online ]  reaload rate after update : " + reloadRate);

        multiplacateurRarety = 1f - (0.2f * ((float)controller.GetRaretyItemSelected()));
        spreadAngle = spreadAngle * multiplacateurRarety;


        Weapon_Visual_Manager visualManager = weaponPrefab.GetComponentInChildren<Weapon_Visual_Manager>();
        GameObject activeModel = visualManager != null ? visualManager.GetActiveModel() : null;

        Transform searchRoot = activeModel != null ? activeModel.transform : weaponPrefab.transform;

        weaponRenderer = null;
        SkinnedMeshRenderer[] allRenderers = searchRoot.GetComponentsInChildren<SkinnedMeshRenderer>(true);
        foreach (SkinnedMeshRenderer smr in allRenderers)
        {
            if (smr.CompareTag("Weapon"))
            {
                weaponRenderer = smr;
                break;
            }
        }

        if (weaponRenderer != null)
        {
            for (global::System.Int32 i = 0; i < weaponRenderer.materials.Length; i++)
            {
                if (weaponRenderer.materials[i].name.Contains("Tank"))
                {
                    instancedWeaponMaterial = weaponRenderer.materials[i];
                }
            }
            if (instancedWeaponMaterial != null)
            {
                instancedWeaponMaterial.SetColor(emissivePropertyID, colorTeam);
            }
        }
        else
        {
            Debug.LogError($"[WeaponColor] Aucun SkinnedMeshRenderer Tag 'Weapon' trouvé dans : {searchRoot.name}");
        }
    }

    public void SetShootOrigin(Transform newMuzzle)
    {
        if (newMuzzle == null || shootOrigin == null) return;

        shootOrigin.SetParent(newMuzzle);

        shootOrigin.localPosition = Vector3.zero;
        shootOrigin.localRotation = Quaternion.identity;
    }
    protected override void OnColorUpdated(Color newColor)
    {
        if (instancedWeaponMaterial != null)
            instancedWeaponMaterial.SetColor(emissivePropertyID, newColor);

        if (slashVFXPrefab != null)
            slashVFXPrefab.SetVector4(vfxColorParameter, newColor);

    }

}