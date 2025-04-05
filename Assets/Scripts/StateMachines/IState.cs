public interface IState
{

    public void Enter();    // 진입 시
    public void Exit();     // 나갈 때
    public void HandleInput();      // IState 중에 입력처리 시
    public void Update();
    public void PhysicsUpdate();

}