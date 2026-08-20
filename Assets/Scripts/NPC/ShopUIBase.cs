using UnityEngine;

public class ShopUIBase : MonoBehaviour
{
    protected bool isOpen = false;
    public bool IsOpen => isOpen;

    protected virtual void Update()
    {
        if (isOpen && Input.GetKeyDown(KeyCode.Escape))
            Hide();
    }

    public virtual void Show()
    {
        isOpen = true;
        gameObject.SetActive(true);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        PlayerController.LookLocked = true;
        PlayerController.InputLocked = true;

        AddonInteractionDetector detector = FindAnyObjectByType<AddonInteractionDetector>();
        detector?.HidePrompt();
    }

    public virtual void Hide()
    {
        isOpen = false;
        gameObject.SetActive(false);

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        PlayerController.LookLocked = false;
        PlayerController.InputLocked = false;
    }
}