using System;

namespace GodotGameTemplate.Gameplay.Common.Pixel;

/// <summary>
/// 行走帧循环（纯逻辑）：按时间推进序列帧序号。
/// </summary>
public static class WalkCycle
{
    /// <summary>
    /// 默认行走帧率（帧/秒）。
    /// </summary>
    public const float FramesPerSecond = 8f;

    /// <summary>
    /// 计算循环帧号；负时间钳为 0。
    /// </summary>
    public static int FrameFor(float timeSeconds, float fps = FramesPerSecond, int frameCount = 4)
    {
        if (timeSeconds <= 0f)
        {
            return 0;
        }

        return (int)(timeSeconds * fps) % frameCount;
    }
}
