using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Generic health/damage receiver.
///
/// Deliberately has no opinion on presentation (health bars, hit VFX, death animation):
/// it just tracks the number and fires UnityEvents
/// </summary>
[DisallowMultipleComponent]
public class Health : MonoBehaviour, IDamageable
{
    [Header("Health")]
    [SerializeField] private float maxHealth = 100f;

    [Tooltip("If true, TakeDamage is ignored entirely.")]
    [SerializeField] private bool isInvulnerable = false;

    [Tooltip("If true, health is clamped at 1 and OnDeath never fires")]
    [SerializeField] private bool preventDeath = true;

    [Header("Debug")]
    [Tooltip("Logs every hit to the Console.")]
    [SerializeField] private bool logDamageToConsole = true;

    [Header("Events")]
    [Tooltip("Invoked whenever damage is applied. Params: amount dealt, current health.")]
    [SerializeField] private UnityEvent<float, float> onDamaged;

    [Tooltip("Invoked once when health reaches 0 (only if Prevent Death is off).")]
    [SerializeField] private UnityEvent onDeath;

    /// <summary>
    /// Same moment as onDeath above, but a plain C# event instead of an Inspector-wired
    /// UnityEvent for code that wants to subscribe without a designer having to drag
    /// a reference into every prefab (e.g. EnemyAIController shutting itself down).
    /// </summary>
    public event System.Action Died;

    private float currentHealth;
    private bool isDead;

    public float CurrentHealth => currentHealth;
    public float MaxHealth => maxHealth;
    public bool IsDead => isDead;

    private void Awake()
    {
        currentHealth = maxHealth;
    }

    public void TakeDamage(float amount, GameObject source)
    {
        if (isInvulnerable || isDead || amount <= 0f)
            return;

        currentHealth -= amount;

        if (preventDeath)
            currentHealth = Mathf.Max(currentHealth, 1f);
        else
            currentHealth = Mathf.Max(currentHealth, 0f);

        if (logDamageToConsole)
        {
            Debug.Log($"{name} took {amount} damage from {(source != null ? source.name : "unknown")} " +
                $" {currentHealth}/{maxHealth} HP left.", this);
        }

        onDamaged?.Invoke(amount, currentHealth);

        if (!preventDeath && currentHealth <= 0f && !isDead)
        {
            isDead = true;
            onDeath?.Invoke();
            Died?.Invoke();
        }
    }

    /// <summary>Resets health back to full and clears the dead flag.</summary>
    public void ResetHealth()
    {
        currentHealth = maxHealth;
        isDead = false;
    }
}