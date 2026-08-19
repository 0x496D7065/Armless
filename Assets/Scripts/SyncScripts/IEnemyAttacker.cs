using System;

/// <summary>
/// Minimal contract between EnemyAIController and EnemyAttackController. Kept as an
/// interface so any attack implementation can be swapped in without touching the AI
/// state machine.
/// </summary>
public interface IEnemyAttacker
{
    /// <summary>Start attacking. Called once, on entering the Attack state.</summary>
    void BeginAttack();

    /// <summary>
    /// Stop attacking as soon as it's safe to (implementations should let the current
    /// swing finish rather than cutting the animation see EnemyAttackController).
    /// </summary>
    void CancelAttack();

    /// <summary>
    /// Raised once the current attack (and any queued combo chain) has fully finished
    /// on its own, so the AI can decide whether to attack again or go back to chasing.
    /// </summary>
    event Action OnAttackFinished;
}