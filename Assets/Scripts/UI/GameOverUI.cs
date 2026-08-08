using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GameOverUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject panel;
    [SerializeField] private TextMeshProUGUI gameOverText;
    [SerializeField] private Button restartButton;
    [SerializeField] private Button quitButton;

    [Header("References")]
    [SerializeField] private WaveManager waveManager;

    public static GameOverUI Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        restartButton.onClick.AddListener(OnRestartClicked);
        quitButton.onClick.AddListener(OnQuitClicked);

        Hide();
    }

    public void Show()
    {
        panel.SetActive(true);
        gameOverText.text = "GAME OVER";

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        PlayerController.LookLocked = true;
        PlayerController.InputLocked = true;
        Time.timeScale = 0.1f;

        waveManager.SetGameOver(true);

        Debug.Log("[GameOverUI] Game over screen shown.");
    }

    public void Hide()
    {
        panel.SetActive(false);

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        PlayerController.LookLocked = false;
        PlayerController.InputLocked = false;
        Time.timeScale = 1f;
    }

    private void OnRestartClicked()
    {
        Time.timeScale = 1f;
        Hide();
        waveManager.RestartWave();
    }

    private void OnQuitClicked()
    {
        Debug.Log("[GameOverUI] Quit clicked.");
        // TODO: load main menu
        Application.Quit();
    }
}