using UnityEngine;

/// <summary>
/// Implemented by anything that can take combat damage
/// </summary>
public interface IDamageable
{
    void TakeDamage(float amount, GameObject source);
}