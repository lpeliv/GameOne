using System.Collections;
using UnityEngine;

public class WorldDrop : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private ItemDefinition definition;

    private float pickupRadius;
    private float lifetimeDuration;
    private float autoPickupFee;
    private int quantity;
    private float lifetimeTimer;
    private bool isPickedUp;
    private Rigidbody rb;

    public ItemDefinition Definition => definition;
    public int Quantity => quantity;

    private void Update()
    {
        if (isPickedUp) return;

        lifetimeTimer += Time.deltaTime;

        if (lifetimeTimer >= lifetimeDuration)
        {
            AutoPickupWithFee();
            return;
        }

        CheckProximityPickup();
    }

    private void CheckProximityPickup()
    {
        if (PlayerInventory.Instance == null) return;

        float dist = Vector3.Distance(
            transform.position,
            PlayerInventory.Instance.transform.position
        );

        if (dist <= pickupRadius)
            //Debug.Log($"[WorldDrop] Item picked up");
            PickUp(false);
    }

    private void PickUp(bool applyFee)
    {
        if (isPickedUp) return;
        isPickedUp = true;

        int finalQuantity = applyFee
            ? Mathf.FloorToInt(quantity * (1f - autoPickupFee))
            : quantity;

        if (finalQuantity > 0)
            PlayerInventory.Instance.AddItem(definition, finalQuantity);

        Debug.Log($"[WorldDrop] Picked up {finalQuantity}x {definition.itemName}." +
                  $"{(applyFee ? $" Fee applied ({autoPickupFee * 100f}%)." : "")}");

        Destroy(gameObject);
    }

    private void AutoPickupWithFee()
    {
        if (PlayerInventory.Instance == null)
        {
            Destroy(gameObject);
            return;
        }

        PickUp(true);
    }

    public void Initialize(ItemDefinition def, int qty)
    {
        definition = def;
        quantity = qty;
        lifetimeTimer = 0f;
        isPickedUp = false;
        rb = GetComponent<Rigidbody>();

        pickupRadius = def.pickupRadius;
        lifetimeDuration = def.lifetimeDuration;
        autoPickupFee = def.autoPickupFee;

        ScatterOnSpawn();

        Debug.Log($"[WorldDrop] Initialized {def.itemName} x{qty}. RB found: {rb != null}");
    }

    private void ScatterOnSpawn()
    {
        if (rb == null) return;

        Vector3 randomForce = new Vector3(
            UnityEngine.Random.Range(-2f, 2f),
            UnityEngine.Random.Range(2f, 6f),
            UnityEngine.Random.Range(-2f, 2f)
        );

        rb.AddForce(randomForce, ForceMode.Impulse);

        transform.rotation = Quaternion.Euler(
            UnityEngine.Random.Range(0f, 360f),
            UnityEngine.Random.Range(0f, 360f),
            UnityEngine.Random.Range(0f, 360f)
        );

        rb.angularVelocity = new Vector3(
            UnityEngine.Random.Range(-3f, 3f),
            UnityEngine.Random.Range(-3f, 3f),
            UnityEngine.Random.Range(-3f, 3f)
        );
    }
}