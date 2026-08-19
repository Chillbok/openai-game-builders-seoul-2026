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

	[Header("방어 관련 스테이터스")]
	[Tooltip("방어 상태에서 감소시키는 받는 피해의 비율(0~100)")]
	[SerializeField, Range(0f, 100f)]
	private float defenseDamageReductionRate = 70f;

	[Header("패링 관련 스테이터스")]
	[Tooltip("방어 시작 후 공격 피해 판정이 패링으로 인정되는 시간(초)")]
	[SerializeField, Min(0f)]
	private float parryAcceptanceTime;

	[Tooltip("패링 성공 후 다음 일반 공격에 적용되는 피해 배율")]
	[SerializeField, Min(0f)]
	private float parryDamageMultiplier = 3f;

	[Header("처형 관련 스테이터스")]
	[Tooltip("처형이 가능한 적과의 최대 거리")]
	[SerializeField, Min(0f)]
	private float executionDistance;

	[Header("공격 관련 스테이터스")]
	[Tooltip("플레이어의 기본 공격 피해량")]
	[SerializeField, Min(0f)]
	private float attackDamage;

	[Tooltip("한 번 공격한 뒤 다음 공격까지 필요한 시간(초)")]
	[SerializeField, Min(0f)]
	private float attackCooldown;

	[Tooltip("공격이 적중할 수 있는 플레이어와 적 사이의 최대 거리")]
	[SerializeField, Min(0f)]
	private float attackDistance;

	[Tooltip("플레이어 전방을 기준으로 공격이 적용되는 범위각")]
	[SerializeField, Range(0f, 360f)]
	private float attackAngle;

	public float MaxHP => maxHP;
	public float DefaultMoveSpeed => defaultMoveSpeed;
	public float DodgeLength => dodgeLength;
	public float DodgeSpeed => dodgeSpeed;
	public float DodgeRechargeTime => dodgeRechargeTime;
	public float ExhaustedDodgeRechargeTime => exhaustedDodgeRechargeTime;
	public float PerfectDodgeAcceptanceTime => perfectDodgeAcceptanceTime;
	public float PerfectDodgeAttackDamageMultiplier => perfectDodgeAttackDamageMultiplier;
	public float DefenseDamageReductionRate => defenseDamageReductionRate;
	public float ParryAcceptanceTime => parryAcceptanceTime;
	public float ParryDamageMultiplier => parryDamageMultiplier;
	public float HitInvincibilityTime => hitInvincibilityTime;
	public float ExecutionDistance => executionDistance;
	public float AttackDamage => attackDamage;
	public float AttackCooldown => attackCooldown;
	public float AttackDistance => attackDistance;
	public float AttackAngle => attackAngle;

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
	public float CurrentHP { get; set; }
	public int CurrentDodgeCount { get; set; }
	public float CurrentAttackCooldown { get; set; }
	public int CurrentSoulChargeStage { get; set; }
	public int NormalKillCount { get; set; }
	public float SoulChargeRemainingTime { get; set; }

	public PlayerRuntimeState(PlayerData data)
	{
		if (data == null)
		{
			throw new ArgumentNullException(nameof(data));
		}

		CurrentHP = data.MaxHP;
		CurrentDodgeCount = 3;
		CurrentAttackCooldown = 0f;
		CurrentSoulChargeStage = 0;
		NormalKillCount = 0;
		SoulChargeRemainingTime = 0f;
	}
}
