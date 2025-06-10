using UnityEngine;

public class PlayerComboAttackState : PlayerAttackState
{
    private bool alreadyAppliedForce;
    private bool alreadyApplyCombo;

    AttackInfo attackInfo;

    public PlayerComboAttackState(PlayerStateMachine playerStateMachine) : base(playerStateMachine)
    {

    }

    public override void Enter()
    {
        base.Enter();
        StartAnim(stateMachine.Player.AnimationData.ComboAttackParameterHash);

        alreadyApplyCombo = false;  // 진입할 때, 정보 초기화
        alreadyAppliedForce = false;

        int comboIndex = stateMachine.ComboIndex;
        attackInfo = stateMachine.Player.Data.AttackData.GetAttackInfo(comboIndex);
        stateMachine.Player.Animator.SetInteger("Combo", comboIndex);
    }

    public override void Exit()
    {
        base.Exit();
        StopAnim(stateMachine.Player.AnimationData.ComboAttackParameterHash);

        if (!alreadyApplyCombo)
        {
            stateMachine.ComboIndex = 0;
        }
    }

    private void TryComboAttack()
    {
        if (alreadyApplyCombo) return;
        if (attackInfo.ComboStateIndex == -1) return;
        if (!stateMachine.IsAttacking) return;
        alreadyApplyCombo = true;
    }

    private void TryApplyForce()
    {
        if (alreadyAppliedForce) return;
        alreadyAppliedForce = true;

        stateMachine.Player.ForceReceiver.Reset();
        stateMachine.Player.ForceReceiver.AddForce(stateMachine.Player.transform.forward * attackInfo.Force);
    }

    public override void Update()
    {
        base.Update();

        ForceMove();

        float normalizedTime = GetNormalizedTime(stateMachine.Player.Animator, "Attack");
        if (normalizedTime < 1f)
        {
            if (normalizedTime >= attackInfo.ForceTransitionTime)
                TryApplyForce();

            if (normalizedTime >= attackInfo.ComboTransitionTime)
                TryComboAttack();
        }
        else
        {
            if (alreadyApplyCombo)
            {
                stateMachine.ComboIndex = attackInfo.ComboStateIndex;
                stateMachine.ChangeState(stateMachine.ComboAttackState);
            }
            else
            {
                stateMachine.ChangeState(stateMachine.IdleState);
            }
        }
        
    }

}
