using System;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class ExplosionController : MonoBehaviour
{
    [Header("폭발 설정")]
    [SerializeField, Min(0f)]
    [Tooltip("폭발에 닿은 대상에게 주는 피해량. 0 이상 값만 유효 (기본 60)")]
    private float damage = 60f;

    [SerializeField]
    [Tooltip("피해를 받을 대상의 레이어. 체크된 레이어에 속한 Collider만 데미지 판정 (예: Enemy, Player)")]
    private LayerMask targetLayer;

    private Collider2D explosionCollider;
    private readonly Collider2D[] overlapBuffer = new Collider2D[32];
    private readonly HashSet<EnemyStatController> hitEnemies = new HashSet<EnemyStatController>();
    private readonly HashSet<PlayerStatController> hitPlayers = new HashSet<PlayerStatController>();

    /// <summary>
    /// 보라색 폭발 애니메이션이 끝나면 발생하는 이벤트. 애니메이션 이벤트에서 호출되는 OnPurpleExplosionFinished()가 이 이벤트를 발생시킨다.
    /// </summary>
    public event Action PurpleExplosionFinished;

    /// <summary>
    /// 애니메이션 이벤트(AnimationEvent)에서 호출되어 PurpleExplosionFinished 이벤트를 발생시킨다.
    /// 클립 마지막 프레임에 functionName = "OnPurpleExplosionFinished" 로 등록하여 사용한다.
    /// </summary>
    public void OnPurpleExplosionFinished()
    {
        PurpleExplosionFinished?.Invoke();
    }

    private bool destroyed;

    private void DestroySelf()
    {
        if (destroyed) return;
        destroyed = true;
        Destroy(gameObject);
    }

    private void OnEnable()
    {
        destroyed = false;
        PurpleExplosionFinished += DestroySelf;
    }

    private void OnDisable()
    {
        PurpleExplosionFinished -= DestroySelf;
    }

    private void Awake()
    {
        explosionCollider = GetComponent<Collider2D>();
        if (explosionCollider != null)
        {
            explosionCollider.isTrigger = true;
        }
    }

    private void Start()
    {
        if (explosionCollider == null)
        {
            explosionCollider = GetComponent<Collider2D>();
        }

        ApplyInitialOverlaps();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        TryApplyDamage(other);
    }

    private void TryApplyDamage(Collider2D other)
    {
        if (other == null || !IsTargetLayer(other.gameObject.layer))
        {
            return;
        }

        EnemyStatController enemy = other.GetComponentInParent<EnemyStatController>();
        if (enemy != null && enemy.IsInitialized && hitEnemies.Add(enemy))
        {
            enemy.TryTakeDamage(damage, Vector2.zero);
            return;
        }

        PlayerStatController player = other.GetComponentInParent<PlayerStatController>();
        if (player != null && player.IsInitialized && hitPlayers.Add(player))
        {
            player.TryTakeDamage(damage);
        }
    }

    private bool IsTargetLayer(int layer)
    {
        return (targetLayer.value & (1 << layer)) != 0;
    }

    private void ApplyInitialOverlaps()
    {
        if (explosionCollider is not CircleCollider2D circleCollider)
        {
            return;
        }

        float worldRadius = circleCollider.radius * Mathf.Max(transform.lossyScale.x, transform.lossyScale.y);
        int hitCount = Physics2D.OverlapCircleNonAlloc(
            transform.position,
            worldRadius,
            overlapBuffer,
            targetLayer);

        for (int i = 0; i < hitCount; i++)
        {
            if (overlapBuffer[i] != null)
            {
                TryApplyDamage(overlapBuffer[i]);
            }
        }
    }
}
