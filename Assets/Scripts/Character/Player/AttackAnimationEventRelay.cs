using UnityEngine;

// 애니메이션 클립의 Animation Event가 호출하는 진입점.
// 현재 상태가 해당 인터페이스를 구현하지 않으면(공격 상태가 아니거나 상태가 이미 바뀌었으면) 조용히 무시한다.
public class AttackAnimationEventRelay : MonoBehaviour
{
    private Player player;

    private void Awake()
    {
        player = GetComponentInParent<Player>();
    }

    public void OnApplyForce()
    {
        if (player.StateMachine.CurrentState is IForceEventReceiver receiver)
            receiver.OnApplyForce();
    }

    public void OnOpenComboWindow()
    {
        if (player.StateMachine.CurrentState is IComboWindowEventReceiver receiver)
            receiver.OnOpenComboWindow();
    }

    public void OnCloseComboWindow()
    {
        if (player.StateMachine.CurrentState is IComboWindowEventReceiver receiver)
            receiver.OnCloseComboWindow();
    }
}
