public class SkillAction
{
    public SkillType Type { get; private set; }
    public int Value { get; private set; } // 可以表示次数或数值

    public SkillAction(SkillType type, int value)
    {
        Type = type;
        Value = value;
    }
}