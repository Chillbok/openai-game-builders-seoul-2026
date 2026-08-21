using System;
using UnityEngine;

[CreateAssetMenu(fileName = "PlayerData", menuName = "Scriptable Objects/PlayerData")]
public class PlayerData : ScriptableObject
{
	[Header("플레이어 생존 관련 스테이터스")]
	[Tooltip("플레이어가 가질 수 있는 최대 체력")]
	[SerializeField, Min(0f)]
	private float maxHP = 100f;

	[Tooltip("피격 후 추가 피해를 받지 않는 시간(초)")]
	[SerializeField, Min(0f)]
	private float hitInvincibilityTime = 0.1f;

	[Header("플레이어 이동 관련 스테이터스")]
	[Tooltip("플레이어의 기본 이동 속도")]
	[SerializeField, Min(0f)]
	private float defaultMoveSpeed;

	[Header("회피 관련 스테이터스")]
	[Tooltip("회피 시 플레이어가 돌진하는 거리")]
	[SerializeField, Min(0f)]
	private float dodgeLength;

	[Tooltip("회피 시 플레이어가 돌진하는 속도")]
	[SerializeField, Min(0f)]
	private float dodgeSpeed;

	[Tooltip("회피 충전 하나를 회복하는 데 필요한 시간(초)")]
	[SerializeField, Min(0f)]
	private float dodgeRechargeTime = 2f;

	[Tooltip("모든 회피 충전을 소진했을 때 전체 충전에 필요한 시간(초)")]
	[SerializeField, Min(0f)]
	private float exhaustedDodgeRechargeTime = 4f;

	[Header("완벽한 회피 관련 스테이터스")]
	[Tooltip("회피 시작 후 공격 피해 판정이 완벽한 회피로 인정되는 시간(초)")]
	[SerializeField, Min(0f)]
	private float perfectDodgeAcceptanceTime;

	[Tooltip("완벽한 회피 성공 후 다음 일반 공격에 적용되는 피해 배율")]
	[SerializeField, Min(0f)]
	private float perfectDodgeAttackDamageMultiplier = 2f;

	[Header("처형 관련 스테이터스")]
	[Tooltip("처형이 가능한 적과의 최대 거리")]
	[SerializeField, Min(0f)]
	private float executionDistance;

	[Header("공격 관련 스테이터스")]
	[Tooltip("플레이어의 기본 공격 피해량")]
	[SerializeField, Min(0f)]
	private float attackDamage;

	[Tooltip("공격 모션의 기본 재생 속도 배율")]
	[SerializeField, Min(0f)]
	private float attackSpeed = 1f;

	[Tooltip("공격이 적중할 수 있는 플레이어와 적 사이의 최대 거리")]
	[SerializeField, Min(0f)]
	private float attackDistance;

	[Tooltip("플레이어 전방을 기준으로 공격이 적용되는 범위각")]
	[SerializeField, Range(0f, 360f)]
	private float attackAngle;

	[Header("영혼 충전 관련 스테이터스")]
	[Tooltip("영혼 충전 단계가 1 이상일 때 다음 단계 감소까지 유지되는 시간(초)")]
	[SerializeField, Min(0f)]
	private float soulChargeDuration = 12f;

	[Tooltip("영혼 충전 1단계의 받는 피해 추가 감소율(0~100)")]
	[SerializeField, Range(0f, 100f)]
	private float soulChargeDamageReductionRate = 30f;

	[Tooltip("영혼 충전 2단계의 공격 모션 재생 속도 증가율(0~100)")]
	[SerializeField, Range(0f, 100f)]
	private float soulChargeAttackSpeedIncreaseRate = 30f;

	[Tooltip("영혼 충전 3단계의 공격 피해 배율")]
	[SerializeField, Min(0f)]
	private float soulChargeAttackDamageMultiplier = 2f;

	[Tooltip("영혼 충전 4단계 처치 시 광역 폭발의 피해량")]
	[SerializeField, Min(0f)]
	private float soulChargeExplosionDamage;

	[Tooltip("영혼 충전 4단계 처치 시 광역 폭발의 반경")]
	[SerializeField, Min(0f)]
	private float soulChargeExplosionRadius;

	public float MaxHP => maxHP;
	public float DefaultMoveSpeed => defaultMoveSpeed;
	public float DodgeLength => dodgeLength;
	public float DodgeSpeed => dodgeSpeed;
	public float DodgeRechargeTime => dodgeRechargeTime;
	public float ExhaustedDodgeRechargeTime => exhaustedDodgeRechargeTime;
	public float PerfectDodgeAcceptanceTime => perfectDodgeAcceptanceTime;
	public float PerfectDodgeAttackDamageMultiplier => perfectDodgeAttackDamageMultiplier;
	public float HitInvincibilityTime => hitInvincibilityTime;
	public float ExecutionDistance => executionDistance;
	public float AttackDamage => attackDamage;
	public float AttackSpeed => attackSpeed;
	public float AttackDistance => attackDistance;
	public float AttackAngle => attackAngle;
	public float SoulChargeDuration => soulChargeDuration;
	public float SoulChargeDamageReductionRate => soulChargeDamageReductionRate;
	public float SoulChargeAttackSpeedIncreaseRate => soulChargeAttackSpeedIncreaseRate;
	public float SoulChargeAttackDamageMultiplier => soulChargeAttackDamageMultiplier;
	public float SoulChargeExplosionDamage => soulChargeExplosionDamage;
	public float SoulChargeExplosionRadius => soulChargeExplosionRadius;

	/// <summary>
	/// 설정 에셋을 변경하지 않고, 플레이어 한 명이 사용할 런타임 상태를 생성한다.
	/// </summary>
	public PlayerRuntimeState CreateRuntimeState()
	{
		return new PlayerRuntimeState(this);
	}
}

/// <summary>
/// 한 플레이어의 전투 중 상태다. PlayerData 에셋에 저장하지 않는다.
/// </summary>
[Serializable]
public sealed class PlayerRuntimeState
{
	public const int MaxDodgeCount = 3;
	public const int MaxSoulChargeStage = 4;

	private float currentHP;
	private int currentDodgeCount;
	private int currentAttackCount;
	private int currentSoulChargeStage;

	public float CurrentHP
	{
		get => currentHP;
		set => currentHP = Mathf.Clamp(value, 0f, data.MaxHP);
	}

	public int CurrentDodgeCount
	{
		get => currentDodgeCount;
		set => currentDodgeCount = Mathf.Clamp(value, 0, MaxDodgeCount);
	}

	public int CurrentAttackCount
	{
		get => currentAttackCount;
		set => currentAttackCount = Mathf.Max(0, value);
	}

	public int CurrentSoulChargeStage
	{
		get => currentSoulChargeStage;
		set => currentSoulChargeStage = Mathf.Clamp(value, 0, MaxSoulChargeStage);
	}
	public int NormalKillCount { get; set; }
	public float SoulChargeRemainingTime { get; set; }

	private readonly PlayerData data;

	public PlayerRuntimeState(PlayerData data)
	{
		if (data == null)
		{
			throw new ArgumentNullException(nameof(data));
		}

		this.data = data;

		CurrentHP = data.MaxHP;
		CurrentDodgeCount = MaxDodgeCount;
		CurrentAttackCount = 0;
		CurrentSoulChargeStage = 0;
		NormalKillCount = 0;
		SoulChargeRemainingTime = 0f;
	}
}
