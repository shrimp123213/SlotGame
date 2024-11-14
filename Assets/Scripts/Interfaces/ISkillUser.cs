public interface ISkillUser
{
    bool CanMoveForward();
    void MoveForward();
    void PerformMeleeAttack(TargetType targetType);
    void PerformRangedAttack(TargetType targetType);
    void IncreaseDefense(int value, TargetType targetType);
    void TakeDamage(int damage);
    void Heal(int amount);
}