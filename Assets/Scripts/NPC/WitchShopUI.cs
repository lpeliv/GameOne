using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class WitchShopUI : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject witchShopPanel;
    [SerializeField] private GameObject shopTabContent;
    [SerializeField] private GameObject potionsTabContent;

    [Header("Dialogue")]
    [SerializeField] private TextMeshProUGUI dialogue1Text;
    [SerializeField] private TextMeshProUGUI dialogue2Text;
    [SerializeField] private string defaultDialogue1 = "Ah, a weary traveller...";
    [SerializeField] private string defaultDialogue2 = "What have you brought me today?";

    [Header("Shop")]
    [SerializeField] private TextMeshProUGUI goldText;
    [SerializeField] private Transform itemListContainer;
    [SerializeField] private GameObject itemEntryPrefab;

    [Header("Tabs")]
    [SerializeField] private Button shopTabButton;
    [SerializeField] private Button potionsTabButton;

    public static WitchShopUI Instance { get; private set; }

    private bool isOpen = false;
    public bool IsOpen => isOpen;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        shopTabButton.onClick.AddListener(ShowShopTab);
        potionsTabButton.onClick.AddListener(ShowPotionsTab);

        Hide();
    }

    public void ShowShopTab()
    {
        shopTabContent.SetActive(true);
        potionsTabContent.SetActive(false);
        PopulateShop();
    }

    public void ShowPotionsTab()
    {
        shopTabContent.SetActive(false);
        potionsTabContent.SetActive(true);
        // TODO: populate potions
    }

    private void Update()
    {
        if (isOpen && Input.GetKeyDown(KeyCode.Escape))
            Hide();
    }

    public void Show()
    {
        isOpen = true;
        gameObject.SetActive(true);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        PlayerController.LookLocked = true;
        PlayerController.InputLocked = true;

        dialogue1Text.text = defaultDialogue1;
        dialogue2Text.text = defaultDialogue2;

        AddonInteractionDetector detector = FindObjectOfType<AddonInteractionDetector>();
        detector?.HidePrompt();

        ShowShopTab();
    }

    public void Hide()
    {
        isOpen = false;
        gameObject.SetActive(false);

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        PlayerController.LookLocked = false;
        PlayerController.InputLocked = false;

        StopAllCoroutines();
    }

    private void PopulateShop()
    {
        foreach (Transform child in itemListContainer)
            Destroy(child.gameObject);

        UpdateGoldDisplay();

        IReadOnlyDictionary<ItemDefinition, int> inventory = PlayerInventory.Instance.GetInventory();

        if (inventory.Count == 0)
        {
            ShowEmptyMessage();
            return;
        }

        foreach (var kvp in inventory)
        {
            if (kvp.Value <= 0) continue;

            GameObject go = Instantiate(itemEntryPrefab, itemListContainer);
            go.SetActive(true);
            ItemEntryUI entry = go.GetComponent<ItemEntryUI>();

            if (entry == null)
            {
                Debug.LogWarning("[WitchShopUI] ItemEntryPrefab missing ItemEntryUI component.");
                continue;
            }

            entry.Setup(kvp.Key, kvp.Value);
        }
    }

    public void UpdateGoldDisplay()
    {
        if (goldText != null)
            goldText.text = $"Gold: {PlayerInventory.Instance?.Gold ?? 0}g";
    }

    public void ShowEmptyMessage()
    {
        foreach (Transform child in itemListContainer)
            Destroy(child.gameObject);

        GameObject emptyMsg = new GameObject("EmptyMessage");
        emptyMsg.transform.SetParent(itemListContainer, false);

        TextMeshProUGUI text = emptyMsg.AddComponent<TextMeshProUGUI>();
        text.text = "Nothing to sell, traveller.";
        text.alignment = TextAlignmentOptions.Center;
        text.fontSize = 24;
        text.color = Color.white;
    }
}