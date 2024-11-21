using UnityEngine;

[CreateAssetMenu(menuName = "UnitStates/FragileState")]
public class FragileState : UnitStateBase
{
    public override string StateName => "Fragile";

    [SerializeField]
    private Sprite icon;
    public override Sprite Icon => icon;

    public override void OnEnter(UnitController unit)
    {
        Debug.Log($"{unit.unitData.unitName} 进入脆弱状态");
        unit.UpdateUnitUI();
    }

    public override void OnExit(UnitController unit)
    {
        Debug.Log($"{unit.unitData.unitName} 退出脆弱状态");
    }
}