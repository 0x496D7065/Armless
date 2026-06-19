using UnityEngine;

public class ragdoll : MonoBehaviour
{
    public GameObject[] joints;
    //public Animator animator;
    public bool isDead = false;

    void Start()
    {
        foreach (GameObject joint in joints)
        {
            joint.GetComponent<Rigidbody>().isKinematic = true;
        }
    }
    public void TakeDamage()
    {
        Die();
    }

    void Die()
    {
        if (isDead)
            return;
        isDead = true;
        //animator.enabled = false;
        foreach (GameObject joint in joints)
        {
            joint.GetComponent<Rigidbody>().isKinematic = false;
        }
    }
}
