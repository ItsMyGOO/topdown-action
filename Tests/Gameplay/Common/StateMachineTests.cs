using GodotGameTemplate.Gameplay.Common.StateMachine;

namespace GodotGameTemplate.Tests.Gameplay.Common;

public sealed class StateMachineTests
{
    [Fact]
    public void ChangeState_CallsExitThenEnterInOrder()
    {
        var context = new object();
        var log = new List<string>();
        var machine = new StateMachine<object>(context);
        var idle = new FakeState("Idle", log);
        var move = new FakeState("Move", log);

        machine.ChangeState(idle);
        machine.ChangeState(move);

        Assert.Equal(new[] { "Idle:Enter", "Idle:Exit", "Move:Enter" }, log);
    }

    [Fact]
    public void ChangeState_SameStateInstance_DoesNotReEnter()
    {
        var context = new object();
        var log = new List<string>();
        var machine = new StateMachine<object>(context);
        var idle = new FakeState("Idle", log);

        machine.ChangeState(idle);
        machine.ChangeState(idle);

        Assert.Equal(new[] { "Idle:Enter" }, log);
    }

    [Fact]
    public void Update_DelegatesToCurrentStateOnly()
    {
        var context = new object();
        var log = new List<string>();
        var machine = new StateMachine<object>(context);
        var idle = new FakeState("Idle", log);

        machine.ChangeState(idle);
        machine.Update(0.016);
        machine.PhysicsUpdate(0.016);

        Assert.Equal(new[] { "Idle:Enter", "Idle:Update", "Idle:PhysicsUpdate" }, log);
    }

    private sealed class FakeState : IState<object>
    {
        private readonly string _name;
        private readonly List<string> _log;

        public FakeState(string name, List<string> log)
        {
            _name = name;
            _log = log;
        }

        public void Enter(object context) => _log.Add($"{_name}:Enter");

        public void Exit(object context) => _log.Add($"{_name}:Exit");

        public void Update(object context, double delta) => _log.Add($"{_name}:Update");

        public void PhysicsUpdate(object context, double delta) =>
            _log.Add($"{_name}:PhysicsUpdate");
    }
}
