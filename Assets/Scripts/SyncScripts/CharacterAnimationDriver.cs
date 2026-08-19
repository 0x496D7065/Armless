using UnityEngine;

/// <summary>
/// Feeds the character-relative move input into the Model's Animator to drive the
/// Locomotion blend tree. Lives on PlayerRoot rather than the Model itself, matching
/// AttackController's pattern of a serialized Animator reference instead of
/// RequireComponent, since the Animator lives on a different GameObject (Model).
/// </summary>
public class CharacterAnimationDriver : MonoBehaviour
{
    [SerializeField] private RigidbodyCharacterController characterController;
    [SerializeField] private Animator animator;

    [Tooltip("How quickly MoveX/MoveY ease toward the target input, in seconds. " +
        "Uses Animator's built-in SetFloat damping rather than manual smoothing.")]
    [SerializeField] private float dampTime = 0.15f;

    private static readonly int MoveXHash = Animator.StringToHash("MoveX");
    private static readonly int MoveYHash = Animator.StringToHash("MoveY");

    private void Awake()
    {
        if (characterController == null)
            Debug.LogError($"{nameof(CharacterAnimationDriver)}: Character Controller is not assigned.", this);
        if (animator == null)
            Debug.LogError($"{nameof(CharacterAnimationDriver)}: Animator is not assigned.", this);
    }

    private void Update()
    {
        if (characterController == null || animator == null) return;

        Vector2 input = characterController.MoveInput;
        animator.SetFloat(MoveXHash, input.x, dampTime, Time.deltaTime);
        animator.SetFloat(MoveYHash, input.y, dampTime, Time.deltaTime);
    }
}