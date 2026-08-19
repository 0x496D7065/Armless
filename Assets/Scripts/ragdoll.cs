using UnityEngine;

public class Ragdoll : MonoBehaviour
{
    [Header("Joints")]
    public GameObject[] joints;

    [Header("Player Collision")]
    [Tooltip("Root of the player. Colliders under this are cached once at Awake and " +
        "ignored against every ragdoll joint's collider when the ragdoll activates. " +
        "Leave empty to resolve via the 'Player' tag.")]
    [SerializeField] private Transform playerRoot;

    private Collider[] jointColliders;
    private Collider[] playerColliders;

    void Awake()
    {
        if (playerRoot == null)
        {
            var playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
                playerRoot = playerObj.transform;
            else
                Debug.LogError($"{nameof(Ragdoll)}: No Player Root assigned and no " +
                    "GameObject tagged 'Player' was found. Ragdoll won't be able to " +
                    "ignore collision with the player.", this);
        }

        playerColliders = playerRoot != null
            ? playerRoot.GetComponentsInChildren<Collider>()
            : new Collider[0];

        jointColliders = new Collider[joints.Length];
        for (int i = 0; i < joints.Length; i++)
        {
            var rb = joints[i].GetComponent<Rigidbody>();
            if (rb != null)
                rb.isKinematic = true;

            jointColliders[i] = joints[i].GetComponent<Collider>();
        }
    }

    public void ActivateRagdoll()
    {
        foreach (GameObject joint in joints)
        {
            joint.GetComponent<Rigidbody>().isKinematic = false;
        }

        if (playerColliders.Length == 0)
            return;

        foreach (var jointCollider in jointColliders)
        {
            if (jointCollider == null) continue;

            foreach (var playerCollider in playerColliders)
            {
                if (playerCollider == null) continue;
                Physics.IgnoreCollision(jointCollider, playerCollider, true);
            }
        }
    }
}
