using UnityEngine;

/// <summary>
/// 控制Boss单位的行为
/// </summary>
public class BossController : UnitController
{
    public SkillSO bossAbilitySkillSO; // Boss 特有的技能

    /// <summary>
    /// 初始化Boss单位
    /// </summary>
    protected override void Init()
    {
        base.Init(); // 调用UnitController的Init方法

        // 初始化Boss特有的技能
        if (bossAbilitySkillSO != null)
        {
            // 可以在这里添加额外的初始化逻辑
            Debug.Log($"Boss单位 {unitData.unitName} 初始化Boss特有的技能！");
        }
        else
        {
            Debug.LogWarning($"Boss单位 {unitData.unitName} 没有配置Boss特有的能力技能！");
        }
    }

    /// <summary>
    /// 使用主技能或支援技能
    /// </summary>
    public override void UseMainSkillOrSupport()
    {
        // Boss在使用技能时，优先执行Boss特有的能力
        if (CanUseBossAbility())
        {
            ExecuteBossAbility();
        }
        else
        {
            base.UseMainSkillOrSupport();
        }
    }

    /// <summary>
    /// 判断Boss是否可以使用其特有能力
    /// </summary>
    /// <returns></returns>
    private bool CanUseBossAbility()
    {
        // 定义Boss使用特有能力的条件，例如生命值低于一定值
        // 这里以生命值低于50%为例
        return unitData.health <= unitData.maxHealth / 2 && bossAbilitySkillSO != null;
    }

    /// <summary>
    /// 执行Boss的特有能力
    /// </summary>
    public void ExecuteBossAbility()
    {
        if (bossAbilitySkillSO != null)
        {
            // 使用SkillManager执行Boss的特有技能
            SkillManager.Instance.ExecuteSkill(bossAbilitySkillSO, this);
            Debug.Log($"{unitData.unitName} 执行了Boss特有的能力！");
        }
        else
        {
            Debug.LogWarning($"{unitData.unitName} 没有配置Boss特有的能力技能！");
        }
    }

    // 如果Boss有其他特有的方法，可以在这里添加
}