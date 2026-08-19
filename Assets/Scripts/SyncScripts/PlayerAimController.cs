using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Drives player aim directly, following Cinemachine's own recommended pattern for
/// Third Person Follow: aim control does NOT live on the camera at all.
/// AimPivot and Head are deliberately plain GameObjects with NO Rigidbody component.
/// </summary>
public class PlayerAimController : MonoBehaviour
{
    [Header("Input")]
    [SerializeField] private InputActionReference lookActionReference;

    [Header("Pivots")]
    [Tooltip("Child of PlayerRoot. Yaw is applied here. Model/Sword should be parented under this.")]
    [SerializeField] private Transform aimPivot;

    [Tooltip("Child of AimPivot. Pitch is applied here. This is what Cinemachine's " +
        "Third Person Follow Tracking Target should be set to.")]
    [SerializeField] private Transform head;

    [Header("Sensitivity")]
    [Tooltip("Mouse Delta is raw pixel movement since last frame, NOT a normalized " +
        "-1..1 stick value \u2014 so this is a small direct multiplier, not a degrees/second " +
        "rate. Typical values are 0.05-0.3. Do NOT multiply this by Time.deltaTime: the " +
        "delta already represents per-frame movement, so doing so double-scales it and " +
        "makes sensitivity framerate-dependent.")]
    [SerializeField] private float yawSensitivity = 0.15f;
    [SerializeField] private float pitchSensitivity = 0.15f;

    [Header("Pitch Limits")]
    [SerializeField] private float minPitch = -60f;
    [SerializeField] private float maxPitch = 75f;

    private InputAction lookAction;
    private float yaw;
    private float pitch;

    /// <summary>Current aim-relative forward, for RigidbodyCharacterController's movement math.</summary>
    public Vector3 Forward => aimPivot.forward;

    /// <summary>Current aim-relative right, for RigidbodyCharacterController's movement math.</summary>
    public Vector3 Right => aimPivot.right;

    private void Awake()
    {
        lookAction = lookActionReference != null ? lookActionReference.action : null;
        if (lookAction == null)
            Debug.LogError($"{nameof(PlayerAimController)}: Look Action Reference is not assigned.", this);

        if (aimPivot == null)
            Debug.LogError($"{nameof(PlayerAimController)}: Aim Pivot is not assigned.", this);
        if (head == null)
            Debug.LogError($"{nameof(PlayerAimController)}: Head is not assigned.", this);

        // Seed yaw from whatever the pivot's current placed rotation is, so it doesn't
        // snap to 0 the moment play starts.
        if (aimPivot != null)
            yaw = aimPivot.eulerAngles.y;
    }

    private void OnEnable()
    {
        lookAction?.Enable();
    }

    private void OnDisable()
    {
        lookAction?.Disable();
    }

    private void LateUpdate()
    {
        if (aimPivot == null || head == null) return;

        Vector2 lookInput = lookAction != null ? lookAction.ReadValue<Vector2>() : Vector2.zero;

        yaw += lookInput.x * yawSensitivity;
        pitch = Mathf.Clamp(pitch - lookInput.y * pitchSensitivity, minPitch, maxPitch);

        aimPivot.rotation = Quaternion.Euler(0f, yaw, 0f);
        head.localRotation = Quaternion.Euler(pitch, 0f, 0f);
    }
}