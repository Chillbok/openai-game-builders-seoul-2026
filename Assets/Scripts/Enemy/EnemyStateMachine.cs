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
    private const float AttackAnimationFallbackDuration = 0.7f;
    private const float DeathAnimationDuration = 1f;
    private const float DefaultKnockbackDuration = 0.2f;

    [Header("공격 예고")]
    [SerializeField]
    [Tooltip("공격 준비 시작부터 실제 타격 직전까지 적 중심에 표시할 비콘 자식 오브젝트")]
    private Transform attackBeaconTransform;

    private Rigidbody2D enemyRigidbody;
    private EnemyStatController enemyStatController;
    private EnemyAnimationController enemyAnimationController;
    private Collider2D bodyCollider;

    private Transform playerTransform;
    private PlayerStatController playerStatController;
    private Collider2D playerBodyCollider;
    private EnemyState state;
    private float stateTimer;
    private Vector2 knockbackDirection;
    private Animator attackBeaconAnimator;
    private bool diedHandled;
    private bool attackPreparePaused;
    private bool attackHitTriggered;
    private NoPushCollisionMover2D collisionMover;

    // 컴포넌트 참조와 플레이어를 초기화하고 추적 상태로 시작한다.
    private void Awake()
    {
        enemyRigidbody = GetComponent<Rigidbody2D>();
        enemyStatController = GetComponent<EnemyStatController>();
        enemyAnimationController = GetComponent<EnemyAnimationController>();
        bodyCollider = GetComponent<Collider2D>();
        CacheAttackBeacon();

        state = EnemyState.Chase;
        CachePlayerReferences();

        int playerLayer = LayerMask.NameToLayer("Player");
        int enemyLayer = LayerMask.NameToLayer("Enemy");
        int obstacleLayer = LayerMask.NameToLayer("Obstacle");
        int blockingLayerMask = 0;
        if (playerLayer >= 0) blockingLayerMask |= 1 << playerLayer;
        if (enemyLayer >= 0) blockingLayerMask |= 1 << enemyLayer;
        if (obstacleLayer >= 0) blockingLayerMask |= 1 << obstacleLayer;

        NoPushCollisionMover2D.ConfigureNoPushContact(bodyCollider, playerLayer);
        collisionMover = new NoPushCollisionMover2D(
            enemyRigidbody,
            bodyCollider,
            transform,
            blockingLayerMask);
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
        HideAttackBeacon();
        ResetAttackAnimation();

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

        if (playerTransform == null)
        {
            CachePlayerReferences();
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

        if (!attackPreparePaused)
        {
            return;
        }

        stateTimer -= Time.deltaTime;
        if (stateTimer <= 0f)
        {
            attackPreparePaused = false;
            enemyAnimationController.ResumeAnimation();
            state = EnemyState.Attack;
            stateTimer = AttackAnimationFallbackDuration;
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

        FinishAttack();
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

    // 공격 애니메이션을 즉시 시작하고 준비 구간 이벤트를 기다린다.
    private void BeginPrepareAttack()
    {
        attackPreparePaused = false;
        attackHitTriggered = false;
        enemyAnimationController.SetMoving(false);
        state = EnemyState.PrepareAttack;
        enemyAnimationController.PlayAttack();
    }

    // Attack 애니메이션의 준비 동작이 끝나는 이벤트에서 호출된다.
    public void OnAttackPreparePause()
    {
        if (state != EnemyState.PrepareAttack || attackPreparePaused)
        {
            return;
        }

        attackPreparePaused = true;
        stateTimer = enemyStatController.AttackPrepareTime;
        enemyAnimationController.PauseAttackAnimation();
        ShowAttackBeacon(stateTimer);
    }

    // Attack 애니메이션의 실제 타격 프레임에서 호출된다.
    public void OnAttackHit()
    {
        if (state != EnemyState.Attack || attackHitTriggered)
        {
            return;
        }

        attackHitTriggered = true;
        HideAttackBeacon();
        TryDealAttackDamage();
    }

    // Attack 애니메이션의 마지막 프레임에서 호출된다.
    public void OnAttackFinished()
    {
        if (state != EnemyState.Attack)
        {
            return;
        }

        FinishAttack();
    }

    // 실제 타격 프레임의 플레이어 위치와 회피 상태를 기준으로 피해를 시도한다.
    private void TryDealAttackDamage()
    {
        if (enemyStatController == null || !enemyStatController.IsInitialized || playerTransform == null)
        {
            return;
        }

        CacheMissingPlayerComponents();
        if (playerStatController == null || !IsInAttackRange())
        {
            return;
        }

        playerStatController.TryTakeDamage(enemyStatController.AttackDamage);
    }

    // 공격 종료 이벤트 또는 타이머 폴백에서 공격 상태를 종료한다.
    private void FinishAttack()
    {
        HideAttackBeacon();
        enemyAnimationController.ResumeAnimation();
        enemyStatController.ConsumeAttack();
        state = EnemyState.Chase;
    }

    // 피해를 받으면 넉백 상태로 전환하고 피격 애니메이션을 재생한다.
    private void HandleDamaged(Vector2 newKnockbackDirection)
    {
        if (state == EnemyState.Dead || enemyStatController.IsDead)
        {
            return;
        }

        HideAttackBeacon();
        ResetAttackAnimation();
        attackPreparePaused = false;
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
        HideAttackBeacon();
        ResetAttackAnimation();
        attackPreparePaused = false;
        state = EnemyState.Dead;
        enemyAnimationController.SetMoving(false);
        enemyAnimationController.PlayDeath();

        if (bodyCollider != null)
        {
            bodyCollider.enabled = false;
        }

        PlayerStatController playerStats = playerStatController != null
            ? playerStatController
            : FindFirstObjectByType<PlayerStatController>();
        if (playerStats != null && playerStats.IsInitialized)
        {
            playerStats.RegisterNormalKill();
        }

        Destroy(gameObject, DeathAnimationDuration);
    }

    // 공격 준비를 알리는 자식 비콘을 켜고 준비 시간에 맞춰 재생 속도를 조절한다.
    private void ShowAttackBeacon(float duration)
    {
        if (attackBeaconTransform == null || duration <= 0f)
        {
            return;
        }

        GameObject beaconObject = attackBeaconTransform.gameObject;
        beaconObject.SetActive(true);

        if (attackBeaconAnimator == null)
        {
            attackBeaconAnimator = attackBeaconTransform.GetComponent<Animator>();
        }

        if (attackBeaconAnimator != null)
        {
            float animationDuration = GetAnimationDuration(attackBeaconAnimator);
            if (animationDuration > 0f)
            {
                // 클립 전체가 준비 시간에 정확히 끝나도록 재생 속도를 보정한다.
                attackBeaconAnimator.speed = animationDuration / duration;
            }

            // 이전 재생 상태를 남기지 않고 매 공격 준비마다 처음부터 재생한다.
            attackBeaconAnimator.Rebind();
            attackBeaconAnimator.Update(0f);
        }
    }

    // 비콘 Animator의 기본 상태에서 재생되는 클립 길이를 가져온다.
    private static float GetAnimationDuration(Animator animator)
    {
        RuntimeAnimatorController controller = animator.runtimeAnimatorController;
        if (controller == null || controller.animationClips == null || controller.animationClips.Length == 0)
        {
            return 0f;
        }

        float duration = 0f;
        foreach (AnimationClip clip in controller.animationClips)
        {
            if (clip != null)
            {
                duration = Mathf.Max(duration, clip.length);
            }
        }

        return duration;
    }

    // 프리팹에 배치된 자식 비콘과 Animator 참조를 캐시한다.
    private void CacheAttackBeacon()
    {
        if (attackBeaconTransform == null)
        {
            return;
        }

        attackBeaconAnimator = attackBeaconTransform.GetComponent<Animator>();
    }

    // 현재 공격 예고 비콘을 비활성화해 다음 공격에 재사용한다.
    private void HideAttackBeacon()
    {
        if (attackBeaconTransform == null)
        {
            return;
        }

        attackBeaconTransform.gameObject.SetActive(false);
    }

    // 피격·사망·비활성화 시 일시정지된 공격 애니메이션을 안전하게 복구한다.
    private void ResetAttackAnimation()
    {
        if (enemyAnimationController == null)
        {
            return;
        }

        enemyAnimationController.ResumeAnimation();
        enemyAnimationController.ResetTriggers();
    }

    // 본체 콜라이더 사이의 간격을 기준으로 현재 플레이어가 공격 사거리 안에 있는지 확인한다.
    private bool IsInAttackRange()
    {
        if (playerTransform == null)
        {
            return false;
        }

        CacheMissingPlayerComponents();
        if (bodyCollider != null && playerBodyCollider != null)
        {
            ColliderDistance2D colliderDistance = bodyCollider.Distance(playerBodyCollider);
            if (colliderDistance.isValid)
            {
                return colliderDistance.distance <= enemyStatController.AttackRange;
            }
        }

        float attackRange = enemyStatController.AttackRange;
        return (playerTransform.position - transform.position).sqrMagnitude <= attackRange * attackRange;
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
        collisionMover?.Move(delta);
    }

    // 씬의 플레이어와 공격 판정에 필요한 본체 컴포넌트를 캐시한다.
    private void CachePlayerReferences()
    {
        GameObject playerObject = GameObject.FindGameObjectWithTag(PlayerTag);
        playerTransform = playerObject != null ? playerObject.transform : null;
        playerStatController = playerObject != null ? playerObject.GetComponent<PlayerStatController>() : null;
        playerBodyCollider = playerObject != null ? playerObject.GetComponent<Collider2D>() : null;
    }

    // 플레이어 오브젝트는 유지된 채 컴포넌트 캐시만 비어 있는 경우를 보정한다.
    private void CacheMissingPlayerComponents()
    {
        if (playerTransform == null)
        {
            return;
        }

        if (playerStatController == null)
        {
            playerStatController = playerTransform.GetComponent<PlayerStatController>();
        }

        if (playerBodyCollider == null)
        {
            playerBodyCollider = playerTransform.GetComponent<Collider2D>();
        }
    }
}
