using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class EngineerShopUI : ShopUIBase
{
    [Header("Panels")]
    [SerializeField] private GameObject engineerShopPanel;
    [SerializeField] private GameObject turretsTabContent;
    [SerializeField] private GameObject mattsTabContent;

    [Header("Dialogue")]
    [SerializeField] private TextMeshProUGUI dialogue1Text;
    [SerializeField] private TextMeshProUGUI dialogue2Text;
    [SerializeField] private string defaultDialogue1 = "Need some firepower?";
    [SerializeField] private string defaultDialogue2 = "Let's see what blueprints you've found.";

    [Header("Shop")]
    [SerializeField] private TextMeshProUGUI goldText;

    [Header("Base Navigator")]
    [SerializeField] private Button basePrevButton;
    [SerializeField] private Button baseNextButton;
    [SerializeField] private TextMeshProUGUI baseIndexText;
    [SerializeField] private RawImage baseModelDisplay;
    [SerializeField] private TextMeshProUGUI baseNameText;
    [SerializeField] private TextMeshProUGUI baseStatsText;
    [SerializeField] private TextMeshProUGUI baseCostText;
    [SerializeField] private GameObject baseActionRow;
    [SerializeField] private Button baseBuyButton;
    [SerializeField] private Button sellBaseButton;
    [SerializeField] private GameObject baseProgressPanel;
    [SerializeField] private Slider baseProgressSlider;
    [SerializeField] private TextMeshProUGUI baseProgressText;

    [Header("Addon Navigator")]
    [SerializeField] private Button prevButton;
    [SerializeField] private Button nextButton;
    [SerializeField] private TextMeshProUGUI blueprintIndexText;
    [SerializeField] private RectTransform modelDisplayArea;
    [SerializeField] private RawImage modelDisplay;
    [SerializeField] private GameObject actionRow;
    [SerializeField] private Button buyButton;
    [SerializeField] private Button sellButton;
    [SerializeField] private TextMeshProUGUI blueprintNameText;
    [SerializeField] private TextMeshProUGUI statsText;
    [SerializeField] private TextMeshProUGUI costText;
    [SerializeField] private GameObject progressPanel;
    [SerializeField] private Slider progressSlider;
    [SerializeField] private TextMeshProUGUI progressText;

    [Header("Blueprints")]
    [SerializeField] private List<TurretDefinition> availableBases;
    [SerializeField] private List<AddonDefinition> availableAddons;

    [Header("Animation")]
    [SerializeField] private float slideSpeed = 5f;

    [Header("Tabs")]
    [SerializeField] private Button turretsTabButton;
    [SerializeField] private Button mattsTabButton;

    [Header("Sub-Tabs")]
    [SerializeField] private GameObject basesSection;
    [SerializeField] private GameObject addonsSection;
    [SerializeField] private Button basesSubTabButton;
    [SerializeField] private Button addonsSubTabButton;

    private int currentBaseIndex = 0;
    private int currentIndex = 0;
    private Coroutine slideCoroutine;
    private float modelDisplayRestX;

    private void Awake()
    {
        turretsTabButton.onClick.AddListener(ShowTurretsTab);
        mattsTabButton.onClick.AddListener(ShowMattsTab);

        if (basePrevButton != null) basePrevButton.onClick.AddListener(PrevBase);
        if (baseNextButton != null) baseNextButton.onClick.AddListener(NextBase);
        if (baseBuyButton != null) baseBuyButton.onClick.AddListener(TryBuyBase);
        if (sellBaseButton != null) sellBaseButton.onClick.AddListener(TrySellBase);

        if (basesSubTabButton != null) basesSubTabButton.onClick.AddListener(ShowBasesSection);
        if (addonsSubTabButton != null) addonsSubTabButton.onClick.AddListener(ShowAddonsSection);

        prevButton.onClick.AddListener(PrevBlueprint);
        nextButton.onClick.AddListener(NextBlueprint);
        buyButton.onClick.AddListener(TryBuy);
        sellButton.onClick.AddListener(TrySell);
        modelDisplayRestX = modelDisplayArea.anchoredPosition.x;
        Hide();
    }

    public void ShowTurretsTab()
    {
        turretsTabContent.SetActive(true);
        mattsTabContent.SetActive(false);
        ShowBasesSection();
    }

    public void ShowBasesSection()
    {
        if (basesSection != null) basesSection.SetActive(true);
        if (addonsSection != null) addonsSection.SetActive(false);
        PopulateBases();
    }

    public void ShowAddonsSection()
    {
        if (basesSection != null) basesSection.SetActive(false);
        if (addonsSection != null) addonsSection.SetActive(true);
        PopulateBlueprints();
    }

    public void ShowMattsTab()
    {
        turretsTabContent.SetActive(false);
        mattsTabContent.SetActive(true);
    }

    public override void Show()
    {
        base.Show();

        engineerShopPanel.SetActive(true);
        dialogue1Text.text = defaultDialogue1;
        dialogue2Text.text = defaultDialogue2;
        currentBaseIndex = 0;
        currentIndex = 0;
        ShowTurretsTab();
    }

    public override void Hide()
    {
        base.Hide();
        if (slideCoroutine != null)
        {
            StopCoroutine(slideCoroutine);
            slideCoroutine = null;
        }
        StopAllCoroutines();
    }

    private void PopulateBases()
    {
        UpdateGoldDisplay();

        if (availableBases == null || availableBases.Count == 0)
        {
            if (baseNameText != null) baseNameText.text = "No base blueprints";
            if (baseStatsText != null) baseStatsText.text = "";
            if (baseCostText != null) baseCostText.text = "";
            if (baseActionRow != null) baseActionRow.SetActive(false);
            if (baseProgressPanel != null) baseProgressPanel.SetActive(false);
            if (baseIndexText != null) baseIndexText.text = "0 / 0";
            if (basePrevButton != null) basePrevButton.interactable = false;
            if (baseNextButton != null) baseNextButton.interactable = false;
            return;
        }

        if (basePrevButton != null) basePrevButton.interactable = availableBases.Count > 1;
        if (baseNextButton != null) baseNextButton.interactable = availableBases.Count > 1;
        ShowCurrentBase();
    }

    private void ShowCurrentBase()
    {
        if (availableBases == null || availableBases.Count == 0) return;

        currentBaseIndex = Mathf.Clamp(currentBaseIndex, 0, availableBases.Count - 1);
        TurretDefinition def = availableBases[currentBaseIndex];
        BlueprintProgress progress = PlayerInventory.Instance.BlueprintProgress;

        Debug.Log($"[EngineerShopUI] BlueprintProgress instance: {progress != null}");
        Debug.Log($"[EngineerShopUI] Checking id: '{def.blueprintId}'");
        Debug.Log($"[EngineerShopUI] Parts found: {progress.GetPartsFound(def.blueprintId)}");
        Debug.Log($"[EngineerShopUI] ShowCurrentBase: name='{def.displayName}', blueprintId='{def.blueprintId}', unlocked={progress.IsUnlocked(def.blueprintId)}");

        bool unlocked = progress.IsUnlocked(def.blueprintId);

        if (baseIndexText != null) baseIndexText.text = $"{currentBaseIndex + 1} / {availableBases.Count}";
        if (baseNameText != null) baseNameText.text = unlocked ? def.displayName : "???";

        if (unlocked)
        {
            if (baseModelDisplay != null) baseModelDisplay.color = Color.white;
            if (baseActionRow != null) baseActionRow.SetActive(true);
            if (baseBuyButton != null) baseBuyButton.interactable = PlayerInventory.Instance.HasGold(def.buildCost);
            if (baseStatsText != null) baseStatsText.text = $"HP: {def.maxHealth}\nCylinders: {def.cylinderCount}";
            if (baseCostText != null) baseCostText.text = $"{def.buildCost}g";
            if (baseProgressPanel != null) baseProgressPanel.SetActive(false);
        }
        else
        {
            if (baseModelDisplay != null) baseModelDisplay.color = new Color(0.2f, 0.2f, 0.2f, 1f);
            if (baseActionRow != null) baseActionRow.SetActive(false);
            if (baseStatsText != null) baseStatsText.text = "";
            if (baseCostText != null) baseCostText.text = "";
            if (baseProgressPanel != null) baseProgressPanel.SetActive(true);
            int partsFound = progress.GetPartsFound(def.blueprintId);
            if (baseProgressSlider != null)
            {
                baseProgressSlider.interactable = false;
                baseProgressSlider.maxValue = 3;
                baseProgressSlider.value = partsFound;
            }
            if (baseProgressText != null) baseProgressText.text = $"{partsFound}/3 parts found";
        }

        UpdateGoldDisplay();
    }

    public void NextBase()
    {
        if (availableBases == null || availableBases.Count <= 1) return;
        currentBaseIndex = (currentBaseIndex + 1) % availableBases.Count;
        ShowCurrentBase();
    }

    public void PrevBase()
    {
        if (availableBases == null || availableBases.Count <= 1) return;
        currentBaseIndex = (currentBaseIndex - 1 + availableBases.Count) % availableBases.Count;
        ShowCurrentBase();
    }

    private void TryBuyBase()
    {
        if (PlayerInventory.Instance == null) return;
        if (availableBases == null || availableBases.Count == 0) return;

        TurretDefinition def = availableBases[currentBaseIndex];
        if (!PlayerInventory.Instance.BlueprintProgress.IsUnlocked(def.blueprintId)) return;

        if (!PlayerInventory.Instance.SpendGold(def.buildCost))
        {
            Debug.Log($"[EngineerShopUI] Not enough gold for {def.displayName}.");
            return;
        }

        PlayerInventory.Instance.AddBase(def);
        Debug.Log($"[EngineerShopUI] Purchased base {def.displayName} for {def.buildCost}g.");

        UpdateGoldDisplay();
        ShowCurrentBase();
    }

    private void TrySellBase()
    {
        if (PlayerInventory.Instance == null) return;
        if (availableBases == null || availableBases.Count == 0) return;

        TurretDefinition def = availableBases[currentBaseIndex];
        if (!PlayerInventory.Instance.BlueprintProgress.IsUnlocked(def.blueprintId)) return;
        if (!PlayerInventory.Instance.HasBase(def)) return;

        int refund = def.buildCost / 2;
        PlayerInventory.Instance.RemoveBase(def);
        PlayerInventory.Instance.AddGold(refund);
        Debug.Log($"[EngineerShopUI] Sold base {def.displayName} for {refund}g.");

        UpdateGoldDisplay();
        ShowCurrentBase();
    }

    private void PopulateBlueprints()
    {
        UpdateGoldDisplay();

        if (availableAddons == null || availableAddons.Count == 0)
        {
            blueprintNameText.text = "No blueprints available";
            statsText.text = "";
            costText.text = "";
            actionRow.SetActive(false);
            progressPanel.SetActive(false);
            blueprintIndexText.text = "0 / 0";
            prevButton.interactable = false;
            nextButton.interactable = false;
            return;
        }

        prevButton.interactable = availableAddons.Count > 1;
        nextButton.interactable = availableAddons.Count > 1;
        ShowCurrentBlueprint();
    }

    private void ShowCurrentBlueprint()
    {
        if (availableAddons == null || availableAddons.Count == 0) return;

        AddonDefinition def = availableAddons[currentIndex];
        BlueprintProgress progress = PlayerInventory.Instance.BlueprintProgress;
        bool unlocked = progress.IsUnlocked(def.blueprintId);

        blueprintIndexText.text = $"{currentIndex + 1} / {availableAddons.Count}";
        blueprintNameText.text = unlocked ? def.displayName : "???";

        if (unlocked)
        {
            modelDisplay.color = Color.white;
            actionRow.SetActive(true);
            buyButton.interactable = PlayerInventory.Instance.HasGold(def.buildCost);
            sellButton.interactable = true;
            statsText.text = $"Damage: {def.damage}\nRange: {def.range}\nFire Rate: {def.fireRate}/s";
            costText.text = $"{def.buildCost}g";
            progressPanel.SetActive(false);
        }
        else
        {
            modelDisplay.color = new Color(0.2f, 0.2f, 0.2f, 1f);
            actionRow.SetActive(false);
            statsText.text = "";
            costText.text = "";
            progressPanel.SetActive(true);
            int partsFound = progress.GetPartsFound(def.blueprintId);
            progressSlider.interactable = false;
            progressSlider.maxValue = 3;
            progressSlider.value = partsFound;
            progressText.text = $"{partsFound}/3 parts found";
        }

        UpdateGoldDisplay();
    }

    public void NextBlueprint()
    {
        if (availableAddons == null || availableAddons.Count <= 1) return;
        int newIndex = (currentIndex + 1) % availableAddons.Count;
        SlideToBlueprint(newIndex, 1);
    }

    public void PrevBlueprint()
    {
        if (availableAddons == null || availableAddons.Count <= 1) return;
        int newIndex = (currentIndex - 1 + availableAddons.Count) % availableAddons.Count;
        SlideToBlueprint(newIndex, -1);
    }

    private void SlideToBlueprint(int newIndex, int direction)
    {
        if (slideCoroutine != null)
            StopCoroutine(slideCoroutine);
        slideCoroutine = StartCoroutine(SlideCoroutine(newIndex, direction));
    }

    private IEnumerator SlideCoroutine(int newIndex, int direction)
    {
        float slideDistance = modelDisplayArea.rect.width;
        float slideOutTarget = modelDisplayRestX - direction * slideDistance;
        float slideInStart = modelDisplayRestX + direction * slideDistance;

        while (Mathf.Abs(modelDisplayArea.anchoredPosition.x - slideOutTarget) > 1f)
        {
            float newX = Mathf.Lerp(modelDisplayArea.anchoredPosition.x, slideOutTarget, Time.deltaTime * slideSpeed);
            modelDisplayArea.anchoredPosition = new Vector2(newX, modelDisplayArea.anchoredPosition.y);
            yield return null;
        }

        currentIndex = newIndex;
        ShowCurrentBlueprint();

        modelDisplayArea.anchoredPosition = new Vector2(slideInStart, modelDisplayArea.anchoredPosition.y);

        while (Mathf.Abs(modelDisplayArea.anchoredPosition.x - modelDisplayRestX) > 1f)
        {
            float newX = Mathf.Lerp(modelDisplayArea.anchoredPosition.x, modelDisplayRestX, Time.deltaTime * slideSpeed);
            modelDisplayArea.anchoredPosition = new Vector2(newX, modelDisplayArea.anchoredPosition.y);
            yield return null;
        }

        modelDisplayArea.anchoredPosition = new Vector2(modelDisplayRestX, modelDisplayArea.anchoredPosition.y);
        slideCoroutine = null;
    }

    private void TryBuy()
    {
        if (PlayerInventory.Instance == null) return;
        if (availableAddons == null || availableAddons.Count == 0) return;

        AddonDefinition def = availableAddons[currentIndex];
        if (!PlayerInventory.Instance.BlueprintProgress.IsUnlocked(def.blueprintId)) return;

        if (!PlayerInventory.Instance.SpendGold(def.buildCost))
        {
            Debug.Log($"[EngineerShopUI] Not enough gold for {def.displayName}.");
            return;
        }

        PlayerInventory.Instance.AddAddon(def);
        Debug.Log($"[EngineerShopUI] Purchased addon {def.displayName} for {def.buildCost}g.");

        UpdateGoldDisplay();
        ShowCurrentBlueprint();
    }

    private void TrySell()
    {
        if (PlayerInventory.Instance == null) return;
        if (availableAddons == null || availableAddons.Count == 0) return;

        AddonDefinition def = availableAddons[currentIndex];
        if (!PlayerInventory.Instance.BlueprintProgress.IsUnlocked(def.blueprintId)) return;

        int refund = def.buildCost / 2;
        PlayerInventory.Instance.AddGold(refund);
        Debug.Log($"[EngineerShopUI] Sold {def.displayName} for {refund}g.");

        UpdateGoldDisplay();
        ShowCurrentBlueprint();
    }

    public void UpdateGoldDisplay()
    {
        if (goldText != null)
            goldText.text = $"Gold: {PlayerInventory.Instance?.Gold ?? 0}g";
    }

    private void OnDestroy()
    {
        if (basePrevButton != null) basePrevButton.onClick.RemoveAllListeners();
        if (baseNextButton != null) baseNextButton.onClick.RemoveAllListeners();
        if (baseBuyButton != null) baseBuyButton.onClick.RemoveAllListeners();
        if (sellBaseButton != null) sellBaseButton.onClick.RemoveAllListeners();
        if (basesSubTabButton != null) basesSubTabButton.onClick.RemoveAllListeners();
        if (addonsSubTabButton != null) addonsSubTabButton.onClick.RemoveAllListeners();
        prevButton.onClick.RemoveAllListeners();
        nextButton.onClick.RemoveAllListeners();
        buyButton.onClick.RemoveAllListeners();
        sellButton.onClick.RemoveAllListeners();
    }
}
