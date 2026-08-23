using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(CircleCollider2D))]
public class EnemyAttackRangeSensor : MonoBehaviour
{
    [Header("대상 레이어")]
    [SerializeField]
    private LayerMask targetLayer;

    [Tooltip("센서 반경을 EnemyData.AttackRange와 자동 동기화할지 여부")]
    [SerializeField]
    private bool autoSyncRadius = true;

    public LayerMask TargetLayer
    {
        get => targetLayer;
        set => targetLayer = value;
    }

    private CircleCollider2D sensorCollider;
    private EnemyStatController enemyStatController;
    private readonly HashSet<Collider2D> targetsInRange = new HashSet<Collider2D>();
    private float lastSyncedRange = -1f;

    public bool CanAttack { get; private set; }

    private void Awake()
    {
        sensorCollider = GetComponent<CircleCollider2D>();
        enemyStatController = GetComponentInParent<EnemyStatController>();

        if (sensorCollider == null)
        {
            Debug.LogError("EnemyAttackRangeSensor에 CircleCollider2D가 필요합니다.", this);
            enabled = false;
            return;
        }

        sensorCollider.isTrigger = true;

        if (enemyStatController == null)
        {
            Debug.LogWarning("EnemyAttackRangeSensor: 부모에서 EnemyStatController를 찾지 못했습니다. 자동 동기화가 제한됩니다.", this);
        }

        // 인스펙터에서 미설정 시 기본값으로 Player 레이어 사용
        if (targetLayer.value == 0)
        {
            int playerLayer = LayerMask.NameToLayer("Player");
            if (playerLayer >= 0)
            {
                targetLayer = 1 << playerLayer;
            }
        }

        SyncRadius();
    }

    private void OnEnable()
    {
        SyncRadius();
        ValidateTargets();
    }

    private void Update()
    {
        if (autoSyncRadius)
        {
            SyncRadius();
        }

        // OnTriggerExit 누락 보정: 비활성화된 오브젝트나 텔레포트 등으로 Exit가 안 온 경우 주기적으로 재검증
        // 매 프레임이 아니라 CanAttack 상태일 때만 가볍게 검증
        if (CanAttack && targetsInRange.Count > 0)
        {
            ValidateTargets();
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!IsTargetLayer(other.gameObject.layer))
        {
            return;
        }

        if (other.transform.IsChildOf(transform.root) && other.transform.IsChildOf(enemyStatController != null ? enemyStatController.transform : transform))
        {
            // 자기 자신 계층의 콜라이더 제외 (히트박스 등)
            if (other.transform.IsChildOf(transform.parent != null ? transform.parent : transform))
            {
                // 더 정확한 자기 자신 체크는 EnemyStateMachine에서 bodyCollider로 하지만, 여기서도 부모 계층이면 무시
                // 단, 센서 자신의 콜라이더는 제외됨 (other != sensorCollider)
                if (other == sensorCollider)
                {
                    return;
                }
            }
        }

        if (other == sensorCollider)
        {
            return;
        }

        // 자기 자신(적 본체)의 콜라이더 제외
        if (enemyStatController != null && other.transform.IsChildOf(enemyStatController.transform))
        {
            return;
        }

        targetsInRange.Add(other);
        CanAttack = true;
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!IsTargetLayer(other.gameObject.layer))
        {
            return;
        }

        targetsInRange.Remove(other);
        if (targetsInRange.Count == 0)
        {
            CanAttack = false;
        }
    }

    private void OnDisable()
    {
        targetsInRange.Clear();
        CanAttack = false;
    }

    private void SyncRadius()
    {
        if (!autoSyncRadius || sensorCollider == null || enemyStatController == null || !enemyStatController.IsInitialized)
        {
            return;
        }

        float range = enemyStatController.AttackRange;
        if (Mathf.Approximately(range, lastSyncedRange))
        {
            return;
        }

        // CircleCollider2D radius는 로컬 공간, 부모 스케일 영향을 받으므로 월드 반경을 맞추기 위해 스케일로 나눈다
        // 센서는 적 본체의 자식이므로 부모 스케일을 고려해야 함
        float parentScale = Mathf.Max(Mathf.Abs(transform.lossyScale.x), Mathf.Abs(transform.lossyScale.y));
        if (parentScale < 0.0001f)
        {
            parentScale = 1f;
        }

        float localRadius = range / parentScale;
        sensorCollider.radius = Mathf.Max(0f, localRadius);
        lastSyncedRange = range;
    }

    private void ValidateTargets()
    {
        if (targetsInRange.Count == 0)
        {
            CanAttack = false;
            return;
        }

        // 파괴된 오브젝트(null)나 비활성화된 콜라이더 정리
        targetsInRange.RemoveWhere(c => c == null || !c.gameObject.activeInHierarchy || !IsTargetLayer(c.gameObject.layer));

        // 여전히 남아있는 타겟이 센서 범위 안에 실제로 존재하는지 Overlap으로 재확인 (Exit 누락 보정)
        if (targetsInRange.Count == 0)
        {
            CanAttack = false;
            return;
        }

        // 센서가 꺼져있으면 CanAttack false
        if (sensorCollider == null || !sensorCollider.enabled || !gameObject.activeInHierarchy)
        {
            CanAttack = false;
            return;
        }

        CanAttack = targetsInRange.Count > 0;
    }

    private bool IsTargetLayer(int layer)
    {
        return (targetLayer.value & (1 << layer)) != 0;
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (sensorCollider == null)
        {
            sensorCollider = GetComponent<CircleCollider2D>();
            if (sensorCollider == null) return;
        }

        Gizmos.color = CanAttack ? new Color(1f, 0.3f, 0.3f, 0.9f) : new Color(0.3f, 1f, 0.3f, 0.5f);
        Vector3 worldCenter = transform.TransformPoint(sensorCollider.offset);
        float worldRadius = sensorCollider.radius * Mathf.Max(Mathf.Abs(transform.lossyScale.x), Mathf.Abs(transform.lossyScale.y));
        // Unity Gizmos는 2D에서도 3D로 그리므로 와이어 스피어 대신 원 근사
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
#endif
}
