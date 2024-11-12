using UnityEngine;

[CreateAssetMenu(menuName = "UnitStates/InvincibleState")]
public class InvincibleState : ScriptableObject, IUnitState
{
    public string StateName => "Invincible";
    [SerializeField] private Sprite icon;
    public Sprite Icon => icon;
    public void OnEnter(UnitController unit)
    {
        Debug.Log($"{unit.unitData.unitName} 进入无敌状态");
        unit.UpdateUnitUI();
    }

    public void OnExit(UnitController unit)
    {
        Debug.Log($"{unit.unitData.unitName} 退出无敌状态");
        unit.UpdateUnitUI();
    }

    /*public void Update(UnitController unit)
    {
        // 无敌状态下通常不需要每帧更新
    }*/
}
