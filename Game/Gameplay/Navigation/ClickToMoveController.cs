using Godot;

namespace GodotGameTemplate.Gameplay.Navigation;

/// <summary>
/// 点击移动控制器：使用 <see cref="NavigationAgent2D"/> 计算路径，并输出“下一步期望速度”。
/// 这是 Godot 胶水层（依赖节点与导航），便于 PlayerController 将其接入现有移动逻辑。
/// </summary>
public partial class ClickToMoveController : Node
{
    [Export]
    public NavigationAgent2D? Agent { get; set; }

    /// <summary>
    /// 视为到达目标的距离阈值。
    /// </summary>
    private const float ArrivalDistance = 8f;

    [Export]
    public float MaxSpeed { get; set; } = 120f;

    public Vector2 DesiredVelocity { get; private set; } = Vector2.Zero;

    public override void _Ready()
    {
        // 场景中手写的 NodePath 导出可能不解析（无编辑器保存兜底），在此按固定路径解析。
        Agent ??= GetNodeOrNull<NavigationAgent2D>("../NavigationAgent2D");
    }

    public void SetDestination(Vector2 destination)
    {
        if (Agent == null)
        {
            return;
        }

        Agent.TargetPosition = destination;
    }

    public void Stop()
    {
        DesiredVelocity = Vector2.Zero;
        if (Agent != null)
        {
            // 让 Agent “认为已到达”，避免继续输出路径点。
            var owner = GetParent<Node2D>();
            if (owner != null)
            {
                Agent.TargetPosition = owner.GlobalPosition;
            }

            Agent.Velocity = Vector2.Zero;
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        if (Agent == null)
        {
            DesiredVelocity = Vector2.Zero;
            return;
        }

        var owner = GetParent<Node2D>();
        if (owner == null)
        {
            DesiredVelocity = Vector2.Zero;
            return;
        }

        // 已到达目标：停止（Stop() 也是把目标设为自身来实现的）。
        if (Agent.TargetPosition.DistanceTo(owner.GlobalPosition) <= ArrivalDistance)
        {
            DesiredVelocity = Vector2.Zero;
            Agent.Velocity = Vector2.Zero;
            return;
        }

        var next = Agent.GetNextPathPosition();
        var dir = next - owner.GlobalPosition;

        if (dir.Length() <= ArrivalDistance)
        {
            // 导航地图为空/无路径时下一路径点退化为当前位置：
            // 兜底直线朝目标移动，保证无 NavigationRegion2D 的场景点击可走。
            dir = Agent.TargetPosition - owner.GlobalPosition;
        }

        DesiredVelocity = dir.Normalized() * MaxSpeed;
        Agent.Velocity = DesiredVelocity;
    }
}
