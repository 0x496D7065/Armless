using UnityEngine;

public class BladeTrigger : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        Debug.Log("TRIGGER: " + other.name);
        ragdoll ragdollScript = other.gameObject.GetComponentInParent<ragdoll>();

        if (ragdollScript != null)
            ragdollScript.TakeDamage();
    }
}