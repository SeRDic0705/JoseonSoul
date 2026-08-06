
public abstract class StateMachine
{
    protected IState currentState;

    public IState CurrentState => currentState;

    public void ChangeState(IState newState)
    {
        currentState?.Exit();   // 이전 상태 종료
        currentState = newState;    // 새로운 state 할당
        currentState?.Enter();  // 할당된 새 state 진입
    }

    public void HandleInput()
    {
        currentState?.HandleInput();
    }

    public void Update()
    {
        currentState?.Update();
    }

    public void PhysicsUpdate()
    {
        currentState?.PhysicsUpdate();
    }
}
