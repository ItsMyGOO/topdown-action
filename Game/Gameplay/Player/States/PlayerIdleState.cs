using GodotGameTemplate.Gameplay.Actors;
using GodotGameTemplate.Gameplay.Common.StateMachine;

namespace GodotGameTemplate.Gameplay.Player.States;

public sealed class PlayerIdleState : IState<ActorContext>
{
    public void Enter(ActorContext context)
    {
    }

    public void Exit(ActorContext context)
    {
    }

    public void Update(ActorContext context, double delta)
    {
    }

    public void PhysicsUpdate(ActorContext context, double delta)
    {
        if (context.HasMoveInput)
        {
            context.Facing = context.Intent.Move.Normalized();
        }
    }
}
