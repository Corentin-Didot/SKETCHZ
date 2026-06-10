using UnityEngine;

public class Weapon_Visual : TeamColorObserver
{
    #region  Variable
    [Header("Configurations")]
    [SerializeField] private Transform shootOrigin;

    [Header("Points d'Attache")]
    [SerializeField] private Transform handSocket;

    [Header("Recoil Parent")]
    [SerializeField] private Transform recoilGameObject;

    [Header("Arms Color FX")]
    [SerializeField] private string swapColorPropertyName = "_Swap_Color";

    [Header("Properties")]
    [SerializeField] private bool shouldSpawnArms;

    [Header("Arm Forward Offset (Rifle / Knife)")]
    [SerializeField] private float armsForwardOffset = 0.2f;

    [Tooltip("Enfant du prefab bras contenant UNIQUEMENT le mesh visuel.\n" +
             "Seul cet objet est avance - les muzzle points restent fixes.\n" +
             "Laisser vide = deplace tout visualArmsInstance (ancien comportement).")]
    [SerializeField] private Transform armsOffsetPivot;

    //----- Private Variable -----
    private int swapColorPropertyID;

    private GameObject visualArmsInstance;
    private GameObject visualWeaponInstance;
    private Weapon_Config weaponConfig;
    private Material armsMaterial;
    WeaponController_Online controller;
    #endregion
    public void Init()
    {
        swapColorPropertyID = Shader.PropertyToID(swapColorPropertyName);
        controller = GetComponentInParent<WeaponController_Online>();

        if (weaponConfig == null)
        {
            Debug.LogError("WeaponConfig manquant !");
            return;
        }

        if (weaponConfig.armsPrefab == null)
        {
            Debug.LogError("armsprefab manquant !");
            return;
        }

        SetVisualWeapon();

        if (weaponConfig == null)
        {
            Debug.LogError("weapon config vraiment manquant");
            return;
        }
    }

    /// <summary>
    /// Met le barel au bonne endroit par rapport au point pr�sent sur le prefab de l'arme
    /// </summary>
    private void BindMuzzle()
    {
        Transform muzzlePoint = null;

        if (visualArmsInstance != null)
        {
            foreach (Transform child in visualArmsInstance.GetComponentsInChildren<Transform>())
            {
                if (child.name == "MuzzlePoint" || child.CompareTag("Muzzle"))
                {
                    muzzlePoint = child;
                    break;
                }
            }
        }

        if (muzzlePoint != null && shootOrigin != null)
        {
            shootOrigin.SetParent(muzzlePoint);
            shootOrigin.localPosition = Vector3.zero;
            shootOrigin.localRotation = Quaternion.identity;

            if (controller != null) controller.UpdateShootOrigin(shootOrigin);

        }
        else
        {
            Debug.LogWarning($"MuzzlePoint introuvable. VisualArmsInstance est null ? {visualArmsInstance == null}");
        }
    }
    public void SetWeaponConfig(Weapon_Config _weapon_Config)
    {
        weaponConfig = _weapon_Config;
    }
    public void SetAndApplyWeaponConfig(Weapon_Config _weapon_Config)
    {
        foreach (Transform child in handSocket)
        {
            Destroy(child.gameObject);
        }

        weaponConfig = _weapon_Config;

        SetVisualWeapon();

        OnColorUpdated(colorTeam);
    }

    /// <summary>
    /// Permet d'instancier le weapon et de changer la couleur 
    /// </summary>
    private void SetVisualWeapon()
    {
        if (this.shouldSpawnArms)
        {
            if (visualArmsInstance != null)
            {
                visualArmsInstance = null;
            }

            visualArmsInstance = Instantiate(weaponConfig.armsPrefab, handSocket);
            visualArmsInstance.transform.localPosition = Vector3.zero;
            visualArmsInstance.transform.localRotation = Quaternion.identity;
            visualArmsInstance.transform.localScale = Vector3.one;

            ApplyWeaponOffset();
            SetupVisuals();
        }
    }

    /// <summary>
    /// Avance les bras sur l'axe local Z pour les armes qui le nécessitent (Rifle, Knife).
    /// La valeur d'offset est réglable via l'Inspector (armsForwardOffset).
    /// </summary>
    private void ApplyWeaponOffset()
    {
        if (visualArmsInstance == null || weaponConfig == null) return;

        bool needsForwardOffset = weaponConfig.weaponID == "Rifle" ||
                                  weaponConfig.weaponID == "Knife";

        // Si un pivot dedie est assigne, on deplace UNIQUEMENT le mesh visuel.
        // Les muzzle points restent enfants de visualArmsInstance a leur position d'origine.
        Transform target = armsOffsetPivot != null
            ? armsOffsetPivot
            : visualArmsInstance.transform;

        target.localPosition = needsForwardOffset
            ? new Vector3(0f, 0f, armsForwardOffset)
            : Vector3.zero;
    }

    private void SetupVisuals()
    {
        // Setup du Material
        Renderer[] armsRenderers = visualArmsInstance.GetComponentsInChildren<Renderer>(true);
        foreach (Renderer r in armsRenderers)
        {
            if (r.name == "LP_FPS_Player")
            {
                armsMaterial = r.material;
                armsMaterial.SetColor(swapColorPropertyID, colorTeam);
                break;
            }
        }

        foreach (Transform child in visualArmsInstance.GetComponentsInChildren<Transform>())
        {
            if (child.CompareTag("Weapon"))
            {
                visualWeaponInstance = child.gameObject;
                break;
            }
        }

        BindMuzzle();
    }

    public void RefreshVisualsAfterSwitch()
    {
        Weapon_Visual_Manager manager = GetComponentInChildren<Weapon_Visual_Manager>();
        if (manager == null) return;

        GameObject activeModel = manager.GetActiveModel();
        if (activeModel == null) return;

        string targetMuzzleName = "muzzlePoint_" + weaponConfig.weaponID;
        Transform foundMuzzle = null;

        foreach (Transform child in activeModel.GetComponentsInChildren<Transform>(true))
        {
            if (child.name.Equals(targetMuzzleName, System.StringComparison.OrdinalIgnoreCase) || child.CompareTag("Muzzle"))
            {
                foundMuzzle = child;
                break;
            }
        }

        if (foundMuzzle != null && shootOrigin != null)
        {
            shootOrigin.SetParent(foundMuzzle);
            shootOrigin.localPosition = Vector3.zero;
            shootOrigin.localRotation = Quaternion.identity;

            if (controller != null) controller.UpdateShootOrigin(foundMuzzle);

            Debug.Log($"<color=green>[SUCCESS]</color> Muzzle lié au nouveau modèle : {foundMuzzle.name}");
        }
    }

    public void UpdateConfig(Weapon_Config newConfig)
    {
        this.weaponConfig = newConfig;
        ApplyWeaponOffset();
    }

    /// <summary>
    /// permet de detruire un child clone 
    /// </summary>
    /// <param name="prefabName"></param>
    void RemoveChildByName(string prefabName)
    {
        Transform child = transform.Find(prefabName + "(Clone)");

        if (child != null)
        {
            Destroy(child.gameObject);
        }
    }

    protected override void OnColorUpdated(Color newColor)
    {
        if (armsMaterial != null)
        {
            if (swapColorPropertyID == 0)
            {
                swapColorPropertyID = Shader.PropertyToID(swapColorPropertyName);
            }

            armsMaterial.SetColor(swapColorPropertyID, newColor);
            Debug.Log("Les bras ont chang� de couleur !");
        }
    }
}