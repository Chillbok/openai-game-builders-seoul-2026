using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(PlayerStatController))]
[RequireComponent(typeof(PlayerMoveController))]
public class PlayerDodge : MonoBehaviour
{
    private Rigidbody2D playerRigidbody;
    private PlayerStatController playerStatController;
    private PlayerMoveController playerMoveController;
    private InputAction dodgeAction;

    private bool isDodging;
    private float dodgeTimer;
    private Vector2 dodgeDirection;
    private float dodgeStartTime;
    private bool perfectDodgeConsumed;

    // 컴포넌트 참조와 입력 액션을 초기화한다.
    private void Awake()
    {
        playerRigidbody = GetComponent<Rigidbody2D>();
        playerStatController = GetComponent<PlayerStatController>();
        playerMoveController = GetComponent<PlayerMoveController>();
        dodgeAction = GetComponent<PlayerInput>().actions.FindAction("Dodge", true);
    }

    // 회피 타이머 갱신과 회피 입력 감지를 처리한다.
    private void Update()
    {
        if (!playerStatController.IsInitialized)
        {
            return;
        }

        if (isDodging)
        {
            dodgeTimer -= Time.deltaTime;
            if (dodgeTimer <= 0f)
            {
                EndDodge();
            }
            return;
        }

        if (dodgeAction.WasPressedThisFrame())
        {
            TryStartDodge();
        }
    }

    private Collider2D bodyCollider;
    private NoPushCollisionMover2D dodgeCollisionMover;

    private void Start()
    {
        bodyCollider = GetComponent<Collider2D>();
        int obstacleLayer = LayerMask.NameToLayer("Obstacle");
        int dodgeBlockingMask = 0;
        if (obstacleLayer >= 0) dodgeBlockingMask |= 1 << obstacleLayer;
        dodgeCollisionMover = new NoPushCollisionMover2D(
            playerRigidbody,
            bodyCollider,
            transform,
            dodgeBlockingMask);
    }

    // 회피 중에는 적을 통과하되 벽과 장애물에는 막히도록 물리 이동을 적용한다.
    private void FixedUpdate()
    {
        if (!isDodging)
        {
            return;
        }

        Vector2 delta = dodgeDirection * playerStatController.DodgeSpeed * Time.fixedDeltaTime;
        dodgeCollisionMover?.Move(delta);
    }

    // 회피 차지 소모 후 방향과 지속시간을 설정하고 회피를 시작한다.
    private void TryStartDodge()
    {
        if (!playerStatController.TryConsumeDodge())
        {
            return;
        }

        Vector2 moveInput = playerMoveController.MovementInput;
        if (moveInput.sqrMagnitude < 0.0001f)
        {
            moveInput = new Vector2(playerMoveController.transform.localScale.x > 0 ? 1f : -1f, 0f);
        }

        dodgeDirection = moveInput.normalized;
        float dodgeDuration = playerStatController.DodgeLength / Mathf.Max(0.0001f, playerStatController.DodgeSpeed);

        isDodging = true;
        dodgeTimer = dodgeDuration;
        dodgeStartTime = Time.time;
        perfectDodgeConsumed = false;
        playerMoveController.CanMove = false;
    }

    // 회피 시작 후 완벽한 회피 인정 시간이 지났는지 확인한다.
    private bool IsPastPerfectDodgeAcceptance()
    {
        return Time.time - dodgeStartTime > playerStatController.PerfectDodgeAcceptanceTime;
    }

    // 회피 시작 후 인정 시간 안에 피해 판정이 들어오면 완벽한 회피로 인정한다. 회피당 한 번만 인정된다.
    public bool TryMarkPerfectDodge()
    {
        if (!isDodging || perfectDodgeConsumed || IsPastPerfectDodgeAcceptance())
        {
            return false;
        }

        perfectDodgeConsumed = true;
        return true;
    }

    // 회피 상태를 해제하고 일반 이동을 다시 허용한다.
    private void EndDodge()
    {
        isDodging = false;
        dodgeTimer = 0f;
        perfectDodgeConsumed = false;
        playerMoveController.CanMove = true;
    }

    // 현재 회피 중인지 반환한다. 회피 중에는 무적이므로 피해 판정 무효화에도 사용된다.
    public bool IsDodging => isDodging;
}
