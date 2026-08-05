using UnityEngine;

/// <summary>
/// Data for a single attack in a weapon's combo chain. Pure data
/// the AttackController reads this to know what to play and when
/// the next input is accepted.
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

    [Tooltip("Total duration of this attack, in seconds. Drives timing for the " +
        "combo window and movement speed penalty below.")]
    public float duration = 0.5f;

    [Header("Combo Window")]
    [Tooltip("Normalized time (0-1 of duration) after which pressing attack again " +
        "will queue the next combo attack instead of being ignored.")]
    [Range(0f, 1f)] public float comboWindowStart = 0.5f;

    [Tooltip("Normalized time (0-1 of duration) after which the combo window closes. " +
        "If no input was queued by then, the combo resets to idle.")]
    [Range(0f, 1f)] public float comboWindowEnd = 0.9f;

    [Header("Movement")]
    [Tooltip("Multiplies the character's move speed while this specific attack is " +
        "playing (on top of the weapon's equipped-speed multiplier). 1 = no penalty.")]
    [Range(0f, 1f)] public float attackMoveSpeedMultiplier = 0.6f;

    [Header("Next Attack")]
    [Tooltip("Index into the weapon's combo array to play if the player queues an " +
        "input during this attack's combo window. -1 means this attack ends the combo " +
        "(next input starts back at index 0).")]
    public int nextAttackIndex = -1;

    [Header("Damage (placeholder - not used yet)")]
    [Tooltip("Reserved for future hitbox/damage implementation. Not read by anything yet.")]
    public float damage = 0f;
}