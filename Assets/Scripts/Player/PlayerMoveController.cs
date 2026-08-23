using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(PlayerInput))]
[RequireComponent(typeof(PlayerStatController))]
public class PlayerMoveController : MonoBehaviour
{
    private float currentMoveSpeed;
    private Rigidbody2D playerRigidbody;
    private PlayerStatController playerStatController;
    private InputAction moveAction;
    private Collider2D bodyCollider;
    private int blockingLayerMask;

    public Vector2 MovementInput { get; private set; }

    public bool CanMove { get; set; } = true;

    private void Awake()
    {
        playerRigidbody = GetComponent<Rigidbody2D>();
        playerStatController = GetComponent<PlayerStatController>();
        moveAction = GetComponent<PlayerInput>().actions.FindAction("Move", true);
        bodyCollider = GetComponent<Collider2D>();

        int enemyLayer = LayerMask.NameToLayer("Enemy");
        int obstacleLayer = LayerMask.NameToLayer("Obstacle");
        blockingLayerMask = 0;
        if (enemyLayer >= 0) blockingLayerMask |= 1 << enemyLayer;
        if (obstacleLayer >= 0) blockingLayerMask |= 1 << obstacleLayer;
    }

    private void Start()
    {
        currentMoveSpeed = playerStatController.DefaultMoveSpeed;
    }

    private void FixedUpdate()
    {
        MovementInput = Vector2.ClampMagnitude(moveAction.ReadValue<Vector2>(), 1f);

        if (!CanMove)
        {
            return;
        }

        Vector2 delta = MovementInput * currentMoveSpeed * Time.fixedDeltaTime;
        TryMove(delta);
    }

    private void TryMove(Vector2 delta)
    {
        if (delta.sqrMagnitude < 0.000001f)
        {
            return;
        }

        Vector2 target = playerRigidbody.position + delta;
        if (!IsBlocked(target, delta))
        {
            playerRigidbody.MovePosition(target);
            return;
        }

        // 가로막혔을 때 축별로 슬라이딩 시도 (밀어내지 않고 막힘만 유지)
        Vector2 deltaX = new Vector2(delta.x, 0f);
        Vector2 deltaY = new Vector2(0f, delta.y);
        Vector2 targetX = playerRigidbody.position + deltaX;
        Vector2 targetY = playerRigidbody.position + deltaY;

        bool blockedX = Mathf.Abs(delta.x) < 0.000001f || IsBlocked(targetX, deltaX);
        bool blockedY = Mathf.Abs(delta.y) < 0.000001f || IsBlocked(targetY, deltaY);

        if (!blockedX && blockedY)
        {
            playerRigidbody.MovePosition(targetX);
        }
        else if (blockedX && !blockedY)
        {
            playerRigidbody.MovePosition(targetY);
        }
        // 둘 다 막히면 이동하지 않음 (밀어내지 않음)
    }

    private bool IsBlocked(Vector2 targetPosition, Vector2 delta)
    {
        if (bodyCollider == null || blockingLayerMask == 0)
        {
            return false;
        }

        Vector2 worldOffset = transform.TransformVector(bodyCollider.offset);
        Vector2 worldCenter = targetPosition + worldOffset;
        Vector2 worldSize = bodyCollider.bounds.size;

        Collider2D hit = Physics2D.OverlapBox(worldCenter, worldSize, 0f, blockingLayerMask);
        if (hit == null || hit == bodyCollider || hit.isTrigger)
        {
            return false;
        }

        // 자기 자신 계층의 콜라이더 제외 (자식 히트박스는 트리거이므로 이미 제외됨)
        if (hit.transform.IsChildOf(transform))
        {
            return false;
        }

        // 이미 겹쳐있는 상태에서 멀어지는 방향이면 허용 (빠져나올 수 있게)
        if (delta.sqrMagnitude > 0.000001f)
        {
            Vector2 toHit = (Vector2)hit.bounds.center - worldCenter;
            if (Vector2.Dot(delta.normalized, toHit.normalized) < -0.2f)
            {
                return false;
            }
        }

        return true;
    }
}
