using TMPro;
using UnityEngine;
using UnityEngine.UI;

public enum BuildMenuTab { Base, Addon }

public enum BuildState { Inactive, Selecting, Placing }

public class BuildMenuUI : MonoBehaviour
{
    [SerializeField] private RawImage modelDisplay;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI statsText;
    [SerializeField] private TextMeshProUGUI tabIndicatorText;
    [SerializeField] private TextMeshProUGUI hintText;
    [SerializeField] private GameObject panel;

    [Header("Addon Placement")]
    [SerializeField] private AddonCarrySystem addonCarrySystem;

    private BuildMenuTab currentTab = BuildMenuTab.Base;
    private int currentBaseIndex = 0;
    private int currentAddonIndex = 0;
    private BuildState buildState = BuildState.Inactive;
    private bool waitingForAddonPlace;
    private AddonDefinition placingAddonDef;

    public BuildMenuTab CurrentTab => currentTab;
    public BuildState BuildState => buildState;
    public bool IsPlacing => buildState == BuildState.Placing;
    public bool IsSelecting => buildState == BuildState.Selecting;

    private void Start()
    {
        if (addonCarrySystem != null)
            addonCarrySystem.OnAddonPlaced += HandleAddonPlaced;
    }

    private void OnDestroy()
    {
        if (addonCarrySystem != null)
            addonCarrySystem.OnAddonPlaced -= HandleAddonPlaced;
    }

    private void HandleAddonPlaced()
    {
        Debug.Log($"[BuildMenuUI] OnAddonPlaced fired. waitingForAddonPlace={waitingForAddonPlace}, placingAddonDef={placingAddonDef?.displayName ?? "null"}");
        if (!waitingForAddonPlace) return;

        if (placingAddonDef != null)
        {
            Debug.Log($"[BuildMenuUI] Addon consumed: {placingAddonDef.displayName}");
            PlayerInventory.Instance.RemoveAddon(placingAddonDef);
            Debug.Log($"[BuildMenuUI] Addon count after removal: {PlayerInventory.Instance.CountOwnedAddons(placingAddonDef)}");
        }
        else
        {
            Debug.LogWarning("[BuildMenuUI] placingAddonDef was null on addon placed!");
        }

        waitingForAddonPlace = false;
        placingAddonDef = null;
        ConfirmPlacement();
    }

    private void Update()
    {
        if (buildState == BuildState.Selecting)
        {
            if (Input.GetMouseButtonDown(0))
                CycleBase();
            if (Input.GetMouseButtonDown(1))
                CycleAddon();
        }
    }

    public void ToggleBuildMode()
    {
        ReturnCarriedAddonToInventory();

        switch (buildState)
        {
            case BuildState.Inactive:
                buildState = BuildState.Selecting;
                panel.SetActive(true);
                RefreshDisplay();
                break;
            case BuildState.Selecting:
                buildState = BuildState.Inactive;
                panel.SetActive(false);
                break;
            case BuildState.Placing:
                waitingForAddonPlace = false;
                placingAddonDef = null;
                buildState = BuildState.Selecting;
                panel.SetActive(true);
                RefreshDisplay();
                break;
        }
    }

    private void ReturnCarriedAddonToInventory()
    {
        if (addonCarrySystem == null || !addonCarrySystem.IsCarrying) return;

        TurretAddon carried = addonCarrySystem.CarriedAddon;
        AddonDefinition def = carried != null ? carried.Definition : null;

        TurretAddon dropped = addonCarrySystem.Drop();
        if (dropped != null)
            Object.Destroy(dropped.gameObject);

        if (!waitingForAddonPlace && def != null)
        {
            PlayerInventory.Instance.AddAddon(def);
            Debug.Log($"[BuildMenuUI] Returned addon to inventory: {def.displayName}");
        }
        else if (waitingForAddonPlace)
        {
            Debug.Log($"[BuildMenuUI] Cancelled build-menu addon placement: {placingAddonDef?.displayName ?? "null"} (already in inventory)");
        }
    }

    public bool TryConfirmSelection()
    {
        if (buildState != BuildState.Selecting) return false;

        if (currentTab == BuildMenuTab.Base)
        {
            if (GetSelectedBase() == null) return false;

            buildState = BuildState.Placing;
            panel.SetActive(false);
            return true;
        }

        AddonDefinition addonDef = GetSelectedAddon();
        if (addonDef == null) return false;
        if (addonDef.addonPrefab == null) return false;

        Debug.Log($"[BuildMenuUI] Instantiating addon prefab: {addonDef.addonPrefab.name}");
        GameObject addonGO = Object.Instantiate(addonDef.addonPrefab);
        TurretAddon addon = addonGO.GetComponent<TurretAddon>();
        if (addon == null)
        {
            Debug.LogWarning("[BuildMenuUI] Addon prefab missing TurretAddon component.");
            Object.Destroy(addonGO);
            return false;
        }

        addon.Initialize(addonDef);
        Debug.Log($"[BuildMenuUI] Addon initialized: {addonDef.displayName}, calling PickUp");
        addonCarrySystem.PickUp(addon);
        Debug.Log($"[BuildMenuUI] PickUp called. IsCarrying: {addonCarrySystem.IsCarrying}");
        placingAddonDef = addonDef;
        waitingForAddonPlace = true;
        Debug.Log($"[BuildMenuUI] waitingForAddonPlace set TRUE, placingAddonDef={addonDef.displayName}");

        buildState = BuildState.Placing;
        panel.SetActive(false);
        return true;
    }

    public void CancelPlacement()
    {
        Debug.Log($"[BuildMenuUI] CancelPlacement called. waitingForAddonPlace={waitingForAddonPlace}, placingAddonDef={placingAddonDef?.displayName ?? "null"}");
        waitingForAddonPlace = false;
        placingAddonDef = null;
        buildState = BuildState.Inactive;
        panel.SetActive(false);
    }

    public void ConfirmPlacement()
    {
        waitingForAddonPlace = false;
        placingAddonDef = null;
        buildState = BuildState.Inactive;
        panel.SetActive(false);
    }

    private void CycleBase()
    {
        currentTab = BuildMenuTab.Base;
        var bases = PlayerInventory.Instance.OwnedBases;
        if (bases.Count > 0)
            currentBaseIndex = (currentBaseIndex + 1) % bases.Count;
        RefreshDisplay();
    }

    private void CycleAddon()
    {
        currentTab = BuildMenuTab.Addon;
        var addons = PlayerInventory.Instance.OwnedAddons;
        if (addons.Count > 0)
            currentAddonIndex = (currentAddonIndex + 1) % addons.Count;
        RefreshDisplay();
    }

    private int CountBases(TurretDefinition def)
    {
        int count = 0;
        var bases = PlayerInventory.Instance.OwnedBases;
        for (int i = 0; i < bases.Count; i++)
            if (bases[i] == def) count++;
        return count;
    }

    private int CountAddons(AddonDefinition def)
    {
        int count = 0;
        var addons = PlayerInventory.Instance.OwnedAddons;
        for (int i = 0; i < addons.Count; i++)
            if (addons[i] == def) count++;
        return count;
    }

    private void RefreshDisplay()
    {
        if (currentTab == BuildMenuTab.Base)
        {
            tabIndicatorText.text = "BASE";
            var bases = PlayerInventory.Instance.OwnedBases;
            if (bases.Count == 0)
            {
                nameText.text = "No items available";
                statsText.text = "";
            }
            else
            {
                currentBaseIndex = Mathf.Clamp(currentBaseIndex, 0, bases.Count - 1);
                var def = bases[currentBaseIndex];
                int count = CountBases(def);
                nameText.text = $"{def.displayName} (x{count})";
                statsText.text = $"HP: {def.maxHealth}\nCylinders: {def.cylinderCount}";
            }
            hintText.text = "LMB: Cycle Base | RMB: Switch to Addon | E: Confirm";
        }
        else
        {
            tabIndicatorText.text = "ADDON";
            var addons = PlayerInventory.Instance.OwnedAddons;
            if (addons.Count == 0)
            {
                nameText.text = "No items available";
                statsText.text = "";
            }
            else
            {
                currentAddonIndex = Mathf.Clamp(currentAddonIndex, 0, addons.Count - 1);
                var def = addons[currentAddonIndex];
                int count = CountAddons(def);
                nameText.text = $"{def.displayName} (x{count})";
                statsText.text = $"Damage: {def.damage}\nRange: {def.range}\nFire Rate: {def.fireRate}/s";
            }
            hintText.text = "RMB: Cycle Addon | LMB: Switch to Base | E: Confirm";
        }
    }

    public TurretDefinition GetSelectedBase()
    {
        var bases = PlayerInventory.Instance.OwnedBases;
        if (bases.Count == 0) return null;
        currentBaseIndex = Mathf.Clamp(currentBaseIndex, 0, bases.Count - 1);
        return bases[currentBaseIndex];
    }

    public AddonDefinition GetSelectedAddon()
    {
        var addons = PlayerInventory.Instance.OwnedAddons;
        if (addons.Count == 0) return null;
        currentAddonIndex = Mathf.Clamp(currentAddonIndex, 0, addons.Count - 1);
        return addons[currentAddonIndex];
    }
}
