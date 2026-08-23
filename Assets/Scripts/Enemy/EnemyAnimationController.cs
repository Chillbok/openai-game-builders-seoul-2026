using UnityEngine;

[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(SpriteRenderer))]
public class EnemyAnimationController : MonoBehaviour
{
    private const string IsMovingParameterName = "IsMoving";
    private const string AttackParameterName = "Attack";
    private const string HurtParameterName = "Hurt";
    private const string DeathParameterName = "Death";

    private Animator animator;
    private SpriteRenderer spriteRenderer;

    private int isMovingParameterHash;
    private int attackParameterHash;
    private int hurtParameterHash;
    private int deathParameterHash;

    // 애니메이터와 스프라이트 렌더러, 패러미터 해시를 초기화한다.
    private void Awake()
    {
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        isMovingParameterHash = Animator.StringToHash(IsMovingParameterName);
        attackParameterHash = Animator.StringToHash(AttackParameterName);
        hurtParameterHash = Animator.StringToHash(HurtParameterName);
        deathParameterHash = Animator.StringToHash(DeathParameterName);
    }

    // 추적 중 이동 여부를 애니메이터의 이동 Bool 패러미터에 반영한다.
    public void SetMoving(bool moving)
    {
        animator.SetBool(isMovingParameterHash, moving);
    }

    // 공격 트리거 패러미터를 설정해 공격 애니메이션을 재생한다.
    public void PlayAttack()
    {
        animator.SetTrigger(attackParameterHash);
    }

    // 준비 구간에서 공격 애니메이션을 현재 프레임에 멈춘다.
    public void PauseAttackAnimation()
    {
        animator.speed = 0f;
    }

    // 일시정지한 공격 애니메이션을 현재 프레임부터 재개한다.
    public void ResumeAnimation()
    {
        animator.speed = 1f;
    }

    // 피격 트리거 패러미터를 설정해 피격 애니메이션을 재생한다.
    public void PlayHurt()
    {
        animator.SetTrigger(hurtParameterHash);
    }

    // 사망 트리거 패러미터를 설정해 사망 애니메이션을 재생한다.
    public void PlayDeath()
    {
        animator.SetTrigger(deathParameterHash);
    }

    // 모든 트리거 패러미터를 초기 상태로 되돌린다.
    public void ResetTriggers()
    {
        animator.ResetTrigger(attackParameterHash);
        animator.ResetTrigger(hurtParameterHash);
        animator.ResetTrigger(deathParameterHash);
    }

    // 바라보는 방향에 따라 스프라이트를 좌우 반전한다.
    public void SetFacingRight(bool facingRight)
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.flipX = !facingRight;
        }
    }
}
