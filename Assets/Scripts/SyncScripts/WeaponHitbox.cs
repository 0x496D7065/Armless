using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// The weapon's actual damage-dealing hitbox. Lives on the weapon alongside
/// AttackController (same object or a child of it), and reads the current attack's
/// damage from AttackController when it opens. (placeholder until proper dmg calculations)
///
/// TIMING IS EVENT-DRIVEN, LIKE AttackController: OnHitboxOpen() and OnHitboxClose()
/// are meant to be called via Animation Events on the attack's clip
///
/// SETUP REMINDER PER CLIP: place an OnHitboxOpen event where the blade should start
/// dealing damage, and OnHitboxClose where it should stop.
///
/// Requires a Collider on this GameObject, set to Is Trigger (this script forces that
/// at runtime).
/// Unity trigger events need a Rigidbody on at least one side of the collision, if hits
/// aren't registering, check that either this weapon's hierarchy or the target has one
/// (a kinematic Rigidbody on the target is enough, and won't fight the character
/// controller's own physics).
/// </summary>
[RequireComponent(typeof(Collider))]
public class WeaponHitbox : MonoBehaviour
{
    [Header("Source")]
    [Tooltip("The AttackController driving this weapon. Used to read the current " +
        "attack's damage the moment the hitbox opens.")]
    [SerializeField] private AttackController attackController;

    [Header("Owner")]
    [Tooltip("Root of the character wielding this weapon. Colliders under this root " +
        "are ignored, so the weapon can never hit its own wielder. Also passed along " +
        "as the damage source. Leave empty if not needed yet.")]
    [SerializeField] private Transform ownerRoot;

    private Collider hitboxCollider;
    private bool isOpen;
    private float currentDamage;

    // Cleared every time the hitbox opens, so a single swing can only hit each target
    // once even if the trigger overlap lingers across several physics frames.
    private readonly HashSet<IDamageable> hitTargetsThisWindow = new HashSet<IDamageable>();

    private void Awake()
    {
        hitboxCollider = GetComponent<Collider>();
        hitboxCollider.isTrigger = true;
        hitboxCollider.enabled = false; // closed until an Animation Event opens it

        if (attackController == null)
            Debug.LogError($"{nameof(WeaponHitbox)}: Attack Controller is not assigned.", this);
    }

    // ---- Called via Animation Events placed on each attack's clip ----

    /// <summary>Placed as an Animation Event where the blade should start dealing damage.</summary>
    public void OnHitboxOpen()
    {
        currentDamage = attackController != null ? attackController.CurrentAttackDamage : 0f;
        hitTargetsThisWindow.Clear();
        isOpen = true;
        hitboxCollider.enabled = true;
    }

    /// <summary>Placed as an Animation Event where the blade should stop dealing damage.</summary>
    public void OnHitboxClose()
    {
        isOpen = false;
        hitboxCollider.enabled = false;
    }

    // --------------------------------------------------------------

    private void OnTriggerEnter(Collider other)
    {
        if (!isOpen)
            return;
        Debug.Log("Collider Triggered");
        if (ownerRoot != null && other.transform.IsChildOf(ownerRoot))
            return;

        var damageable = other.GetComponentInParent<IDamageable>();
        if (damageable == null)
            return;

        if (!hitTargetsThisWindow.Add(damageable))
            return; // already hit this target during this hitbox activation

        damageable.TakeDamage(currentDamage, ownerRoot != null ? ownerRoot.gameObject : gameObject);
    }
}