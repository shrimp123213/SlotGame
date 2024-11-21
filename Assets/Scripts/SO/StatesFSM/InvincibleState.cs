using UnityEngine;

[CreateAssetMenu(menuName = "UnitStates/InvincibleState")]
public class InvincibleState : UnitStateBase
{
    public override string StateName => "Invincible";
    [SerializeField] private Sprite icon;
    public override Sprite Icon => icon;
    public override void OnEnter(UnitController unit)
    {
        Debug.Log($"{unit.unitData.unitName} 进入无敌状态");
        unit.UpdateUnitUI();
    }

    public override void OnExit(UnitController unit)
    {
        Debug.Log($"{unit.unitData.unitName} 退出无敌状态");
        unit.UpdateUnitUI();
    }
}
