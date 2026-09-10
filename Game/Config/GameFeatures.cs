namespace GodotGameTemplate.Config;

/// <summary>
/// 游戏功能开关。用于快速裁剪/关闭模块，而不需要大规模改代码。
/// 注意：此类型不继承 Godot Resource，避免在 Tests 中实例化导致宿主崩溃。
/// </summary>
public sealed class GameFeatures
{
    public bool EnableLoot { get; set; } = true;

    public bool EnableInventory { get; set; } = true;

    public bool EnableSkills { get; set; } = true;

    public bool EnableLeveling { get; set; } = true;

    public bool EnableTown { get; set; } = true;

    public bool EnableAoeIndicator { get; set; } = true;

    public bool EnableGamepad { get; set; } = true;

    public bool EnableMobileTouch { get; set; } = true;
}
