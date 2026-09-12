using GodotGameTemplate.Gameplay.Common.Pixel;

namespace GodotGameTemplate.Tests.Gameplay.Common.Pixel;

public sealed class WalkCycleTests
{
    [Fact]
    public void FrameFor_AtZeroTime_IsFirstFrame()
    {
        Assert.Equal(0, WalkCycle.FrameFor(0f));
    }

    [Fact]
    public void FrameFor_AdvancesWithTime()
    {
        // 8fps：0.25 秒推进 2 帧（0→2：帧 0 占 [0,0.125)，帧 1 [0.125,0.25)，帧 2 [0.25,0.375)）。
        Assert.Equal(1, WalkCycle.FrameFor(0.125f));
        Assert.Equal(2, WalkCycle.FrameFor(0.25f));
    }

    [Fact]
    public void FrameFor_WrapsAround()
    {
        Assert.Equal(0, WalkCycle.FrameFor(0.5f));
        Assert.Equal(3, WalkCycle.FrameFor(0.4999f));
    }

    [Fact]
    public void FrameFor_CustomFpsAndCount()
    {
        // 4fps 走 1 秒 → 已推进 4 帧。
        Assert.Equal(4, WalkCycle.FrameFor(1f, fps: 4f, frameCount: 8));
    }

    [Fact]
    public void FrameFor_NegativeTime_ClampsToZero()
    {
        Assert.Equal(0, WalkCycle.FrameFor(-1f));
    }
}
