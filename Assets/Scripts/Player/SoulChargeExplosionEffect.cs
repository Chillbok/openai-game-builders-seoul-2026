using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public sealed class SoulChargeExplosionEffect : MonoBehaviour
{
    private const int DefaultEnemyLayer = 7;
    private const float DefaultLifetime = 0.4f;

    [Header("폭발 설정")]
    [SerializeField, Min(0f)]
    [Tooltip("폭발 프리팹의 X·Y 스케일에 곱할 균일한 배율")]
    private float explosionScaleMultiplier = 1.5f;

    [SerializeField, Min(0f)]
    [Tooltip("폭발에 닿은 적에게 주는 피해량")]
    private float explosionDamage = 15f;

    [SerializeField]
    [Tooltip("폭발 피해 대상 레이어. 기본값은 Enemy 레이어")]
    private LayerMask targetLayers = 1 << DefaultEnemyLayer;

    [SerializeField, Min(0f)]
    [Tooltip("폭발 이펙트를 유지할 시간")]
    private float lifetime = DefaultLifetime;

    private Collider2D explosionCollider;
    private readonly Collider2D[] overlapBuffer = new Collider2D[32];
    private readonly HashSet<EnemyStatController> hitTargets = new HashSet<EnemyStatController>();

    private void Awake()
    {
        explosionCollider = GetComponent<Collider2D>();
        explosionCollider.isTrigger = true;
        transform.localScale *= explosionScaleMultiplier;
    }

    private void Start()
    {
        ApplyInitialOverlaps();
        Destroy(gameObject, lifetime > 0f ? lifetime : 0.01f);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        TryApplyDamage(other);
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
            targetLayers);

        for (int i = 0; i < hitCount; i++)
        {
            if (overlapBuffer[i] != null)
            {
                TryApplyDamage(overlapBuffer[i]);
            }
        }
    }

    private void TryApplyDamage(Collider2D other)
    {
        if (other == null || !IsTargetLayer(other.gameObject.layer))
        {
            return;
        }

        EnemyStatController target = other.GetComponentInParent<EnemyStatController>();
        if (target == null || !target.IsInitialized || !hitTargets.Add(target))
        {
            return;
        }

        Vector2 knockbackDirection = target.transform.position - transform.position;
        target.TryTakeDamage(
            explosionDamage,
            knockbackDirection.sqrMagnitude > 0.0001f ? knockbackDirection.normalized : Vector2.zero,
            EnemyDeathReason.SoulChargeExplosion);
    }

    private bool IsTargetLayer(int layer)
    {
        return (targetLayers.value & (1 << layer)) != 0;
    }
}
