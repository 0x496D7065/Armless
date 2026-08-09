using UnityEngine;

/// <summary>
/// Data for a single attack in a weapon's combo chain. Timing (combo window open/close,
/// attack end) is driven by Animation Events placed on the clip itself — NOT by fields
/// here — so behavior stays correct even if attack speed changes (animator.speed on a
/// dedicated combat layer, etc). See AttackController.OnComboWindowOpen/Close/OnAttackEnd.
/// </summary>
[System.Serializable]
public class ComboAttackData
{
    [Header("Identity")]
    [Tooltip("Used for debugging/animator triggers, not gameplay logic.")]
    public string attackName = "Attack";

    [Header("Animation")]
    [Tooltip("Name of the Animator state to CrossFade into for this attack.")]
    public string animationStateName;

    [Header("Safety Timeout")]
    [Tooltip("Fallback")]
    public float maxDuration = 3f;

    [Header("Movement")]
    [Tooltip("Multiplies the character's move speed while this specific attack is " +
        "playing (on top of the weapon's equipped-speed multiplier). 1 = no penalty.")]
    [Range(0f, 1f)] public float attackMoveSpeedMultiplier = 0.6f;

    [Header("Next Attack")]
    [Tooltip("Index into the weapon's combo array to play if the player queues an " +
        "input while the combo window is open (see Animation Events). -1 means this " +
        "attack ends the combo (next input starts back at index 0).")]
    public int nextAttackIndex = -1;

    [Header("Damage (placeholder - not used yet)")]
    [Tooltip("Reserved for future hitbox/damage implementation. Not read by anything yet.")]
    public float damage = 0f;
}