/// <summary>
/// Exposes the damage value of whatever attack is currently active, so a WeaponHitbox
/// can read it the moment it opens without caring whether the attack is being driven
/// by player input (AttackController) or an enemy AI (EnemyAttackController).
/// </summary>
public interface IAttackDamageSource
{
    float CurrentAttackDamage { get; }
}