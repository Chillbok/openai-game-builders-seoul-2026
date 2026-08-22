using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 적 공격 시 저장된 위치에 생성되는 투사체 피해 오브젝트.
/// 애니메이션 시작부터 종료(AnimationEvent)까지 트리거를 유지하며, 특정 레이어 대상에게 1회만 피해를 준다.
/// 데미지는 생성한 적의 EnemyStatController.AttackDamage를 Initialize로 주입받는다.
/// </summary>
[RequireComponent(typeof(Collider2D))]
[RequireComponent(typeof(Animator))]
public class EnemyDamageInstance : MonoBehaviour
{
    [Header("피해 대상 레이어")]
    [Tooltip("피해를 받을 대상 레이어 (인스펙터에서 선택)")]
    [SerializeField]
    private LayerMask targetLayers;

    private float damage;
    private Collider2D damageCollider;
    private readonly HashSet<Collider2D> hitTargets = new HashSet<Collider2D>();
    private bool isDestroyed;

    /// <summary>
    /// 피해량을 설정한다. 수명은 애니메이션 이벤트로 결정된다.
    /// </summary>
    public void Initialize(float damageAmount)
    {
        damage = damageAmount;
    }

    /// <summary>
    /// 기존 시그니처 호환용. lifetime은 무시하고 damage만 사용한다.
    /// </summary>
    public void Initialize(float damageAmount, float lifeTime)
    {
        Initialize(damageAmount);
    }

    private void Awake()
    {
        damageCollider = GetComponent<Collider2D>();
        if (damageCollider == null)
        {
            Debug.LogError("EnemyDamageInstance에 Collider2D가 필요합니다.", this);
            enabled = false;
            return;
        }

        damageCollider.isTrigger = true;
        damageCollider.enabled = true;

        // 인스펙터 미설정 시 기본값으로 Player 레이어 사용 (HitboxController/센서 패턴)
        if (targetLayers.value == 0)
        {
            int playerLayer = LayerMask.NameToLayer("Player");
            if (playerLayer >= 0)
            {
                targetLayers = 1 << playerLayer;
            }
        }
    }

    private void OnEnable()
    {
        hitTargets.Clear();
        isDestroyed = false;
    }

    private void Start()
    {
        // AnimationEvent 유실 대비 안전망: 5초 후 강제 파괴
        Destroy(gameObject, 5f);
    }

    /// <summary>
    /// 애니메이션 종료 시 호출되는 AnimationEvent. 공격 윈도우 종료 = 오브젝트 파괴.
    /// </summary>
    public void OnSlashAnimationEnd()
    {
        if (isDestroyed) return;
        isDestroyed = true;
        Destroy(gameObject);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if ((targetLayers.value & (1 << other.gameObject.layer)) == 0)
        {
            return;
        }

        if (!hitTargets.Add(other))
        {
            return;
        }

        // 플레이어 대상 피해 (공유 프리팹이므로 PlayerStatController 우선)
        PlayerStatController player = other.GetComponentInParent<PlayerStatController>();
        if (player != null && player.IsInitialized)
        {
            player.TryTakeDamage(damage);
            return;
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (damageCollider == null)
        {
            damageCollider = GetComponent<Collider2D>();
            if (damageCollider == null) return;
        }

        Gizmos.color = new Color(1f, 0.3f, 0.3f, 0.4f);
        if (damageCollider is CircleCollider2D circle)
        {
            Vector3 worldCenter = transform.TransformPoint(circle.offset);
            float worldRadius = circle.radius * Mathf.Max(Mathf.Abs(transform.lossyScale.x), Mathf.Abs(transform.lossyScale.y));
            const int segments = 32;
            Vector3 prev = worldCenter + new Vector3(worldRadius, 0, 0);
            for (int i = 1; i <= segments; i++)
            {
                float angle = i * 2f * Mathf.PI / segments;
                Vector3 next = worldCenter + new Vector3(Mathf.Cos(angle) * worldRadius, Mathf.Sin(angle) * worldRadius, 0);
                Gizmos.DrawLine(prev, next);
                prev = next;
            }
        }
        else if (damageCollider is BoxCollider2D box)
        {
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawWireCube(box.offset, box.size);
        }
    }
}
