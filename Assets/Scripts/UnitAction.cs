using UnityEngine;
using System.Collections.Generic;

public enum UnitActionState
{
    MoveForward,
    ExecuteMainSkills,
    SupportFrontUnit,
    EndAction
}

public class UnitAction
{
    private Unit unit;
    private GridManager gridManager;

    public UnitAction(Unit unit, GridManager gridManager)
    {
        this.unit = unit;
        this.gridManager = gridManager;
    }

    public void Execute()
    {
        UnitActionState actionState = DetermineActionState();

        switch (actionState)
        {
            case UnitActionState.MoveForward:
                MoveForward();
                break;
            case UnitActionState.ExecuteMainSkills:
                ExecuteMainSkills();
                break;
            case UnitActionState.SupportFrontUnit:
                SupportFrontUnit();
                break;
            case UnitActionState.EndAction:
                EndAction();
                break;
            default:
                EndAction();
                break;
        }
    }

    /// <summary>
    /// 確定單位的行動狀態
    /// </summary>
    /// <returns>行動狀態</returns>
    private UnitActionState DetermineActionState()
    {
        if (IsFrontEmpty())
        {
            return UnitActionState.MoveForward;
        }

        if (HasMovePoints())
        {
            return UnitActionState.ExecuteMainSkills;
        }

        if (IsFrontFriendlyUnit())
        {
            return UnitActionState.SupportFrontUnit;
        }

        if (IsFrontUnit())
        {
            if (IsObstacleIntact())
            {
                return UnitActionState.ExecuteMainSkills;
            }
            else
            {
                if (HasRemainingMainActions())
                {
                    return UnitActionState.ExecuteMainSkills;
                }
                else
                {
                    return UnitActionState.EndAction;
                }
            }
        }

        if (HasRemainingMainActions())
        {
            return UnitActionState.ExecuteMainSkills;
        }
        else
        {
            return UnitActionState.EndAction;
        }
    }

    // 以下是判斷方法和行動方法，需要根據實際遊戲邏輯實現。

    private bool IsFrontEmpty()
    {
        // 檢查前方是否為空格
        GridCell currentCell = gridManager.GetUnitCell(unit);
        if (currentCell != null)
        {
            GridCell frontCell = gridManager.GetFrontCell(currentCell, unit.IsPlayerOwned);
            if (frontCell != null && frontCell.IsEmpty())
            {
                return true;
            }
        }
        return false;
    }

    private void MoveForward()
    {
        // 執行移動邏輯
        GridCell currentCell = gridManager.GetUnitCell(unit);
        if (currentCell != null)
        {
            GridCell frontCell = gridManager.GetFrontCell(currentCell, unit.IsPlayerOwned);
            if (frontCell != null && frontCell.IsEmpty())
            {
                gridManager.MoveUnit(unit, currentCell, frontCell);
            }
        }
    }

    private bool HasMovePoints()
    {
        // 檢查單位是否有移動點數
        return unit.unitData.mainSkills.Exists(skill => skill.skillType == SkillType.Move);
    }

    private void ExecuteMainSkills()
    {
        // 執行主技能（除了移動、防衛、近戰、破壞、暗殺）
        foreach (var skill in unit.unitData.mainSkills)
        {
            if (skill.skillType != SkillType.Move &&
                skill.skillType != SkillType.Defense &&
                skill.skillType != SkillType.Melee &&
                skill.skillType != SkillType.Destroy &&
                skill.skillType != SkillType.Assassinate)
            {
                // 在這裡實現技能的具體效果
            }
            else
            {
                // 處理移動、防衛、近戰、破壞、暗殺技能
                switch (skill.skillType)
                {
                    case SkillType.Move:
                        // 處理移動技能
                        break;
                    case SkillType.Defense:
                        // 處理防衛技能
                        break;
                    case SkillType.Melee:
                        // 處理近戰技能
                        break;
                    case SkillType.Destroy:
                        // 處理破壞技能
                        break;
                    case SkillType.Assassinate:
                        // 處理暗殺技能
                        ExecuteAssassinateSkill(skill);
                        break;
                }
            }
        }
    }

    private bool IsFrontFriendlyUnit()
    {
        // 檢查前方是否為友軍單位
        GridCell currentCell = gridManager.GetUnitCell(unit);
        if (currentCell != null)
        {
            GridCell frontCell = gridManager.GetFrontCell(currentCell, unit.IsPlayerOwned);
            if (frontCell != null && frontCell.OccupiedUnit != null &&
                frontCell.OccupiedUnit.IsPlayerOwned == unit.IsPlayerOwned)
            {
                return true;
            }
        }
        return false;
    }

    private void SupportFrontUnit()
    {
        // 執行支援邏輯
        GridCell currentCell = gridManager.GetUnitCell(unit);
        if (currentCell != null)
        {
            GridCell frontCell = gridManager.GetFrontCell(currentCell, unit.IsPlayerOwned);
            if (frontCell != null && frontCell.OccupiedUnit != null)
            {
                // 對前方單位應用支援技能
                foreach (var supportSkill in unit.unitData.supportSkills)
                {
                    // 實現支援技能效果
                }
            }
        }
    }

    private bool IsFrontUnit()
    {
        // 檢查前方是否有單位
        GridCell currentCell = gridManager.GetUnitCell(unit);
        if (currentCell != null)
        {
            GridCell frontCell = gridManager.GetFrontCell(currentCell, unit.IsPlayerOwned);
            if (frontCell != null && frontCell.OccupiedUnit != null)
            {
                return true;
            }
        }
        return false;
    }

    private bool IsObstacleIntact()
    {
        // 檢查部位/建築是否仍未被破壞
        // 根據實際遊戲邏輯實現
        return true; // 示例
    }

    private bool HasRemainingMainActions()
    {
        // 檢查是否有未執行的主技能次數
        // 根據實際遊戲邏輯實現
        return true; // 示例
    }

    private void EndAction()
    {
        // 結束單位的行動
    }

    private void ExecuteAssassinateSkill(Skill skill)
    {
        GridCell currentCell = gridManager.GetUnitCell(unit);
        if (currentCell != null)
        {
            bool isAtEdge = (unit.IsPlayerOwned && currentCell.Position.y == gridManager.columns - 1) ||
                            (!unit.IsPlayerOwned && currentCell.Position.y == 0);

            if (isAtEdge)
            {
                // 對敵方 Boss 造成傷害
                DealDamageToBoss(skill.value);
            }
        }
    }

    private void DealDamageToBoss(int damage)
    {
        // 實現對 Boss 造成傷害的邏輯
        Debug.Log($"{unit.unitData.dataName} 對 Boss 造成了 {damage} 點暗殺傷害！");
    }
}
