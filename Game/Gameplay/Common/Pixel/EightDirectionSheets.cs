using System;
using System.Collections.Generic;
using Godot;

namespace GodotGameTemplate.Gameplay.Common.Pixel;

/// <summary>
/// 八向动画引用（用户素材槽位）：
/// <para>
/// 预留 8 个方向的跑动表路径（idle 同理可扩展）。路径指向的文件**存在即自动生效**，
/// 不存在则回退到默认表 + 翻转——放入素材无需改任何代码。
/// </para>
/// 放置规范：`Game/Art/Player/8Dir/Run_{方向}.png`，表规格与 Run.png 一致
/// （15 列 × 8 行，帧 128×128，行 0 为默认变体）。
/// </summary>
public static class EightDirectionSheets
{
    private const string BaseDir = "res://Game/Art/Player/8Dir";

    /// <summary>
    /// 方向 → 预期文件名（放进 BaseDir 即生效）。
    /// </summary>
    public static readonly IReadOnlyDictionary<FacingDirection, string> ExpectedFiles =
        new Dictionary<FacingDirection, string>
        {
            [FacingDirection.Right] = "Run_Right.png",
            [FacingDirection.DownRight] = "Run_DownRight.png",
            [FacingDirection.Down] = "Run_Down.png",
            [FacingDirection.DownLeft] = "Run_DownLeft.png",
            [FacingDirection.Left] = "Run_Left.png",
            [FacingDirection.UpLeft] = "Run_UpLeft.png",
            [FacingDirection.Up] = "Run_Up.png",
            [FacingDirection.UpRight] = "Run_UpRight.png",
        };

    private static readonly Dictionary<FacingDirection, bool> AvailabilityCache = new();

    /// <summary>
    /// 指定方向的八向表是否存在（结果缓存）。
    /// </summary>
    public static bool HasSheet(FacingDirection direction)
    {
        if (AvailabilityCache.TryGetValue(direction, out var cached))
        {
            return cached;
        }

        var path = SheetPath(direction);
        var exists = FileAccess.FileExists(path);
        AvailabilityCache[direction] = exists;

        return exists;
    }

    /// <summary>
    /// 指定方向的表路径；不存在返回 null（调用方回退默认表）。
    /// </summary>
    public static string? SheetPath(FacingDirection direction)
    {
        return ExpectedFiles.TryGetValue(direction, out var file) ? $"{BaseDir}/{file}" : null;
    }

    /// <summary>
    /// 运行时清除缓存（编辑器下热放入素材后调用）。
    /// </summary>
    public static void ClearCache() => AvailabilityCache.Clear();
}
