public class PlayerComboAttackState : PlayerComboAttackStateBase
{
    public PlayerComboAttackState(PlayerStateMachine playerStateMachine) : base(playerStateMachine)
    {
    }

    protected override AttackInfo GetAttackInfo(int comboIndex) => stateMachine.Player.Data.AttackData.GetAttackInfo(comboIndex);
    protected override bool IsAirCombo => false;
    protected override int ComboAnimatorParameterHash => stateMachine.Player.AnimationData.ComboAttackParameterHash;
}
