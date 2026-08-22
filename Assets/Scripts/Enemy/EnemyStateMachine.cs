using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(EnemyStatController))]
[RequireComponent(typeof(EnemyAnimationController))]
public class EnemyStateMachine : MonoBehaviour
{
    private enum EnemyState
    {
        Chase,
        PrepareAttack,
        Attack,
        Knockback,
        Dead
    }

    private const string PlayerTag = "Player";
    private const float AttackAnimationDuration = 0.7f;
    private const float DeathAnimationDuration = 1f;
    private const float DefaultKnockbackDuration = 0.2f;

    private Rigidbody2D enemyRigidbody;
    private EnemyStatController enemyStatController;
    private EnemyAnimationController enemyAnimationController;
    private EnemyAttackRangeSensor rangeSensor;
    private Collider2D bodyCollider;

    private Transform playerTransform;
    private EnemyState state;
    private float stateTimer;
    private Vector2 knockbackDirection;
    private bool diedHandled;
    private int blockingLayerMask;

    // 컴포넌트 참조와 플레이어를 초기화하고 추적 상태로 시작한다.
    private void Awake()
    {
        enemyRigidbody = GetComponent<Rigidbody2D>();
        enemyStatController = GetComponent<EnemyStatController>();
        enemyAnimationController = GetComponent<EnemyAnimationController>();
        rangeSensor = GetComponentInChildren<EnemyAttackRangeSensor>();
        bodyCollider = GetComponent<Collider2D>();

        if (rangeSensor == null)
        {
            EnsureRangeSensor();
            rangeSensor = GetComponentInChildren<EnemyAttackRangeSensor>();
        }

        state = EnemyState.Chase;
        playerTransform = FindPlayerTransform();

        int playerLayer = LayerMask.NameToLayer("Player");
        int enemyLayer = LayerMask.NameToLayer("Enemy");
        int obstacleLayer = LayerMask.NameToLayer("Obstacle");
        blockingLayerMask = 0;
        if (playerLayer >= 0) blockingLayerMask |= 1 << playerLayer;
        if (enemyLayer >= 0) blockingLayerMask |= 1 << enemyLayer;
        if (obstacleLayer >= 0) blockingLayerMask |= 1 << obstacleLayer;
    }

    // 적이 피해를 받거나 사망했을 때 상태를 갱신한다.
    private void OnEnable()
    {
        if (enemyStatController != null)
        {
            enemyStatController.Damaged += HandleDamaged;
            enemyStatController.Died += HandleDied;
        }
    }

    private void OnDisable()
    {
        if (enemyStatController != null)
        {
            enemyStatController.Damaged -= HandleDamaged;
            enemyStatController.Died -= HandleDied;
        }
    }

    // 상태 머신의 현재 상태에 따라 매 프레임 갱신한다.
    private void Update()
    {
        if (!enemyStatController.IsInitialized || state == EnemyState.Dead)
        {
            return;
        }

        if (rangeSensor == null)
        {
            EnsureRangeSensor();
            rangeSensor = GetComponentInChildren<EnemyAttackRangeSensor>();
        }

        if (playerTransform == null)
        {
            playerTransform = FindPlayerTransform();
            return;
        }

        switch (state)
        {
            case EnemyState.Chase:
                UpdateChaseState();
                break;
            case EnemyState.PrepareAttack:
                UpdatePrepareAttackState();
                break;
            case EnemyState.Attack:
                UpdateAttackState();
                break;
            case EnemyState.Knockback:
                UpdateKnockbackState();
                break;
        }
    }

    // 물리 이동을 물리 갱신 주기에 맞춰 처리한다.
    private void FixedUpdate()
    {
        if (!enemyStatController.IsInitialized || state == EnemyState.Dead)
        {
            return;
        }

        if (state == EnemyState.Chase && playerTransform != null)
        {
            MoveTowardPlayer();
        }
        else if (state == EnemyState.Knockback)
        {
            ApplyKnockbackMovement();
        }
    }

    // 플레이어를 향해 이동하며 바라보는 방향과 이동 애니메이션을 갱신한다.
    private void UpdateChaseState()
    {
        enemyAnimationController.SetFacingRight(transform.position.x < playerTransform.position.x);

        if (IsInAttackRange() && enemyStatController.ReadyToAttack)
        {
            BeginPrepareAttack();
            return;
        }

        enemyAnimationController.SetMoving(true);
    }

    // 공격 준비 시간 동안 대기한 뒤 공격으로 전환한다.
    private void UpdatePrepareAttackState()
    {
        enemyAnimationController.SetMoving(false);
        stateTimer -= Time.deltaTime;
        if (stateTimer <= 0f)
        {
            BeginAttack();
        }
    }

    // 공격 애니메이션을 진행하고 종료 후 쿨타임을 소모하며 추적으로 돌아간다.
    private void UpdateAttackState()
    {
        enemyAnimationController.SetMoving(false);
        stateTimer -= Time.deltaTime;
        if (stateTimer > 0f)
        {
            return;
        }

        enemyStatController.ConsumeAttack();
        state = EnemyState.Chase;
    }

    // 넉백 이동을 진행하고 종료 후 추적으로 돌아간다.
    private void UpdateKnockbackState()
    {
        stateTimer -= Time.deltaTime;
        if (stateTimer <= 0f)
        {
            state = EnemyState.Chase;
        }
    }

    // 공격을 준비하고 이동을 멈춘다.
    private void BeginPrepareAttack()
    {
        enemyAnimationController.SetMoving(false);
        state = EnemyState.PrepareAttack;
        stateTimer = enemyStatController.AttackPrepareTime;
    }

    // 공격 애니메이션을 시작하고 플레이어 위치에 일회성 피해 오브젝트를 생성한다.
    private void BeginAttack()
    {
        enemyAnimationController.PlayAttack();
        SpawnDamageInstance();

        state = EnemyState.Attack;
        stateTimer = AttackAnimationDuration;
    }

    // 사거리 내 플레이어 위치에 일회성 피해 오브젝트를 생성한다. 수명은 애니메이션 1회와 동일.
    private void SpawnDamageInstance()
    {
        if (playerTransform == null || enemyStatController == null || !enemyStatController.IsInitialized)
        {
            return;
        }

        // AttackRangeSensor가 CanAttack을 보장하므로 추가 거리 검사 없이 생성
        Vector3 spawnPosition = playerTransform.position;
        GameObject damageObject = new GameObject("EnemyDamageInstance");
        damageObject.transform.position = spawnPosition;

        EnemyDamageInstance damageInstance = damageObject.AddComponent<EnemyDamageInstance>();
        damageInstance.Initialize(enemyStatController.AttackDamage, AttackAnimationDuration);
    }

    // 피해를 받으면 넉백 상태로 전환하고 피격 애니메이션을 재생한다.
    private void HandleDamaged(Vector2 newKnockbackDirection)
    {
        if (state == EnemyState.Dead || enemyStatController.IsDead)
        {
            return;
        }

        knockbackDirection = newKnockbackDirection.normalized;
        state = EnemyState.Knockback;
        stateTimer = DefaultKnockbackDuration;
        enemyAnimationController.PlayHurt();
    }

    // 사망 처리 후 사망 애니메이션을 재생하고 일정 시간 뒤 제거한다.
    private void HandleDied()
    {
        if (diedHandled)
        {
            return;
        }

        diedHandled = true;
        state = EnemyState.Dead;
        enemyAnimationController.ResetTriggers();
        enemyAnimationController.SetMoving(false);
        enemyAnimationController.PlayDeath();

        if (bodyCollider != null)
        {
            bodyCollider.enabled = false;
        }

        PlayerStatController playerStatController = FindFirstObjectByType<PlayerStatController>();
        if (playerStatController != null && playerStatController.IsInitialized)
        {
            playerStatController.RegisterNormalKill();
        }

        Destroy(gameObject, DeathAnimationDuration);
    }

    // 현재 플레이어가 공격 사거리 안에 있는지 확인한다. (콜라이더 센서 기반, 폴백으로 피벗 거리)
    private bool IsInAttackRange()
    {
        if (rangeSensor != null)
        {
            return rangeSensor.CanAttack;
        }

        if (playerTransform == null)
        {
            return false;
        }

        return (playerTransform.position - transform.position).sqrMagnitude <= enemyStatController.AttackRange * enemyStatController.AttackRange;
    }

    // 사거리 센서가 없으면 런타임에 생성하고 반경을 AttackRange로 동기화한다.
    private void EnsureRangeSensor()
    {
        if (enemyStatController == null)
        {
            return;
        }

        float range = 1.5f;
        if (enemyStatController.Data != null)
        {
            range = enemyStatController.Data.AttackRange;
        }
        else if (enemyStatController.IsInitialized)
        {
            range = enemyStatController.AttackRange;
        }

        GameObject sensorObject = new GameObject("AttackRangeSensor");
        sensorObject.transform.SetParent(transform, false);
        sensorObject.transform.localPosition = Vector3.zero;
        sensorObject.transform.localRotation = Quaternion.identity;
        sensorObject.transform.localScale = Vector3.one;

        CircleCollider2D circle = sensorObject.AddComponent<CircleCollider2D>();
        circle.isTrigger = true;
        float parentScale = Mathf.Max(Mathf.Abs(transform.lossyScale.x), Mathf.Abs(transform.lossyScale.y), Mathf.Abs(transform.localScale.x), Mathf.Abs(transform.localScale.y));
        if (parentScale < 0.0001f) parentScale = 1f;
        // Sensor의 월드 반경이 range가 되도록 로컬 반경을 보정 (부모 스케일 고려)
        circle.radius = Mathf.Max(0f, range / parentScale);
        circle.offset = Vector2.zero;

        EnemyAttackRangeSensor sensor = sensorObject.AddComponent<EnemyAttackRangeSensor>();
        int playerLayer = LayerMask.NameToLayer("Player");
        if (playerLayer >= 0)
        {
            sensor.TargetLayer = 1 << playerLayer;
        }
    }

    // 플레이어를 향해 이동한다. (플레이어/다른 적/장애물에 가로막히되 밀어내지 않음)
    private void MoveTowardPlayer()
    {
        Vector2 direction = (playerTransform.position - transform.position).normalized;
        Vector2 delta = direction * enemyStatController.DefaultMoveSpeed * Time.fixedDeltaTime;
        TryEnemyMove(delta);
    }

    // 넉백 방향으로 지정된 거리만큼 이동시킨다. (벽/장애물에만 가로막힘, 플레이어는 밀어내지 않음)
    private void ApplyKnockbackMovement()
    {
        float knockbackSpeed = enemyStatController.KnockbackDistance / Mathf.Max(0.0001f, DefaultKnockbackDuration);
        Vector2 delta = knockbackDirection * knockbackSpeed * Time.fixedDeltaTime;
        // 넉백은 적끼리/장애물에는 막히되 플레이어를 밀지 않도록 별도 마스크 없이 이동
        // 플레이어를 밀어내지 않으려면 넉백 이동도 차단 검사하되 플레이어 레이어는 제외하지 않음 -> 동일 차단 로직 사용
        TryEnemyMove(delta);
    }

    private void TryEnemyMove(Vector2 delta)
    {
        if (delta.sqrMagnitude < 0.000001f)
        {
            return;
        }

        Vector2 target = enemyRigidbody.position + delta;
        if (!IsEnemyBlocked(target, delta))
        {
            enemyRigidbody.MovePosition(target);
            return;
        }

        Vector2 deltaX = new Vector2(delta.x, 0f);
        Vector2 deltaY = new Vector2(0f, delta.y);
        Vector2 targetX = enemyRigidbody.position + deltaX;
        Vector2 targetY = enemyRigidbody.position + deltaY;
        bool blockedX = Mathf.Abs(delta.x) < 0.000001f || IsEnemyBlocked(targetX, deltaX);
        bool blockedY = Mathf.Abs(delta.y) < 0.000001f || IsEnemyBlocked(targetY, deltaY);

        if (!blockedX && blockedY)
        {
            enemyRigidbody.MovePosition(targetX);
        }
        else if (blockedX && !blockedY)
        {
            enemyRigidbody.MovePosition(targetY);
        }
    }

    private bool IsEnemyBlocked(Vector2 targetPosition, Vector2 delta)
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

        if (hit.transform.IsChildOf(transform))
        {
            return false;
        }

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

    // 씬의 플레이어 오브젝트를 태그로 찾는다.
    private Transform FindPlayerTransform()
    {
        GameObject playerObject = GameObject.FindGameObjectWithTag(PlayerTag);
        return playerObject != null ? playerObject.transform : null;
    }
}