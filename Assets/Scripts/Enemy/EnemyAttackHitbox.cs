using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class EnemyAttackHitbox : MonoBehaviour
{
    private Collider2D attackCollider;
    private EnemyStatController enemyStatController;
    private HashSet<Collider2D> hitTargets;

    private void Awake()
    {
        attackCollider = GetComponent<Collider2D>();
        enemyStatController = GetComponentInParent<EnemyStatController>();
        hitTargets = new HashSet<Collider2D>();

        if (attackCollider == null)
        {
            Debug.LogError("EnemyAttackHitbox에 공격 콜라이더가 할당되지 않았습니다.", this);
            enabled = false;
        }
    }

    // 컴포넌트가 활성화될 때마다 공격 콜라이더를 비활성 상태로 시작한다.
    private void OnEnable()
    {
        if (attackCollider != null)
        {
            attackCollider.enabled = false;
        }
    }

    // 공격 애니메이션 시점에 호출해 히트박스를 활성화한다. 이미 맞은 대상은 다시 맞지 않는다.
    public void EnableHitbox()
    {
        if (attackCollider == null || !enabled)
        {
            return;
        }

        hitTargets.Clear();
        attackCollider.enabled = true;
    }

    // 공격 애니메이션 종료 시 호출해 히트박스를 비활성화한다.
    public void DisableHitbox()
    {
        if (attackCollider == null)
        {
            return;
        }

        attackCollider.enabled = false;
        hitTargets.Clear();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!attackCollider.enabled || other.gameObject.layer != LayerMask.NameToLayer("Player") || !hitTargets.Add(other))
        {
            return;
        }

        if (enemyStatController == null || !enemyStatController.IsInitialized)
        {
            return;
        }

        PlayerStatController playerStatController = other.GetComponentInParent<PlayerStatController>();
        if (playerStatController == null || !playerStatController.IsInitialized)
        {
            return;
        }

        playerStatController.TryTakeDamage(enemyStatController.AttackDamage);
    }
}