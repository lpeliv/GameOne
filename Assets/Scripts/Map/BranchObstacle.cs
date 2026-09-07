using UnityEngine;

public class BranchObstacle : MonoBehaviour, IInteractable
{
    private Vector2Int gridPos;
    private int branchIndex;
    private bool isRemoved;

    public Vector2Int GridPos => gridPos;
    public int BranchIndex => branchIndex;
    public bool IsRemoved => isRemoved;

    public string InteractionPrompt
    {
        get
        {
            if (WaveManager.Instance == null) return "";
            if (WaveManager.Instance.ObstacleRemoverCount > 0)
                return "Remove Obstacle [E]";
            return "Need obstacle remover";
        }
    }

    public void Initialize(Vector2Int gridPos, int branchIndex)
    {
        this.gridPos = gridPos;
        this.branchIndex = branchIndex;
        isRemoved = false;
    }

    public void Remove()
    {
        if (isRemoved) return;
        isRemoved = true;
        OnRemoved();
    }

    protected virtual void OnRemoved()
    {
        Destroy(gameObject);
    }

    public void OnInteract()
    {
        if (isRemoved) return;

        Debug.Log($"[BranchObstacle] Obstacle branchIndex: {branchIndex} interacted, GridPos: {gridPos}");

        if (WaveManager.Instance == null)
        {
            Debug.LogWarning("[BranchObstacle] WaveManager.Instance is null.");
            return;
        }

        if (WaveManager.Instance.ObstacleRemoverCount <= 0)
        {
            Debug.Log("[BranchObstacle] No obstacle removers available.");
            return;
        }

        WaveManager.Instance.TryUseObstacleRemover(branchIndex);
    }
}
