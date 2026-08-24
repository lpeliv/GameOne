using UnityEngine;

public class BedInteractable : MonoBehaviour, IInteractable
{
    [SerializeField] private WaveManager waveManager;

    private bool zoneCompleteMode = false;

    public string InteractionPrompt => zoneCompleteMode ? "Rest [E]" : "Start Wave [E]";

    public void SetZoneCompleteMode()
    {
        zoneCompleteMode = true;
        Debug.Log("[BedInteractable] Zone complete mode activated.");
    }

    public void OnInteract()
    {
        if (zoneCompleteMode)
        {
            GameProgressionManager.Instance?.TriggerZoneTransition();
            return;
        }

        if (waveManager == null) return;
        if (waveManager.WaveActive) return;
        if (!waveManager.CanStartNextWave()) return;

        waveManager.TryStartWave();
    }
}