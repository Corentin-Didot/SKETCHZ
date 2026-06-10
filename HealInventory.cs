using System.Collections.Generic;
using UnityEngine;

public class HealInventory : MonoBehaviour
{
    public static HealInventory Instance { get; private set; }

    public event System.Action<ItemRarity, int> OnStockChanged;
    public event System.Action<ItemRarity> OnSelectionChanged;

    [SerializeField] private int maxStockPerRarity = 3;

    private Dictionary<ItemRarity, int> _stocks = new();
    private Dictionary<ItemRarity, int> _healAmounts = new();

    public ItemRarity SelectedRarity { get; private set; } = ItemRarity.Common;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    // ── Ajout ────────────────────────────────────────────────────────────
    public bool Add(Weapon_Config config, ItemRarity rarity)
    {
        int current = GetStock(rarity);
        if (current >= maxStockPerRarity)
        {
            Debug.LogWarning($"[HealInventory] Stock max atteint pour {rarity}");
            return false;
        }

        int multiplier = 1 + (int)rarity;
        _healAmounts[rarity] = config.healAmount * multiplier;
        _stocks[rarity] = Mathf.Min(current + config.healCharges, maxStockPerRarity);

        if (GetStock(SelectedRarity) <= 0 || rarity > SelectedRarity)
        {
            SelectedRarity = rarity;
            OnSelectionChanged?.Invoke(SelectedRarity);
        }

        OnStockChanged?.Invoke(rarity, _stocks[rarity]);
        return true;
    }
    // ── Utilisation ───────────────────────────────────────────────────────
    public bool TryUse()
    {
        if (GetStock(SelectedRarity) <= 0) return false;

        _stocks[SelectedRarity]--;
        OnStockChanged?.Invoke(SelectedRarity, _stocks[SelectedRarity]);

        if (_stocks[SelectedRarity] <= 0)
        {
            foreach (var kv in _stocks)
            {
                if (kv.Value > 0)
                {
                    SelectedRarity = kv.Key;
                    OnSelectionChanged?.Invoke(SelectedRarity);
                    break;
                }
            }
        }

        return true;
    }

    // ── Switch de sélection ───────────────────────────────────────────────
    public void SwitchRarity()
    {
        var available = new List<ItemRarity>();
        foreach (var kv in _stocks)
            if (kv.Value > 0) available.Add(kv.Key);

        if (available.Count <= 1) return;

        int idx = available.IndexOf(SelectedRarity);
        SelectedRarity = available[(idx + 1) % available.Count];
        OnSelectionChanged?.Invoke(SelectedRarity);
    }

    // ── Accesseurs ────────────────────────────────────────────────────────
    public int GetStock(ItemRarity rarity) => _stocks.TryGetValue(rarity, out int v) ? v : 0;
    public int GetHealAmount(ItemRarity rarity) => _healAmounts.TryGetValue(rarity, out int v) ? v : 0;
    public int CurrentHealAmount => GetHealAmount(SelectedRarity);
    public bool HasAny() => GetStock(SelectedRarity) > 0;
    public bool CanSwitch() => _stocks.Count > 1;
}