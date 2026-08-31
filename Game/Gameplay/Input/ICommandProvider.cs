using GodotGameTemplate.Gameplay.Input.Commands;

namespace GodotGameTemplate.Gameplay.Input;

/// <summary>
/// 玩家命令提供者：将“输入设备采样结果”转换为一帧的 <see cref="PlayerCommand"/>。
/// <para>
/// 该接口用于承载“设备相关”的采集逻辑（键鼠/手柄/触屏），而游戏规则（如追击、连招、点击解析）
/// 应该由上层控制器/编排器处理。
/// </para>
/// </summary>
public interface ICommandProvider
{
    /// <summary>
    /// 获取当前帧的玩家命令快照。
    /// </summary>
    PlayerCommand GetCommand();
}
