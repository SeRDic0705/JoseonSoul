public class PlayerComboAttackState : PlayerComboAttackStateBase
{
    public PlayerComboAttackState(PlayerStateMachine playerStateMachine) : base(playerStateMachine)
    {
    }

    protected override AttackInfo GetAttackInfo(int comboIndex) => stateMachine.Player.Data.AttackData.GetAttackInfo(comboIndex);
    protected override IState ComboChainState => stateMachine.ComboAttackState;
    protected override int ComboAnimatorParameterHash => stateMachine.Player.AnimationData.ComboAttackParameterHash;
}
