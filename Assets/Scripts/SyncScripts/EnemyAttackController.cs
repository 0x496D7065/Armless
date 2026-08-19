using UnityEngine;

/// <summary>
/// Default enemy attack: no WeaponData, no combo chain for the common case of an
/// enemy that attacks with its own body (claws, bite, fists, a slam) rather than a
/// separate weapon prop. BeginAttack() plays one animation; OnAttackEnd (Animation
/// Event) reports it's done. Lives directly on the enemy, since there's no weapon
/// object to separate it from.
///
/// Pairs with a WeaponHitbox (the name's generic despite saying "Weapon" it just
/// needs a trigger Collider and something implementing IAttackDamageSource) placed on
/// whichever part of the enemy actually deals the hit, e.g. a hand or mouth bone.
/// </summary>
public class EnemyAttackController : MonoBehaviour, IEnemyAttacker, IAttackDamageSource
{
    private enum State { Idle, Attacking }

    [Header("Attack")]
    [Tooltip("The Animator state to CrossFade into.")]
    [SerializeField] private string animationStateName = "Attack";

    [SerializeField] private float damage = 10f;

    [Tooltip("Safety timeout if OnAttackEnd's Animation Event doesn't fire, so the " +
        "enemy can't get stuck permanently mid-swing.")]
    [SerializeField] private float maxDuration = 2f;

    [SerializeField] private float animationBlendDuration = 0.1f;

    [Header("Animation")]
    [SerializeField] private Animator animator;

    [Tooltip("Damage of the attack currently playing, for WeaponHitbox to read when it opens. 0 if idle.")]
    public float CurrentAttackDamage => state == State.Attacking ? damage : 0f;

    public event System.Action OnAttackFinished;

    private State state = State.Idle;
    private float attackTimer;

    private void Awake()
    {
        if (animator == null)
            Debug.LogError($"{nameof(EnemyAttackController)}: Animator is not assigned.", this);
    }

    private void Update()
    {
        if (state != State.Attacking) return;

        attackTimer += Time.deltaTime;
        if (attackTimer >= maxDuration)
        {
            Debug.LogWarning($"{nameof(EnemyAttackController)}: attack hit its safety " +
                "timeout without an OnAttackEnd Animation Event firing. Did you forget " +
                "to place the event on this clip? Force-ending the attack.", this);
            EndAttack();
        }
    }

    // ---- IEnemyAttacker ----

    public void BeginAttack()
    {
        if (state == State.Attacking) return; // already mid-swing, ignore re-trigger

        state = State.Attacking;
        attackTimer = 0f;
        animator.CrossFade(animationStateName, animationBlendDuration);
    }

    public void CancelAttack()
    {
        // Single-hit attacks are short just let the current one finish naturally
        // rather than cutting the animation. There's no combo chain to stop anyway.
    }

    /// <summary>
    /// Place as an Animation Event at the last frame of the attack clip. Pair with a
    /// WeaponHitbox's OnHitboxOpen/OnHitboxClose events somewhere inside this window to
    /// actually deal damage.
    /// </summary>
    public void OnAttackEnd()
    {
        EndAttack();
    }

    // --------------------------------------------------------------

    private void EndAttack()
    {
        state = State.Idle;
        attackTimer = 0f;
        OnAttackFinished?.Invoke();
    }
}