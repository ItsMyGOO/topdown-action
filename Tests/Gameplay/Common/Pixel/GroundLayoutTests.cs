using GodotGameTemplate.Gameplay.Common.Pixel;

namespace GodotGameTemplate.Tests.Gameplay.Common.Pixel;

public sealed class GroundLayoutTests
{
    [Fact]
    public void VariantAt_SameSeedAndCell_Deterministic()
    {
        var a = GroundLayout.VariantAt(3, 5, seed: 42, variants: 3);
        var b = GroundLayout.VariantAt(3, 5, seed: 42, variants: 3);

        Assert.Equal(a, b);
    }

    [Fact]
    public void VariantAt_DifferentCells_CanDiffer()
    {
        var seen = new System.Collections.Generic.HashSet<int>();

        for (var x = 0; x < 12; x++)
        {
            seen.Add(GroundLayout.VariantAt(x, 0, seed: 7, variants: 3));
        }

        Assert.True(seen.Count > 1, $"expected variety, got {seen.Count}");
    }

    [Fact]
    public void VariantAt_AlwaysWithinRange()
    {
        for (var x = 0; x < 20; x++)
        {
            for (var y = 0; y < 12; y++)
            {
                var variant = GroundLayout.VariantAt(x, y, seed: 9, variants: 3);

                Assert.InRange(variant, 0, 2);
            }
        }
    }

    [Fact]
    public void Generate_FillsWholeGrid()
    {
        var grid = GroundLayout.Generate(20, 12, seed: 1, variants: 3);

        Assert.Equal(20, grid.GetLength(0));
        Assert.Equal(12, grid.GetLength(1));
        Assert.All(grid.Cast<int>(), variant => Assert.InRange(variant, 0, 2));
    }

    [Fact]
    public void Generate_SameSeed_SameGrid()
    {
        Assert.Equal(GroundLayout.Generate(8, 8, 5, 3), GroundLayout.Generate(8, 8, 5, 3));
    }
}
