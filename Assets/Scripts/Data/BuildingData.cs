using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewBuildingData", menuName = "Data/Building")]
public class BuildingData : BaseData
{
    public int health;                 // 生命值
    public BuildingType buildingType;  // 建築類型
    public List<Skill> buildingSkills; // 建築技能
}