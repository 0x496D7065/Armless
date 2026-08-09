using UnityEngine;

public class BladeTrigger : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        Debug.Log("TRIGGER: " + other.name);
        Ragdoll ragdollScript = other.gameObject.GetComponentInParent<Ragdoll>();

        //if (ragdollScript != null)
            //ragdollScript.TakeDamage();
    }
}