public class PlayerAirComboAttackState : PlayerComboAttackStateBase, IAirborneState
{
    public PlayerAirComboAttackState(PlayerStateMachine playerStateMachine) : base(playerStateMachine)
    {
    }

    protected override AttackInfo GetAttackInfo(int comboIndex) => stateMachine.Player.Data.AttackData.GetAirAttackInfo(comboIndex);
    protected override bool IsAirCombo => true;
    protected override bool FacesLockedTarget => stateMachine.AirborneFacesLockedTarget;    // 지상->공중 경계에서 스냅샷된 값을 그대로 물려받음
    protected override int ComboAnimatorParameterHash => stateMachine.Player.AnimationData.AirComboAttackParameterHash;
    protected override int AttackFamilyParameterHash => stateMachine.Player.AnimationData.AirAttackParameterHash;
}
