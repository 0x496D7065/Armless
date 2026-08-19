using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Top-level enemy brain: a state machine that turns "where's the player" into
/// NavMeshAgent movement, Animator parameters, and attack triggers.
///
/// AWARENESS: no stealth/detection system by design the enemy always knows the
/// player's position (playerTarget is resolved once at spawn via the "Player" tag, or
/// assign it directly in the Inspector for pre-placed enemies).
///
/// PATHFINDING & AVOIDANCE: uses Unity's NavMeshAgent for both. Obstacle geometry
/// avoidance comes from the baked NavMesh; avoidance of OTHER agents (enemies/allies
/// also on NavMeshAgents, so they don't clip into each other) is handled automatically
/// by NavMeshAgent's built-in local avoidance no custom steering code needed here.
/// Tune "Obstacle Avoidance Type" and "Avoidance Priority" on each agent's Inspector;
/// give different priorities to different enemy types if some should yield to others.
///
/// JUMPING (RESERVED, NOT ACTIVE YET): off-mesh links are how a NavMeshAgent crosses
/// gaps it can't walk across (e.g. jump-down or jump-across links baked between
/// platforms). autoTraverseOffMeshLink is left TRUE for now, so the agent already
/// crosses these automatically in a straight line the instant it's faster than going
/// around "uses jumps to get there faster" already works, just without a jump
/// animation. When ready to add one:
///   1. Set agent.autoTraverseOffMeshLink = false (Inspector or Awake).
///   2. In Update, check agent.isOnOffMeshLink and route into a new
///      HandleOffMeshLink() that calls SetState(EnemyState.Jump), plays the jump clip,
///      and same animation-event pattern as AttackController waits for an
///      OnJumpLand event to call agent.CompleteOffMeshLink() instead of guessing timing.
/// The Jump state and IsJumping animator param already exist and are wired into the
/// param feed below, so that change is contained to this one hook.
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
public class EnemyAIController : MonoBehaviour
{
    public enum EnemyState { Idle, Chase, Attack, Jump }

    [Header("Target")]
    [Tooltip("The player to chase/attack. If left empty, resolved once at Awake via " +
        "the 'Player' tag.")]
    [SerializeField] private Transform playerTarget;

    [Header("Ranges")]
    [Tooltip("Distance at which the enemy stops chasing and enters the Attack state.")]
    [SerializeField] private float attackRange = 2f;
    [Tooltip("Extra distance beyond attackRange before dropping back to Chase, so the " +
        "enemy doesn't flicker between the two states right at the boundary.")]
    [SerializeField] private float attackRangeBuffer = 0.3f;

    [Header("Movement")]
    [Tooltip("How often (seconds) the destination is re-issued to the NavMeshAgent " +
        "while chasing. Doesn't need to be every frame the player rarely moves far " +
        "in a fraction of a second, and this saves pathfinding cost with many enemies.")]
    [SerializeField] private float destinationUpdateInterval = 0.2f;

    [Tooltip("How fast the enemy turns to face the player while attacking, in " +
        "degrees/second. NavMeshAgent handles rotation on its own during Chase.")]
    [SerializeField] private float attackFacingTurnSpeed = 360f;

    [Header("Animation")]
    [SerializeField] private Animator animator;
    [Tooltip("Float param, roughly 0-1, fed from the agent's current speed vs its max " +
        "speed. Drive an Idle/Walk/Run blend tree from this.")]
    [SerializeField] private string speedParam = "Speed";
    [Tooltip("Bool param, true whenever state == Attack.")]
    [SerializeField] private string isAttackingParam = "IsAttacking";
    [Tooltip("Bool param, true whenever state == Jump. Not driven by any transition " +
        "yet (see class docs), but the Animator can already be wired to it.")]
    [SerializeField] private string isJumpingParam = "IsJumping";

    [Header("Combat")]
    [Tooltip("Whatever this enemy uses to actually deal damage usually an " +
        "EnemyAttackController on the enemy itself, or an EnemyWeaponAttackController " +
        "on a weapon prop for enemies that carry one. Drag the component in here " +
        "it's referenced through IEnemyAttacker so either works without changing this " +
        "script. Leave empty for a non-attacking enemy.")]
    [SerializeField] private MonoBehaviour attackerBehaviour;
    private IEnemyAttacker attacker;

    private NavMeshAgent agent;
    private Health health;
    private EnemyState state = EnemyState.Idle;
    private float destinationTimer;

    public EnemyState State => state;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();

        health = GetComponent<Health>();
        if (health == null)
            Debug.LogError($"{nameof(EnemyAIController)}: No Health component found on " +
                "this GameObject the enemy will never stop computing on death.", this);

        if (playerTarget == null)
        {
            var playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
                playerTarget = playerObj.transform;
            else
                Debug.LogError($"{nameof(EnemyAIController)}: No Player Target assigned " +
                    "and no GameObject tagged 'Player' was found.", this);
        }

        if (animator == null)
            Debug.LogError($"{nameof(EnemyAIController)}: Animator is not assigned.", this);

        if (attackerBehaviour != null)
        {
            attacker = attackerBehaviour as IEnemyAttacker;
            if (attacker == null)
                Debug.LogError($"{nameof(EnemyAIController)}: Attacker Behaviour is " +
                    $"assigned but doesn't implement {nameof(IEnemyAttacker)}.", this);
        }

        // No detection system by design: an enemy that exists is already "aware".
        SetState(EnemyState.Chase);
    }

    private void OnEnable()
    {
        if (attacker != null)
            attacker.OnAttackFinished += HandleAttackFinished;

        if (health != null)
            health.Died += HandleDeath;
    }

    private void OnDisable()
    {
        if (attacker != null)
            attacker.OnAttackFinished -= HandleAttackFinished;

        if (health != null)
            health.Died -= HandleDeath;
    }

    /// <summary>
    /// Fired once by Health.Died when this enemy's HP hits 0 (requires Prevent Death to
    /// be off on its Health component it's on by default, which never fires this).
    /// Stops the agent and disables this component so Update() never runs again
    /// </summary>
    private void HandleDeath()
    {
        // Stop the attack/combo chain immediately
        attacker?.CancelAttack();

        agent.isStopped = true;
        agent.velocity = Vector3.zero;
        agent.enabled = false; // stop nav computation entirely, not just movement

        if (animator != null)
        {
            animator.SetBool(isAttackingParam, false);
            animator.SetBool(isJumpingParam, false);
        }

        // Disabling this component is what stops Update() from running at all.
        // OnDisable() fires as a result, which also unsubscribes from Died and
        // attacker.OnAttackFinished automatically.
        enabled = false;
    }

    private void Update()
    {
        if (playerTarget == null) return;

        switch (state)
        {
            case EnemyState.Chase:
                TickChase();
                break;
            case EnemyState.Attack:
                TickAttack();
                break;
            case EnemyState.Jump:
                // Reserved see class docs. Currently unreachable: autoTraverseOffMeshLink
                // is true and nothing transitions into this state yet.
                break;
            case EnemyState.Idle:
                break;
        }

        UpdateAnimator();
    }

    private void TickChase()
    {
        float distance = Vector3.Distance(transform.position, playerTarget.position);
        if (distance <= attackRange)
        {
            SetState(EnemyState.Attack);
            return;
        }

        destinationTimer -= Time.deltaTime;
        if (destinationTimer <= 0f)
        {
            agent.SetDestination(playerTarget.position);
            destinationTimer = destinationUpdateInterval;
        }
    }

    private void TickAttack()
    {
        // Agent is stopped during Attack (see SetState), so rotation isn't driven by
        // the NavMeshAgent here face the player manually instead.
        Vector3 toPlayer = playerTarget.position - transform.position;
        toPlayer.y = 0f;
        if (toPlayer.sqrMagnitude > 0.001f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(toPlayer);
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation, targetRotation, attackFacingTurnSpeed * Time.deltaTime);
        }

        float distance = Vector3.Distance(transform.position, playerTarget.position);
        if (distance > attackRange + attackRangeBuffer)
            SetState(EnemyState.Chase);
    }

    private void HandleAttackFinished()
    {
        // Only re-evaluate state once the attacker reports its combo is fully done
        // never mid-swing, same "don't interrupt the animation" spirit as
        // AttackController's own combo handling.
        float distance = playerTarget != null
            ? Vector3.Distance(transform.position, playerTarget.position)
            : Mathf.Infinity;
        SetState(distance <= attackRange + attackRangeBuffer ? EnemyState.Attack : EnemyState.Chase);
    }

    private void UpdateAnimator()
    {
        if (animator == null) return;

        float normalizedSpeed = agent.speed > 0f ? agent.velocity.magnitude / agent.speed : 0f;
        animator.SetFloat(speedParam, normalizedSpeed);
        animator.SetBool(isAttackingParam, state == EnemyState.Attack);
        animator.SetBool(isJumpingParam, state == EnemyState.Jump);
    }

    private void SetState(EnemyState newState)
    {
        if (state == newState) return;

        if (state == EnemyState.Attack && newState != EnemyState.Attack)
            attacker?.CancelAttack();

        state = newState;

        switch (state)
        {
            case EnemyState.Chase:
                agent.isStopped = false;
                destinationTimer = 0f; // force an immediate destination update
                break;
            case EnemyState.Attack:
                agent.isStopped = true;
                agent.velocity = Vector3.zero;
                attacker?.BeginAttack();
                break;
            case EnemyState.Jump:
                agent.isStopped = true;
                break;
            case EnemyState.Idle:
                agent.isStopped = true;
                break;
        }
    }
}