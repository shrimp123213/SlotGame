using UnityEngine;

[CreateAssetMenu(menuName = "UnitStates/DiseaseState")]
public class DiseaseState : UnitStateBase
{
    public override string StateName => "Disease";

    [SerializeField]
    private Sprite icon;
    public override Sprite Icon => icon;

    public override void OnEnter(UnitController unit)
    {
        Debug.Log($"{unit.unitData.unitName} 进入疾病状态");
        unit.UpdateUnitUI();
    }

    public override void OnExit(UnitController unit)
    {
        Debug.Log($"{unit.unitData.unitName} 退出疾病状态");
    }
}