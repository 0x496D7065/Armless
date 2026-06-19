using UnityEngine;

public class AnimatorEventBus : MonoBehaviour
{
    [Header("Reference")]
    [SerializeField] private SwordPose script;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.T))
        {
            transform.localPosition += Vector3.right * 0.2f;
            Debug.Log("Manual move applied");
        }
    }
    void OnAttackStarted()
    {
        if (!script)
            return;
        script.OnAttackStarted();
    }
    void OnAttackFinished()
    {
        if (!script)
            return;
        script.OnAttackFinished();
    }
    void ChainWindowEntered()
    {
        if (!script)
            return;
        script.ChainWindowEntered();
    }
}
