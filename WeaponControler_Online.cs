using Online.Client;
using Online.Shared.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class WeaponController_Online : MonoBehaviour
{
    public enum FireMode
    {
        SemiAuto,
        Burst,
        Automatic
    }

    #region Variable

    [SerializeField] private Bomb_Launch bombLauncher;        // Script pour les bombes
    private Ranged_Weapon_Online logicWeapon; // Script pour le tir normal
    private Weapon_Visual visualWeapon;      // Script pour le visuel (bras/skins)
    [SerializeField] private Melee_Attack_Online meleeLogic;

    [SerializeField] MonoBehaviour weaponBehaviour;
    [SerializeField] bool shootWeapon = false;
    [SerializeField] bool doesDamages = true;
    [SerializeField] HotbarComponent hotbarComponent;
    public FireMode fireMode = FireMode.SemiAuto;

    [Header("Burst")]
    public int burstCount = 3;
    public float burstInterval = 0.08f;

    [Header("Weapon Config")]
    [SerializeField] public Weapon_Config weaponConfig;

    [Header("HUD Manager")]
    [SerializeField] private WeaponHUD_Manager hudManager;

    private InputAction quickGrenadeAction;
    private ScriptableObject previousWeaponConfig;
    private bool isQuickThrowing = false;


    public System.Action<Vector3, Vector3> OnShootRequest;
    public bool IsLocalPlayer;

    public System.Action<float, int> onAmmoChanged;
    /// ------ PRIVATE -----    
    bool isBursting;
    private Color colorTeam;

    private WeaponType currentWeaponType;

    IWeapon weapon;

    InputAction meleeInput;
    InputAction attackAction;
    InputAction changeFireModeAction;
    InputAction changeWeapon;
    InputAction switchExplosiveAction;
    InputAction switchHealAction;

    // Networking shi
    private Validator<NetworkClient> net;

    //WeaponSwitch
    public event Action<Weapon_Config> OnWeaponChanged;
    #endregion

    void Awake()
    {
        visualWeapon = gameObject.GetComponentInChildren<Weapon_Visual>();
        logicWeapon = gameObject.GetComponentInChildren<Ranged_Weapon_Online>();
        meleeLogic = GetComponentInChildren<Melee_Attack_Online>();
        bombLauncher = gameObject.GetComponentInChildren<Bomb_Launch>();

        weapon = weaponBehaviour as IWeapon;
        if (weapon == null)
        {
            Debug.LogError("WeaponBehaviour n’implémente pas IWeapon");
        }

        if (visualWeapon != null)
        {
            visualWeapon.SetWeaponConfig(weaponConfig);
            visualWeapon.Init();
        }

        if (logicWeapon != null)
        {
            logicWeapon.SetWeaponConfig(weaponConfig);
            logicWeapon.Init();
        }

        if (bombLauncher != null)
            bombLauncher.Init();

        if (meleeLogic != null) meleeLogic.Init();

        if (TeamColorManager.instance != null)
        {
            colorTeam = TeamColorManager.instance.playerColor;
        }

        if (weaponConfig != null)
        {
            currentWeaponType = weaponConfig.weaponType;
            fireMode = weaponConfig.fireMode;
            burstCount = weaponConfig.burstCount;
            burstInterval = weaponConfig.burstInterval;
        }

        transform.localPosition = Vector3.zero;
        // input 
        attackAction = InputSystem.actions.FindAction("Attack");
        changeFireModeAction = InputSystem.actions.FindAction("ChangeFireMode");
        changeWeapon = InputSystem.actions.FindAction("ChangeWeapon");
        meleeInput = InputSystem.actions.FindAction("CaC_Attack");
        quickGrenadeAction = InputSystem.actions.FindAction("Bomb_Lanch");
        switchExplosiveAction = InputSystem.actions.FindAction("SwitchExplosive");
        switchHealAction = InputSystem.actions.FindAction("SwitchHeal");
    }

    private void Start()
    {
        this.net = new Validator<NetworkClient>();
        if (NetworkClient.GetInstance() != null)
        {
            this.net.Set(NetworkClient.GetInstance(), true);
        }
        if (weaponConfig != null)
        {
            ChangeWeaponConfig(weaponConfig);
        }

        if (hotbarComponent != null)
        {
            hotbarComponent.Hotbar.OnChanged += OnHotbarChanged;
            hotbarComponent.Hotbar.OnItemAdded += OnHotbarItemAdded;
            hotbarComponent.Hotbar.OnSetup += OnHotbarChanged;
        }

        Invoke(nameof(OnHotbarChanged), 0.1f);
    }

    private void OnDestroy()
    {
        if (hotbarComponent != null)
        {
            hotbarComponent.Hotbar.OnChanged -= OnHotbarChanged;
            hotbarComponent.Hotbar.OnItemAdded -= OnHotbarItemAdded;
            hotbarComponent.Hotbar.OnSetup -= OnHotbarChanged;
        }
    }
    void Update()
    {
        ChangeWeapon();
        HandleQuickGrenade();
        HandleSwitchExplosive();
        HandleSwitchHeal();

        if (meleeInput.WasPressedThisFrame())
            TryMelee();


    }

    void LateUpdate()
    {
        if (!shootWeapon) return;
        if (!IsLocalPlayer) return;

        if (currentWeaponType == WeaponType.Melee)
        {
            if (attackAction.WasPressedThisFrame())
                TryMelee();
        }
        else if (currentWeaponType == WeaponType.Ranged)
        {
            switch (fireMode)
            {
                case FireMode.SemiAuto:
                    if (attackAction.WasPressedThisFrame()) TryLocalShoot();
                    break;

                case FireMode.Automatic:
                    if (attackAction.IsPressed()) TryLocalShoot();
                    break;

                case FireMode.Burst:
                    if (attackAction.WasPressedThisFrame() && !isBursting)
                    {
                        bool enoughAmmo = logicWeapon.Debug_InfiniteAmmo
                                       || logicWeapon.CurrentAmmo >= burstCount;
                        if (enoughAmmo)
                            StartCoroutine(LocalBurstRoutine());
                    }
                    break;

            }
        }
    }
    private void HandleSwitchHeal()
    {
        if (switchHealAction == null) return;
        if (!switchHealAction.WasPressedThisFrame()) return;

        if (HealInventory.Instance != null && HealInventory.Instance.CanSwitch())
            HealInventory.Instance.SwitchRarity();
    }

    private void HandleSwitchExplosive()
    {
        if (switchExplosiveAction == null) return;
        if (!switchExplosiveAction.WasPressedThisFrame()) return;

        if (ExplosiveInventory.Instance != null && ExplosiveInventory.Instance.CanSwitch())
            ExplosiveInventory.Instance.SwitchType();
    }

    private void HandleQuickGrenade()
    {
        if (quickGrenadeAction == null || bombLauncher == null) return;

        if (quickGrenadeAction.WasPressedThisFrame() && !isQuickThrowing)
        {
            if (!HasGrenadeInInventory()) return;

            Weapon_Config config = ExplosiveInventory.Instance.GetSelectedConfig();
            bombLauncher.SetAndApplyConfig(config);
            bombLauncher.RespawnProjectile();

            isQuickThrowing = true;
            bombLauncher.gameObject.SetActive(true);
            visualWeapon.gameObject.SetActive(false);
            bombLauncher.PrepareQuickThrow();
            AudioManager.instance?.PlaySound("ActivationExplosion", gameObject, true);
        }


        if (quickGrenadeAction.IsPressed() && isQuickThrowing)
        {
            bombLauncher.HoldAim();
        }
        if (quickGrenadeAction.WasReleasedThisFrame() && isQuickThrowing)
        {

            bombLauncher.Shoot();
            AudioManager.instance?.PlaySound("Throw_Explosive", gameObject, true);

            bombLauncher.gameObject.SetActive(false);
            visualWeapon.gameObject.SetActive(true);
            isQuickThrowing = false;
        }
    }

    public bool HasGrenadeInInventory()
    {
        if (ExplosiveInventory.Instance == null) return false;
        return ExplosiveInventory.Instance.GetSelectedConfig() != null;
    }

    private void TryLocalShoot()
    {
        if (logicWeapon != null)
        {
            if (logicWeapon.LocalClientShoot(this.doesDamages, out Vector3 origin, out Vector3 dir, out int _hitID))
            {
                if (this.net.TryGet(out NetworkClient net))
                {
                    net.SendShoot(origin, dir, _hitID);
                }
            }
        }
    }

    IEnumerator LocalBurstRoutine()
    {
        isBursting = true;

        int availableShots = logicWeapon.Debug_InfiniteAmmo
            ? burstCount
            : Mathf.Min(burstCount, Mathf.FloorToInt(logicWeapon.CurrentAmmo));

        if (availableShots <= 0)
        {
            AudioManager.instance?.PlaySound("EmptyWeapon", gameObject);
            isBursting = false;
            yield break;
        }

        AudioManager.instance?.PlaySound("Shoot_Shotgun", gameObject, true);

        for (int i = 0; i < availableShots; i++)
        {
            bool isFirstShot = (i == 0);

            bool shotFired = logicWeapon.LocalClientShoot(
                doesDamages,
                out Vector3 origin, out Vector3 dir, out int hitID,
                playFeedback: isFirstShot,
                ignoreCooldown: i > 0
            );

            if (shotFired && this.net.TryGet(out NetworkClient net))
                net.SendShoot(origin, dir, hitID);

            if (i < availableShots - 1 && burstInterval > 0f)
                yield return new WaitForSeconds(burstInterval);
        }

        if (weaponConfig != null && weaponConfig.burstCooldown > 0f)
            yield return new WaitForSeconds(weaponConfig.burstCooldown);

        isBursting = false;
    }
    void ChangeWeapon()
    {
        if (isQuickThrowing) return;
        float scroll = changeWeapon.ReadValue<Vector2>().y;
        if (scroll > 0)
        {
            if (hotbarComponent.Hotbar.IncrementSelectedSlot(1))
            {
                // get weapon
                Debug.Log("Next Weapon");
                Debug.Log("[Weapon controler online : Rarety in inventory ]" + hotbarComponent.Hotbar.GetSelectedItem().Data.Rarity);
                AudioManager.instance?.PlaySound("SwitchWeapon", gameObject, true);
                ChangeWeaponConfig(hotbarComponent.Hotbar.GetSelectedItem().Data.Script);
            }
        }
        if (scroll < 0)
        {
            if (hotbarComponent.Hotbar.IncrementSelectedSlot(-1))
            {
                // get weapon
                Debug.Log("Previous Weapon");
                Debug.Log("[Weapon controler online : Rarety in inventory ]" + hotbarComponent.Hotbar.GetSelectedItem().Data.Rarity);
                AudioManager.instance?.PlaySound("SwitchWeapon", gameObject, true);
                ChangeWeaponConfig(hotbarComponent.Hotbar.GetSelectedItem().Data.Script);
            }
        }
    }

    public void ChangeWeaponConfig(ScriptableObject scriptable)
    {
        if (scriptable is Weapon_Config newConfig)
        {
            this.weaponConfig = newConfig;
            currentWeaponType = newConfig.weaponType;
            fireMode = newConfig.fireMode;
            burstCount = newConfig.burstCount;
            burstInterval = newConfig.burstInterval;

            Weapon_Visual_Manager visualManager = GetComponentInChildren<Weapon_Visual_Manager>();
            if (visualManager != null)
                visualManager.SwitchWeaponVisual(newConfig.weaponID, newConfig.animatorController);

            if (visualWeapon != null)
            {
                visualWeapon.UpdateConfig(newConfig);
                visualWeapon.RefreshVisualsAfterSwitch();
            }

            if (logicWeapon != null)
                logicWeapon.SetAndApplyWeaponConfig(newConfig);

            OnWeaponChanged?.Invoke(weaponConfig);
            UpdateHUD();
        }
    }

    public void UpdateHUD()
    {
        if (hudManager == null || hotbarComponent == null) return;
        var hotbar = hotbarComponent.Hotbar;
        if (hotbar == null) return;

        List<Weapon_Config> inventoryList = new List<Weapon_Config>();
        int selectedIndex = hotbar.GetSelectedSlotIndex();

        for (int i = 0; i < hotbar.Size; i++)
        {
            if (i == selectedIndex) continue;

            var slot = hotbar.GetSlot(i);
            if (slot != null && !slot.IsEmpty && slot.Item?.Data != null)
            {
                if (slot.Item.Data.Script is Weapon_Config cfg)
                    inventoryList.Add(cfg);
            }
        }

        hudManager.UpdateDisplay(this.weaponConfig, inventoryList, WorldItem_UI.GetRarityColor(GetRaretyItemSelected()));
    }

    public void UpdateShootOrigin(Transform newMuzzle)
    {
        if (logicWeapon != null)
        {
            logicWeapon.SetShootOrigin(newMuzzle);
        }
    }

    private void TryMelee()
    {
        if (meleeLogic == null) return;

        if (meleeLogic.CanShoot())
        {
            Weapon_Visual_Manager visualManager = GetComponentInChildren<Weapon_Visual_Manager>();

            if (visualManager != null)
            {
                GameObject activeModel = visualManager.GetActiveModel();
                if (activeModel != null)
                {
                    Animator currentAnim = activeModel.GetComponentInParent<Animator>();

                    if (currentAnim != null)
                    {
                        currentAnim.SetTrigger("Cut");
                    }
                }
            }

            meleeLogic.Shoot();
        }
    }
    public ItemRarity GetRaretyItemSelected()
    {
        return hotbarComponent.Hotbar.GetSelectedItem()?.Data.Rarity ?? ItemRarity.Common;
    }

    private void OnHotbarItemAdded(Item item)
    {
        UpdateHUD();
    }

    private void OnHotbarChanged()
    {
        var selectedItem = hotbarComponent?.Hotbar?.GetSelectedItem();

        if (selectedItem?.Data?.Script is Weapon_Config newConfig && newConfig != this.weaponConfig)
        {
            ChangeWeaponConfig(newConfig);
        }
        else
        {
            UpdateHUD();
        }
    }

}