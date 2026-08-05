using UnityEngine;
using Unity.Cinemachine;


[RequireComponent(typeof(Rigidbody))]
public class SwordKinematicFollow : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform target; // SwordAnchor

    private Rigidbody rb;
    void Awake()
    {
        rb = GetComponent<Rigidbody>();

        CinemachineCore.CameraUpdatedEvent.AddListener(OnCameraUpdated);
    }

    private void OnDisable()
    {
        CinemachineCore.CameraUpdatedEvent.RemoveListener(OnCameraUpdated);
    }

    void OnCameraUpdated(CinemachineBrain Brain)
    {
        transform.SetPositionAndRotation(target.position, target.rotation);
    }
}
