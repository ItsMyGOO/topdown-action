using Godot;
using GodotGameTemplate.Gameplay.Common.Pixel;

namespace GodotGameTemplate.Tests.Gameplay.Common.Pixel;

public sealed class FacingAnimationResolverTests
{
    [Fact]
    public void Resolve_FacingDown_RowZero_IdleFrame()
    {
        var anim = FacingAnimationResolver.Resolve(Vector2.Down, moving: false);

        Assert.Equal(0, anim.Row);
        Assert.Equal(0, anim.Frame);
    }

    [Fact]
    public void Resolve_FacingUp_RowOne()
    {
        Assert.Equal(1, FacingAnimationResolver.Resolve(Vector2.Up, false).Row);
    }

    [Fact]
    public void Resolve_FacingLeft_RowTwo()
    {
        Assert.Equal(2, FacingAnimationResolver.Resolve(Vector2.Left, false).Row);
    }

    [Fact]
    public void Resolve_FacingRight_RowThree()
    {
        Assert.Equal(3, FacingAnimationResolver.Resolve(Vector2.Right, false).Row);
    }

    [Fact]
    public void Resolve_DiagonalFacing_PrefersHorizontalAxis()
    {
        // 右下 45°：x 分量更占视觉主导时取横向（本项目约定对角线一律先判 x）。
        Assert.Equal(3, FacingAnimationResolver.Resolve(new Vector2(1f, 0.5f), false).Row);
        Assert.Equal(2, FacingAnimationResolver.Resolve(new Vector2(-1f, -0.5f), false).Row);

        // 纯纵向对角线（y 更长）取纵向。
        Assert.Equal(0, FacingAnimationResolver.Resolve(new Vector2(0.5f, 1f), false).Row);
    }

    [Fact]
    public void Resolve_ZeroFacing_DefaultsDown()
    {
        Assert.Equal(0, FacingAnimationResolver.Resolve(Vector2.Zero, false).Row);
    }

    [Fact]
    public void Resolve_Moving_ProvidesWalkCycleFrame()
    {
        var anim = FacingAnimationResolver.Resolve(Vector2.Down, moving: true, cycleTime: 0.25f);

        Assert.Equal(0, anim.Row);
        Assert.Equal(2, anim.Frame);
    }

    [Fact]
    public void Resolve_Idle_AlwaysFrameZero()
    {
        var anim = FacingAnimationResolver.Resolve(Vector2.Down, moving: false, cycleTime: 99f);

        Assert.Equal(0, anim.Frame);
    }
}
