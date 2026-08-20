using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SkillTreeWorld : MonoBehaviour
{
    public static SkillTreeWorld Instance { get; private set; }

    [Header("References")]
    [SerializeField] private Camera skillTreeCamera;
    [SerializeField] private RawImage skillTreeView;
    [SerializeField] private List<SkillTreeBranch> branches;

    [Header("Camera Control")]
    [SerializeField] private float rotationSpeed = 60f;
    [SerializeField] private float zoomSpeed = 3f;
    [SerializeField] private float minSize = 3f;
    [SerializeField] private float maxSize = 20f;

    [Header("Trunk")]
    [SerializeField] private Transform skillTreeRoot;

    private MilestoneNode lastHovered;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        RefreshTree();
    }

    public void RefreshTree()
    {
        foreach (SkillTreeBranch branch in branches)
            if (branch != null)
                branch.SyncWithUpgradeLevel();
    }

    public void GrowBranch(HammerUpgradeStat stat)
    {
        foreach (SkillTreeBranch branch in branches)
        {
            if (branch != null && branch.Stat == stat)
            {
                branch.GrowToNextMilestone();
                return;
            }
        }
    }

    private void Update()
    {
        HandleRotation();
        HandleZoom();
        HandleHover();
    }

    private void HandleRotation()
    {
        float input = 0f;
        if (Input.GetKey(KeyCode.A)) input = -1f;
        if (Input.GetKey(KeyCode.D)) input = 1f;

        if (Mathf.Abs(input) > 0.01f)
            skillTreeRoot.Rotate(Vector3.up, input * rotationSpeed * Time.deltaTime);
    }

    private void HandleZoom()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) > 0.01f)
        {
            skillTreeCamera.orthographicSize = Mathf.Clamp(
                skillTreeCamera.orthographicSize - scroll * zoomSpeed,
                minSize, maxSize);
        }
    }

    private void HandleHover()
    {
        RectTransform rt = skillTreeView.rectTransform;
        Vector2 localPoint;

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
            rt, Input.mousePosition, null, out localPoint)) return;

        Vector2 normalized = new Vector2(
            (localPoint.x / rt.sizeDelta.x) + 0.5f,
            (localPoint.y / rt.sizeDelta.y) + 0.5f);

        if (normalized.x < 0 || normalized.x > 1 ||
            normalized.y < 0 || normalized.y > 1)
        {
            lastHovered?.OnHoverExit();
            lastHovered = null;
            return;
        }

        Ray ray = skillTreeCamera.ViewportPointToRay(normalized);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, 1000f))
        {
            MilestoneNode node = hit.collider.GetComponent<MilestoneNode>();

            if (node != lastHovered)
            {
                lastHovered?.OnHoverExit();
                lastHovered = node;
                lastHovered?.OnHoverEnter();
            }

            if (Input.GetMouseButtonDown(0) && node != null)
                node.TryUpgrade();
        }
        else
        {
            lastHovered?.OnHoverExit();
            lastHovered = null;
        }
    }

}