using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class HitboxController : MonoBehaviour
{
    [Header("대상 적 레이어")]
    [SerializeField]
    private LayerMask hitboxTargetLayers;

    private Collider2D hitboxCollider;

    private PlayerStatController playerStatController;
    private HashSet<Collider2D> hitTargets;

    private void Awake()
    {
        playerStatController = GetComponentInParent<PlayerStatController>();
        hitTargets = new HashSet<Collider2D>();
        hitboxCollider = GetComponent<Collider2D>();

        if (hitboxCollider == null)
        {
            Debug.LogError("HitboxController에 히트박스 콜라이더가 할당되지 않았습니다.", this);
            enabled = false;
        }
    }

    // 컴포넌트가 활성화될 때마다 히트박스 콜라이더를 비활성 상태로 시작한다.
    private void OnEnable()
    {
        if (hitboxCollider != null)
        {
            hitboxCollider.enabled = false;
        }
    }

    // 공격 애니메이션 이벤트에서 호출해 히트박스를 활성화한다.
    public void EnableHitbox()
    {
        if (hitboxCollider == null || !enabled)
        {
            return;
        }

        hitTargets.Clear();
        hitboxCollider.enabled = true;
    }

    // 공격 애니메이션 이벤트에서 호출해 히트박스를 비활성화한다.
    public void DisableHitbox()
    {
        if (hitboxCollider == null)
        {
            return;
        }

        hitboxCollider.enabled = false;
        hitTargets.Clear();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!hitboxCollider.enabled || !IsTargetLayer(other.gameObject.layer) || !hitTargets.Add(other))
        {
            return;
        }

        if (playerStatController == null || !playerStatController.IsInitialized)
        {
            return;
        }

        // 적 구현 전 임시 확인용 로그. 적 데미지 처리 로직은 추후 구현한다.
        Debug.Log($"적이 데미지 {playerStatController.AttackDamage} 입음", other.gameObject);
    }

    private bool IsTargetLayer(int layer)
    {
        return (hitboxTargetLayers.value & (1 << layer)) != 0;
    }
}