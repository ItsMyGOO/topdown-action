using Godot;
using GodotGameTemplate.Gameplay.Common.Pixel;

namespace GodotGameTemplate.Tests.Gameplay.Common.Pixel;

public sealed class AnimationCatalogTests
{
    [Fact]
    public void Catalog_ProvidesAllPlayerAnims()
    {
        foreach (PlayerAnim anim in Enum.GetValues<PlayerAnim>())
        {
            var def = AnimationCatalog.Get(anim);

            Assert.True(def.FrameCount > 0, $"{anim} frames");
            Assert.True(def.Fps > 0, $"{anim} fps");
            Assert.True(def.Columns == 15 && def.Rows == 8, $"{anim} grid");
        }
    }

    [Fact]
    public void IdleAndRun_Loop_AttackOnce()
    {
        Assert.True(AnimationCatalog.Get(PlayerAnim.Idle).Loop);
        Assert.True(AnimationCatalog.Get(PlayerAnim.Run).Loop);
        Assert.False(AnimationCatalog.Get(PlayerAnim.Melee).Loop);
        Assert.False(AnimationCatalog.Get(PlayerAnim.Hurt).Loop);
    }

    [Fact]
    public void FrameFor_Looping_WrapsWithinFrameCount()
    {
        var run = AnimationCatalog.Get(PlayerAnim.Run);

        for (var t = 0f; t < 3f; t += 0.05f)
        {
            var frame = AnimationCatalog.FrameFor(t, run);

            Assert.InRange(frame, 0, run.FrameCount - 1);
        }
    }

    [Fact]
    public void FrameFor_NonLooping_ClampsAtLastFrame()
    {
        var melee = AnimationCatalog.Get(PlayerAnim.Melee);

        Assert.Equal(melee.FrameCount - 1, AnimationCatalog.FrameFor(999f, melee));
    }

    [Fact]
    public void RowFor_Vector_MainAxisSelectsRow()
    {
        Assert.Equal(0, AnimationCatalog.RowFor(Vector2.Right));
        Assert.Equal(3, AnimationCatalog.RowFor(Vector2.Down));
        Assert.Equal(9, AnimationCatalog.RowFor(Vector2.Left));
        Assert.Equal(6, AnimationCatalog.RowFor(Vector2.Up));
        Assert.Equal(3, AnimationCatalog.RowFor(Vector2.Zero)); // 零向量默认朝下。
    }

    [Fact]
    public void Sheets_PointToKnightPack()
    {
        foreach (PlayerAnim anim in Enum.GetValues<PlayerAnim>())
        {
            var def = AnimationCatalog.Get(anim);

            Assert.Contains("2D HD Character Knight", def.Sheet);
        }
    }
}
