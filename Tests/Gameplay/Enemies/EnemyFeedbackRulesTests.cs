using GodotGameTemplate.Gameplay.Enemies.Feedback;

namespace GodotGameTemplate.Tests.Gameplay.Enemies;

public sealed class EnemyFeedbackRulesTests
{
    [Fact]
    public void ShowHealthBar_FullHealth_Hidden()
    {
        Assert.False(EnemyFeedbackRules.ShowHealthBar(3, 3));
    }

    [Fact]
    public void ShowHealthBar_Damaged_Visible()
    {
        Assert.True(EnemyFeedbackRules.ShowHealthBar(2, 3));
    }

    [Fact]
    public void ShowHealthBar_ZeroHealth_VisibleEmptyBar()
    {
        Assert.True(EnemyFeedbackRules.ShowHealthBar(0, 3));
    }

    [Fact]
    public void ShowHealthBar_NoHealthPool_Hidden()
    {
        Assert.False(EnemyFeedbackRules.ShowHealthBar(0, 0));
    }
}
