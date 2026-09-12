using System;

namespace GodotGameTemplate.Gameplay.Common.Pixel;

/// <summary>
/// 地面瓦片变体布局（纯逻辑）：确定性整型哈希，保证同种子同布局。
/// </summary>
public static class GroundLayout
{
    /// <summary>
    /// 计算单个格子的地面变体索引（0..<paramref name="variants"/>-1）。
    /// </summary>
    public static int VariantAt(int x, int y, int seed, int variants)
    {
        if (variants <= 0)
        {
            return 0;
        }
        unchecked
        {
            var hash = x * 374761393 + y * 668265263 + seed * 974634599;
            hash = (int)((hash ^ (hash >> 13)) * 1274126177);
            hash ^= hash >> 16;

            return ((hash % variants) + variants) % variants;
        }
    }

    /// <summary>
    /// 生成整片地面变体网格（<c>result[x, y]</c>）。
    /// </summary>
    public static int[,] Generate(int width, int height, int seed, int variants)
    {
        var grid = new int[width, height];

        for (var x = 0; x < width; x++)
        {
            for (var y = 0; y < height; y++)
            {
                grid[x, y] = VariantAt(x, y, seed, variants);
            }
        }

        return grid;
    }
}
