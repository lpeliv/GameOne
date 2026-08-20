using UnityEngine;

public class BedInteractable : MonoBehaviour, IInteractable
{
    [SerializeField] private WaveManager waveManager;

    public string InteractionPrompt => "Start Wave [E]";

    public void OnInteract()
    {
        if (waveManager == null)
        {
            Debug.LogWarning("[BedInteractable] No WaveManager assigned.");
            return;
        }

        if (waveManager.WaveActive)
        {
            Debug.Log("[BedInteractable] Wave already active.");
            return;
        }

        if (!waveManager.CanStartNextWave())
        {
            Debug.Log("[BedInteractable] Cannot start wave yet — remove obstacle first.");
            return;
        }

        waveManager.TryStartWave();
    }
}