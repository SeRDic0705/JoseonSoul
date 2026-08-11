public class PlayerAirComboAttackState : PlayerComboAttackStateBase
{
    public PlayerAirComboAttackState(PlayerStateMachine playerStateMachine) : base(playerStateMachine)
    {
    }

    protected override AttackInfo GetAttackInfo(int comboIndex) => stateMachine.Player.Data.AttackData.GetAirAttackInfo(comboIndex);
    protected override IState ComboChainState => stateMachine.AirComboAttackState;
    protected override int ComboAnimatorParameterHash => stateMachine.Player.AnimationData.AirComboAttackParameterHash;
    protected override int AttackFamilyParameterHash => stateMachine.Player.AnimationData.AirAttackParameterHash;

    public override void Enter()
    {
        base.Enter();
        stateMachine.Player.ForceReceiver.SuspendGravity();    // 공격 모션 재생 중 제자리에 호버(Design/AirState_Design.md §8-3)
    }

    public override void Exit()
    {
        base.Exit();
        stateMachine.Player.ForceReceiver.ResumeGravity();
    }
}
