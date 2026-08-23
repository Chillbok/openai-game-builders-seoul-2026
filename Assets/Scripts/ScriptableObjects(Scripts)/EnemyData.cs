using System;
using UnityEngine;

[CreateAssetMenu(fileName = "EnemyData", menuName = "Scriptable Objects/EnemyData")]
public class EnemyData : ScriptableObject
{
	[Header("적 생존 관련 스테이터스")]
	[Tooltip("적이 가질 수 있는 최대 체력. 현재 HP와 구분하며 기절 진입을 판정하는 기준값이다.")]
	[SerializeField, Min(0f)]
	private float maxHP = 30f;

	[Header("적 이동 관련 스테이터스")]
	[Tooltip("추적 상태일 때 플레이어를 향해 이동하는 속도")]
	[SerializeField, Min(0f)]
	private float moveSpeed = 1.5f;

	[Header("적 공격 관련 스테이터스")]
	[Tooltip("공격이 적중했을 때 플레이어 HP에서 차감하는 피해량")]
	[SerializeField, Min(0f)]
	private float attackDamage = 10f;

	[Tooltip("한 번 공격한 뒤 다음 공격을 수행할 때까지 필요한 시간(초)")]
	[SerializeField, Min(0f)]
	private float attackCooldown = 2f;

	[Tooltip("공격을 시작할 수 있는 플레이어와의 거리")]
	[SerializeField, Min(0f)]
	private float attackRange = 1.5f;

	[Tooltip("공격 준비를 시작한 뒤 피해 판정을 내기까지 대기하는 시간(초)")]
	[SerializeField, Min(0f)]
	private float attackPrepareTime = 0.4f;

	[Header("넉백 관련 스테이터스")]
	[Tooltip("플레이어 공격에 맞았을 때 적이 밀려나는 거리")]
	[SerializeField, Min(0f)]
	private float knockbackDistance = 1.5f;

	public float MaxHP => maxHP;
	public float MoveSpeed => moveSpeed;
	public float AttackDamage => attackDamage;
	public float AttackCooldown => attackCooldown;
	public float AttackRange => attackRange;
	public float AttackPrepareTime => attackPrepareTime;
	public float KnockbackDistance => knockbackDistance;

	/// <summary>
	/// 설정 에셋을 변경하지 않고, 적 개체 하나가 사용할 런타임 상태를 생성한다.
	/// </summary>
	public EnemyRuntimeState CreateRuntimeState()
	{
		return new EnemyRuntimeState(this);
	}
}

/// <summary>
/// 한 적 개체의 전투 중 상태다. EnemyData 에셋에 저장하지 않는다.
/// </summary>
[Serializable]
public sealed class EnemyRuntimeState
{
	private float currentHP;
	private float currentAttackCooldown;

	private readonly EnemyData data;

	public float CurrentHP
	{
		get => currentHP;
		set => currentHP = Mathf.Clamp(value, 0f, data.MaxHP);
	}

	public float CurrentAttackCooldown
	{
		get => currentAttackCooldown;
		set => currentAttackCooldown = Mathf.Max(0f, value);
	}

	public EnemyRuntimeState(EnemyData data)
	{
		if (data == null)
		{
			throw new ArgumentNullException(nameof(data));
		}

		this.data = data;

		CurrentHP = data.MaxHP;
		CurrentAttackCooldown = 0f;
	}
}