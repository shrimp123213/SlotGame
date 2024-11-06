public interface ISkillUser
{
    bool CanMoveForward();
    void MoveForward();
    void PerformMeleeAttack();
    void PerformRangedAttack();
    void IncreaseDefense(int value);
    void TakeDamage(int damage);
    void Heal(int amount);
}