using UnityEngine;

public class Ragdoll : MonoBehaviour
{
    public GameObject[] joints;
    //public Animator animator;

    void Start()
    {
        foreach (GameObject joint in joints)
        {
            joint.GetComponent<Rigidbody>().isKinematic = true;
        }
    }

    public void ActivateRagdoll()
    {
        foreach (GameObject joint in joints)
        {
            joint.GetComponent<Rigidbody>().isKinematic = false;
        }
    }
}
