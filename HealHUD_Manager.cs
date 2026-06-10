using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class HealHUD_Manager : MonoBehaviour
{
    [System.Serializable]
    public struct HealSpriteEntry
    {
        public Image image;
        public ItemRarity rarity;
    }

    [SerializeField] HealSpriteEntry[] healSprites; 
    [SerializeField] TextMeshProUGUI quantityText;

    void Start()
    {
        RefreshHUD();

        if (HealInventory.Instance != null)
        {
            HealInventory.Instance.OnStockChanged += (_, __) => RefreshHUD();
            HealInventory.Instance.OnSelectionChanged += _ => RefreshHUD();
        }
    }

    void OnDestroy()
    {
        if (HealInventory.Instance != null)
        {
            HealInventory.Instance.OnStockChanged -= (_, __) => RefreshHUD();
            HealInventory.Instance.OnSelectionChanged -= _ => RefreshHUD();
        }
    }

    private void RefreshHUD()
    {
        if (HealInventory.Instance == null)
        {
            foreach (var entry in healSprites)
                entry.image.gameObject.SetActive(false);
            quantityText.gameObject.SetActive(false);
            return;
        }

        ItemRarity selected = HealInventory.Instance.SelectedRarity;
        int stock = HealInventory.Instance.GetStock(selected);
        bool hasStock = stock > 0;

        foreach (var entry in healSprites)
        {
            bool show = HealInventory.Instance.GetStock(entry.rarity) > 0
                     && entry.rarity == selected;
            entry.image.gameObject.SetActive(show);
        }

        quantityText.gameObject.SetActive(hasStock);
        if (hasStock)
            quantityText.text = stock.ToString();
    }
}