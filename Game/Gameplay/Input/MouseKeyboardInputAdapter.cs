using Godot;
using GodotGameTemplate.Gameplay.Input.Commands;

namespace GodotGameTemplate.Gameplay.Input;

/// <summary>
/// 键鼠输入适配：负责把鼠标按钮与快捷键采样结果转换为 <see cref="PlayerCommand"/>。
/// <para>
/// 注意：该类型仅负责“采集”，不实现任何游戏规则（例如：点击地面解析落点、点击目标拾取/追击等）。
/// 点击解析需要依赖 Godot 的世界拾取/射线检测，后续应在控制器（Godot 胶水层）完成。
/// </para>
/// </summary>
public sealed class MouseKeyboardInputAdapter : ICommandProvider
{
    /// <summary>
    /// 读取当前帧的输入状态并生成命令快照。
    /// </summary>
    public PlayerCommand GetCommand()
    {
        // 注意：本文件位于 GodotGameTemplate.Gameplay.Input 命名空间下，
        // 因此必须使用 Godot.Input 来避免与命名空间名冲突。
        var toggleInventory = Godot.Input.IsActionJustPressed("open_inventory");
        var interact = Godot.Input.IsActionJustPressed("interact");
        var evade = Godot.Input.IsActionJustPressed("evade");

        // 数字键技能（1-4）
        var s1 = Godot.Input.IsActionJustPressed("skill_1");
        var s2 = Godot.Input.IsActionJustPressed("skill_2");
        var s3 = Godot.Input.IsActionJustPressed("skill_3");
        var s4 = Godot.Input.IsActionJustPressed("skill_4");

        // 鼠标按钮：这里只采集“按下状态”，具体的点击落点/目标解析后续在胶水层实现。
        var primary = Godot.Input.IsMouseButtonPressed(MouseButton.Left);
        var secondary = Godot.Input.IsMouseButtonPressed(MouseButton.Right);

        return new PlayerCommand(
            ClickMoveDestination: null,
            ClickTargetInstanceId: null,
            EvadePressed: evade,
            InteractPressed: interact,
            ToggleInventoryPressed: toggleInventory,
            PrimaryPressed: primary,
            SecondaryPressed: secondary,
            Skill1Pressed: s1,
            Skill2Pressed: s2,
            Skill3Pressed: s3,
            Skill4Pressed: s4
        );
    }
}
