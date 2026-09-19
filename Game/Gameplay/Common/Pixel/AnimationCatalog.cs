using System;
using Godot;

namespace GodotGameTemplate.Gameplay.Common.Pixel;

/// <summary>
/// 动画类别（玩家骑士包的动作子集）。
/// </summary>
public enum PlayerAnim
{
    /// <summary>待机（Idle 表，循环）。</summary>
    Idle,

    /// <summary>跑动（Run 表，循环）。</summary>
    Run,

    /// <summary>普攻（Melee 表，单次）。</summary>
    Melee,

    /// <summary>施法（CastSpell 表，单次）。</summary>
    Cast,

    /// <summary>受击（TakeDamage 表，单次）。</summary>
    Hurt,

    /// <summary>翻滚（Rolling 表，单次）。</summary>
    Roll,
}

/// <summary>
/// 一段动画的定义（纯逻辑）：表名、网格、帧率、循环。
/// </summary>
/// <param name="Sheet">贴图路径（res:// 下，不含扩展名前的目录）。</param>
/// <param name="Columns">网格列数。</param>
/// <param name="Rows">网格行数。</param>
/// <param name="FrameCount">实际帧数（按行优先取前 N 帧）。</param>
/// <param name="Fps">帧率。</param>
/// <param name="Loop">是否循环。</param>
public readonly record struct AnimDefinition(
    string Sheet,
    int Columns,
    int Rows,
    int FrameCount,
    float Fps,
    bool Loop
)
{
    public const int ColumnsPerSheet = 16;
    public const int RowsPerSheet = 10;

    public static AnimDefinition Looping(PlayerAnim anim, int frameCount, float fps) =>
        new(SheetOf(anim), ColumnsPerSheet, RowsPerSheet, frameCount, fps, true);

    public static AnimDefinition Once(PlayerAnim anim, int frameCount, float fps) =>
        new(SheetOf(anim), ColumnsPerSheet, RowsPerSheet, frameCount, fps, false);

    private static string SheetOf(PlayerAnim anim) =>
        anim switch
        {
            PlayerAnim.Run =>
                "res://Game/Art/2D HD Character Knight/Spritesheets/With shadows/Run.png",
            PlayerAnim.Melee =>
                "res://Game/Art/2D HD Character Knight/Spritesheets/With shadows/Melee.png",
            PlayerAnim.Cast =>
                "res://Game/Art/2D HD Character Knight/Spritesheets/With shadows/CastSpell.png",
            PlayerAnim.Hurt =>
                "res://Game/Art/2D HD Character Knight/Spritesheets/With shadows/TakeDamage.png",
            PlayerAnim.Roll =>
                "res://Game/Art/2D HD Character Knight/Spritesheets/With shadows/Rolling.png",
            _ => "res://Game/Art/2D HD Character Knight/Spritesheets/With shadows/Idle.png",
        };
}

/// <summary>
/// 行 → 朝向映射（骑士包每行一个朝向；实测行 0=右、行 3=下、行 6=上、行 9=左）。
/// </summary>
public enum FacingDirection
{
    Right,
    Up,
    Left,
    Down,
}

/// <summary>
/// 动画目录（纯逻辑）：动作 → 定义 与 帧推进。
/// </summary>
public static class AnimationCatalog
{
    /// <summary>
    /// 朝向 → 表行号（实测映射；其余行为同动作变体备用）。
    /// </summary>
    public static int RowFor(FacingDirection direction) =>
        direction switch
        {
            FacingDirection.Right => 0,
            FacingDirection.Down => 3,
            FacingDirection.Up => 6,
            FacingDirection.Left => 9,
            _ => 3,
        };

    /// <summary>
    /// 朝向向量 → 行（主轴判定：横向取左右、纵向取上下）。
    /// </summary>
    public static int RowFor(Vector2 facing)
    {
        if (facing == Vector2.Zero)
        {
            return RowFor(FacingDirection.Down);
        }

        return MathF.Abs(facing.X) >= MathF.Abs(facing.Y)
            ? RowFor(facing.X >= 0 ? FacingDirection.Right : FacingDirection.Left)
            : RowFor(facing.Y >= 0 ? FacingDirection.Down : FacingDirection.Up);
    }

    public static AnimDefinition Get(PlayerAnim anim) =>
        anim switch
        {
            PlayerAnim.Idle => AnimDefinition.Looping(PlayerAnim.Idle, frameCount: 15, fps: 12f),
            PlayerAnim.Run => AnimDefinition.Looping(PlayerAnim.Run, frameCount: 15, fps: 14f),
            PlayerAnim.Melee => AnimDefinition.Once(PlayerAnim.Melee, frameCount: 10, fps: 16f),
            PlayerAnim.Cast => AnimDefinition.Once(PlayerAnim.Cast, frameCount: 10, fps: 14f),
            PlayerAnim.Hurt => AnimDefinition.Once(PlayerAnim.Hurt, frameCount: 6, fps: 12f),
            PlayerAnim.Roll => AnimDefinition.Once(PlayerAnim.Roll, frameCount: 13, fps: 14f),
            _ => AnimDefinition.Looping(PlayerAnim.Idle, 15, 12f),
        };

    /// <summary>
    /// 按播放时间推进帧号；非循环动画停在最后一帧。
    /// </summary>
    public static int FrameFor(float timeSeconds, AnimDefinition definition)
    {
        if (timeSeconds <= 0f)
        {
            return 0;
        }

        var frame = (int)(timeSeconds * definition.Fps);

        return definition.Loop
            ? frame % definition.FrameCount
            : Math.Min(frame, definition.FrameCount - 1);
    }
}
