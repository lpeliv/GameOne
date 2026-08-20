using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BlacksmithShopUI : ShopUIBase
{
    [Header("Panels")]
    [SerializeField] private GameObject blacksmithShopPanel;
    [SerializeField] private GameObject hammerTabContent;
    [SerializeField] private GameObject abilitiesTabContent;

    [Header("Tabs")]
    [SerializeField] private Button hammerTabButton;
    [SerializeField] private Button abilitiesTabButton;

    [Header("Dialogue")]
    [SerializeField] private TextMeshProUGUI dialogue1Text;
    [SerializeField] private TextMeshProUGUI dialogue2Text;
    [SerializeField] private string defaultDialogue1 = "Welcome, warrior...";
    [SerializeField] private string defaultDialogue2 = "What would you like to upgrade?";

    [Header("Gold")]
    [SerializeField] private TextMeshProUGUI goldText;

    public static BlacksmithShopUI Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        hammerTabButton.onClick.AddListener(ShowHammerTab);
        abilitiesTabButton.onClick.AddListener(ShowAbilitiesTab);
        Hide();
    }

    public override void Show()
    {
        base.Show();
        blacksmithShopPanel.SetActive(true);
        dialogue1Text.text = defaultDialogue1;
        dialogue2Text.text = defaultDialogue2;
        UpdateGoldDisplay();
        ShowHammerTab();
    }

    public override void Hide()
    {
        base.Hide();
        blacksmithShopPanel.SetActive(false);
    }

    public void ShowHammerTab()
    {
        hammerTabContent.SetActive(true);
        abilitiesTabContent.SetActive(false);
    }

    public void ShowAbilitiesTab()
    {
        hammerTabContent.SetActive(false);
        abilitiesTabContent.SetActive(true);
        // TODO: populate abilities
    }

    public void UpdateGoldDisplay()
    {
        if (goldText != null)
            goldText.text = $"Gold: {PlayerInventory.Instance?.Gold ?? 0}g";
    }

    public void UpdateSkillInfo(int branchIndex) { }
   
}