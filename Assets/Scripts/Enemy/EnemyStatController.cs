using System;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class EnemyStatController : MonoBehaviour
{
    [Header("적 데이터")]
    [SerializeField]
    [Tooltip("적 종류별 기본 스탯을 담은 데이터 에셋")]
    private EnemyData enemyData;

    private EnemyRuntimeState runtimeState;

    public EnemyData Data => enemyData;
    public bool IsInitialized => runtimeState != null;
    public bool IsDead => IsInitialized && runtimeState.CurrentHP <= 0f;

    public float MaxHP => enemyData != null ? enemyData.MaxHP : 0f;
    public float CurrentHP => runtimeState != null ? runtimeState.CurrentHP : 0f;
    public float DefaultMoveSpeed => enemyData != null ? enemyData.MoveSpeed : 0f;
    public float AttackDamage => enemyData != null ? enemyData.AttackDamage : 0f;
    public float AttackCooldown => enemyData != null ? enemyData.AttackCooldown : 0f;
    public float AttackRange => enemyData != null ? enemyData.AttackRange : 0f;
    public float AttackPrepareTime => enemyData != null ? enemyData.AttackPrepareTime : 0f;
    public float KnockbackDistance => enemyData != null ? enemyData.KnockbackDistance : 0f;
    public float CurrentAttackCooldown => runtimeState != null ? runtimeState.CurrentAttackCooldown : 0f;
    public bool ReadyToAttack => IsInitialized && runtimeState.CurrentAttackCooldown <= 0f;

    public event Action<float> CurrentHPChanged;
    public event Action<Vector2> Damaged;
    public event Action Died;

    // 컴포넌트가 활성화될 때 적의 런타임 스탯을 초기화한다.
    private void Awake()
    {
        Initialize();
    }

    // 매 프레임 공격 쿨타임을 감소시킨다.
    private void Update()
    {
        TickAttackCooldown(Time.deltaTime);
    }

    // 현재 공격 쿨타임이 0보다 크면 매 프레임 줄인다.
    private void TickAttackCooldown(float deltaTime)
    {
        if (!IsInitialized || deltaTime <= 0f || runtimeState.CurrentAttackCooldown <= 0f)
        {
            return;
        }

        runtimeState.CurrentAttackCooldown = Mathf.Max(0f, runtimeState.CurrentAttackCooldown - deltaTime);
    }

    // EnemyData 설정으로 전투 중 사용할 런타임 상태를 생성한다.
    public void Initialize()
    {
        if (runtimeState != null)
        {
            return;
        }

        if (enemyData == null)
        {
            Debug.LogError("EnemyStatController에 EnemyData가 할당되지 않았습니다.", this);
            enabled = false;
            return;
        }

        runtimeState = enemyData.CreateRuntimeState();
    }

    // 적의 현재 체력을 감소시키고 받은 넉백 방향을 전달하며 사망 여부를 판정한다.
    public bool TryTakeDamage(float damage, Vector2 knockbackDirection)
    {
        if (!IsInitialized || IsDead || damage <= 0f)
        {
            return false;
        }

        float previousHP = runtimeState.CurrentHP;
        runtimeState.CurrentHP -= damage;

        if (!Mathf.Approximately(previousHP, runtimeState.CurrentHP))
        {
            CurrentHPChanged?.Invoke(runtimeState.CurrentHP);
        }

        Damaged?.Invoke(knockbackDirection);

        if (previousHP > 0f && runtimeState.CurrentHP <= 0f)
        {
            Died?.Invoke();
        }

        Debug.Log($"적에게 {previousHP - runtimeState.CurrentHP} 피해 입음, 남은 체력: {runtimeState.CurrentHP}", this);

        return true;
    }

    // 공격을 시작하면 현재 공격 쿨타임을 공격 쿨타임 값으로 초기화한다.
    public void ConsumeAttack()
    {
        if (!IsInitialized)
        {
            return;
        }

        runtimeState.CurrentAttackCooldown = AttackCooldown;
    }
}