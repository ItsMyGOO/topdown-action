namespace GodotGameTemplate.Gameplay.Common.StateMachine;

public interface IState<TContext>
{
    void Enter(TContext context);

    void Exit(TContext context);

    void Update(TContext context, double delta);

    void PhysicsUpdate(TContext context, double delta);
}
