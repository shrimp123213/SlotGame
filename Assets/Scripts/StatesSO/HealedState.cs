using UnityEngine;

[CreateAssetMenu(menuName = "UnitStates/HealedState")]
public class HealedState : ScriptableObject, IUnitState
{
    public string StateName => "Healed";
    public Sprite Icon { get; }

    public void OnEnter(UnitController unit)
    {
        Debug.Log($"{unit.unitData.unitName} 被治疗，移除负伤状态");
        unit.RemoveState<InjuredState>();
        unit.UpdateUnitUI();
    }

    public void OnExit(UnitController unit)
    {
        Debug.Log($"{unit.unitData.unitName} 退出治疗状态");
    }

    /*public void Update(UnitController unit)
    {
        // 治疗状态下通常不需要每帧更新
    }*/
}
