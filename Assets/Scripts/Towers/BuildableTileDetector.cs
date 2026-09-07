using UnityEngine;

public class BuildableTileDetector : MonoBehaviour
{
    [Header("Detection")]
    [SerializeField] private Camera playerCamera;
    [SerializeField] private float interactRange = 8f;
    [SerializeField] private LayerMask tileLayer;
    [SerializeField] private LayerMask interactLayer;

    [Header("References")]
    [SerializeField] private TowerManager towerManager;
    [SerializeField] private BuildMenuUI buildMenuUI;
    [SerializeField] private AddonInteractionDetector addonInteractionDetector;
    [SerializeField] private AddonCarrySystem addonCarrySystem;

    [Header("Build Mode")]
    [SerializeField] private KeyCode buildModeKey = KeyCode.B;

    private BuildableTile currentDetectedTile;

    private void Update()
    {
        if (Input.GetKeyDown(buildModeKey))
            buildMenuUI.ToggleBuildMode();

        if (buildMenuUI.IsSelecting && Input.GetKeyDown(KeyCode.E))
            buildMenuUI.TryConfirmSelection();

        if (buildMenuUI.IsPlacing)
        {
            if (buildMenuUI.CurrentTab == BuildMenuTab.Base)
                DetectTile();
            else
                HandleAddonPlacing();
        }
        else
        {
            ClearCurrentTile();
        }
    }

    private void DetectTile()
    {
        Ray ray = playerCamera.ScreenPointToRay(new Vector3(Screen.width / 2f, Screen.height / 2f));
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, interactRange, tileLayer))
        {
            BuildableTile tile = hit.collider.GetComponent<BuildableTile>();

            if (tile != null && tile != currentDetectedTile)
            {
                ClearCurrentTile();
                currentDetectedTile = tile;

                bool canBuild = towerManager.CanBuildAt(tile);
                currentDetectedTile.Highlight(canBuild);
            }

            if (currentDetectedTile != null && Input.GetMouseButtonDown(0))
                TryPlaceBase(currentDetectedTile);
        }
        else
        {
            ClearCurrentTile();
        }
    }

    private void HandleAddonPlacing()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Debug.Log("[BuildableTileDetector] LMB in addon placing mode, calling TryInteract.");
            addonInteractionDetector.TryInteract();
        }

        if (Input.GetMouseButtonDown(1))
        {
            if (addonCarrySystem.IsCarrying)
            {
                Debug.Log("[BuildableTileDetector] RMB while carrying addon — dropping and returning to inventory.");
                TurretAddon carried = addonCarrySystem.Drop();
                if (carried != null)
                {
                    if (carried.Definition != null)
                        PlayerInventory.Instance.AddAddon(carried.Definition);
                    Destroy(carried.gameObject);
                    Debug.Log("[BuildMenu] Carried addon returned to inventory.");
                }
                buildMenuUI.CancelPlacement();
            }
            else
            {
                TryDetachAddonToInventory();
            }
        }
    }

    private void TryDetachAddonToInventory()
    {
        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
        RaycastHit hit;

        if (!Physics.Raycast(ray, out hit, interactRange, interactLayer)) return;
        if (((1 << hit.collider.gameObject.layer) & interactLayer) == 0) return;

        TurretJoint joint = hit.collider.GetComponentInParent<TurretJoint>();
        if (joint == null || !joint.IsOccupied)
        {
            Debug.Log("[BuildableTileDetector] RMB: no occupied joint found.");
            return;
        }

        TurretAddon addon = joint.Detach();
        if (addon == null) return;

        if (addon.Definition != null)
            PlayerInventory.Instance.AddAddon(addon.Definition);

        Destroy(addon.gameObject);
        Debug.Log("[BuildMenu] Addon detached from joint and returned to inventory.");
    }

    private void TryPlaceBase(BuildableTile tile)
    {
        if (!towerManager.CanBuildAt(tile)) return;

        TurretDefinition baseDef = buildMenuUI.GetSelectedBase();
        if (baseDef == null) return;

        towerManager.RequestBuild(tile, baseDef);
        buildMenuUI.ConfirmPlacement();
    }

    private void ClearCurrentTile()
    {
        if (currentDetectedTile == null) return;
        currentDetectedTile.ClearHighlight();
        currentDetectedTile = null;
    }
}
