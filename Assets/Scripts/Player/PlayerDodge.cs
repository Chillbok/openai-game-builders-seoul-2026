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

    // 회피 중일 때 물리 이동을 적용한다.
    private void FixedUpdate()
    {
        if (!isDodging)
        {
            return;
        }

        playerRigidbody.MovePosition(
            playerRigidbody.position + dodgeDirection * playerStatController.DodgeSpeed * Time.fixedDeltaTime);
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
        playerMoveController.CanMove = false;
    }

    // 회피 상태를 해제하고 일반 이동을 다시 허용한다.
    private void EndDodge()
    {
        isDodging = false;
        dodgeTimer = 0f;
        playerMoveController.CanMove = true;
    }

    // 현재 회피 중인지 여부를 반환한다.
    public bool IsDodging => isDodging;
}