using System;

namespace GodotGameTemplate.Gameplay.Combat;

/// <summary>
/// 伤害数学（纯逻辑）：减法抗性（抗性不为负，负值不放大伤害），保底 1 点。
/// </summary>
public static class DamageMath
{
    public static int Apply(int rawDamage, int resistFlat)
    {
        return Math.Max(1, rawDamage - Math.Max(0, resistFlat));
    }
}
