using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float walkSpeed = 5f;
    [SerializeField] private float sprintSpeed = 9f;
    [SerializeField] private float crouchSpeed = 2.5f;
    [SerializeField] private float jumpForce = 5f;
    [SerializeField] private float gravity = -20f;

    [Header("Crouch")]
    [SerializeField] private float standHeight = 0.2f;
    [SerializeField] private float crouchHeight = 1f;
    [SerializeField] private float crouchTransitionSpeed = 8f;

    [Header("Look")]
    [SerializeField] private float mouseSensitivity = 0.2f;
    [SerializeField] private float verticalLookClamp = 85f;
    [SerializeField] private Transform cameraRoot;
    [SerializeField] private AddonInteractionDetector addonInteractionDetector;

    [Header("Hammer")]
    [SerializeField] private Transform hammerRoot;
    [SerializeField] private float hammerRange = 2f;
    [SerializeField] private float swingDuration = 0.4f;
    [SerializeField] private float swingAngle = 80f;
    [SerializeField] private float hammerStrength = 1f;
    [SerializeField] private float meleeDamage = 20f;
    [SerializeField] private LayerMask hammerHitLayers;

    private float swingTimer;
    private bool isSwinging;
    private Quaternion hammerRestRotation;
    private HashSet<Collider> hitThisSwing = new HashSet<Collider>();
    private PlayerState stateBeforeSwing;

    private CharacterController controller;
    private PlayerInputActions input;

    private Vector3 velocity;
    private float verticalRotation;
    private float targetHeight;

    private PlayerState currentState;

    private Vector2 moveInput;
    private Vector2 lookInput;
    private Vector3 horizontalMove;
    private bool jumpPressed;
    private bool sprintHeld;
    private bool crouchHeld;

    public static bool LookLocked = false;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        input = new PlayerInputActions();

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        targetHeight = standHeight;
        currentState = PlayerState.Idle;
    }

    private void OnEnable()
    {
        input.Player.Enable();

        input.Player.Move.performed += ctx => moveInput = ctx.ReadValue<Vector2>();
        input.Player.Move.canceled += ctx => moveInput = Vector2.zero;

        input.Player.Look.performed += ctx => lookInput = ctx.ReadValue<Vector2>();
        input.Player.Look.canceled += ctx => lookInput = Vector2.zero;

        input.Player.Attack.performed += ctx => TryStartSwing();
        input.Player.Jump.performed += ctx => jumpPressed = true;
        input.Player.Sprint.performed += ctx => sprintHeld = true;
        input.Player.Sprint.canceled += ctx => sprintHeld = false;
        input.Player.Crouch.performed += ctx => crouchHeld = true;
        input.Player.Crouch.canceled += ctx => crouchHeld = false;
        input.Player.Interact.performed += ctx => addonInteractionDetector.TryInteract();
    }

    private void OnDisable()
    {
        input.Player.Disable();
    }

    private void Update()
    {
        UpdateState();
        HandleLook();
        HandleMovement();
        HandleCrouch();
        HandleSwing();
        ApplyGravity();
    }

    private void UpdateState()
    {
        bool grounded = controller.isGrounded;
        bool moving = moveInput.sqrMagnitude > 0.01f;

        if (!grounded && velocity.y < -0.1f)
        {
            currentState = PlayerState.Falling;
            return;
        }

        if (velocity.y > 0.1f)
        {
            currentState = PlayerState.Jumping;
            return;
        }

        if (crouchHeld && grounded)
        {
            currentState = PlayerState.Crouching;
            return;
        }

        if (moving && sprintHeld && grounded)
        {
            currentState = PlayerState.Sprinting;
            return;
        }

        if (moving && grounded)
        {
            currentState = PlayerState.Walking;
            return;
        }

        if (isSwinging)
        {
            currentState = PlayerState.Attacking;
            return;
        }

        currentState = PlayerState.Idle;
    }

    private void HandleLook()
    {
        if (LookLocked) return;
        float horizontalRotation = lookInput.x * mouseSensitivity;
        transform.Rotate(Vector3.up, horizontalRotation);

        verticalRotation -= lookInput.y * mouseSensitivity;
        verticalRotation = Mathf.Clamp(verticalRotation, -verticalLookClamp, verticalLookClamp);
        cameraRoot.localRotation = Quaternion.Euler(verticalRotation, 0f, 0f);
    }
    
    private void HandleMovement()
    {
        float speed = currentState switch
        {
            PlayerState.Sprinting => sprintSpeed,
            PlayerState.Crouching => crouchSpeed,
            PlayerState.Attacking => walkSpeed * 0.5f,
            _ => walkSpeed
        };

        
        horizontalMove = transform.right  * moveInput.x + transform.forward * moveInput.y;
        horizontalMove *= speed;
    }

    private void HandleCrouch()
    {
        targetHeight = crouchHeld ? crouchHeight : standHeight;

        if(Mathf.Abs(controller.height - targetHeight) > 0.01f)
        {
            controller.height = Mathf.Lerp(controller.height, targetHeight, crouchTransitionSpeed * Time.deltaTime);
            controller.center = new Vector3(0f, controller.height / 2f, 0f);
            float cameraY = controller.height - 0.4f;
            cameraRoot.localPosition = new Vector3(0f, cameraY, 0f);
        }
    }

    private void ApplyGravity()
    {
        if(controller.isGrounded && velocity.y < 0)
        velocity.y = -2f;

        if(jumpPressed && controller.isGrounded && currentState != PlayerState.Crouching)
                velocity.y = Mathf.Sqrt(jumpForce * -2f * gravity);
        
        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);

        Vector3 finalMove = horizontalMove + Vector3.up * velocity.y;
        controller.Move(finalMove * Time.deltaTime);
        jumpPressed = false;
    }

    public PlayerState CurrentState => currentState;

    private void TryStartSwing()
    {
        if (isSwinging) return;
        if (currentState == PlayerState.Jumping ||
            currentState == PlayerState.Falling) return;

        stateBeforeSwing = currentState;
        currentState = PlayerState.Attacking;
        isSwinging = true;
        swingTimer = 0f;
        hitThisSwing.Clear();
        hammerRestRotation = hammerRoot.localRotation;
    }

    private void HandleSwing()
    {
        if (!isSwinging) return;

        swingTimer += Time.deltaTime;
        float t = swingTimer / swingDuration;

        float swingT = Mathf.Sin(t * Mathf.PI);
        float currentAngle = swingAngle * swingT;

        hammerRoot.localRotation = Quaternion.Euler(
            hammerRestRotation.eulerAngles.x + currentAngle,
            hammerRestRotation.eulerAngles.y,
            hammerRestRotation.eulerAngles.z
        );

        if (t >= 0.3f && t <= 0.7f)
            CheckHammerHits();

        if (swingTimer >= swingDuration)
        {
            hammerRoot.localRotation = hammerRestRotation;
            isSwinging = false;
            currentState = stateBeforeSwing;
            hitThisSwing.Clear();
        }
    }
    
    private void CheckHammerHits()
    {
        Ray ray = new Ray(cameraRoot.position, cameraRoot.forward);
        RaycastHit hit;

        if (!Physics.Raycast(ray, out hit, hammerRange, hammerHitLayers))
            return;

        Collider hitCollider = hit.collider;

        if (hitThisSwing.Contains(hitCollider)) return;
        hitThisSwing.Add(hitCollider);

        IHammerHittable hittable = hitCollider.GetComponent<IHammerHittable>();
        if (hittable != null)
            hittable.OnHammerHit(hammerStrength);

        EnemyHealth enemyHealth = hitCollider.GetComponent<EnemyHealth>();
        if (enemyHealth != null)
            enemyHealth.TakeDamage(meleeDamage);

        Debug.Log($"[PlayerController] Hammer hit: {hitCollider.gameObject.name}");
    }
}
