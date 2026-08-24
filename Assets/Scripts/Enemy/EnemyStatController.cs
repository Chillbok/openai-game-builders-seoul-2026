using System;
using UnityEngine;

public enum EnemyDeathReason
{
    Normal,
    Execution
}

[DisallowMultipleComponent]
public sealed class EnemyStatController : MonoBehaviour
{
    [Header("적 데이터")]
    [SerializeField]
    [Tooltip("적 종류별 기본 스탯을 담은 데이터 에셋")]
    private EnemyData enemyData;

    private EnemyRuntimeState runtimeState;
    private bool executionLocked;

    public EnemyData Data => enemyData;
    public bool IsInitialized => runtimeState != null;
    public bool IsDead => IsInitialized && runtimeState.CurrentHP <= 0f;
    public bool CanBeStunned => runtimeState != null && runtimeState.CanBeStunned;
    public bool IsExecutionLocked => executionLocked;

    public float MaxHP => enemyData != null ? enemyData.MaxHP : 0f;
    public bool IsBoss => enemyData != null && enemyData.IsBoss;
    public float CurrentHP => runtimeState != null ? runtimeState.CurrentHP : 0f;
    public float DefaultMoveSpeed => enemyData != null ? enemyData.MoveSpeed : 0f;
    public float AttackDamage => enemyData != null ? enemyData.AttackDamage : 0f;
    public float AttackCooldown => enemyData != null ? enemyData.AttackCooldown : 0f;
    public float AttackRange => enemyData != null ? enemyData.AttackRange : 0f;
    public float AttackPrepareTime => enemyData != null ? enemyData.AttackPrepareTime : 0f;
    public float KnockbackDistance => enemyData != null ? enemyData.KnockbackDistance : 0f;
    public float StunThresholdPercent => enemyData != null ? enemyData.StunThresholdPercent : 0f;
    public float StunDuration => enemyData != null ? enemyData.StunDuration : 0f;
    public float CurrentAttackCooldown => runtimeState != null ? runtimeState.CurrentAttackCooldown : 0f;
    public bool ReadyToAttack => IsInitialized && runtimeState.CurrentAttackCooldown <= 0f;

    public event Action<float> CurrentHPChanged;
    public event Action<Vector2> Damaged;
    public event Action StunStarted;
    public event Action<EnemyDeathReason> Died;

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
        executionLocked = false;
    }

    // 적의 현재 체력을 감소시키고 사망 또는 최초 기절 여부를 판정한다.
    public bool TryTakeDamage(float damage, Vector2 knockbackDirection)
    {
        if (!IsInitialized || IsDead || executionLocked || damage <= 0f)
        {
            return false;
        }

        float previousHP = runtimeState.CurrentHP;
        runtimeState.CurrentHP -= damage;

        if (!Mathf.Approximately(previousHP, runtimeState.CurrentHP))
        {
            CurrentHPChanged?.Invoke(runtimeState.CurrentHP);
        }

        bool died = previousHP > 0f && runtimeState.CurrentHP <= 0f;
        if (died)
        {
            // 기존 사망 흐름처럼 치명타에도 피해 이벤트를 먼저 알린다.
            Damaged?.Invoke(knockbackDirection);
            Died?.Invoke(EnemyDeathReason.Normal);
        }
        else if (TryEnterStun())
        {
            // 기절 진입 피해는 넉백 상태로 전환하지 않는다.
            StunStarted?.Invoke();
        }
        else
        {
            Damaged?.Invoke(knockbackDirection);
        }

        Debug.Log($"적에게 {previousHP - runtimeState.CurrentHP} 피해 입음, 남은 체력: {runtimeState.CurrentHP}", this);

        return true;
    }

    // 처형 연출 중 일반 피해와 상태 변경을 막는다.
    public bool TryBeginExecution()
    {
        if (!IsInitialized || IsDead || executionLocked)
        {
            return false;
        }

        executionLocked = true;
        return true;
    }

    // 처형 타격 시 HP를 즉시 0으로 만들고 처형 사망 이벤트를 한 번 발생시킨다.
    public bool TryCompleteExecution()
    {
        if (!IsInitialized || IsDead || !executionLocked)
        {
            return false;
        }

        runtimeState.CurrentHP = 0f;
        CurrentHPChanged?.Invoke(runtimeState.CurrentHP);
        Died?.Invoke(EnemyDeathReason.Execution);
        return true;
    }

    // 처형 연출이 중단되면 일반 전투 판정을 다시 허용한다.
    public bool CancelExecution()
    {
        if (!IsInitialized || IsDead || !executionLocked)
        {
            return false;
        }

        executionLocked = false;
        return true;
    }

    // 현재 체력 비율이 기준 이하이고 아직 기절하지 않은 적을 최초 기절시킨다.
    private bool TryEnterStun()
    {
        if (!IsInitialized || IsDead || !runtimeState.CanBeStunned || MaxHP <= 0f)
        {
            return false;
        }

        float currentHPPercent = runtimeState.CurrentHP / MaxHP * 100f;
        if (currentHPPercent > StunThresholdPercent)
        {
            return false;
        }

        runtimeState.CanBeStunned = false;
        Debug.Log($"적 기절 진입 판정: 현재 HP {runtimeState.CurrentHP}/{MaxHP}, 기절 기준 {StunThresholdPercent}%", this);
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
