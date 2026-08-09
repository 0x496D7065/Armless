using UnityEngine;


[RequireComponent(typeof(Rigidbody))]
public class SwordPhysicsFollow : MonoBehaviour
{
    [Header("Target")]
    public Transform target; // SwordAnchor

    [Header("Position Follow")]
    public float positionStrength = 1200f;
    public float positionDamping = 60f;

    [Header("Rotation Follow")]
    public float rotationStrength = 250f;
    public float rotationDamping = 25f;

    [Header("Limits")]
    public float maxForce = 1000f;
    public float maxTorque = 500f;

    private Rigidbody rb;
    private Vector3 physicsTargetPosition;
    private Quaternion physicsTargetRotation;
    private Vector3 cachedTargetPosition;
    private Quaternion cachedTargetRotation;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;

        rb.maxAngularVelocity = 25f;
        rb.angularDamping = 8f;

        physicsTargetPosition = target.position;
        physicsTargetRotation = target.rotation;
    }

    void FixedUpdate()
    {
        physicsTargetPosition = cachedTargetPosition;

        physicsTargetRotation = cachedTargetRotation;

        FollowPosition();
        FollowRotation();
    }

    private void LateUpdate()
    {
        cachedTargetPosition = target.position;
        cachedTargetRotation = target.rotation;
    }

    void FollowPosition()
    {
        Vector3 delta = physicsTargetPosition - rb.position;


        Vector3 force = delta * positionStrength - rb.linearVelocity * positionDamping;

        force = Vector3.ClampMagnitude(force, maxForce);

        rb.AddForce(force, ForceMode.Acceleration);
    }

    void FollowRotation()
    {
        Quaternion deltaRot = physicsTargetRotation * Quaternion.Inverse(rb.rotation);

        deltaRot.ToAngleAxis(out float angle, out Vector3 axis);

        if (angle > 180f)
            angle -= 360f;

        float angleRad = angle * Mathf.Deg2Rad;

        if (Mathf.Abs(angleRad) < 0.001f)
            return;

        Vector3 torque = angleRad * rotationStrength * axis.normalized - rb.angularVelocity * rotationDamping;

        torque = Vector3.ClampMagnitude(torque, maxTorque);

        rb.AddTorque(torque, ForceMode.Acceleration);
    }
}
