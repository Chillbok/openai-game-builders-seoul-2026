using UnityEngine;
using UnityEngine.InputSystem;
// Fix: Input System Only - Keyboard direct check, no PlayerInput reference

/// <summary>
/// 생존 완료 + 잔여 적 0 시 문 개방, 문 상호작용 시 다음 맵 전이를 담당한다.
/// 기획: 게임 시스템 세부 기획(게임 핵심 순환) - 생존(30+5*mapIndex) 완료 && 활성 적 0이면 문 개방, EnterTrigger 상호작용으로 전이.
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
    private bool isTransitioning;

    public bool IsOpen => isOpen;

    private void Awake()
    {
        CacheReferences();
        UpdateDoorVisual(true);
    }

    private void Start()
    {
        // 초기 문 위치를 방 내부 빈 셀 중 랜덤으로 배치 (반드시 방 안에 생성)
        // MapGenerator.Awake에서 이미 Generate/Bake가 완료된 뒤이므로 1프레임 지연 후 시도
        if (!TryPlaceDoorRandomly())
        {
            Invoke(nameof(TryPlaceDoorRandomly), 0.1f);
        }
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

    /// <summary>
    /// 문을 방 내부 빈 셀 중 랜덤한 위치에 배치. 반드시 방 안에 생성되며 벽 위가 아니다.
    /// 시드 결정론을 위해 MapGenerator.CurrentSeed 기반 System.Random을 사용한다.
    /// </summary>
    public bool TryPlaceDoorRandomly()
    {
        CacheReferences();
        if (mapGenerator == null || mapGenerator.CurrentLayout == null || mapGenerator.WallsTilemap == null)
            return false;

        var spawnProvider = mapGenerator.SpawnProvider;
        if (spawnProvider == null || !spawnProvider.HasBakedData)
            return false;

        // 빈 셀 목록에서 랜덤 선택. 가운데(플레이어 시작점) 근처 3셀은 제외해 겹침 방지.
        var layout = mapGenerator.CurrentLayout;
        Vector2Int center = layout.GetCenter();
        var emptyCells = spawnProvider.EmptyCells;
        if (emptyCells == null || emptyCells.Count == 0) return false;

        System.Random rng = new System.Random(mapGenerator.CurrentSeed + 9999 + mapGenerator.MapIndex * 7919);
        // 후보 필터링: 중심 반경 3셀 제외
        System.Collections.Generic.List<Vector2Int> candidates = new System.Collections.Generic.List<Vector2Int>(emptyCells.Count);
        float clearRadiusSq = 3f * 3f;
        foreach (var cell in emptyCells)
        {
            float dx = cell.x - center.x;
            float dy = cell.y - center.y;
            if (dx * dx + dy * dy <= clearRadiusSq + 0.001f) continue;
            candidates.Add(cell);
        }
        if (candidates.Count == 0) candidates.AddRange(emptyCells);

        // 최대 10회 시도해 겹침 없는 위치 선택 (SpawnAreaProvider와 동일한 0.4 반경 검사 재사용)
        for (int attempt = 0; attempt < 10; attempt++)
        {
            int idx = rng.Next(candidates.Count);
            Vector2Int cell = candidates[idx];
            Vector3 worldPos = mapGenerator.WallsTilemap.GetCellCenterWorld(new Vector3Int(cell.x, cell.y, 0));
            worldPos.z = transform.position.z;
            // 벽이 아닌 빈 셀 중심이므로 IsWall 검사 불필요하나, 안전하게 재확인
            if (layout.IsWall(cell.x, cell.y)) continue;
            transform.position = worldPos;
            playerInsideTrigger = false;
            return true;
        }

        // 폴백: 그냥 랜덤 하나 배치
        Vector2Int fallback = candidates[rng.Next(candidates.Count)];
        Vector3 fallbackWorld = mapGenerator.WallsTilemap.GetCellCenterWorld(new Vector3Int(fallback.x, fallback.y, 0));
        fallbackWorld.z = transform.position.z;
        transform.position = fallbackWorld;
        playerInsideTrigger = false;
        return true;
    }

    private void Update()
    {
        if (GameOverController.IsGameOverStatic)
        {
            if (isOpen)
            {
                isOpen = false;
                UpdateDoorVisual(false);
            }
            return;
        }

        // 생존 완료 && 활성 적 0 기반 잠금/개방 갱신
        bool shouldOpen = ShouldBeOpen();
        if (shouldOpen != isOpen)
        {
            isOpen = shouldOpen;
            UpdateDoorVisual(false);
        }
    }

    private bool ShouldBeOpen()
    {
        if (GameOverController.IsGameOverStatic) return false;

        if (waveSpawner != null)
        {
            return waveSpawner.AliveCount == 0 && waveSpawner.IsSurvivalComplete;
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

    private bool IsPlayerCollider(Collider2D other)
    {
        if (other == null) return false;
        if (other.CompareTag("Player")) return true;
        // TagManager에 Player 태그가 없을 때를 대비한 레이어 폴백
        int playerLayer = LayerMask.NameToLayer("Player");
        if (playerLayer >= 0 && other.gameObject.layer == playerLayer) return true;
        // Player 프리팹이 Player 레이어가 아닌 경우 Transform 이름 폴백은 사용하지 않음
        return false;
    }

    // 상호작용 키 없이 접촉 즉시 전이하므로 WasInteractPressed는 사용하지 않는다
    // 과거 Input System Only 키 체크는 제거됨

    private void TryTransitToNextMap()
    {
        if (GameOverController.IsGameOverStatic) return;
        if (isTransitioning) return;
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

        isTransitioning = true;
        // 입력 잠금 및 페이드 아웃 → 재생성 → 재배치 → 페이드 인
        if (playerTransform != null)
        {
            var mover = playerTransform.GetComponent<PlayerMoveController>();
            if (mover != null) mover.CanMove = false;
        }

        var fade = FindFirstObjectByType<ScreenFadeController>();
        if (fade == null)
        {
            GameObject fadeObj = new GameObject("ScreenFade");
            fade = fadeObj.AddComponent<ScreenFadeController>();
        }
        StartCoroutine(TransitWithFade(fade));
    }

    private System.Collections.IEnumerator TransitWithFade(ScreenFadeController fade)
    {
        // 페이드 아웃
        yield return fade.FadeOut(null);
        DoTransit();
        // 한 프레임 대기 후 페이드 인 (TilemapCollider 리프레시 완료 보장)
        yield return null;
        yield return fade.FadeIn();
        isTransitioning = false;
        if (playerTransform != null)
        {
            var mover = playerTransform.GetComponent<PlayerMoveController>();
            if (mover != null && mover.CanMove == false)
            {
                // Door가 다시 잠길 때까지는 이동 제한 유지, 이후 Update에서 자동 해제되지 않으므로 여기서 해제
                // 단, 사망 상태가 아니면 이동 허용
                var stat = playerTransform.GetComponent<PlayerStatController>();
                if (stat == null || !stat.IsDead) mover.CanMove = true;
            }
        }
    }

    private void DoTransit()
    {
        // 맵 재생성 (새 시드)
        bool regenerated = mapGenerator.TryRegenerateNextMap();
        if (!regenerated)
        {
            mapGenerator.RegenerateNextMap();
        }
        Debug.Log($"DoorController: 다음 맵 전이 seed={mapGenerator.CurrentSeed} mapIndex={mapGenerator.MapIndex}", this);

        // 플레이어 중앙 재배치
        if (recenterPlayerOnTransit)
        {
            RecenterPlayer();
        }

        // 다음 웨이브 스폰 (SpawnAreaProvider Bake 이후) - 생존 타이머 리셋
        if (waveSpawner != null)
        {
            waveSpawner.Invoke(nameof(EnemyWaveSpawner.SpawnNextWave), 0f);
        }

        // 새 맵에서는 문 위치도 랜덤 재배치 (반드시 방 내부 빈 셀)
        Invoke(nameof(TryPlaceDoorRandomly), 0.05f);
        TryPlaceDoorRandomly();

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

    public void ForceLockedVisual()
    {
        isOpen = false;
        UpdateDoorVisual(true);
        playerInsideTrigger = false;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (GameOverController.IsGameOverStatic) return;
        if (!IsPlayerCollider(other)) return;
        playerInsideTrigger = true;
        // 문이 개방된 상태에서 닿자마자 즉시 전이
        if (isOpen) TryTransitToNextMap();
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!IsPlayerCollider(other)) return;
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
