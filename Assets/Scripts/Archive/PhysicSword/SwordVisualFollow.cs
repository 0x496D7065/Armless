using UnityEngine;

public class SwordVisualFollow : MonoBehaviour
{
    [Header("Target")]
    public Transform target; // PhysicSword

    [Header("Smoothing")]
    public float positionSharpness = 30f;
    public float rotationSharpness = 30f;

    private void LateUpdate()
    {
        float posT = 1f - Mathf.Exp(-positionSharpness * Time.deltaTime);
        float rotT = 1f - Mathf.Exp(-rotationSharpness * Time.deltaTime);

        
        transform.SetPositionAndRotation(Vector3.Lerp(transform.position, target.position, posT), Quaternion.Slerp( transform.rotation, target.rotation, rotT));
    }
}
