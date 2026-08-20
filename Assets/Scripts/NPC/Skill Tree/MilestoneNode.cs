using UnityEngine;

public class MilestoneNode : MonoBehaviour
{
    private HammerUpgradeStat stat;
    private int milestoneIndex;
    private Color branchColor;
    private bool isReached;
    private bool isNext;
    private bool isLocked;

    private Renderer rend;
    private Material matInstance;

    [Header("Colors")]
    [SerializeField] private Color nextColor = Color.white;
    [SerializeField] private Color lockedColor = new Color(0.15f, 0.15f, 0.15f);

    public bool IsNext => isNext;
    public bool IsLocked => isLocked;

    public void Initialize(HammerUpgradeStat upgradeStat, int index, Color color)
    {
        stat = upgradeStat;
        milestoneIndex = index;
        branchColor = color;

        rend = GetComponent<Renderer>();
        matInstance = new Material(rend.material);
        rend.material = matInstance;
    }

    public void SetState(bool reached, bool next, bool locked)
    {
        isReached = reached;
        isNext = next;
        isLocked = locked;

        UpdateVisual();
    }

    private void UpdateVisual()
    {
        if (isLocked)
            matInstance.color = lockedColor;
        else if (isReached)
            matInstance.color = branchColor;
        else if (isNext)
            matInstance.color = nextColor;
        else
            matInstance.color = lockedColor;
    }

    public void OnHoverEnter()
    {
        if (!isNext || isLocked) return;
        matInstance.color = Color.Lerp(nextColor, Color.white, 0.5f);
    }

    public void OnHoverExit()
    {
        UpdateVisual();
    }

    public void TryUpgrade()
    {
        if (!isNext || isLocked) return;

        bool success = HammerUpgradeManager.Instance.TryUpgrade(stat);
        if (!success) return;

        SkillTreeWorld.Instance?.GrowBranch(stat);
        BlacksmithShopUI.Instance?.UpdateGoldDisplay();
    }
}