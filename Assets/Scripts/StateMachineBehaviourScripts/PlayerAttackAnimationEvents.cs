using UnityEngine;

public class PlayerAttackAnimationEvents : StateMachineBehaviour
{
    private const string AttackYParameterName = "AttackY";
    private const float VerticalAttackDeadZone = 0.0001f;

    private static readonly int AttackYParameterHash = Animator.StringToHash(AttackYParameterName);

    // OnStateEnter is called when a transition starts and the state machine starts to evaluate this state
    public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        PlayerMoveController moveController = animator.GetComponent<PlayerMoveController>();
        if (moveController != null)
        {
            moveController.CanMove = false;
        }

        ApplyVerticalAttackFlip(animator);
    }

    // OnStateUpdate is called on each Update frame between OnStateEnter and OnStateExit callbacks
    //override public void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    //{
    //    
    //}

    // OnStateExit is called when a transition ends and the state machine finishes evaluating this state
    public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        PlayerMoveController moveController = animator.GetComponent<PlayerMoveController>();
        if (moveController != null)
        {
            moveController.CanMove = true;
        }

        SpriteFlip spriteFlip = animator.GetComponent<SpriteFlip>();
        if (spriteFlip != null)
        {
            spriteFlip.ClearFlipXOverride();
        }
    }

    // OnStateMove is called right after Animator.OnAnimatorMove()
    //override public void OnStateMove(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    //{
    //    // Implement code that processes and affects root motion
    //}

    // OnStateIK is called right after Animator.OnAnimatorIK()
    //override public void OnStateIK(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    //{
    //    // Implement code that sets up animation IK (inverse kinematics)
    //}

    // 위/아래 공격 상태에서는 원본 스프라이트 방향이 어울리도록 좌우 반전을 취소한다.
    private void ApplyVerticalAttackFlip(Animator animator)
    {
        SpriteFlip spriteFlip = animator.GetComponent<SpriteFlip>();
        if (spriteFlip == null)
        {
            return;
        }

        if (Mathf.Abs(animator.GetFloat(AttackYParameterHash)) > VerticalAttackDeadZone)
        {
            spriteFlip.SetFlipXOverride(false);
        }
        else
        {
            spriteFlip.ClearFlipXOverride();
        }
    }
}