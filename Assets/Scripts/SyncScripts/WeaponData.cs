using UnityEngine;

/// <summary>
/// Defines a weapon: its combo chain and movement behavior. Every weapon is a separate
/// asset instance of this SAME type (Create > Weapons > Weapon Data)
/// </summary>
[CreateAssetMenu(fileName = "New Weapon", menuName = "Weapons/Weapon Data")]
public class WeaponData : ScriptableObject
{
    [Header("Identity")]
    public string weaponName = "Weapon";
    public Sprite icon;

    [Header("Movement")]
    [Tooltip("Multiplies the character's move speed at all times while this weapon is " +
        "equipped (idle or attacking). 1 = no penalty. Stacks with the per-attack " +
        "multiplier below during swings.")]
    [Range(0f, 1f)] public float equippedMoveSpeedMultiplier = 1f;

    [Header("Combo Chain")]
    [Tooltip("The full set of attacks for this weapon. Index 0 is always the combo's " +
        "starting attack.")]
    public ComboAttackData[] combos;
}