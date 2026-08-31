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

    [Export]
    public float MaxSpeed { get; set; } = 120f;

    public Vector2 DesiredVelocity { get; private set; } = Vector2.Zero;

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

        if (Agent.IsNavigationFinished())
        {
            DesiredVelocity = Vector2.Zero;
            Agent.Velocity = Vector2.Zero;
            return;
        }

        var owner = GetParent<Node2D>();
        if (owner == null)
        {
            DesiredVelocity = Vector2.Zero;
            Agent.Velocity = Vector2.Zero;
            return;
        }

        var next = Agent.GetNextPathPosition();
        var dir = next - owner.GlobalPosition;
        if (dir == Vector2.Zero)
        {
            DesiredVelocity = Vector2.Zero;
        }
        else
        {
            DesiredVelocity = dir.Normalized() * MaxSpeed;
        }

        // 将期望速度喂给 Agent（便于启用避障/速度约束等）。
        Agent.Velocity = DesiredVelocity;
    }
}
