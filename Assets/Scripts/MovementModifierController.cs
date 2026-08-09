using UnityEngine;

/// <summary>
/// Owns the combat-driven movement speed multiplier and feeds it into
/// RigidbodyCharacterController.ExternalSpeedMultiplier.
///
/// Combines two sources (equipped weapon baseline + active attack swing), smooths the
/// transition, and exposes a small public API for the AttackController to
/// drive.
/// </summary>
[RequireComponent(typeof(RigidbodyCharacterController))]
public class MovementModifierController : MonoBehaviour
{
    [Header("Smoothing")]
    [Tooltip("How fast the multiplier moves toward its target, in units/second. Higher " +
        "= snappier transitions into/out of an attack's speed penalty.")]
    [SerializeField] private float multiplierSmoothSpeed = 6f;

    private RigidbodyCharacterController characterController;

    // Baseline from the equipped weapon. Set once on equip, no smoothing needed
    // (per design: no mid-fight weapon switching).
    private float equippedMultiplier = 1f;

    // Set by the AttackController while a swing with a speed penalty is playing.
    private bool isAttacking;
    private float currentAttackMultiplier = 1f;

    // The actual value fed to the character controller, smoothed every FixedUpdate.
    private float smoothedMultiplier = 1f;

    private void Awake()
    {
        characterController = GetComponent<RigidbodyCharacterController>();
        characterController.ExternalSpeedMultiplier = () => smoothedMultiplier;
    }

    private void FixedUpdate()
    {
        float target = equippedMultiplier * (isAttacking ? currentAttackMultiplier : 1f);
        smoothedMultiplier = Mathf.MoveTowards(
            smoothedMultiplier, target, multiplierSmoothSpeed * Time.fixedDeltaTime);
    }

    /// <summary>
    /// Call when a weapon is equipped (before entering a fight / between fights).
    /// Applies instantly — no mid-fight weapon switching means no need to smooth this.
    /// </summary>
    public void SetEquippedWeapon(WeaponData weapon)
    {
        equippedMultiplier = weapon != null ? weapon.equippedMoveSpeedMultiplier : 1f;
    }

    /// <summary>
    /// Call from the AttackController when an attack with a movement penalty starts.
    /// Pass the current ComboAttackData's attackMoveSpeedMultiplier.
    /// </summary>
    public void BeginAttackSpeedPenalty(float attackMoveSpeedMultiplier)
    {
        isAttacking = true;
        currentAttackMultiplier = attackMoveSpeedMultiplier;
    }

    /// <summary>
    /// Call from the AttackController when the attack ends (or the combo resets to idle).
    /// Smoothly returns to the equipped-weapon baseline.
    /// </summary>
    public void EndAttackSpeedPenalty()
    {
        isAttacking = false;
        currentAttackMultiplier = 1f;
    }
}