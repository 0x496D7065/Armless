using UnityEngine;

/// <summary>
/// Lives on "Visuals", a plain child of PlayerRoot (holding Model and Sword) that
/// deliberately has NO Rigidbody component of its own. PlayerRoot's Rigidbody never has
/// its rotation written to (see RigidbodyCharacterController.RefreshFacingFromCamera) —
/// direct Transform writes on a Rigidbody with interpolation enabled were fighting
/// Unity's own automatic physics interpolation for control of the same Transform each
/// frame, which is what caused the jitter, regardless of how the smoothing math itself
/// was written. Since Visuals has no Rigidbody, no such system contests it — this
/// script is the only thing that ever sets its rotation, so the smoothing actually holds.
///
/// Reads the camera directly (not PlayerRoot, which never rotates) and eases this
/// object's rotation toward the camera's yaw every LateUpdate. Position is left alone
/// and inherits normally from the parent hierarchy.
/// </summary>
public class VisualRotationSmoother : MonoBehaviour
{
    [Tooltip("The camera whose yaw this should visually follow. Leave empty to auto-use Camera.main.")]
    [SerializeField] private Transform cameraTransform;

    [Tooltip("Higher = snappier/closer to instant. Lower = more visible lag on turns.")]
    [SerializeField] private float smoothSpeed = 25f;

    private void Awake()
    {
        if (cameraTransform == null && Camera.main != null)
            cameraTransform = Camera.main.transform;

        if (cameraTransform == null)
            Debug.LogError($"{nameof(VisualRotationSmoother)}: Camera Transform is not assigned and no Camera.main was found.", this);
    }

    private void LateUpdate()
    {
        if (cameraTransform == null) return;

        // Same forward-vector-projection approach as the controller previously used —
        // avoids Transform.eulerAngles, which is prone to small decomposition jitter
        // once the camera has pitch.
        Vector3 camForwardFlat = cameraTransform.forward;
        camForwardFlat.y = 0f;
        if (camForwardFlat.sqrMagnitude < 0.0001f) return; // camera looking straight up/down: hold last facing

        Quaternion targetRotation = Quaternion.LookRotation(camForwardFlat.normalized, Vector3.up);

        float t = 1f - Mathf.Exp(-smoothSpeed * Time.deltaTime);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, t);
    }
}