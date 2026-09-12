using System;
using Godot;

namespace GodotGameTemplate.Gameplay.Common.Pixel;

/// <summary>
/// 一帧序列帧动画的行列坐标（对应 spritesheet 的 FrameCoords）。
/// </summary>
/// <param name="Row">方向行（0=下 1=上 2=左 3=右）。</param>
/// <param name="Frame">列帧号。</param>
public readonly record struct PixelAnim(int Row, int Frame);

/// <summary>
/// 朝向 → 序列帧行/列 的映射（纯逻辑）。
/// 行分配与 <c>player_sheet.png</c> 的行序一致：下/上/左/右。
/// </summary>
public static class FacingAnimationResolver
{
    public static PixelAnim Resolve(Vector2 facing, bool moving, float cycleTime = 0f)
    {
        var row = RowFor(facing);
        var frame = moving ? WalkCycle.FrameFor(cycleTime) : 0;

        return new PixelAnim(row, frame);
    }

    private static int RowFor(Vector2 facing)
    {
        if (facing == Vector2.Zero)
        {
            return 0;
        }

        // 主轴判定：|x| ≥ |y| 视为横向（左右行走更常见），否则纵向。
        return MathF.Abs(facing.X) >= MathF.Abs(facing.Y)
            ? (facing.X >= 0 ? 3 : 2)
            : (facing.Y >= 0 ? 0 : 1);
    }
}
