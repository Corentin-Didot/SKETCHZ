using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class HealUser : MonoBehaviour
{
    [SerializeField] private Player_Health playerHealth;

    private InputAction healAction;

    private static readonly Dictionary<ItemRarity, string> _healSounds = new()
{
    { ItemRarity.Common,    "Health_Common"    },
    { ItemRarity.Rare,      "Health_Rare"      },
};

    void Awake()
    {
        healAction = InputSystem.actions.FindAction("UseHeal");

        if (playerHealth == null)
            playerHealth = GetComponentInParent<Player_Health>();
    }

    void Update()
    {
        if (healAction.WasPressedThisFrame())
            TryHeal();
    }

    private void TryHeal()
    {
        if (HealInventory.Instance == null || playerHealth == null) return;
        if (playerHealth.IsDead()) return;

        if (playerHealth.GetHealth() >= playerHealth.GetMaximumHealth())
        {
            Debug.LogWarning("[HealUser] Vie déjà au maximum !");
            return;
        }

        ItemRarity rarity = HealInventory.Instance.SelectedRarity; // snapshot avant TryUse
        if (!HealInventory.Instance.TryUse()) return;

        string sound = _healSounds.TryGetValue(rarity, out string s) ? s : "Health_Common";
        playerHealth.Heal(HealInventory.Instance.GetHealAmount(rarity), sound);
    }
}