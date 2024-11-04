using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewUnitData", menuName = "Data/Unit")]
public class UnitData : BaseData
{
    public PositionType positionType;        // 可放置位置
    public int level;                        // 等級
    public UnitType unitType;                // 單位類型（玩家、敵人等）
    public List<Skill> mainSkills;           // 主技能列表
    public List<Skill> supportSkills;        // 支援技能列表
}