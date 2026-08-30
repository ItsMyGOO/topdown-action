namespace GodotGameTemplate.Gameplay.Common.StateMachine;

public sealed class StateMachine<TContext>
{
    private readonly TContext _context;

    public StateMachine(TContext context)
    {
        _context = context;
    }

    public IState<TContext>? CurrentState { get; private set; }

    public void ChangeState(IState<TContext> nextState)
    {
        if (ReferenceEquals(CurrentState, nextState))
        {
            return;
        }

        CurrentState?.Exit(_context);
        CurrentState = nextState;
        CurrentState.Enter(_context);
    }

    public void Update(double delta)
    {
        CurrentState?.Update(_context, delta);
    }

    public void PhysicsUpdate(double delta)
    {
        CurrentState?.PhysicsUpdate(_context, delta);
    }
}
