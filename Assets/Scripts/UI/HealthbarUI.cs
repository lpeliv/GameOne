using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class HealthBarUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Image healthBarFill;
    [SerializeField] private TextMeshProUGUI healthText;

    [Header("Colors")]
    [SerializeField] private Color fullHealthColor = Color.green;
    [SerializeField] private Color lowHealthColor = Color.red;

    private PlayerHealth playerHealth;

    private void Awake()
    {
        playerHealth = PlayerHealth.Instance;
    }

    private void Update()
    {
        if (playerHealth == null)
        {
            playerHealth = PlayerHealth.Instance;
            return;
        }

        UpdateHealthBar();
    }

    private void UpdateHealthBar()
    {
        float fillAmount = playerHealth.MaxHP > 0f
            ? playerHealth.CurrentHP / playerHealth.MaxHP
            : 0f;

        healthBarFill.fillAmount = fillAmount;
        healthBarFill.color = Color.Lerp(lowHealthColor, fullHealthColor, fillAmount);

        healthText.text = $"{Mathf.CeilToInt(playerHealth.CurrentHP)} / {Mathf.CeilToInt(playerHealth.MaxHP)}";
    }
}