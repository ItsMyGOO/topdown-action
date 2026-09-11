namespace GodotGameTemplate.Gameplay.Enemies.Feedback;

/// <summary>
/// 受击反馈规则（纯逻辑）。
/// </summary>
public static class EnemyFeedbackRules
{
    /// <summary>
    /// 血条显隐：有血池且非满血时显示（0 血显示空条，随后由死亡动画整体隐藏）。
    /// </summary>
    public static bool ShowHealthBar(int hp, int maxHp)
    {
        return maxHp > 0 && hp < maxHp;
    }
}
