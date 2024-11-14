using UnityEngine;

public interface IUnitState
{
    string StateName { get; }
    Sprite Icon { get; }
    
    // 当状态被添加到单位时调用
    void OnEnter(UnitController unit);
    
    // 当状态被移除时调用
    void OnExit(UnitController unit);
    
    // 可选：状态更新逻辑，每帧调用
    //void Update(UnitController unit);
}