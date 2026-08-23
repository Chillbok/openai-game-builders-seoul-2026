using UnityEngine;

/// <summary>
/// 적 공격 시 플레이어 위치에 생성되는 일회성 피해 오브젝트.
/// 생성 직후 1회 피해를 주고, 애니메이션 수명(0.7s) 후 자동 파괴된다.
/// </summary>
public class EnemyDamageInstance : MonoBehaviour
{
    private float damage;
    private float lifetime = 0.7f;
    private bool damageDealt;

    /// <summary>
    /// 피해량과 수명을 설정하고 즉시 피해를 시도한다.
    /// </summary>
    public void Initialize(float damageAmount, float lifeTime)
    {
        damage = damageAmount;
        lifetime = Mathf.Max(0.05f, lifeTime);
    }

    private void Start()
    {
        TryDealDamage();
        Destroy(gameObject, lifetime);
    }

    private void TryDealDamage()
    {
        if (damageDealt) return;
        damageDealt = true;

        // 플레이어 위치에 생성되었으므로 범위 내 플레이어를 직접 찾아 피해 적용
        // 트리거 겹침 없이 코드로 직접 처리해 물리 프레임 의존성 제거
        PlayerStatController player = FindFirstObjectByType<PlayerStatController>();
        if (player == null || !player.IsInitialized || player.IsDead)
        {
            return;
        }

        // 스폰 위치가 플레이어 위치이므로 추가 거리 검사 없이 피해 적용
        // 센서(CanAttack)가 이미 사거리 내임을 보장함
        player.TryTakeDamage(damage);
    }
}
