using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Skill", menuName = "ScriptableObjects/Skill")]
public class SkillSO : ScriptableObject
{
    public string skillName; // 技能名称
    public List<SkillActionData> actions; // 技能包含的所有动作

    /// <summary>
    /// 执行技能动作
    /// </summary>
    /// <param name="unit">执行技能的用户（单位或建筑物）</param>
    public void Execute(ISkillUser user)
    {
        if (user == null)
        {
            Debug.LogError($"SkillSO: {skillName} 试图执行时，用户为 null！");
            return;
        }

        if (SkillManager.Instance != null)
        {
            SkillManager.Instance.ExecuteSkill(this, user);
        }
        else
        {
            Debug.LogError("SkillSO: SkillManager 实例未找到！");
        }
    }
}