using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Skill", menuName = "ScriptableObjects/Skill")]
public class SkillSO : ScriptableObject
{
    public string skillName;                // 技能名称
    public List<SkillActionData> actions;   // 技能包含的所有动作

    /// <summary>
    /// 执行技能动作
    /// </summary>
    /// <param name="unit">执行技能的单位</param>
    public void Execute(UnitController unit, SkillManager skillManager)
    {
        skillManager.ExecuteSkill(this, unit);
    }
}