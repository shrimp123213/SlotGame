using UnityEngine;

/// <summary>
/// 单位状态的基类，继承自 ScriptableObject 并实现 IUnitState 接口
/// </summary>
public abstract class UnitStateBase : ScriptableObject, IUnitState
{
    public abstract string StateName { get; }
    public abstract Sprite Icon { get; }

    // 当状态被添加到单位时调用
    public abstract void OnEnter(UnitController unit);

    // 当状态被移除时调用
    public abstract void OnExit(UnitController unit);
}