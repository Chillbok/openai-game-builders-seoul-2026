using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Animator), typeof(PlayerMoveController), typeof(PlayerStatController))]
public class PlayerAnimationController : MonoBehaviour
{
    private const string AttackActionName = "Attack";
    private const string MoveRightParameterName = "MoveRight";
    private const string AttackXParameterName = "AttackX";
    private const string AttackYParameterName = "AttackY";
    private const string HitParameterName = "Hit";
    private const string DieParameterName = "Die";

    [Header("패러미터 이름들")]
    [Header("이동 관련")]
    [SerializeField]
    [Tooltip("플레이어가 이동 중인지 확인하기 위한 패러미터 이름")]
    private string animatorMoveParameterName;

    [Header("공격 관련")]
    [SerializeField]
    [Tooltip("플레이어의 공격 트리거 패러미터 이름")]
    // 이 변수는 트리거 패러미터임
    private string attackTriggerParameterName;
    
    [SerializeField]
    [Tooltip("공격 카운트를 위한 패러미터 이름")]
    // 이 변수는 int 패러미터임
    private string attackCountParameterName;

    [Header("공격 설정")]
    [SerializeField, Min(0f)]
    [Tooltip("공격 모션이 끝난 뒤 다음 타 입력을 받을 수 있는 시간(초)")]
    private float comboInputWindow = 0.5f;

    private Animator animator;
    private PlayerMoveController playerMoveController;
    private PlayerStatController playerStatController;
    private PlayerExecutionController playerExecutionController;
    private InputAction attackAction;
    private SpriteRenderer spriteRenderer;
    private int animatorMoveParameterHash;
    private int attackTriggerParameterHash;
    private int attackCountParameterHash;
    private int moveRightParameterHash;
    private int attackXParameterHash;
    private int attackYParameterHash;
    private int hitParameterHash;
    private int dieParameterHash;
    private int hitStateHash;
    private float comboWindowRemaining;
    private bool comboWindowWasOpened;
    private bool isAttacking;
    private bool attackAnimationHasStarted;
    private bool isHit;
    private bool isFacingRight = true;

    // 애니메이터와 입력 액션, 방향 관련 참조와 파라미터 해시를 초기화한다.
    private void Awake()
    {
        animator = GetComponent<Animator>();
        playerMoveController = GetComponent<PlayerMoveController>();
        playerStatController = GetComponent<PlayerStatController>();
        playerExecutionController = GetComponent<PlayerExecutionController>();
        attackAction = GetComponent<PlayerInput>().actions.FindAction(AttackActionName, true);
        spriteRenderer = GetComponent<SpriteRenderer>();

        animatorMoveParameterHash = Animator.StringToHash(animatorMoveParameterName);
        attackTriggerParameterHash = Animator.StringToHash(attackTriggerParameterName);
        attackCountParameterHash = Animator.StringToHash(attackCountParameterName);
        moveRightParameterHash = Animator.StringToHash(MoveRightParameterName);
        attackXParameterHash = Animator.StringToHash(AttackXParameterName);
        attackYParameterHash = Animator.StringToHash(AttackYParameterName);
        hitParameterHash = Animator.StringToHash(HitParameterName);
        dieParameterHash = Animator.StringToHash(DieParameterName);
        hitStateHash = Animator.StringToHash("player_hit 0");

        if (spriteRenderer != null)
        {
            isFacingRight = !spriteRenderer.flipX;
        }
    }

    // 실제 피해가 적용되었을 때 공격 상태를 끊고 피격 애니메이션을 시작한다.
    private void OnEnable()
    {
        if (playerStatController != null)
        {
            playerStatController.Damaged += HandleDamaged;
        }
    }

    private void OnDisable()
    {
        if (playerStatController != null)
        {
            playerStatController.Damaged -= HandleDamaged;
        }
    }

    // 매 프레임 이동 방향, 이동 애니메이션, 공격 상태와 공격 입력을 갱신한다.
    private void Update()
    {
        if (playerStatController != null && playerStatController.IsDead)
        {
            return;
        }

        if (playerExecutionController != null && playerExecutionController.IsBusy)
        {
            return;
        }

        UpdateFacingDirection();
        ChangePlayerMoveAnimationPerFrame();
        UpdateAttackAnimationState();
        UpdateHitAnimationState();
        ProcessAttackInput();
    }

    // 피격 애니메이션이 끝날 때까지 이동과 신규 공격 입력을 막는다.
    private void UpdateHitAnimationState()
    {
        if (!isHit)
        {
            return;
        }

        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
        if (stateInfo.shortNameHash == hitStateHash || animator.IsInTransition(0))
        {
            playerMoveController.CanMove = false;
            return;
        }

        isHit = false;
        playerMoveController.CanMove = true;
    }

    private void HandleDamaged()
    {
        if (playerStatController.IsDead)
        {
            return;
        }

        isAttacking = false;
        animator.speed = 1f;
        attackAnimationHasStarted = false;
        comboWindowRemaining = 0f;
        comboWindowWasOpened = false;
        playerStatController.ResetAttackCount();
        animator.ResetTrigger(attackTriggerParameterHash);
        animator.SetTrigger(hitParameterHash);
        isHit = true;
        playerMoveController.CanMove = false;
    }

    // 이동 입력의 유무를 애니메이터의 이동 Bool 파라미터에 반영한다.
    void ChangePlayerMoveAnimationPerFrame()
    {
        bool isMoving = playerMoveController.MovementInput.sqrMagnitude > 0.0001f;
        animator.SetBool(animatorMoveParameterHash, isMoving);
    }

    // 좌우 이동 입력이 있을 때 현재 플레이어가 바라보는 좌우 방향을 갱신한다.
    private void UpdateFacingDirection()
    {
        if (playerMoveController.MovementInput.x > 0f)
        {
            isFacingRight = true;
        }
        else if (playerMoveController.MovementInput.x < 0f)
        {
            isFacingRight = false;
        }
    }

    // 공격 입력을 읽고 신규 공격 또는 콤보 공격을 시작한다.
    private void ProcessAttackInput()
    {
        if (isHit || playerStatController.IsDead || (playerExecutionController != null && playerExecutionController.IsBusy))
        {
            return;
        }

        if (!attackAction.WasPressedThisFrame())
        {
            return;
        }

        if (!isAttacking)
        {
            StartAttack();
            return;
        }

        if (playerStatController.CurrentAttackCount < PlayerStatController.MaxAttackCount && comboWindowRemaining > 0f)
        {
            StartNextAttack();
        }
    }

    // 첫 번째 공격을 시작하고 입력 시점의 공격 방향을 저장한다.
    private void StartAttack()
    {
        isAttacking = true;
        SetAttackAnimatorSpeed();
        playerMoveController.CanMove = false;
        attackAnimationHasStarted = false;
        playerStatController.SetAttackCount(1);
        comboWindowRemaining = 0f;
        comboWindowWasOpened = false;
        SetAttackDirection(playerMoveController.MovementInput);
        animator.SetInteger(attackCountParameterHash, playerStatController.CurrentAttackCount);
        animator.SetTrigger(attackTriggerParameterHash);
    }

    // 콤보 공격을 시작하고 이번 타격의 입력 방향을 저장한다.
    private void StartNextAttack()
    {
        playerStatController.SetAttackCount(playerStatController.CurrentAttackCount + 1);
        SetAttackAnimatorSpeed();
        playerMoveController.CanMove = false;
        comboWindowRemaining = 0f;
        comboWindowWasOpened = false;
        SetAttackDirection(playerMoveController.MovementInput);
        animator.SetInteger(attackCountParameterHash, playerStatController.CurrentAttackCount);
        animator.SetTrigger(attackTriggerParameterHash);
    }

    // 좌우 입력을 우선으로 판단하고, 좌우 입력이 없을 때만 위아래 방향을 선택한다.
    private void SetAttackDirection(Vector2 movementInput)
    {
        const float deadZone = 0.0001f;
        Vector2 attackDirection;

        // 대각선 입력도 X 값이 조금이라도 있으면 좌우 공격으로 처리한다.
        if (Mathf.Abs(movementInput.x) > deadZone)
        {
            isFacingRight = movementInput.x > 0f;
            attackDirection = new Vector2(isFacingRight ? 1f : -1f, 0f);
        }
        // 좌우 입력이 없을 때만 Y 값으로 위아래 공격을 결정한다.
        else if (Mathf.Abs(movementInput.y) > deadZone)
        {
            attackDirection = new Vector2(0f, movementInput.y > 0f ? 1f : -1f);
        }
        // 공격 방향 입력이 없으면 기존 좌우 바라보기 방향으로 공격한다.
        else
        {
            attackDirection = new Vector2(isFacingRight ? 1f : -1f, 0f);
        }

        animator.SetBool(moveRightParameterHash, isFacingRight);
        animator.SetFloat(attackXParameterHash, attackDirection.x);
        animator.SetFloat(attackYParameterHash, attackDirection.y);
    }

    // 공격 애니메이션의 진행 상태와 콤보 입력 가능 시간을 관리한다.
    private void UpdateAttackAnimationState()
    {
        if (!isAttacking)
        {
            animator.speed = 1f;
            return;
        }

        SetAttackAnimatorSpeed();

        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);

        // 공격 상태가 유지되는 동안은 이동을 차단한다. 콤보 전환 시 이전 타의
        // OnStateExit(CanMove = true)가 뒤늦게 발동해도 다음 프레임에 다시 잠근다.
        if (IsAttackState(stateInfo) && comboWindowRemaining <= 0f)
        {
            playerMoveController.CanMove = false;
        }

        // The trigger is evaluated by Animator after this Update. Keep the
        // counter until the attack state has actually been entered.
        if (!attackAnimationHasStarted)
        {
            if (IsAttackState(stateInfo))
            {
                attackAnimationHasStarted = true;
            }
            else
            {
                return;
            }
        }

        if (animator.IsInTransition(0))
        {
            return;
        }

        if (IsAttackState(stateInfo))
        {
            if (stateInfo.normalizedTime >= 1f && comboWindowRemaining <= 0f)
            {
                comboWindowRemaining = comboInputWindow;
                comboWindowWasOpened = true;
            }

            if (comboWindowRemaining > 0f)
            {
                comboWindowRemaining -= Time.deltaTime;
            }

            return;
        }

        if (attackAnimationHasStarted && !comboWindowWasOpened)
        {
            comboWindowRemaining = comboInputWindow;
            comboWindowWasOpened = true;
        }

        if (comboWindowRemaining > 0f)
        {
            comboWindowRemaining -= Time.deltaTime;
            return;
        }

        ResetAttackState();
    }

    // 애니메이터 상태에 Attack 태그가 붙어 있는지 확인한다.
    private bool IsAttackState(AnimatorStateInfo stateInfo)
    {
        return stateInfo.IsTag("Attack");
    }

    // 공격 상태와 콤보 카운터를 초기값으로 되돌린다.
    private void ResetAttackState()
    {
        isAttacking = false;
        animator.speed = 1f;
        attackAnimationHasStarted = false;
        playerStatController.ResetAttackCount();
        comboWindowRemaining = 0f;
        comboWindowWasOpened = false;
        playerMoveController.CanMove = true;
        animator.SetInteger(attackCountParameterHash, playerStatController.CurrentAttackCount);
    }

    // 처형 시작 시 일반 공격과 콤보 상태를 중단한다.
    public void CancelForExecution()
    {
        isAttacking = false;
        animator.speed = 1f;
        attackAnimationHasStarted = false;
        comboWindowRemaining = 0f;
        comboWindowWasOpened = false;
        playerStatController.ResetAttackCount();
        animator.ResetTrigger(attackTriggerParameterHash);
        playerMoveController.CanMove = false;
    }

    // 사망 시 Die 애니메이션을 재생하고 모든 공격 상태를 중단한다.
    public void PlayDie()
    {
        isAttacking = false;
        isHit = false;
        animator.speed = 1f;
        attackAnimationHasStarted = false;
        comboWindowRemaining = 0f;
        comboWindowWasOpened = false;
        if (playerStatController != null)
        {
            playerStatController.ResetAttackCount();
        }
        animator.ResetTrigger(attackTriggerParameterHash);
        animator.ResetTrigger(hitParameterHash);
        animator.SetTrigger(dieParameterHash);
        if (playerMoveController != null)
        {
            playerMoveController.CanMove = false;
        }
    }

    // 영혼 충전 2단계부터 공격 모션의 재생 속도만 높인다.
    private void SetAttackAnimatorSpeed()
    {
        float soulChargeMultiplier = playerStatController.CurrentSoulChargeStage >= 2
            ? playerStatController.SoulChargeAttackSpeedMultiplier
            : 1f;
        animator.speed = Mathf.Max(0f, playerStatController.AttackSpeed) * soulChargeMultiplier;
    }
}
