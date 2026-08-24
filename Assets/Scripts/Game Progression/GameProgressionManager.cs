using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameProgressionManager : MonoBehaviour
{
    public static GameProgressionManager Instance { get; private set; }

    [Header("References")]
    [SerializeField] private WaveManager waveManager;
    [SerializeField] private List<ZoneDoor> allDoors;
    [SerializeField] private GameObject endScreen;

    [Header("Zones")]
    [SerializeField] private Side startingZone = Side.Left;

    private Side currentZone;
    private bool zoneComplete = false;

    public bool ZoneComplete => zoneComplete;

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
        currentZone = startingZone;
    }

    public void OnZoneComplete()
    {
        zoneComplete = true;
        Debug.Log($"[GameProgressionManager] Zone {currentZone} complete.");

        // Notify bed to change prompt
        BedInteractable bed = FindFirstObjectByType<BedInteractable>();
        if (bed != null)
            bed.SetZoneCompleteMode();
    }

    public void TriggerZoneTransition()
    {
        StartCoroutine(ZoneTransitionSequence());
    }

    private IEnumerator ZoneTransitionSequence()
    {
        // Stub cutscene — fade to black
        Debug.Log("[GameProgressionManager] Zone transition started.");

        // Lock player input
        PlayerController.InputLocked = true;
        PlayerController.LookLocked = true;

        // Wait for cutscene stub
        yield return new WaitForSeconds(2f);

        // For alpha — just show end screen
        ShowEndScreen();
    }

    private void ShowEndScreen()
    {
        if (endScreen != null)
            endScreen.SetActive(true);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        Debug.Log("[GameProgressionManager] End screen shown.");
    }
}