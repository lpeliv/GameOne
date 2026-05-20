using UnityEngine;

public class BranchObstacle : MonoBehaviour
{
    private Vector2Int gridPos;
    private int branchIndex;
    private bool isRemoved;

    public Vector2Int GridPos => gridPos;
    public int BranchIndex => branchIndex;
    public bool IsRemoved => isRemoved;

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

    public void Interact()
    {
        Remove();
    }
}