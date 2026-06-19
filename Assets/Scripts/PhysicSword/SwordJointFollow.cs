using UnityEngine;

[RequireComponent(typeof(ConfigurableJoint))]
public class SwordJointFollow : MonoBehaviour
{
    public Transform target;

    private ConfigurableJoint joint;

    void Awake()
    {
        joint = GetComponent<ConfigurableJoint>();
    }

    void FixedUpdate()
    {
        joint.targetPosition = transform.InverseTransformPoint(target.position);

        Quaternion targetRot = Quaternion.Inverse(transform.rotation) * target.rotation;
        joint.targetRotation = targetRot;
    }
}