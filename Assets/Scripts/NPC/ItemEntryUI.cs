using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ItemEntryUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private RawImage itemModelDisplay;
    [SerializeField] private TextMeshProUGUI itemNameText;
    [SerializeField] private TextMeshProUGUI valueText;
    [SerializeField] private Button sell1Button;
    [SerializeField] private Button sell5Button;
    [SerializeField] private Button sell10Button;
    [SerializeField] private TextMeshProUGUI countText;

    private ItemDefinition definition;
    private int currentCount;

    public void Setup(ItemDefinition def, int count)
    {
        definition = def;
        currentCount = count;
        countText.text = $"x{count}";

        itemNameText.text = def.itemName;
        valueText.text = $"{def.goldValue}g";

        UpdateButtons();

        sell1Button.onClick.AddListener(() => TrySell(1));
        sell5Button.onClick.AddListener(() => TrySell(5));
        sell10Button.onClick.AddListener(() => TrySell(10));
    }

    private void TrySell(int amount)
    {
        if (PlayerInventory.Instance == null) return;

        int actualAmount = Mathf.Min(amount, currentCount);
        if (actualAmount <= 0) return;

        bool removed = PlayerInventory.Instance.RemoveItem(definition, actualAmount);
        if (!removed) return;

        int goldEarned = actualAmount * definition.goldValue;
        PlayerInventory.Instance.AddGold(goldEarned);

        currentCount -= actualAmount;
        WitchShopUI.Instance?.UpdateGoldDisplay();

        countText.text = $"x{currentCount}";
        if (currentCount <= 0)
        {
            Destroy(gameObject);
            WitchShopUI.Instance?.ShowEmptyMessage();
            return;
        }

        UpdateButtons();
    }

    private void UpdateButtons()
    {
        sell1Button.interactable = currentCount >= 1;
        sell5Button.interactable = currentCount >= 5;
        sell10Button.interactable = currentCount >= 10;
    }

    private void OnDestroy()
    {
        sell1Button.onClick.RemoveAllListeners();
        sell5Button.onClick.RemoveAllListeners();
        sell10Button.onClick.RemoveAllListeners();
    }
}