using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
[RequireComponent(typeof(PlayerInput))]
[RequireComponent(typeof(PlayerMoveController))]
[RequireComponent(typeof(PlayerStatController))]
[RequireComponent(typeof(PlayerDodge))]
[RequireComponent(typeof(PlayerAnimationController))]
[RequireComponent(typeof(PlayerAttackHitboxController))]
public sealed class PlayerExecutionController : MonoBehaviour
{
    private enum ExecutionState
    {
        Idle,
        Approaching,
        Presenting
    }

    private const string InteractActionName = "Interact";
    private const string ExecutionAnimationStateName = "player_attack_right_1";
    private const string ExecutionBackgroundObjectName = "ExecutionBackgroundEffect";
    private const float DefaultApproachSpeed = 12f;
    private const float DefaultContactDistance = 0.08f;
    private const float DefaultApproachTimeout = 2f;
    private const float DefaultAttackWindup = 0.18f;
    private const float DefaultAttackPause = 0.08f;
    private const float DefaultAttackRecovery = 0.36f;
    private const float DefaultAttackMoveDistance = 0.5f;
    private const float DefaultExecutionAnchorOffset = 0.6f;
    private const float DefaultCameraZoomMultiplier = 0.7f;

    [Header("접근")]
    [SerializeField, Min(0f)]
    [Tooltip("처형 접근 단계에서 플레이어가 대상에게 이동하는 속도")]
    private float approachSpeed = DefaultApproachSpeed;

    [SerializeField, Min(0f)]
    [Tooltip("대상 Collider와 이 거리 이하가 되면 접촉으로 처리하는 거리")]
    private float contactDistance = DefaultContactDistance;

    [SerializeField, Min(0f)]
    [Tooltip("장애물에 막혀 처형 접근이 끝나지 않을 때 취소하기까지의 제한 시간")]
    private float approachTimeout = DefaultApproachTimeout;

    [SerializeField]
    [Tooltip("처형 대상 검색에 사용할 레이어. 기본값은 Enemy 레이어")]
    private LayerMask executionTargetLayers = 1 << 7;

    [Header("연출")]
    [SerializeField, Min(0f)]
    [Tooltip("검격 전 애니메이션을 진행하는 시간")]
    private float attackWindup = DefaultAttackWindup;

    [SerializeField, Min(0f)]
    [Tooltip("검격 직전에 애니메이션을 멈추는 시간")]
    private float attackPause = DefaultAttackPause;

    [SerializeField, Min(0f)]
    [Tooltip("검격 후 연출을 유지하는 시간")]
    private float attackRecovery = DefaultAttackRecovery;

    [SerializeField, Min(0f)]
    [Tooltip("검격 시 플레이어가 대상의 오른쪽으로 이동하는 거리")]
    private float attackMoveDistance = DefaultAttackMoveDistance;

    [SerializeField, Min(0f)]
    [Tooltip("적 중심에서 왼쪽으로 떨어진 처형 시작 위치까지의 거리")]
    private float executionAnchorOffset = DefaultExecutionAnchorOffset;

    [SerializeField, Range(0.1f, 1f)]
    [Tooltip("처형 연출 중 카메라 orthographic size에 곱할 확대 배율")]
    private float cameraZoomMultiplier = DefaultCameraZoomMultiplier;

    [SerializeField, Min(0f)]
    [Tooltip("카메라가 처형 연출 위치와 확대 배율에 도달하는 시간. 값이 작을수록 빠르게 확대된다")]
    private float cameraZoomDuration = DefaultAttackWindup;

    [Header("렌더 순서")]
    [SerializeField]
    [Tooltip("처형 애니메이션 중 플레이어와 대상 SpriteRenderer에 적용할 Order in Layer. 숫자가 클수록 앞에 표시된다")]
    private int executionSortingOrder = 10;

    [Header("처형 보상")]
    [SerializeField, Min(0f)]
    [Tooltip("일반 적 처형 성공 시 회복하는 체력")]
    private float executionHealAmount = 25f;

    [Header("기존 씬 연결")]
    [SerializeField]
    [Tooltip("비워 두면 현재 씬에서 이름이 ExecutionBackgroundEffect인 오브젝트를 찾는다")]
    private GameObject executionBackgroundEffect;

    private Rigidbody2D playerRigidbody;
    private Collider2D playerCollider;
    private PlayerInput playerInput;
    private PlayerMoveController playerMoveController;
    private PlayerStatController playerStatController;
    private PlayerDodge playerDodge;
    private PlayerAnimationController playerAnimationController;
    private PlayerAttackHitboxController playerAttackHitboxController;
    private SpriteFlip spriteFlip;
    private Animator animator;
    private InputAction executionAction;
    private NoPushCollisionMover2D approachMover;
    private Collider2D[] targetBuffer = new Collider2D[32];
    private EnemyStateMachine target;
    private ExecutionState state;
    private float approachElapsed;
    private float presentationElapsed;
    private bool attackPaused;
    private bool executionApplied;
    private float previousAnimatorSpeed = 1f;
    private Camera presentationCamera;
    private Vector3 previousCameraPosition;
    private Quaternion previousCameraRotation;
    private float previousOrthographicSize;
    private bool cameraStateStored;
    private readonly List<SortingOrderSnapshot> sortingOrderSnapshots = new List<SortingOrderSnapshot>();

    private readonly struct SortingOrderSnapshot
    {
        public SortingOrderSnapshot(SpriteRenderer renderer)
        {
            Renderer = renderer;
            SortingLayerId = renderer.sortingLayerID;
            SortingOrder = renderer.sortingOrder;
        }

        public SpriteRenderer Renderer { get; }
        public int SortingLayerId { get; }
        public int SortingOrder { get; }
    }

    public bool IsBusy => state != ExecutionState.Idle;
    public bool IsPresenting => state == ExecutionState.Presenting;
    public EnemyStateMachine CurrentTarget => target;

    private void Awake()
    {
        playerRigidbody = GetComponent<Rigidbody2D>();
        playerCollider = GetComponent<Collider2D>();
        playerInput = GetComponent<PlayerInput>();
        playerMoveController = GetComponent<PlayerMoveController>();
        playerStatController = GetComponent<PlayerStatController>();
        playerDodge = GetComponent<PlayerDodge>();
        playerAnimationController = GetComponent<PlayerAnimationController>();
        playerAttackHitboxController = GetComponent<PlayerAttackHitboxController>();
        spriteFlip = GetComponent<SpriteFlip>();
        animator = GetComponent<Animator>();
        executionAction = playerInput.actions.FindAction(InteractActionName, true);

        int obstacleLayer = LayerMask.NameToLayer("Obstacle");
        int obstacleMask = obstacleLayer >= 0 ? 1 << obstacleLayer : 0;
        approachMover = new NoPushCollisionMover2D(
            playerRigidbody,
            playerCollider,
            transform,
            obstacleMask);

        if (executionBackgroundEffect == null)
        {
            executionBackgroundEffect = GameObject.Find(ExecutionBackgroundObjectName);
        }

        // 씬에 배치된 이펙트는 처형 중에만 보이도록 초기 상태를 끈다.
        SetExecutionBackgroundVisible(false);
    }

    private void OnEnable()
    {
        if (playerStatController != null)
        {
            playerStatController.Died += HandlePlayerDied;
        }
    }

    private void OnDisable()
    {
        if (playerStatController != null)
        {
            playerStatController.Died -= HandlePlayerDied;
        }

        CancelExecution();
    }

    private void Update()
    {
        if (playerStatController == null || !playerStatController.IsInitialized)
        {
            return;
        }

        if (state == ExecutionState.Idle)
        {
            if (!playerStatController.IsDead && executionAction.WasPressedThisFrame())
            {
                TryStartExecution();
            }

            return;
        }

        if (state == ExecutionState.Presenting)
        {
            UpdatePresentation(Time.deltaTime);
        }
    }

    private void FixedUpdate()
    {
        if (state != ExecutionState.Approaching)
        {
            return;
        }

        if (!IsApproachTargetValid())
        {
            CancelExecution();
            return;
        }

        approachElapsed += Time.fixedDeltaTime;
        if (approachTimeout > 0f && approachElapsed >= approachTimeout)
        {
            CancelExecution();
            return;
        }

        Vector2 executionAnchor = GetExecutionAnchor(target);
        if (IsAtExecutionAnchor(executionAnchor))
        {
            BeginPresentation();
            return;
        }

        Vector2 direction = (executionAnchor - playerRigidbody.position).normalized;
        if (spriteFlip != null)
        {
            const float deadZone = 0.001f;
            if (direction.x > deadZone || direction.x < -deadZone)
            {
                bool faceRight = direction.x > 0f;
                spriteFlip.SetFlipXOverride(!faceRight);
            }
        }

        approachMover.Move(direction * approachSpeed * Time.fixedDeltaTime);
        playerMoveController.CanMove = false;
    }

    private void LateUpdate()
    {
        if (state != ExecutionState.Presenting || target == null || presentationCamera == null)
        {
            return;
        }

        Vector3 midpoint = (transform.position + target.transform.position) * 0.5f;
        midpoint.z = previousCameraPosition.z;
        float cameraProgress = cameraZoomDuration <= 0f
            ? 1f
            : Mathf.Clamp01(presentationElapsed / cameraZoomDuration);
        presentationCamera.transform.position = Vector3.Lerp(
            previousCameraPosition,
            midpoint,
            cameraProgress);
        presentationCamera.transform.rotation = previousCameraRotation;
        presentationCamera.orthographicSize = Mathf.Lerp(
            previousOrthographicSize,
            previousOrthographicSize * cameraZoomMultiplier,
            cameraProgress);
    }

    private void TryStartExecution()
    {
        EnemyStateMachine selectedTarget = FindNearestExecutionTarget();
        if (selectedTarget == null)
        {
            return;
        }

        if (playerDodge.IsDodging)
        {
            playerDodge.CancelForExecution();
        }

        playerAnimationController.CancelForExecution();
        playerAttackHitboxController.DisableAllHitboxes();

        previousAnimatorSpeed = animator != null ? animator.speed : 1f;
        playerMoveController.CanMove = false;
        target = selectedTarget;
        approachElapsed = 0f;
        state = ExecutionState.Approaching;
    }

    private EnemyStateMachine FindNearestExecutionTarget()
    {
        int hitCount = Physics2D.OverlapCircleNonAlloc(
            transform.position,
            playerStatController.ExecutionDistance,
            targetBuffer,
            executionTargetLayers);

        EnemyStateMachine nearestTarget = null;
        float nearestDistance = float.PositiveInfinity;
        for (int i = 0; i < hitCount; i++)
        {
            Collider2D candidateCollider = targetBuffer[i];
            if (candidateCollider == null)
            {
                continue;
            }

            EnemyStateMachine candidate = candidateCollider.GetComponentInParent<EnemyStateMachine>();
            if (candidate == null || !candidate.CanBeExecuted)
            {
                continue;
            }

            float distance = (candidate.transform.position - transform.position).sqrMagnitude;
            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                nearestTarget = candidate;
            }
        }

        return nearestTarget;
    }

    private bool IsApproachTargetValid()
    {
        return target != null
            && target.gameObject.activeInHierarchy
            && target.CanBeExecuted
            && !playerStatController.IsDead;
    }

    private Vector2 GetExecutionAnchor(EnemyStateMachine candidate)
    {
        Vector2 targetPosition = candidate != null ? (Vector2)candidate.transform.position : (Vector2)transform.position;
        return targetPosition + Vector2.left * executionAnchorOffset;
    }

    private bool IsAtExecutionAnchor(Vector2 anchor)
    {
        return (anchor - playerRigidbody.position).sqrMagnitude <= contactDistance * contactDistance;
    }

    private bool IsAtTarget(EnemyStateMachine candidate)
    {
        Collider2D targetCollider = candidate.GetComponent<Collider2D>();
        if (targetCollider != null && playerCollider != null)
        {
            ColliderDistance2D distance = playerCollider.Distance(targetCollider);
            if (distance.isValid)
            {
                return distance.distance <= contactDistance;
            }
        }

        return (candidate.transform.position - transform.position).sqrMagnitude
            <= contactDistance * contactDistance;
    }

    private void BeginPresentation()
    {
        if (target == null || !target.TryBeginExecution())
        {
            CancelExecution();
            return;
        }

        state = ExecutionState.Presenting;
        presentationElapsed = 0f;
        attackPaused = false;
        executionApplied = false;
        playerStatController.SetExecutionInvulnerable(true);
        playerMoveController.CanMove = false;
        SetExecutionBackgroundVisible(true);
        StoreCameraState();
        ApplyExecutionSortingOrder();

        if (spriteFlip != null)
        {
            spriteFlip.SetFlipXOverride(false);
        }

        if (animator != null)
        {
            previousAnimatorSpeed = animator.speed;
            animator.speed = 1f;
            int stateHash = Animator.StringToHash(ExecutionAnimationStateName);
            if (animator.HasState(0, stateHash))
            {
                animator.Play(stateHash, 0, 0f);
            }
            else
            {
                animator.Play(ExecutionAnimationStateName, 0, 0f);
            }
        }
    }

    private void UpdatePresentation(float deltaTime)
    {
        if (target == null || (!executionApplied && !target.IsExecuting))
        {
            CancelExecution();
            return;
        }

        presentationElapsed += deltaTime;
        playerMoveController.CanMove = false;
        playerStatController.SetExecutionInvulnerable(true);

        if (!attackPaused && presentationElapsed >= attackWindup)
        {
            attackPaused = true;
            if (animator != null)
            {
                animator.speed = 0f;
            }
        }

        if (attackPaused && presentationElapsed >= attackWindup + attackPause)
        {
            if (animator != null)
            {
                animator.speed = previousAnimatorSpeed;
            }

            if (!executionApplied)
            {
                executionApplied = target.TryCompleteExecution();
                if (executionApplied)
                {
                    playerStatController.Heal(target.IsBoss ? playerStatController.MaxHP : executionHealAmount);
                    playerStatController.RegisterExecutionKill();
                    MoveThroughTarget();
                }
            }
        }

        float endTime = attackWindup + attackPause + attackRecovery;
        if (presentationElapsed >= endTime)
        {
            FinishExecution();
        }
    }

    private void MoveThroughTarget()
    {
        if (target == null || attackMoveDistance <= 0f)
        {
            return;
        }

        playerRigidbody.MovePosition(playerRigidbody.position + Vector2.right * attackMoveDistance);
    }

    private void StoreCameraState()
    {
        presentationCamera = Camera.main;
        if (presentationCamera == null)
        {
            return;
        }

        previousCameraPosition = presentationCamera.transform.position;
        previousCameraRotation = presentationCamera.transform.rotation;
        previousOrthographicSize = presentationCamera.orthographicSize;
        cameraStateStored = true;
    }

    private void RestoreCameraState()
    {
        if (!cameraStateStored || presentationCamera == null)
        {
            return;
        }

        presentationCamera.transform.position = previousCameraPosition;
        presentationCamera.transform.rotation = previousCameraRotation;
        presentationCamera.orthographicSize = previousOrthographicSize;
        cameraStateStored = false;
    }

    private void ApplyExecutionSortingOrder()
    {
        RestoreExecutionSortingOrder();
        StoreExecutionSortingOrder(gameObject);
        StoreExecutionSortingOrder(target != null ? target.gameObject : null);
    }

    private void StoreExecutionSortingOrder(GameObject root)
    {
        if (root == null)
        {
            return;
        }

        SpriteRenderer[] renderers = root.GetComponentsInChildren<SpriteRenderer>(true);
        foreach (SpriteRenderer renderer in renderers)
        {
            if (renderer == null)
            {
                continue;
            }

            sortingOrderSnapshots.Add(new SortingOrderSnapshot(renderer));
            renderer.sortingOrder = executionSortingOrder;
        }
    }

    private void RestoreExecutionSortingOrder()
    {
        foreach (SortingOrderSnapshot snapshot in sortingOrderSnapshots)
        {
            if (snapshot.Renderer == null)
            {
                continue;
            }

            snapshot.Renderer.sortingLayerID = snapshot.SortingLayerId;
            snapshot.Renderer.sortingOrder = snapshot.SortingOrder;
        }

        sortingOrderSnapshots.Clear();
    }

    private void FinishExecution()
    {
        if (state == ExecutionState.Idle)
        {
            return;
        }

        RestoreTemporaryState();
        target = null;
        state = ExecutionState.Idle;
    }

    private void CancelExecution()
    {
        if (state == ExecutionState.Idle && target == null)
        {
            return;
        }

        RestoreTemporaryState();
        target = null;
        state = ExecutionState.Idle;
    }

    private void RestoreTemporaryState()
    {
        if (!executionApplied && target != null)
        {
            target.CancelExecution();
        }

        if (animator != null)
        {
            animator.speed = previousAnimatorSpeed;
        }

        if (playerStatController != null)
        {
            playerStatController.SetExecutionInvulnerable(false);
        }

        if (playerAttackHitboxController != null)
        {
            playerAttackHitboxController.DisableAllHitboxes();
        }

        if (playerMoveController != null)
        {
            playerMoveController.CanMove = playerStatController == null || !playerStatController.IsDead;
        }
        SetExecutionBackgroundVisible(false);
        RestoreCameraState();
        RestoreExecutionSortingOrder();

        if (spriteFlip != null)
        {
            if (state == ExecutionState.Presenting)
            {
                spriteFlip.SetFlipXOverride(false);
                spriteFlip.ClearFlipXOverride();
            }
            else
            {
                spriteFlip.ClearFlipXOverride();
            }
        }

        presentationElapsed = 0f;
        approachElapsed = 0f;
        attackPaused = false;
        executionApplied = false;
    }

    private void HandlePlayerDied()
    {
        CancelExecution();
    }

    private void SetExecutionBackgroundVisible(bool visible)
    {
        if (executionBackgroundEffect != null)
        {
            executionBackgroundEffect.SetActive(visible);
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (playerStatController == null || playerStatController.Data == null)
        {
            return;
        }

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, playerStatController.ExecutionDistance);
    }
}
