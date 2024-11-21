using UnityEngine;

[CreateAssetMenu(menuName = "UnitStates/InjuredState")]
public class InjuredState : UnitStateBase
{
    public override string StateName => "Injured";
    [SerializeField] private Sprite icon;
    public override Sprite Icon => icon;

    public override void OnEnter(UnitController unit)
    {
        Debug.Log($"{unit.unitData.unitName} 进入负伤状态");
        unit.UpdateUnitUI();
    }

    public override void OnExit(UnitController unit)
    {
        Debug.Log($"{unit.unitData.unitName} 退出负伤状态");
        unit.UpdateUnitUI();
    }
}
