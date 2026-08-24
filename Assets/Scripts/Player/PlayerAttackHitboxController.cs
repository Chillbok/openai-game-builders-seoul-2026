using UnityEngine;

[RequireComponent(typeof(Animator))]
public class PlayerAttackHitboxController : MonoBehaviour
{
    private const string AttackCounterParameterName = "AttackCounter";
    private const string AttackXParameterName = "AttackX";
    private const string AttackYParameterName = "AttackY";

    [Header("방향별 히트박스")]
    [Tooltip("오른쪽 공격의 콤보 단계(1~3) 순서로 배치한 히트박스 컨트롤러들")]
    [SerializeField]
    private HitboxController[] rightAttackHitboxes;

    [Tooltip("왼쪽 공격의 콤보 단계(1~3) 순서로 배치한 히트박스 컨트롤러들")]
    [SerializeField]
    private HitboxController[] leftAttackHitboxes;

    [Tooltip("위쪽 공격의 콤보 단계(1~3) 순서로 배치한 히트박스 컨트롤러들")]
    [SerializeField]
    private HitboxController[] upAttackHitboxes;

    [Tooltip("아래쪽 공격의 콤보 단계(1~3) 순서로 배치한 히트박스 컨트롤러들")]
    [SerializeField]
    private HitboxController[] downAttackHitboxes;

    private Animator animator;
    private PlayerExecutionController playerExecutionController;
    private int attackCounterParameterHash;
    private int attackXParameterHash;
    private int attackYParameterHash;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        playerExecutionController = GetComponent<PlayerExecutionController>();
        attackCounterParameterHash = Animator.StringToHash(AttackCounterParameterName);
        attackXParameterHash = Animator.StringToHash(AttackXParameterName);
        attackYParameterHash = Animator.StringToHash(AttackYParameterName);
    }

    // 공격 애니메이션 이벤트에서 호출해 현재 공격 방향과 콤보 단계에 해당하는 히트박스를 활성화한다.
    public void ActivateHitbox()
    {
        if (playerExecutionController != null && playerExecutionController.IsBusy)
        {
            return;
        }

        HitboxController[] hitboxes = GetCurrentDirectionHitboxes();
        if (hitboxes == null)
        {
            return;
        }

        int hitboxIndex = animator.GetInteger(attackCounterParameterHash) - 1;
        if (hitboxIndex < 0 || hitboxIndex >= hitboxes.Length)
        {
            return;
        }

        HitboxController hitbox = hitboxes[hitboxIndex];
        if (hitbox != null)
        {
            hitbox.EnableHitbox();
        }
    }

    // 공격 애니메이션 이벤트에서 호출해 현재 공격 방향의 모든 히트박스를 비활성화한다.
    public void DeactivateHitbox()
    {
        HitboxController[] hitboxes = GetCurrentDirectionHitboxes();
        if (hitboxes == null)
        {
            return;
        }

        foreach (HitboxController hitbox in hitboxes)
        {
            if (hitbox != null)
            {
                hitbox.DisableHitbox();
            }
        }
    }

    // 처형 시작 또는 처형 종료 시 모든 일반 공격 히트박스를 끈다.
    public void DisableAllHitboxes()
    {
        DisableHitboxes(rightAttackHitboxes);
        DisableHitboxes(leftAttackHitboxes);
        DisableHitboxes(upAttackHitboxes);
        DisableHitboxes(downAttackHitboxes);
    }

    private static void DisableHitboxes(HitboxController[] hitboxes)
    {
        if (hitboxes == null)
        {
            return;
        }

        foreach (HitboxController hitbox in hitboxes)
        {
            if (hitbox != null)
            {
                hitbox.DisableHitbox();
            }
        }
    }

    // 애니메이터의 공격 방향 파라미터로 현재 공격 방향의 히트박스 배열을 찾는다.
    private HitboxController[] GetCurrentDirectionHitboxes()
    {
        float attackX = animator.GetFloat(attackXParameterHash);
        float attackY = animator.GetFloat(attackYParameterHash);

        // PlayerAnimationController와 동일하게 좌우 입력을 우선으로 판단한다.
        if (Mathf.Abs(attackX) > 0.0001f)
        {
            return attackX > 0f ? rightAttackHitboxes : leftAttackHitboxes;
        }

        if (Mathf.Abs(attackY) > 0.0001f)
        {
            return attackY > 0f ? upAttackHitboxes : downAttackHitboxes;
        }

        return null;
    }
}
