public class PlayerAirComboAttackState : PlayerComboAttackStateBase
{
    public PlayerAirComboAttackState(PlayerStateMachine playerStateMachine) : base(playerStateMachine)
    {
    }

    protected override AttackInfo GetAttackInfo(int comboIndex) => stateMachine.Player.Data.AttackData.GetAirAttackInfo(comboIndex);
    protected override IState ComboChainState => stateMachine.AirComboAttackState;
    protected override int ComboAnimatorParameterHash => stateMachine.Player.AnimationData.AirComboAttackParameterHash;
    protected override int AttackFamilyParameterHash => stateMachine.Player.AnimationData.AirAttackParameterHash;
}
