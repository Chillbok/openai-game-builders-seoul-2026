using System;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class PlayerStatController : MonoBehaviour
{
    public const int MaxAttackCount = 3;
    public const int NormalKillsRequiredForSoulCharge = 4;

    [Header("플레이어 데이터")]
    [SerializeField]
    [Tooltip("플레이어의 기본 스탯 설정을 담은 데이터 에셋")]
    private PlayerData playerData;

    private PlayerRuntimeState runtimeState;
    private float damageInvincibilityRemaining;
    private float dodgeFillProgress;
    private bool isRecharging;
    private bool executionInvulnerable;
    private PlayerDodge playerDodge;
    private float nextAttackDamageMultiplier = 1f;

    public PlayerData Data => playerData;
    public bool IsInitialized => runtimeState != null;
    public bool IsDead => IsInitialized && runtimeState.CurrentHP <= 0f;

    public float MaxHP => playerData != null ? playerData.MaxHP : 0f;
    public float CurrentHP => runtimeState != null ? runtimeState.CurrentHP : 0f;
    public float DefaultMoveSpeed => playerData != null ? playerData.DefaultMoveSpeed : 0f;
    public float DodgeLength => playerData != null ? playerData.DodgeLength : 0f;
    public float DodgeSpeed => playerData != null ? playerData.DodgeSpeed : 0f;
    public float DodgeRechargeTime => playerData != null ? playerData.DodgeRechargeTime : 0f;
    public float ExhaustedDodgeRechargeTime => playerData != null ? playerData.ExhaustedDodgeRechargeTime : 0f;
    public int CurrentDodgeCount => runtimeState != null ? runtimeState.CurrentDodgeCount : 0;
    public float DodgeFillProgress => GetDodgeFillProgress();
    public float PerfectDodgeAcceptanceTime => playerData != null ? playerData.PerfectDodgeAcceptanceTime : 0f;
    public float PerfectDodgeAttackDamageMultiplier => playerData != null ? playerData.PerfectDodgeAttackDamageMultiplier : 0f;
    public float HitInvincibilityTime => playerData != null ? playerData.HitInvincibilityTime : 0f;
    public float ExecutionDistance => playerData != null ? playerData.ExecutionDistance : 0f;
    public float AttackDamage => playerData != null ? playerData.AttackDamage : 0f;
    public float AttackSpeed => playerData != null ? playerData.AttackSpeed : 0f;
    public float AttackDistance => playerData != null ? playerData.AttackDistance : 0f;
    public float AttackAngle => playerData != null ? playerData.AttackAngle : 0f;
    public int CurrentAttackCount => runtimeState != null ? runtimeState.CurrentAttackCount : 0;
    public float SoulChargeDuration => playerData != null ? playerData.SoulChargeDuration : 0f;
    public float SoulChargeDamageReductionRate => playerData != null ? playerData.SoulChargeDamageReductionRate : 0f;
    public float SoulChargeAttackSpeedIncreaseRate => playerData != null ? playerData.SoulChargeAttackSpeedIncreaseRate : 0f;
    public float SoulChargeAttackDamageMultiplier => playerData != null ? playerData.SoulChargeAttackDamageMultiplier : 0f;
    public float SoulChargeExplosionDamage => playerData != null ? playerData.SoulChargeExplosionDamage : 0f;
    public float SoulChargeExplosionRadius => playerData != null ? playerData.SoulChargeExplosionRadius : 0f;
    public int CurrentSoulChargeStage => runtimeState != null ? runtimeState.CurrentSoulChargeStage : 0;
    public int NormalKillCount => runtimeState != null ? runtimeState.NormalKillCount : 0;
    public float SoulChargeRemainingTime => runtimeState != null ? runtimeState.SoulChargeRemainingTime : 0f;
    public float DamageInvincibilityRemaining => damageInvincibilityRemaining;
    public bool IsExecutionInvulnerable => executionInvulnerable;
    public float NextAttackDamageMultiplier => nextAttackDamageMultiplier;
    public bool IsPerfectDodgeAttackReady => nextAttackDamageMultiplier > 0f && !Mathf.Approximately(nextAttackDamageMultiplier, 1f);

    public event Action<float> CurrentHPChanged;
    public event Action Damaged;
    public event Action Died;
    public event Action<int> CurrentDodgeCountChanged;
    public event Action<int> CurrentAttackCountChanged;
    public event Action<int> SoulChargeStageChanged;
    public event Action<int> NormalKillCountChanged;
    public event Action PerfectDodgePerformed;

    // 컴포넌트가 활성화될 때 플레이어의 런타임 스탯을 초기화한다.
    private void Awake()
    {
        playerDodge = GetComponent<PlayerDodge>();
        Initialize();
    }

    // 매 프레임 피격 무적 시간, 영혼 충전 유지 시간, 회피 충전 회복을 갱신한다.
    private void Update()
    {
        if (!IsInitialized)
        {
            return;
        }

        if (damageInvincibilityRemaining > 0f)
        {
            damageInvincibilityRemaining = Mathf.Max(0f, damageInvincibilityRemaining - Time.deltaTime);
        }

        TickDodgeRecharge(Time.deltaTime);
        TickSoulCharge(Time.deltaTime);
    }

    // 회피 충전을 자동으로 회복한다. (회피 사용 후에만 작동)
    private void TickDodgeRecharge(float deltaTime)
    {
        if (!IsInitialized || deltaTime <= 0f || !isRecharging)
        {
            return;
        }

        int current = runtimeState.CurrentDodgeCount;
        int max = PlayerRuntimeState.MaxDodgeCount;
        if (current >= max)
        {
            isRecharging = false;
            dodgeFillProgress = 0f;
            return;
        }

        float rechargeTime = current == 0
            ? ExhaustedDodgeRechargeTime
            : DodgeRechargeTime;

        if (rechargeTime <= 0f)
        {
            return;
        }

        dodgeFillProgress += deltaTime / rechargeTime;

        while (dodgeFillProgress >= 1f)
        {
            dodgeFillProgress -= 1f;

            if (current == 0)
            {
                runtimeState.CurrentDodgeCount = max;
                CurrentDodgeCountChanged?.Invoke(max);
                isRecharging = false;
                dodgeFillProgress = 0f;
                break;
            }
            else
            {
                runtimeState.CurrentDodgeCount++;
                CurrentDodgeCountChanged?.Invoke(runtimeState.CurrentDodgeCount);
                current = runtimeState.CurrentDodgeCount;
                if (current >= max)
                {
                    isRecharging = false;
                    dodgeFillProgress = 0f;
                    break;
                }
            }
        }
    }

    // PlayerData 설정으로 전투 중 사용할 런타임 상태를 생성한다.
    public void Initialize()
    {
        if (runtimeState != null)
        {
            return;
        }

        if (playerData == null)
        {
            Debug.LogError("PlayerStatController에 PlayerData가 할당되지 않았습니다.", this);
            enabled = false;
            return;
        }

        runtimeState = playerData.CreateRuntimeState();
        damageInvincibilityRemaining = 0f;
        isRecharging = false;
        dodgeFillProgress = 0f;
        executionInvulnerable = false;
    }

    // 플레이어의 런타임 스탯을 초기 상태로 되돌린다.
    public void ResetRuntimeStats()
    {
        if (!IsInitialized)
        {
            return;
        }

        runtimeState = playerData.CreateRuntimeState();
        damageInvincibilityRemaining = 0f;
        isRecharging = false;
        dodgeFillProgress = 0f;
        executionInvulnerable = false;
        RaiseRuntimeStateChanged();
    }

    // 피격 무적 여부를 확인한 뒤 플레이어에게 피해를 적용한다.
    public bool TryTakeDamage(float damage)
    {
        if (executionInvulnerable)
        {
            return false;
        }

        // 회피 중에는 무적이므로 모든 피해를 무효화한다. 인정 시간 안이면 완벽한 회피로 처리한다.
        if (playerDodge != null && playerDodge.IsDodging)
        {
            if (playerDodge.TryMarkPerfectDodge())
            {
                nextAttackDamageMultiplier = PerfectDodgeAttackDamageMultiplier;
                PerfectDodgePerformed?.Invoke();
                Debug.Log("완벽한 회피: 다음 공격이 강화된다.");
            }
            else
            {
                Debug.Log("일반 회피: 피해를 무효화했다.");
            }

            return false;
        }

        if (!IsInitialized || IsDead || damage <= 0f || damageInvincibilityRemaining > 0f)
        {
            return false;
        }

        float previousHP = runtimeState.CurrentHP;
        runtimeState.CurrentHP -= damage;
        damageInvincibilityRemaining = HitInvincibilityTime;

        if (!Mathf.Approximately(previousHP, runtimeState.CurrentHP))
        {
            CurrentHPChanged?.Invoke(runtimeState.CurrentHP);
        }

        Damaged?.Invoke();

        if (previousHP > 0f && runtimeState.CurrentHP <= 0f)
        {
            Died?.Invoke();
        }
        
		Debug.Log($"플레이어에게 {previousHP - runtimeState.CurrentHP} 피해 입음, 남은 체력: {runtimeState.CurrentHP}");

        return true;
    }

    // 처형 연출 중 플레이어 피해 판정을 무효화한다.
    public void SetExecutionInvulnerable(bool value)
    {
        executionInvulnerable = value;
    }

    // 플레이어의 현재 체력을 최대 HP까지 회복한다.
    public float Heal(float amount)
    {
        if (!IsInitialized || amount <= 0f || IsDead)
        {
            return 0f;
        }

        float previousHP = runtimeState.CurrentHP;
        runtimeState.CurrentHP += amount;
        float healedAmount = runtimeState.CurrentHP - previousHP;

        if (healedAmount > 0f)
        {
            CurrentHPChanged?.Invoke(runtimeState.CurrentHP);
        }

        return healedAmount;
    }

    // 사용 가능한 회피 충전을 하나 소비한다.
    public bool TryConsumeDodge()
    {
        if (!IsInitialized || runtimeState.CurrentDodgeCount <= 0)
        {
            return false;
        }

        runtimeState.CurrentDodgeCount--;
        CurrentDodgeCountChanged?.Invoke(runtimeState.CurrentDodgeCount);

        // 회피 사용 시 회복 시작
        isRecharging = true;
        dodgeFillProgress = 0f;

        return true;
    }

    // 회피 충전을 하나 회복한다. (외부에서 호출 시 회복 중단)
    public bool RestoreDodge()
    {
        if (!IsInitialized || runtimeState.CurrentDodgeCount >= PlayerRuntimeState.MaxDodgeCount)
        {
            return false;
        }

        runtimeState.CurrentDodgeCount++;
        CurrentDodgeCountChanged?.Invoke(runtimeState.CurrentDodgeCount);

        // 외부 회복(아이템 등) 시 자동 회복 중단
        if (runtimeState.CurrentDodgeCount >= PlayerRuntimeState.MaxDodgeCount)
        {
            isRecharging = false;
            dodgeFillProgress = 0f;
        }

        return true;
    }

    // 현재 공격 콤보 카운트를 최대 공격 횟수 범위로 설정한다.
    public void SetAttackCount(int attackCount)
    {
        if (!IsInitialized)
        {
            return;
        }

        int clampedAttackCount = Mathf.Clamp(attackCount, 0, MaxAttackCount);
        if (runtimeState.CurrentAttackCount == clampedAttackCount)
        {
            return;
        }

        runtimeState.CurrentAttackCount = clampedAttackCount;
        CurrentAttackCountChanged?.Invoke(clampedAttackCount);
    }

    // 현재 공격 콤보 카운트를 초기화한다.
    public void ResetAttackCount()
    {
        SetAttackCount(0);
    }

    // 완벽한 회피로 강화된 다음 공격의 데미지를 계산하고 준비 상태를 해제한다.
    public float CalculateNextAttackDamage()
    {
        float damage = AttackDamage * nextAttackDamageMultiplier;
        nextAttackDamageMultiplier = 1f;
        return damage;
    }

    // 일반 처치 누적 수를 올리고 조건을 충족하면 영혼 충전 단계를 올린다.
    public void RegisterNormalKill()
    {
        if (!IsInitialized || CurrentSoulChargeStage >= PlayerRuntimeState.MaxSoulChargeStage)
        {
            return;
        }

        runtimeState.NormalKillCount++;
        if (runtimeState.NormalKillCount >= NormalKillsRequiredForSoulCharge)
        {
            runtimeState.NormalKillCount = 0;
            IncreaseSoulChargeStage();
        }

        NormalKillCountChanged?.Invoke(runtimeState.NormalKillCount);
    }

    // 처형 처치를 처리해 영혼 충전 단계를 즉시 올린다.
    public void RegisterExecutionKill()
    {
        if (!IsInitialized || CurrentSoulChargeStage >= PlayerRuntimeState.MaxSoulChargeStage)
        {
            return;
        }

        IncreaseSoulChargeStage();
    }

    // 영혼 충전 유지 시간을 감소시키고 시간이 끝나면 단계를 낮춘다.
    public void TickSoulCharge(float deltaTime)
    {
        if (!IsInitialized || CurrentSoulChargeStage <= 0 || deltaTime <= 0f)
        {
            return;
        }

        runtimeState.SoulChargeRemainingTime = Mathf.Max(0f, runtimeState.SoulChargeRemainingTime - deltaTime);
        if (runtimeState.SoulChargeRemainingTime > 0f)
        {
            return;
        }

        runtimeState.CurrentSoulChargeStage--;
        runtimeState.NormalKillCount = 0;
        runtimeState.SoulChargeRemainingTime = CurrentSoulChargeStage > 0 ? SoulChargeDuration : 0f;
        SoulChargeStageChanged?.Invoke(runtimeState.CurrentSoulChargeStage);
        NormalKillCountChanged?.Invoke(runtimeState.NormalKillCount);
    }

    // 영혼 충전 단계를 하나 올리고 유지 시간을 초기화한다.
    private void IncreaseSoulChargeStage()
    {
        int previousStage = runtimeState.CurrentSoulChargeStage;
        runtimeState.CurrentSoulChargeStage++;
        runtimeState.SoulChargeRemainingTime = SoulChargeDuration;

        if (previousStage != runtimeState.CurrentSoulChargeStage)
        {
            SoulChargeStageChanged?.Invoke(runtimeState.CurrentSoulChargeStage);
        }
    }

    // 런타임 스탯 초기화 후 모든 상태 변경 이벤트를 알린다.
    private void RaiseRuntimeStateChanged()
    {
        CurrentHPChanged?.Invoke(CurrentHP);
        CurrentDodgeCountChanged?.Invoke(CurrentDodgeCount);
        CurrentAttackCountChanged?.Invoke(CurrentAttackCount);
        SoulChargeStageChanged?.Invoke(CurrentSoulChargeStage);
        NormalKillCountChanged?.Invoke(NormalKillCount);
    }

    // 현재 회피 충전 회복 진행도(0~1)를 반환한다. 최대 충전 시 1, 회복 중이 아니면 0을 반환한다.
    private float GetDodgeFillProgress()
    {
        if (!IsInitialized)
        {
            return 1f;
        }

        int current = runtimeState.CurrentDodgeCount;
        int max = PlayerRuntimeState.MaxDodgeCount;
        if (current >= max)
        {
            return 1f;
        }

        if (!isRecharging)
        {
            return 0f;
        }

        return dodgeFillProgress;
    }
}
