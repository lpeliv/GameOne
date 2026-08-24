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

    [Header("Navigation")]
    [SerializeField] private Button prevButton;
    [SerializeField] private Button nextButton;
    [SerializeField] private TextMeshProUGUI blueprintIndexText;

    [Header("Model Display")]
    [SerializeField] private RectTransform modelDisplayArea;
    [SerializeField] private RawImage modelDisplay;

    [Header("Action Row")]
    [SerializeField] private GameObject actionRow;
    [SerializeField] private Button buyButton;
    [SerializeField] private Button sellButton;

    [Header("Stats Panel")]
    [SerializeField] private TextMeshProUGUI blueprintNameText;
    [SerializeField] private TextMeshProUGUI statsText;
    [SerializeField] private TextMeshProUGUI costText;
    [SerializeField] private GameObject progressPanel;
    [SerializeField] private Slider progressSlider;
    [SerializeField] private TextMeshProUGUI progressText;

    [Header("Blueprints")]
    [SerializeField] private List<AddonDefinition> availableAddons;

    [Header("Animation")]
    [SerializeField] private float slideSpeed = 5f;

    [Header("Tabs")]
    [SerializeField] private Button turretsTabButton;
    [SerializeField] private Button mattsTabButton;

    private int currentIndex = 0;
    private Coroutine slideCoroutine;
    private float modelDisplayRestX;

    private void Awake()
    {
        turretsTabButton.onClick.AddListener(ShowTurretsTab);
        mattsTabButton.onClick.AddListener(ShowMattsTab);
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
        bool unlocked = progress.IsUnlocked(def.displayName);

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
            int partsFound = progress.GetPartsFound(def.displayName);
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
        if (!PlayerInventory.Instance.BlueprintProgress.IsUnlocked(def.displayName)) return;

        if (!PlayerInventory.Instance.SpendGold(def.buildCost))
        {
            Debug.Log($"[EngineerShopUI] Not enough gold for {def.displayName}.");
            return;
        }

        Debug.Log($"[EngineerShopUI] Purchased addon {def.displayName} for {def.buildCost}g.");

        UpdateGoldDisplay();
        ShowCurrentBlueprint();
    }

    private void TrySell()
    {
        if (PlayerInventory.Instance == null) return;
        if (availableAddons == null || availableAddons.Count == 0) return;

        AddonDefinition def = availableAddons[currentIndex];
        if (!PlayerInventory.Instance.BlueprintProgress.IsUnlocked(def.displayName)) return;

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
        prevButton.onClick.RemoveAllListeners();
        nextButton.onClick.RemoveAllListeners();
        buyButton.onClick.RemoveAllListeners();
        sellButton.onClick.RemoveAllListeners();
    }
}
