using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 방 단위 적 일괄 생성기. SpawnAreaProvider의 빈 셀 캐시에서만 스폰한다.
/// 기획: 게임 시스템 세부 기획(게임 핵심 순환) - 방 진입 시 1회 일괄 생성, 추가 생성 없음.
/// </summary>
[DisallowMultipleComponent]
public sealed class EnemyWaveSpawner : MonoBehaviour
{
    [Header("참조")]
    [Tooltip("스폰 영역을 제공하는 프로바이더. 비어 있으면 Map 오브젝트에서 탐색")]
    [SerializeField]
    private SpawnAreaProvider spawnAreaProvider;

    [Tooltip("맵 생성기. 시드와 맵 인덱스를 참조한다")]
    [SerializeField]
    private MapGenerator mapGenerator;

    [Tooltip("스폰할 적 프리팹")]
    [SerializeField]
    private GameObject enemyPrefab;

    [Header("웨이브 규칙 (Inspector 조정)")]
    [Tooltip("기본 스폰 수 (1회차)")]
    [SerializeField, Min(1)]
    private int baseEnemyCount = 4;

    [Tooltip("맵 인덱스당 증가량")]
    [SerializeField, Min(0)]
    private int increasePerMap = 1;

    [Tooltip("최대 스폰 수")]
    [SerializeField, Min(1)]
    private int maxEnemyCount = 12;

    [Header("면적 비례")]
    [Tooltip("기준 내부 면적 (w-2)*(h-2). 30x20 기준 504")]
    [SerializeField, Min(100)]
    private int referenceArea = 504;

    [Tooltip("100셀당 추가 적 수")]
    [SerializeField, Min(0)]
    private int enemiesPer100Cells = 5;

    [Tooltip("작은 방 하한. 면적이 작아도 최소 이 수만큼 스폰")]
    [SerializeField, Min(1)]
    private int minSpawnCount = 4;

    [Tooltip("게임 시작 시 자동으로 첫 웨이브 스폰")]
    [SerializeField]
    private bool autoSpawnOnStart = true;

    private readonly List<GameObject> spawnedEnemies = new List<GameObject>();

    public int BaseEnemyCount => baseEnemyCount;
    public int IncreasePerMap => increasePerMap;
    public int MaxEnemyCount => maxEnemyCount;
    public int ReferenceArea => referenceArea;
    public int EnemiesPer100Cells => enemiesPer100Cells;
    public int MinSpawnCount => minSpawnCount;
    public IReadOnlyList<GameObject> SpawnedEnemies => spawnedEnemies;

    /// <summary>
    /// 현재 방의 살아있는 적 수. Destroy 직후 null 참조는 제외한다.
    /// </summary>
    public int AliveCount
    {
        get
        {
            int cnt = 0;
            for (int i = spawnedEnemies.Count - 1; i >= 0; i--)
            {
                var go = spawnedEnemies[i];
                if (go == null)
                {
                    spawnedEnemies.RemoveAt(i);
                    continue;
                }
                var stat = go.GetComponent<EnemyStatController>();
                if (stat != null && stat.IsDead) continue;
                // 파괴 예약된 오브젝트도 카운트에서 제외 (EnemyStateMachine은 Destroy(gameObject, 1f)로 지연)
                // IsDead가 true면 제외하므로 충분
                cnt++;
            }
            return cnt;
        }
    }

    private void Awake()
    {
        CacheReferences();
    }

    private void Start()
    {
        if (autoSpawnOnStart)
        {
            // MapGenerator Awake에서 이미 Generate가 호출된 뒤이므로 Bake가 완료된 상태
            // 한 프레임 지연 후 스폰하여 Bake 완료 보장
            // 동기적으로 바로 시도하고, 실패 시 다음 프레임에 재시도
            if (!TrySpawnWave())
            {
                Invoke(nameof(DelayedSpawn), 0.1f);
            }
        }
    }

    private void OnValidate()
    {
        baseEnemyCount = Mathf.Max(1, baseEnemyCount);
        increasePerMap = Mathf.Max(0, increasePerMap);
        maxEnemyCount = Mathf.Max(baseEnemyCount, maxEnemyCount);
        referenceArea = Mathf.Max(100, referenceArea);
        enemiesPer100Cells = Mathf.Max(0, enemiesPer100Cells);
        minSpawnCount = Mathf.Clamp(minSpawnCount, 1, maxEnemyCount);
        if (spawnAreaProvider == null) spawnAreaProvider = GetComponent<SpawnAreaProvider>();
        if (spawnAreaProvider == null) spawnAreaProvider = GetComponentInChildren<SpawnAreaProvider>(true);
        if (mapGenerator == null) mapGenerator = GetComponent<MapGenerator>();
        if (mapGenerator == null) mapGenerator = GetComponentInChildren<MapGenerator>(true);
        if (mapGenerator == null) mapGenerator = FindFirstObjectByType<MapGenerator>();
    }

    private void CacheReferences()
    {
        if (spawnAreaProvider == null)
        {
            spawnAreaProvider = GetComponent<SpawnAreaProvider>();
            if (spawnAreaProvider == null) spawnAreaProvider = GetComponentInChildren<SpawnAreaProvider>(true);
            if (spawnAreaProvider == null && mapGenerator != null) spawnAreaProvider = mapGenerator.SpawnProvider;
        }
        if (mapGenerator == null)
        {
            mapGenerator = GetComponent<MapGenerator>();
            if (mapGenerator == null) mapGenerator = GetComponentInChildren<MapGenerator>(true);
            if (mapGenerator == null) mapGenerator = FindFirstObjectByType<MapGenerator>();
        }
        if (spawnAreaProvider == null && mapGenerator != null)
        {
            spawnAreaProvider = mapGenerator.SpawnProvider;
        }
    }

    private void DelayedSpawn()
    {
        TrySpawnWave();
    }

    /// <summary>
    /// 현재 맵 인덱스와 면적에 따른 스폰 수를 계산한다.
    /// 100셀당 5명 증가, 작은 방 하한 minSpawnCount 보호.
    /// </summary>
    public int CalculateSpawnCount()
    {
        int mapIdx = mapGenerator != null ? mapGenerator.MapIndex : 0;
        int baseCount = baseEnemyCount + mapIdx * increasePerMap;

        int area = 504;
        if (mapGenerator != null && mapGenerator.CurrentLayout != null)
        {
            var layout = mapGenerator.CurrentLayout;
            area = (layout.Width - 2) * (layout.Height - 2);
        }
        else if (mapGenerator != null && mapGenerator.ArenaProfile != null)
        {
            var p = mapGenerator.ArenaProfile;
            area = (p.MapWidth - 2) * (p.MapHeight - 2);
        }

        int areaExtra = 0;
        if (enemiesPer100Cells > 0)
        {
            float delta = area - referenceArea;
            areaExtra = Mathf.FloorToInt(delta / 100f * enemiesPer100Cells);
            areaExtra = Mathf.Max(0, areaExtra);
        }

        int count = baseCount + areaExtra;
        count = Mathf.Max(count, minSpawnCount);
        return Mathf.Clamp(count, minSpawnCount, maxEnemyCount);
    }

    /// <summary>
    /// 방 진입 시 1회 일괄 생성. 성공 시 true.
    /// </summary>
    public bool TrySpawnWave()
    {
        CacheReferences();
        if (enemyPrefab == null)
        {
            Debug.LogWarning("EnemyWaveSpawner: enemyPrefab이 할당되지 않았습니다.", this);
            return false;
        }
        if (spawnAreaProvider == null || !spawnAreaProvider.HasBakedData)
        {
            Debug.LogWarning("EnemyWaveSpawner: SpawnAreaProvider Bake 데이터가 없습니다. MapGenerator 생성 이후에 호출하세요.", this);
            return false;
        }

        int count = CalculateSpawnCount();
        int seed = mapGenerator != null ? mapGenerator.CurrentSeed : System.Environment.TickCount;
        System.Random rng = new System.Random(seed + 7919);

        // 기존 추적 리스트 정리 (이전 방 잔류 제거)
        spawnedEnemies.RemoveAll(go => go == null);

        for (int i = 0; i < count; i++)
        {
            Vector3 pos = spawnAreaProvider.GetRandomSpawnPosition(rng);
            // Z는 0으로 고정
            pos.z = 0f;
            GameObject instance = Instantiate(enemyPrefab, pos, Quaternion.identity);
            spawnedEnemies.Add(instance);
        }

        Debug.Log($"EnemyWaveSpawner: 웨이브 스폰 완료 count={count} seed={seed} mapIndex={mapGenerator?.MapIndex}", this);
        return true;
    }

    /// <summary>
    /// 다음 방 진입 시 호출. 기존 추적만 정리하고 새 웨이브 스폰.
    /// MapGenerator가 재생성된 뒤 호출해야 한다.
    /// </summary>
    public void SpawnNextWave()
    {
        // 이전 방 적은 이미 DoorController가 클리어를 확인한 뒤이므로 리스트만 정리
        spawnedEnemies.RemoveAll(go => go == null);
        TrySpawnWave();
    }

    /// <summary>
    /// 외부에서 강제 클리어 (디버그용)
    /// </summary>
    public void ClearTracking()
    {
        spawnedEnemies.Clear();
    }
}
