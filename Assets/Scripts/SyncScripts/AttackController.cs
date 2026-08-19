using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Drives the combo state machine: reads attack input, decides when a queued press
/// advances the combo vs. starts over, plays the corresponding animation, and reports
/// speed penalties to the MovementModifierController. No hitboxes or damage yet —
/// timing/animation/controls only, per current scope.
///
/// TIMING IS EVENT-DRIVEN: OnComboWindowOpen(), OnComboWindowClose(), and OnAttackEnd()
/// are meant to be called via Animation Events placed on each attack's clip (see the
/// Events section in the clip's import settings, or the Animation window for clips
/// authored in-engine). This keeps timing frame-accurate and safe against future
/// attack-speed changes (animator.speed), instead of guessing with a wall-clock timer.
///
/// SETUP REMINDER PER CLIP: place an OnComboWindowOpen event where the combo window
/// should open, OnComboWindowClose where it should close, and OnAttackEnd at the last
/// frame. If OnAttackEnd is forgotten, a safety timeout (ComboAttackData.maxDuration)
/// force-ends the attack and logs a warning instead of soft-locking the character.
///
/// LIVES ON THE WEAPON, NOT THE CHARACTER: this is designed to sit on whatever object
/// actually owns the attack animation (e.g. a floating sword with its own Animator),
/// which may be a different GameObject than the character it affects. Both the Animator
/// and the MovementModifierController are plain serialized references (not
/// GetComponent/RequireComponent) for exactly that reason. Wire "animator" to this
/// object's own Animator, and "movementModifier" to the character's
/// MovementModifierController — either by dragging it in the Inspector if the weapon is
/// a fixed scene object, or via Initialize() at spawn/equip time if weapons are
/// instantiated dynamically.
/// </summary>
public class AttackController : MonoBehaviour, IAttackDamageSource
{
    private enum State { Idle, Attacking }

    [Header("Input")]
    [SerializeField] private InputActionReference attackActionReference;

    [Header("Weapon")]
    [Tooltip("Assigned before entering a fight (or swapped instantly between fights). " +
        "No mid-fight switching support.")]
    [SerializeField] private WeaponData equippedWeapon;

    [Tooltip("Damage of the attack currently playing, for WeaponHitbox to read when it opens. 0 if idle.")]
    public float CurrentAttackDamage => CurrentAttack?.damage ?? 0f;

    [Header("Animation")]
    [Tooltip("The Animator that plays this weapon's attack clips. Usually lives on the " +
        "same object as this component (the weapon), NOT the character.")]
    [SerializeField] private Animator animator;

    [Tooltip("Crossfade blend duration when transitioning into each attack's animation state.")]
    [SerializeField] private float animationBlendDuration = 0.1f;

    [Header("Character Link")]
    [Tooltip("The character's MovementModifierController. Lives on a DIFFERENT " +
        "GameObject than this component if the weapon and character are separate " +
        "(e.g. a floating weapon). Assign in the Inspector for a fixed scene setup, " +
        "or via Initialize() if the weapon is spawned/equipped dynamically.")]
    [SerializeField] private MovementModifierController movementModifier;

    [Header("Input Buffering")]
    [Tooltip("If the player presses attack BEFORE the combo window opens (common when " +
    "spamming), the press is remembered for this many seconds. If the window opens " +
    "within that time, the press still counts as queuing the next attack, instead " +
    "of being silently dropped.")]
    [SerializeField] private float inputBufferTime = 0.15f;

    private InputAction attackAction;

    private State state = State.Idle;
    private int currentAttackIndex = -1;
    private float attackTimer;          // only used for the safety timeout, not real timing
    private bool comboWindowOpen;
    private bool nextAttackQueued;

    private float lastAttackPressTime = -999f;

    private ComboAttackData CurrentAttack =>
        (equippedWeapon != null && currentAttackIndex >= 0 && currentAttackIndex < equippedWeapon.combos.Length)
            ? equippedWeapon.combos[currentAttackIndex]
            : null;

    private void Awake()
    {
        attackAction = attackActionReference != null ? attackActionReference.action : null;
        if (attackAction == null)
            Debug.LogError($"{nameof(AttackController)}: Attack Action Reference is not assigned.", this);

        if (animator == null)
            Debug.LogError($"{nameof(AttackController)}: Animator is not assigned.", this);
        if (movementModifier == null)
            Debug.LogError($"{nameof(AttackController)}: Movement Modifier is not assigned. " +
                "If this weapon is instantiated at runtime, call Initialize() before use.", this);

        if (equippedWeapon != null && movementModifier != null)
            movementModifier.SetEquippedWeapon(equippedWeapon);
    }

    /// <summary>
    /// Use when the weapon is instantiated/equipped at runtime rather than pre-wired in
    /// the Inspector (e.g. spawning a sword prefab and attaching it to a character that
    /// wasn't known at edit time). Call this once, right after instantiating, before the
    /// player can attack.
    /// </summary>
    public void Initialize(MovementModifierController targetMovementModifier)
    {
        movementModifier = targetMovementModifier;
        if (equippedWeapon != null)
            movementModifier.SetEquippedWeapon(equippedWeapon);
    }

    private void OnEnable()
    {
        if (attackAction != null)
        {
            attackAction.Enable();
            attackAction.performed += OnAttackPerformed;
        }
    }

    private void OnDisable()
    {
        if (attackAction != null)
        {
            attackAction.performed -= OnAttackPerformed;
            attackAction.Disable();
        }
    }

    /// <summary>
    /// Assign a weapon before entering a fight, or instantly between fights.
    /// Not designed to be called mid-combo/mid-fight.
    /// </summary>
    public void EquipWeapon(WeaponData weapon)
    {
        if (equippedWeapon != null)
        {
            equippedWeapon = weapon;
            movementModifier.SetEquippedWeapon(weapon);
        }
    }

    private void OnAttackPerformed(InputAction.CallbackContext context)
    {
        if (equippedWeapon == null || equippedWeapon.combos == null || equippedWeapon.combos.Length == 0)
            return;

        if (state == State.Idle)
        {
            StartAttack(0);
            return;
        }

        // Already attacking: only queue the press if the combo window is currently open
        // (per Animation Events on the clip) AND this attack actually leads somewhere.
        ComboAttackData current = CurrentAttack;
        if (current == null || current.nextAttackIndex < 0)
            return;

        if (comboWindowOpen)
        {
            nextAttackQueued = true;
        }
        else
        {
            // Too early (window not open yet) — buffer it. OnComboWindowOpen will check
            // for this and honor it retroactively if it opens soon enough.
            lastAttackPressTime = Time.time;
        }
    }

    private void Update()
    {
        if (state != State.Attacking)
            return;

        // Safety-net only: real attack timing comes from Animation Events below.
        ComboAttackData current = CurrentAttack;
        if (current == null)
            return;

        if (comboWindowOpen && current.nextAttackIndex >= 0 &&
            attackAction != null && attackAction.IsPressed())
        {
            nextAttackQueued = true;
        }

        attackTimer += Time.deltaTime;
        if (attackTimer >= current.maxDuration)
        {
            Debug.LogWarning($"{nameof(AttackController)}: '{current.attackName}' " +
                $"(state '{current.animationStateName}') hit its safety timeout without " +
                $"an OnAttackEnd Animation Event firing. Did you forget to place the " +
                $"event on this clip? Force-ending the attack.", this);
            EndCombo();
        }
    }

    // ---- Called via Animation Events placed on each attack's clip ----

    /// <summary>Place as an Animation Event where the combo window should open.</summary>
    public void OnComboWindowOpen()
    {
        comboWindowOpen = true;

        // Honor an early press that arrived just before the window opened.
        if (Time.time - lastAttackPressTime <= inputBufferTime)
            nextAttackQueued = true;
    }

    /// <summary>
    /// Place as an Animation Event where the combo window should close. This is also
    /// the chain decision point: if a next attack was queued, it starts RIGHT HERE via
    /// CrossFade, which cuts this clip's remaining follow-through short by blending
    /// straight into the next attack's windup. If nothing was queued, this clip is left
    /// to play out its full follow-through naturally, ending at OnAttackEnd().
    /// </summary>
    public void OnComboWindowClose()
    {
        comboWindowOpen = false;

        ComboAttackData current = CurrentAttack;
        if (current != null && nextAttackQueued && current.nextAttackIndex >= 0)
            StartAttack(current.nextAttackIndex);
    }

    /// <summary>
    /// Place as an Animation Event at the last frame of the clip. Only reached when the
    /// window closed WITHOUT a queued chain (or this attack has no next attack) — if a
    /// chain did happen, this event never fires, because the Animator already blended
    /// into the next attack's state before playback would reach it.
    /// </summary>
    public void OnAttackEnd()
    {
        EndCombo();
    }

    // --------------------------------------------------------------

    private void StartAttack(int index)
    {
        currentAttackIndex = index;
        attackTimer = 0f;
        comboWindowOpen = false;
        nextAttackQueued = false;
        lastAttackPressTime = -999f; // clear so a stale buffered press can't leak
        state = State.Attacking;

        ComboAttackData attack = CurrentAttack;
        animator.CrossFade(attack.animationStateName, animationBlendDuration);
        movementModifier.BeginAttackSpeedPenalty(attack.attackMoveSpeedMultiplier);
    }

    private void EndCombo()
    {
        state = State.Idle;
        currentAttackIndex = -1;
        attackTimer = 0f;
        comboWindowOpen = false;
        nextAttackQueued = false;
        movementModifier.EndAttackSpeedPenalty();
    }
}