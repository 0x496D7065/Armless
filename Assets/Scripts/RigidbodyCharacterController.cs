using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Leave "Camera Transform" empty to auto-use Camera.main. Works fine with Cinemachine,
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class RigidbodyCharacterController : MonoBehaviour
{
    [Header("Input")]
    [SerializeField] private InputActionReference moveActionReference;
    [SerializeField] private InputActionReference jumpActionReference;
    [SerializeField] private InputActionReference dodgeActionReference;

    [Header("Camera")]
    [Tooltip("Leave empty to auto-use Camera.main. Works with Cinemachine since Cinemachine " +
        "drives this same Transform/Camera component, it doesn't need a separate reference.")]
    [SerializeField] private Transform cameraTransform;

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 8f;
    [SerializeField] private float acceleration = 60f;      // how fast we reach target speed
    [SerializeField] private float deceleration = 60f;      // how fast we stop
    [SerializeField] private float airControlMultiplier = 0.6f;

    [Header("Gravity")]
    [SerializeField] private float gravityMultiplier = 3f;  // extra gravity for a snappier fall
    [SerializeField] private float groundCheckDistance = 0.2f;
    [SerializeField] private LayerMask groundMask = ~0;

    [Header("Jump")]
    [SerializeField] private float jumpForce = 9f;
    [SerializeField] private float coyoteTime = 0.1f;       // grace period to jump after leaving ground
    [SerializeField] private float jumpBufferTime = 0.1f;   // grace period if jump pressed just before landing

    [Header("Dash")]
    [SerializeField] private float dashSpeed = 22f;
    [SerializeField] private float dashDuration = 0.15f;
    [SerializeField] private float dashCooldown = 0.4f;
    [SerializeField] private bool dashIgnoresGravity = true;

    private Rigidbody rb;
    private InputAction moveAction;
    private InputAction jumpAction;
    private InputAction dodgeAction;

    private Vector2 moveInput;

    private bool isDashing;
    private float dashTimer;
    private float dashCooldownTimer;
    private Vector3 dashDirection;

    private bool isGrounded;
    private float lastGroundedTime;
    private float lastJumpPressedTime = -999f;

    // The character's own facing, re-synced to the camera's yaw every physics step.
    // Movement/dash always read THESE, never the camera directly.
    private Vector3 facingForward = Vector3.forward;
    private Vector3 facingRight = Vector3.right;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

        moveAction = moveActionReference != null ? moveActionReference.action : null;
        jumpAction = jumpActionReference != null ? jumpActionReference.action : null;
        dodgeAction = dodgeActionReference != null ? dodgeActionReference.action : null;

        if (moveAction == null)
            Debug.LogError($"{nameof(RigidbodyCharacterController)}: Move Action Reference is not assigned.", this);
        if (jumpAction == null)
            Debug.LogError($"{nameof(RigidbodyCharacterController)}: Jump Action Reference is not assigned.", this);
        if (dodgeAction == null)
            Debug.LogError($"{nameof(RigidbodyCharacterController)}: Dodge Action Reference is not assigned.", this);

        if (cameraTransform == null && Camera.main != null)
            cameraTransform = Camera.main.transform;
    }

    private void OnEnable()
    {
        moveAction?.Enable();

        if (jumpAction != null)
        {
            jumpAction.Enable();
            jumpAction.performed += OnJumpPerformed;
        }

        if (dodgeAction != null)
        {
            dodgeAction.Enable();
            dodgeAction.performed += OnDodgePerformed;
        }
    }

    private void OnDisable()
    {
        moveAction?.Disable();

        if (jumpAction != null)
        {
            jumpAction.performed -= OnJumpPerformed;
            jumpAction.Disable();
        }

        if (dodgeAction != null)
        {
            dodgeAction.performed -= OnDodgePerformed;
            dodgeAction.Disable();
        }
    }

    private void OnJumpPerformed(InputAction.CallbackContext context)
    {
        lastJumpPressedTime = Time.time; // buffer the press, actual jump happens in FixedUpdate
    }

    private void OnDodgePerformed(InputAction.CallbackContext context)
    {
        const float deadzone = 0.1f;

        // Snapshot whatever direction is currently held on Move; if nothing is held,
        // default to dashing straight forward (the character's current facing).
        Vector2 dodgeInput = moveInput.sqrMagnitude > deadzone * deadzone ? moveInput : Vector2.up;
        TryStartDash(dodgeInput);
    }

    private void Update()
    {
        moveInput = moveAction != null ? moveAction.ReadValue<Vector2>() : Vector2.zero;

        if (dashCooldownTimer > 0f)
            dashCooldownTimer -= Time.deltaTime;

        if (isDashing)
        {
            dashTimer -= Time.deltaTime;
            if (dashTimer <= 0f)
                isDashing = false;
        }
    }

    private void TryStartDash(Vector2 inputDirection)
    {
        if (dashCooldownTimer > 0f) return;

        isDashing = true;
        dashTimer = dashDuration;
        dashCooldownTimer = dashCooldown + dashDuration;
        dashDirection = GetCharacterRelativeDirection(inputDirection);
    }

    /// <summary>
    /// Converts a raw (x, y) input into a world-space direction relative to the
    /// character's OWN current facing (facingForward/facingRight) — never the camera
    /// directly. This is what keeps movement/dash camera-agnostic.
    /// </summary>
    private Vector3 GetCharacterRelativeDirection(Vector2 input)
    {
        Vector3 dir = facingRight * input.x + facingForward * input.y;
        return dir.sqrMagnitude > 1f ? dir.normalized : dir;
    }

    /// <summary>
    /// Instantly snaps the character's yaw to match the camera's yaw (no smoothing/turn
    /// speed, by design — response time over polish for now) and refreshes
    /// facingForward/facingRight from it. Pitch/roll are ignored so the character stays
    /// upright regardless of camera angle.
    /// </summary>
    private void SyncFacingWithCamera()
    {
        if (cameraTransform == null)
        {
            facingForward = transform.forward;
            facingRight = transform.right;
            return;
        }

        Quaternion yawOnlyRotation = Quaternion.Euler(0f, cameraTransform.eulerAngles.y, 0f);
        rb.MoveRotation(yawOnlyRotation);

        facingForward = yawOnlyRotation * Vector3.forward;
        facingRight = yawOnlyRotation * Vector3.right;
    }

    private void FixedUpdate()
    {
        SyncFacingWithCamera();
        CheckGrounded();
        TryConsumeJump();

        if (isDashing)
        {
            ApplyDash();
        }
        else
        {
            ApplyMovement();
        }

        ApplyGravity();
    }

    private void CheckGrounded()
    {
        isGrounded = Physics.Raycast(transform.position, Vector3.down, groundCheckDistance + 0.05f, groundMask);
        if (isGrounded)
            lastGroundedTime = Time.time;
    }

    private void TryConsumeJump()
    {
        bool withinCoyoteTime = Time.time - lastGroundedTime <= coyoteTime;
        bool jumpWasBuffered = Time.time - lastJumpPressedTime <= jumpBufferTime;

        if (jumpWasBuffered && withinCoyoteTime)
        {
            Vector3 vel = rb.linearVelocity;
            vel.y = jumpForce;
            rb.linearVelocity = vel;

            // Consume both so it doesn't re-trigger next physics step.
            lastJumpPressedTime = -999f;
            lastGroundedTime = -999f;
        }
    }

    private void ApplyMovement()
    {
        Vector3 wishDir = GetCharacterRelativeDirection(moveInput);

        Vector3 targetVelocity = wishDir * moveSpeed;
        Vector3 currentVelocity = rb.linearVelocity;
        Vector3 flatVelocity = new Vector3(currentVelocity.x, 0f, currentVelocity.z);

        float accel = wishDir.sqrMagnitude > 0.01f ? acceleration : deceleration;
        if (!isGrounded) accel *= airControlMultiplier;

        Vector3 newFlatVelocity = Vector3.MoveTowards(flatVelocity, targetVelocity, accel * Time.fixedDeltaTime);

        rb.linearVelocity = new Vector3(newFlatVelocity.x, currentVelocity.y, newFlatVelocity.z);
    }

    private void ApplyDash()
    {
        Vector3 vel = dashDirection * dashSpeed;
        if (!dashIgnoresGravity)
            vel.y = rb.linearVelocity.y;
        else
            vel.y = 0f;

        rb.linearVelocity = vel;
    }

    private void ApplyGravity()
    {
        if (isDashing && dashIgnoresGravity) return;

        // Extra gravity multiplier on top of Physics.gravity for a snappier, less floaty fall.
        rb.linearVelocity += Physics.gravity * (gravityMultiplier - 1f) * Time.fixedDeltaTime;
    }
}