using UnityEngine;

public class SwordScreenOffset : MonoBehaviour
{
    [Header("Movement")]
    public float sensitivity = 0.3f;
    public float maxOffset = 0.5f;

    [Header("Smoothing")]
    public float smoothSpeed = 10f;
    public float rotationSmoothSpeed = 10f;

    [Header("Rotation Blending")]
    public AnchorPose Origin;
    public AnchorPose poseA;
    public AnchorPose poseB;

    private Vector3 targetLocalPos;
    private Vector3 currentLocalPos;

    [System.Serializable]
    public class AnchorPose
    {
        public Vector3 localPosition;      // Where this pose lives in local space
        public Vector3 localEulerRotation; // Rotation for this pose
    }

    void Start()
    {
        currentLocalPos = transform.localPosition;
        targetLocalPos = currentLocalPos;
    }

    void LateUpdate()
    {
        float mouseX = Input.GetAxis("Mouse X");
        float mouseY = Input.GetAxis("Mouse Y");

        targetLocalPos += new Vector3(mouseX, mouseY, 0f) * sensitivity;

        // Clamp movement area
        targetLocalPos.x = Mathf.Clamp(targetLocalPos.x, -maxOffset, maxOffset);
        targetLocalPos.y = Mathf.Clamp(targetLocalPos.y, -maxOffset, maxOffset);

        currentLocalPos = Vector3.Lerp(
            currentLocalPos,
            targetLocalPos,
            Time.deltaTime * smoothSpeed
        );

        transform.localPosition = currentLocalPos;
        UpdateRotation();
    }

    void UpdateRotation()
    {
        Vector2 current2D = new Vector2(
            currentLocalPos.x,
            currentLocalPos.y
        );

        float distA = Vector2.Distance(current2D, poseA.localPosition);
        float distB = Vector2.Distance(current2D, poseB.localPosition);

        float total = distA + distB;

        float t = 0.5f;

        if (total > 0.0001f)
            t = distA / total;

        Quaternion rotA = Quaternion.Euler(poseA.localEulerRotation);
        Quaternion rotB = Quaternion.Euler(poseB.localEulerRotation);

        Quaternion targetRot = Quaternion.Slerp(rotA, rotB, t);

        transform.localRotation = Quaternion.Slerp(
            transform.localRotation,
            targetRot,
            Time.deltaTime * rotationSmoothSpeed
        );
    }
}
