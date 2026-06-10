using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


public class Weapon_Visual_Manager : MonoBehaviour
{
    [System.Serializable]
    public struct WeaponModelMapping
    {
        public string weaponID;
        public GameObject modelObject;
        public Weapon_Config config; 
    }

    [Header("Configuration des modèles")]
    [SerializeField] private List<WeaponModelMapping> weaponModels = new List<WeaponModelMapping>();

    [Header("Paramètres au démarrage")]
    [SerializeField] private string defaultWeaponID; 
    [SerializeField] private bool disableAllOnStart = true;

    [Header("Animation")]
    [SerializeField] private Animator armsAnimator;

    private WeaponController_Online controller;

    private void Start()
    {
        if (disableAllOnStart)
        {
            DisableAllVisuals();
        }

        controller = GetComponentInParent<WeaponController_Online>();

        defaultWeaponID = controller.weaponConfig.weaponID;


        if (!string.IsNullOrEmpty(defaultWeaponID))
        {
            SwitchWeaponVisual(defaultWeaponID);
        }

    }

    public void SwitchWeaponVisual(string id, RuntimeAnimatorController newController = null)
    {
        if (string.IsNullOrEmpty(id)) { DisableAllVisuals(); return; }

        foreach (var mapping in weaponModels)
        {
            bool isTarget = (mapping.weaponID == id);

            if (mapping.modelObject != null)
                mapping.modelObject.SetActive(isTarget);

            if (isTarget)
            {
                if (armsAnimator != null && mapping.config.animatorController != null)
                {
                    armsAnimator.runtimeAnimatorController = mapping.config.animatorController;
                }

                //if (controller != null && mapping.config != null)
                //{
                //    controller.ChangeWeaponConfig(mapping.config);
                //}

                Debug.Log($"[Switch] Arme activée : {id}");
            }
        }

        Weapon_Visual visual = GetComponentInParent<Weapon_Visual>();
        if (visual != null) visual.RefreshVisualsAfterSwitch();
    }

    public GameObject GetActiveModel()
    {
        return weaponModels.FirstOrDefault(m => m.modelObject != null && m.modelObject.activeSelf).modelObject;
    }

    /// <summary>
    /// Recherche un transform par nom de manière récursive dans les enfants
    /// </summary>
    public Transform FindDeepChild(Transform parent, string name)
    {
        foreach (Transform child in parent.GetComponentsInChildren<Transform>(true))
        {
            if (child.name == name) return child;
        }
        return null;
    }


    /// <summary>
    /// Désactive absolument tous les modèles enregistrés dans la liste.
    /// </summary>
    public void DisableAllVisuals()
    {
        foreach (var mapping in weaponModels)
        {
            if (mapping.modelObject != null)
            {
                mapping.modelObject.SetActive(false);
            }
        }
    }
}