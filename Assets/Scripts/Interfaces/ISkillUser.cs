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
    Camp GetCamp(); // 获取单位的阵营
    
    void ScheduleAction(SkillActionData action);
    IEnumerator ExecuteAction(SkillActionData action);
    //void ApplyStatusEffect(StatusEffect statusEffect);
    void PrepareForNextTurn();
}