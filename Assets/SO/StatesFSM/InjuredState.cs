using UnityEngine;

[CreateAssetMenu(menuName = "UnitStates/InjuredState")]
public class InjuredState : ScriptableObject, IUnitState
{
    public string StateName => "Injured";
    [SerializeField] private Sprite icon;
    public Sprite Icon => icon;

    public void OnEnter(UnitController unit)
    {
        Debug.Log($"{unit.unitData.unitName} 进入负伤状态");
        unit.UpdateUnitUI();
    }

    public void OnExit(UnitController unit)
    {
        Debug.Log($"{unit.unitData.unitName} 退出负伤状态");
        unit.UpdateUnitUI();
    }

    /*public void Update(UnitController unit)
    {
        // 负伤状态下通常不需要每帧更新
    }*/
}
