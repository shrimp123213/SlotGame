using System.Collections;

public interface ISkillUser
{
    bool CanMoveForward();
    IEnumerator MoveForward();
    IEnumerator PerformMeleeAttack(TargetType targetType);
    IEnumerator PerformRangedAttack(TargetType targetType);
    IEnumerator IncreaseDefense(int value, TargetType targetType);
    IEnumerator PerformBreakage(int breakagePoints);
    void TakeDamage(int damage, DamageSource source = DamageSource.Normal);
    void Heal(int amount);
}