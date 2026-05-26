using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BuildPromptUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject panel;
    [SerializeField] private TextMeshProUGUI promptText;
    [SerializeField] private Button confirmButton;
    [SerializeField] private Button cancelButton;

    private BuildableTile currentTile;
    private System.Action onConfirm;

    private void Awake()
    {
        confirmButton.onClick.AddListener(OnConfirmClicked);
        cancelButton.onClick.AddListener(OnCancelClicked);
        Hide();
    }

    public void Show(BuildableTile tile, bool canBuild)
    {
        PlayerController.LookLocked = true;
        currentTile = tile;

        promptText.text = canBuild
            ? "Build turret here?"
            : "Cannot build here.";

        confirmButton.interactable = canBuild;
        panel.SetActive(true);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void Hide()
    {
        PlayerController.LookLocked = false;
        panel.SetActive(false);
        currentTile = null;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public void SetOnConfirm(System.Action action)
    {
        onConfirm = action;
    }

    private void OnConfirmClicked()
    {
        onConfirm?.Invoke();
        Hide();
    }

    private void OnCancelClicked()
    {
        Hide();
    }
}