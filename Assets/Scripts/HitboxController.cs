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
    private EnemyStatController enemyStatController;
    private HashSet<Collider2D> hitTargets;

    private void Awake()
    {
        playerStatController = GetComponentInParent<PlayerStatController>();
        enemyStatController = GetComponentInParent<EnemyStatController>();
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

    // 히트박스에 들어온 대상이 공격자 소속에 따라 데미지를 받는다.
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!hitboxCollider.enabled || !IsTargetLayer(other.gameObject.layer) || !hitTargets.Add(other))
        {
            return;
        }

        if (playerStatController != null && playerStatController.IsInitialized)
        {
            HandlePlayerHitboxHit(other);
        }
        else if (enemyStatController != null && enemyStatController.IsInitialized)
        {
            HandleEnemyHitboxHit(other);
        }
    }

    // 플레이어 히트박스는 대상 적에게 피해와 넉백을 적용한다.
    private void HandlePlayerHitboxHit(Collider2D other)
    {
        EnemyStatController target = other.GetComponentInParent<EnemyStatController>();
        if (target == null || !target.IsInitialized)
        {
            return;
        }

        float damage = playerStatController.CalculateNextAttackDamage();
        Vector2 knockbackDirection = other.transform.position - transform.position;
        bool hitApplied = target.TryTakeDamage(damage, knockbackDirection.normalized);
        if (hitApplied)
        {
            PlayHitSfx();
        }
    }

    private void PlayHitSfx()
    {
        AudioConfig cfg = null;
        if (playerStatController != null)
        {
            // PlayerStatController의 audioConfig via reflection 대신 AudioService 우선
            cfg = AudioService.Instance != null ? AudioService.Instance.Config : Resources.Load<AudioConfig>("DefaultAudioConfig");
#if UNITY_EDITOR
        if (cfg == null) cfg = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioConfig>("Assets/Resources/DefaultAudioConfig.asset");
#endif
        }
        if (cfg == null) cfg = Resources.Load<AudioConfig>("DefaultAudioConfig");
#if UNITY_EDITOR
        if (cfg == null) cfg = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioConfig>("Assets/Resources/DefaultAudioConfig.asset");
#endif
        if (cfg == null || cfg.HitClip == null) return;
        if (AudioService.Instance != null) AudioService.Instance.PlaySFX(cfg.HitClip, "playerHit", 0.04f, AudioService.Priority.High);
        else AudioSource.PlayClipAtPoint(cfg.HitClip, transform.position);
    }

    // 적 히트박스는 대상 플레이어에게 피해를 적용한다.
    private void HandleEnemyHitboxHit(Collider2D other)
    {
        PlayerStatController target = other.GetComponentInParent<PlayerStatController>();
        if (target == null || !target.IsInitialized)
        {
            return;
        }

        target.TryTakeDamage(enemyStatController.AttackDamage);
    }

    private bool IsTargetLayer(int layer)
    {
        return (hitboxTargetLayers.value & (1 << layer)) != 0;
    }
}