using UnityEngine;

/// <summary>
/// 방 클리어 시 문 개방, 문 상호작용 시 다음 맵 전이를 담당한다.
/// 기획: 게임 시스템 세부 기획(게임 핵심 순환) - 활성 적 0개면 문 개방, EnterTrigger 상호작용으로 전이.
/// </summary>
[DisallowMultipleComponent]
public sealed class DoorController : MonoBehaviour
{
    private static readonly Color LockedColor = new Color(0x4D / 255f, 0x4D / 255f, 0x4D / 255f, 1f);
    private static readonly Color OpenColor = Color.white;

    [Header("참조")]
    [Tooltip("맵 생성기")]
    [SerializeField]
    private MapGenerator mapGenerator;

    [Tooltip("웨이브 스포너 (활성 적 카운트 참조)")]
    [SerializeField]
    private EnemyWaveSpawner waveSpawner;

    [Tooltip("문 스프라이트 렌더러 (색상으로 잠금 표시)")]
    [SerializeField]
    private SpriteRenderer doorRenderer;

    [Tooltip("문 콜라이더 (잠금 시 비활성화)")]
    [SerializeField]
    private Collider2D doorCollider;

    [Tooltip("상호작용 트리거 콜라이더")]
    [SerializeField]
    private Collider2D interactTrigger;

    [Tooltip("플레이어 Transform. 비어 있으면 태그로 탐색")]
    [SerializeField]
    private Transform playerTransform;

    [Header("전이")]
    [Tooltip("문이 열려 있을 때만 전이 허용")]
    [SerializeField]
    private bool requireOpenToTransit = true;

    [Tooltip("전이 후 플레이어를 맵 중앙으로 재배치하는지")]
    [SerializeField]
    private bool recenterPlayerOnTransit = true;

    private bool isOpen;
    private bool playerInsideTrigger;

    public bool IsOpen => isOpen;

    private void Awake()
    {
        CacheReferences();
        UpdateDoorVisual(true);
    }

    private void OnValidate()
    {
        if (mapGenerator == null) mapGenerator = FindFirstObjectByType<MapGenerator>();
        if (waveSpawner == null) waveSpawner = FindFirstObjectByType<EnemyWaveSpawner>();
        if (doorRenderer == null) doorRenderer = GetComponent<SpriteRenderer>();
        if (doorCollider == null) doorCollider = GetComponent<Collider2D>();
    }

    private void CacheReferences()
    {
        if (mapGenerator == null) mapGenerator = FindFirstObjectByType<MapGenerator>();
        if (waveSpawner == null) waveSpawner = FindFirstObjectByType<EnemyWaveSpawner>();
        if (doorRenderer == null) doorRenderer = GetComponent<SpriteRenderer>();
        if (doorCollider == null) doorCollider = GetComponent<Collider2D>();
        if (playerTransform == null)
        {
            var playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null) playerTransform = playerObj.transform;
        }
        // interactTrigger가 별도로 없으면 doorCollider를 트리거로 사용
        if (interactTrigger == null && doorCollider != null && doorCollider.isTrigger)
        {
            interactTrigger = doorCollider;
        }
    }

    private void Update()
    {
        // 활성 적 수 기반 잠금/개방 갱신
        bool shouldOpen = ShouldBeOpen();
        if (shouldOpen != isOpen)
        {
            isOpen = shouldOpen;
            UpdateDoorVisual(false);
        }

        // 플레이어가 트리거 안에 있고 개방 상태에서 상호작용 키 입력 시 전이
        if (isOpen && playerInsideTrigger && WasInteractPressed())
        {
            TryTransitToNextMap();
        }
    }

    private bool ShouldBeOpen()
    {
        if (waveSpawner != null)
        {
            return waveSpawner.AliveCount == 0;
        }
        // 폴백: 씬의 EnemyStateMachine 수로 판정
        var enemies = FindObjectsByType<EnemyStateMachine>(FindObjectsSortMode.None);
        int alive = 0;
        foreach (var e in enemies)
        {
            if (e == null) continue;
            var stat = e.GetComponent<EnemyStatController>();
            if (stat != null && stat.IsDead) continue;
            // Destroy 예약 후에도 오브젝트가 남아 있으므로 IsDead로 필터
            alive++;
        }
        return alive == 0;
    }

    private void UpdateDoorVisual(bool immediate)
    {
        if (doorRenderer != null)
        {
            doorRenderer.color = isOpen ? OpenColor : LockedColor;
        }
        if (doorCollider != null)
        {
            doorCollider.enabled = isOpen;
        }
        if (interactTrigger != null)
        {
            interactTrigger.enabled = isOpen;
        }
        // 로그는 상태 변경 시에만
        if (!immediate)
        {
            Debug.Log($"DoorController: 문 {(isOpen ? "개방" : "잠금")} (활성 적 {(waveSpawner != null ? waveSpawner.AliveCount : -1)})", this);
        }
    }

    private bool WasInteractPressed()
    {
        // Input System과 레거시 모두 대응
        // 1) InputSystem Actions: PlayerInput Interact 액션이 있으면 우선
        // 2) 폴백: E 키 또는 Space
#if ENABLE_INPUT_SYSTEM
        // Keyboard 현재 상태로 E 키 감지 (WebGL 호환)
        var keyboard = UnityEngine.InputSystem.Keyboard.current;
        if (keyboard != null && keyboard.eKey.wasPressedThisFrame) return true;
        if (keyboard != null && keyboard.enterKey.wasPressedThisFrame) return true;
#endif
        if (Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space))
            return true;
        return false;
    }

    private void TryTransitToNextMap()
    {
        if (requireOpenToTransit && !isOpen) return;
        if (mapGenerator == null)
        {
            Debug.LogWarning("DoorController: MapGenerator 참조가 없습니다.", this);
            return;
        }
        if (!mapGenerator.RegenerateOnRoomClear)
        {
            Debug.Log("DoorController: regenerateOnRoomClear가 false라 전이를 수행하지 않습니다.", this);
            return;
        }

        // 맵 재생성 (새 시드)
        bool regenerated = mapGenerator.TryRegenerateNextMap();
        if (!regenerated)
        {
            mapGenerator.RegenerateNextMap();
        }
        Debug.Log($"DoorController: 다음 맵 전이 seed={mapGenerator.CurrentSeed} mapIndex={mapGenerator.MapIndex}", this);

        // 플레이어 중앙 재배치 (페이드 아웃 흐름은 후속 연출에서 확장)
        if (recenterPlayerOnTransit)
        {
            RecenterPlayer();
        }

        // 다음 웨이브 스폰 (SpawnAreaProvider Bake 이후)
        if (waveSpawner != null)
        {
            // 다음 프레임에 스폰하여 Bake 완료 보장 (MapGenerator.Generate는 동기지만 물리 갱신 다음 프레임 반영)
            waveSpawner.Invoke(nameof(EnemyWaveSpawner.SpawnNextWave), 0f);
            // 즉시도 시도
            // waveSpawner.SpawnNextWave는 내부에서 HasBakedData 체크하므로 실패 시 Start의 지연 스폰이 커버
        }

        // 문은 즉시 잠금으로 복귀 (새 웨이브가 스폰되면 Update에서 다시 개방 판단)
        isOpen = false;
        UpdateDoorVisual(true);
        playerInsideTrigger = false;
    }

    private void RecenterPlayer()
    {
        if (playerTransform == null)
        {
            var playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null) playerTransform = playerObj.transform;
        }
        if (playerTransform == null || mapGenerator == null || mapGenerator.WallsTilemap == null) return;

        MapLayout layout = mapGenerator.CurrentLayout;
        if (layout == null) return;
        Vector2Int center = layout.GetCenter();
        Vector3 worldCenter = mapGenerator.WallsTilemap.GetCellCenterWorld(new Vector3Int(center.x, center.y, 0));
        worldCenter.z = playerTransform.position.z;
        playerTransform.position = worldCenter;

        // 속도 초기화 (Rigidbody2D가 있으면)
        var rb = playerTransform.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        playerInsideTrigger = true;
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        playerInsideTrigger = false;
    }

    // 트리거가 없는 문 프리팹을 위한 폴백: 거리 기반 상호작용
    private void OnDrawGizmosSelected()
    {
        if (interactTrigger != null) return;
        Gizmos.color = isOpen ? Color.green : Color.red;
        Gizmos.DrawWireSphere(transform.position, 1.2f);
    }
}
