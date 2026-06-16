using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

public enum JointHighlightState
{
    None,
    Empty,
    Occupied,
    Interactable,
}

public class TurretJoint : MonoBehaviour
{
    [Header("Socket Visual")]
    [SerializeField] private GameObject socketVisual;
    [SerializeField] private Renderer socketRenderer;

    [Header("Highlight Colors")]
    [SerializeField] private Color emptyColor = Color.green;
    [SerializeField] private Color occupiedColor = Color.red;
    [SerializeField] private Color interactableColor = Color.yellow;

    private TurretAddon currentAddon;
    private TurretBase turretBase;
    private Material socketMaterial;

    public bool IsOccupied => currentAddon != null;
    public TurretAddon CurrentAddon => currentAddon;

    private void Awake()
    {
        turretBase = GetComponentInParent<TurretBase>();

        if (socketRenderer != null)
        {
            socketMaterial = socketRenderer.material;
            HDMaterial.ValidateMaterial(socketMaterial);
        }
    }

    public void SetHighlight(JointHighlightState state)
    {
        if (socketMaterial == null) return;

        if (state == JointHighlightState.None)
        {
            socketMaterial.SetColor("_EmissiveColor", Color.black * 0f);
            socketMaterial.DisableKeyword("_EMISSION");
            return;
        }

        Color color = state switch
        {
            JointHighlightState.Empty => emptyColor,
            JointHighlightState.Occupied => occupiedColor,
            JointHighlightState.Interactable => interactableColor,
            _ => Color.black
        };

        socketMaterial.EnableKeyword("_EMISSION");
        socketMaterial.SetColor("_EmissiveColor", color * 3f);
    }

    public void Attach(TurretAddon addon)
    {
        if (addon == null) return;

        if (turretBase != null && !turretBase.IsBuilt)
        {
            Debug.LogWarning("[TurretJoint] Cannot attach addon — turret not fully built.");
            return;
        }

        currentAddon = addon;
        addon.transform.SetParent(transform);
        addon.transform.localPosition = Vector3.zero;
        addon.transform.localRotation = Quaternion.identity;
        addon.OnAttached(GetComponentInParent<TurretCylinder>());

    }

    public TurretAddon Detach()
    {
        if (currentAddon == null) return null;

        TurretAddon addon = currentAddon;
        currentAddon = null;
        addon.transform.SetParent(null);
        addon.OnDetached();

        return addon;
    }
}