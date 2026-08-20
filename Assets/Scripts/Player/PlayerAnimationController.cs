using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Animator), typeof(PlayerMoveController))]
public class PlayerAnimationController : MonoBehaviour
{
    private const string AttackActionName = "Attack";
    private const string MoveRightParameterName = "MoveRight";
    private const string MoveUpParameterName = "MoveUp";
    private const int MaxAttackCount = 3;
    private static readonly int AttackState1Hash = Animator.StringToHash("player_attack_right_1");
    private static readonly int AttackState2Hash = Animator.StringToHash("player_attack_right_2");
    private static readonly int AttackState3Hash = Animator.StringToHash("player_attack_right_3");

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
    private InputAction attackAction;
    private SpriteRenderer spriteRenderer;
    private int animatorMoveParameterHash;
    private int attackTriggerParameterHash;
    private int attackCountParameterHash;
    private int moveRightParameterHash;
    private int moveUpParameterHash;
    private int currentAttackCount;
    private float comboWindowRemaining;
    private bool comboWindowWasOpened;
    private bool isAttacking;
    private bool attackAnimationHasStarted;
    private bool isFacingRight = true;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        playerMoveController = GetComponent<PlayerMoveController>();
        attackAction = GetComponent<PlayerInput>().actions.FindAction(AttackActionName, true);
        spriteRenderer = GetComponent<SpriteRenderer>();

        animatorMoveParameterHash = Animator.StringToHash(animatorMoveParameterName);
        attackTriggerParameterHash = Animator.StringToHash(attackTriggerParameterName);
        attackCountParameterHash = Animator.StringToHash(attackCountParameterName);
        moveRightParameterHash = Animator.StringToHash(MoveRightParameterName);
        moveUpParameterHash = Animator.StringToHash(MoveUpParameterName);

        if (spriteRenderer != null)
        {
            isFacingRight = !spriteRenderer.flipX;
        }
    }

    private void Update()
    {
        UpdateFacingDirection();
        ChangePlayerMoveAnimationPerFrame();
        UpdateAttackAnimationState();
        ProcessAttackInput();
    }
    
    void ChangePlayerMoveAnimationPerFrame()
    {
        bool isMoving = playerMoveController.MovementInput.sqrMagnitude > 0.0001f;
        animator.SetBool(animatorMoveParameterHash, isMoving);
    }

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

    private void ProcessAttackInput()
    {
        if (!attackAction.WasPressedThisFrame())
        {
            return;
        }

        if (!isAttacking)
        {
            StartAttack();
            return;
        }

        if (currentAttackCount < MaxAttackCount && comboWindowRemaining > 0f)
        {
            StartNextAttack();
        }
    }

    private void StartAttack()
    {
        isAttacking = true;
        attackAnimationHasStarted = false;
        currentAttackCount = 1;
        comboWindowRemaining = 0f;
        comboWindowWasOpened = false;
        SetAttackDirection(playerMoveController.MovementInput);
        animator.SetInteger(attackCountParameterHash, currentAttackCount);
        animator.SetTrigger(attackTriggerParameterHash);
    }

    private void StartNextAttack()
    {
        currentAttackCount++;
        comboWindowRemaining = 0f;
        comboWindowWasOpened = false;
        SetAttackDirection(playerMoveController.MovementInput);
        animator.SetInteger(attackCountParameterHash, currentAttackCount);
        animator.SetTrigger(attackTriggerParameterHash);
    }

    private void SetAttackDirection(Vector2 movementInput)
    {
        bool hasVerticalInput = Mathf.Abs(movementInput.x) < 0.0001f &&
                                Mathf.Abs(movementInput.y) > 0.0001f;
        bool attackUp = hasVerticalInput && movementInput.y > 0f;

        if (movementInput.x > 0f)
        {
            isFacingRight = true;
        }
        else if (movementInput.x < 0f)
        {
            isFacingRight = false;
        }

        animator.SetBool(moveRightParameterHash, isFacingRight);
        animator.SetBool(moveUpParameterHash, attackUp);
    }

    private void UpdateAttackAnimationState()
    {
        if (!isAttacking)
        {
            return;
        }

        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);

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

    private bool IsAttackState(AnimatorStateInfo stateInfo)
    {
        return stateInfo.shortNameHash == AttackState1Hash ||
               stateInfo.shortNameHash == AttackState2Hash ||
               stateInfo.shortNameHash == AttackState3Hash;
    }

    private void ResetAttackState()
    {
        isAttacking = false;
        attackAnimationHasStarted = false;
        currentAttackCount = 0;
        comboWindowRemaining = 0f;
        comboWindowWasOpened = false;
        animator.SetInteger(attackCountParameterHash, 0);
    }
}
