using UnityEngine;

[CreateAssetMenu(menuName = "UnitStates/HealedState")]
public class HealedState : UnitStateBase
{
    public override string StateName => "Healed";
    
    [SerializeField]
    private Sprite icon;
    public override Sprite Icon => icon;    

    public override void OnEnter(UnitController unit)
    {
        Debug.Log($"{unit.unitData.unitName} 被治疗，移除负伤状态");
        unit.RemoveState<InjuredState>();
        unit.UpdateUnitUI();
    }

    public override void OnExit(UnitController unit)
    {
        Debug.Log($"{unit.unitData.unitName} 退出治疗状态");
    }
}