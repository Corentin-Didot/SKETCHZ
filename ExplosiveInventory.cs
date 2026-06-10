using System.Collections.Generic;
using UnityEngine;

public class ExplosiveInventory : MonoBehaviour
{
    public static ExplosiveInventory Instance { get; private set; }

    private ThrowableType _selectedType = ThrowableType.Bomb;
    public ThrowableType SelectedType => _selectedType;

    public event System.Action<ThrowableType, int> OnStockChanged;
    public event System.Action<ThrowableType> OnSelectionChanged;

    [SerializeField] private int MAX_STOCK = 3;

    private Dictionary<ThrowableType, int> _stock = new();
    private Dictionary<ThrowableType, Weapon_Config> _configs = new();


    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }
    public bool Add(Weapon_Config config, int amount = 1)
    {
        var type = config.throwableType;
        _stock.TryGetValue(type, out int current);

        if (current >= MAX_STOCK)
        {
            Debug.LogWarning($"[ExplosiveInventory] Stock max atteint pour {type}");
            return false;
        }

        _configs[type] = config;
        _stock[type] = Mathf.Min(current + amount, MAX_STOCK);
        OnStockChanged?.Invoke(type, _stock[type]);

        if (!HasAny(_selectedType))
            ForceSelectAvailable();

        return true;
    }

    public bool TryConsume(ThrowableType type)
    {
        if (!_stock.TryGetValue(type, out int current) || current <= 0) return false;
        _stock[type] = current - 1;

        Debug.Log($"[ExplosiveInventory] TryConsume {type} | {current} → {_stock[type]}\n{System.Environment.StackTrace}");

        OnStockChanged?.Invoke(type, _stock[type]);

        if (!HasAny(_selectedType))
            ForceSelectAvailable();

        return true;
    }
    public int GetQuantity(ThrowableType type)
    {
        _stock.TryGetValue(type, out int q);
        return q;
    }

    public bool HasAny(ThrowableType type) => GetQuantity(type) > 0;

    /// <summary> Retourne le config stocké pour ce type, null si aucun.</summary>
    public Weapon_Config GetConfig(ThrowableType type)
    {
        _configs.TryGetValue(type, out Weapon_Config cfg);
        return cfg;
    }

    /// <summary> Retourne le premier type dont le stock > 0, null sinon.</summary>
    public Weapon_Config GetFirstAvailableConfig()
    {
        foreach (var kvp in _stock)
            if (kvp.Value > 0 && _configs.TryGetValue(kvp.Key, out Weapon_Config cfg))
                return cfg;
        return null;
    }

    /// <summary>Switch entre Bomb et Mine si le stock le permet.</summary>
    public void SwitchType()
    {
        if (!CanSwitch())
        {
            Debug.LogWarning("[ExplosiveInventory] Switch impossible, un seul type en stock.");
            return;
        }

        _selectedType = _selectedType == ThrowableType.Bomb
            ? ThrowableType.Mine
            : ThrowableType.Bomb;

        OnSelectionChanged?.Invoke(_selectedType);
    }

    /// <summary>Retourne le config du type actuellement sélectionné.</summary>
    public Weapon_Config GetSelectedConfig()
    {
        return HasAny(_selectedType) ? GetConfig(_selectedType) : null;
    }

    public bool CanSwitch()
    {
        return HasAny(ThrowableType.Bomb) && HasAny(ThrowableType.Mine);
    }

    /// <summary>Sélectionne automatiquement le premier type disponible.</summary>
    private void ForceSelectAvailable()
    {
        foreach (var kvp in _stock)
        {
            if (kvp.Value > 0)
            {
                _selectedType = kvp.Key;
                OnSelectionChanged?.Invoke(_selectedType);
                return;
            }
        }
    }

    public int GetMaxStock()
    {
        return MAX_STOCK;
    }
}