using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 생존 시간 기반 주기적 적 생성기. SpawnAreaProvider의 빈 셀 캐시에서만 스폰한다.
/// 기획: 게임 시스템 세부 기획(게임 핵심 순환) - 30초+5초/방 생존, 5초마다 6마리 주기 생성, 시간 경과 후 중단.
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

    [Header("생존 시간 (Inspector 조정, Min 0)")]
    [Tooltip("기본 생존 시간(초). 0이면 즉시 생존 완료")]
    [SerializeField, Min(0f)]
    private float survivalBaseDuration = 30f;

    [Tooltip("맵 인덱스당 증가 시간(초)")]
    [SerializeField, Min(0f)]
    private float survivalIncreasePerMap = 5f;

    [Header("주기 생성 (Inspector 조정, Min 0)")]
    [Tooltip("생성 주기(초). 0이면 주기 생성 없음")]
    [SerializeField, Min(0f)]
    private float spawnInterval = 5f;

    [Tooltip("주기당 생성 수. 0이면 생성 없음")]
    [SerializeField, Min(0)]
    private int enemiesPerInterval = 6;

    [Tooltip("게임 시작 시 자동으로 첫 웨이브 스폰")]
    [SerializeField]
    private bool autoSpawnOnStart = true;

    private readonly List<GameObject> spawnedEnemies = new List<GameObject>();

    // 생존 타이머 상태
    private float elapsed;
    private float survivalDuration;
    private float spawnTimer;
    private bool isSpawning;
    private int batchIndex;

    public float SurvivalBaseDuration => survivalBaseDuration;
    public float SurvivalIncreasePerMap => survivalIncreasePerMap;
    public float SpawnInterval => spawnInterval;
    public int EnemiesPerInterval => enemiesPerInterval;
    public bool AutoSpawnOnStart => autoSpawnOnStart;
    public IReadOnlyList<GameObject> SpawnedEnemies => spawnedEnemies;
    public float Elapsed => elapsed;
    public float SurvivalDuration => survivalDuration;
    public bool IsSpawning => isSpawning;
    public bool IsSurvivalComplete => elapsed >= survivalDuration;
    public float SurvivalProgress => survivalDuration > 0f ? Mathf.Clamp01(elapsed / survivalDuration) : 1f;

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
            if (!TryBeginSurvivalWave())
            {
                Invoke(nameof(DelayedSpawn), 0.1f);
            }
        }
    }

    private void Update()
    {
        if (GameOverController.IsGameOverStatic)
        {
            isSpawning = false;
            return;
        }

        if (!isSpawning) return;

        elapsed += Time.deltaTime;
        spawnTimer += Time.deltaTime;

        // 생존 시간 경과 시 생성 중단 (잔여 적 유지)
        if (elapsed >= survivalDuration)
        {
            isSpawning = false;
            Debug.Log($"EnemyWaveSpawner: 생존 완료 elapsed={elapsed:F1} survival={survivalDuration:F1} mapIndex={mapGenerator?.MapIndex}", this);
            return;
        }

        // 주기 생성
        if (spawnInterval > 0.001f && enemiesPerInterval > 0 && spawnTimer >= spawnInterval)
        {
            spawnTimer -= spawnInterval;
            SpawnBatch(enemiesPerInterval);
        }
    }

    private void OnValidate()
    {
        survivalBaseDuration = Mathf.Max(0f, survivalBaseDuration);
        survivalIncreasePerMap = Mathf.Max(0f, survivalIncreasePerMap);
        spawnInterval = Mathf.Max(0f, spawnInterval);
        enemiesPerInterval = Mathf.Max(0, enemiesPerInterval);
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
        TryBeginSurvivalWave();
    }

    private float CalculateSurvivalDuration()
    {
        int mapIdx = mapGenerator != null ? mapGenerator.MapIndex : 0;
        return Mathf.Max(0f, survivalBaseDuration + mapIdx * survivalIncreasePerMap);
    }

    /// <summary>
    /// 생존 타이머를 시작하고 첫 배치(6마리)를 즉시 생성한다. 성공 시 true.
    /// </summary>
    public bool TryBeginSurvivalWave()
    {
        if (GameOverController.IsGameOverStatic) return false;
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

        elapsed = 0f;
        spawnTimer = 0f;
        batchIndex = 0;
        survivalDuration = CalculateSurvivalDuration();
        isSpawning = true;

        // 게임플레이 BGM — 방 진입 시 루프 재생 (menuBgm은 스타트 메뉴 구현 시까지 대기)
        if (AudioService.Instance != null && AudioService.Instance.Config != null && AudioService.Instance.Config.BattleBgm != null)
        {
            AudioService.Instance.PlayBGM(AudioService.Instance.Config.BattleBgm, true, 0.3f);
        }

        spawnedEnemies.RemoveAll(go => go == null);

        // 즉시 첫 배치 생성 (생존 시간이 0이면 생성 없이 즉시 완료로 처리)
        if (survivalDuration > 0f && enemiesPerInterval > 0)
        {
            SpawnBatch(enemiesPerInterval);
        }
        else if (survivalDuration <= 0f)
        {
            isSpawning = false;
            Debug.Log($"EnemyWaveSpawner: 생존 시간 0으로 즉시 완료 mapIndex={mapGenerator?.MapIndex}", this);
        }

        Debug.Log($"EnemyWaveSpawner: 생존 웨이브 시작 survival={survivalDuration:F1} interval={spawnInterval:F1} perBatch={enemiesPerInterval} seed={mapGenerator?.CurrentSeed} mapIndex={mapGenerator?.MapIndex}", this);
        return true;
    }

    private void SpawnBatch(int count)
    {
        if (GameOverController.IsGameOverStatic) return;
        if (count <= 0) return;
        CacheReferences();
        if (enemyPrefab == null || spawnAreaProvider == null || !spawnAreaProvider.HasBakedData) return;

        int seed = mapGenerator != null ? mapGenerator.CurrentSeed : System.Environment.TickCount;
        // 배치별 결정론: 시드 + 맵 인덱스 + 배치 인덱스
        System.Random rng = new System.Random(seed + 7919 + batchIndex * 9973 + (mapGenerator != null ? mapGenerator.MapIndex * 7919 : 0));
        batchIndex++;

        spawnedEnemies.RemoveAll(go => go == null);

        for (int i = 0; i < count; i++)
        {
            Vector3 pos = spawnAreaProvider.GetRandomSpawnPosition(rng);
            pos.z = 0f;
            GameObject instance = Instantiate(enemyPrefab, pos, Quaternion.identity);
            spawnedEnemies.Add(instance);
        }

        Debug.Log($"EnemyWaveSpawner: 배치 생성 count={count} batch={batchIndex} seed={seed} mapIndex={mapGenerator?.MapIndex}", this);
    }

    // --- 호환 래퍼 (기존 DoorController 등에서 호출) ---

    /// <summary>
    /// 호환용: 기존 일괄 생성 호출을 생존 웨이브 시작로 위임한다.
    /// </summary>
    public bool TrySpawnWave()
    {
        return TryBeginSurvivalWave();
    }

    /// <summary>
    /// 다음 방 진입 시 호출. 생존 타이머를 리셋하고 새 웨이브를 시작한다.
    /// MapGenerator가 재생성된 뒤 호출해야 한다.
    /// </summary>
    public void SpawnNextWave()
    {
        spawnedEnemies.RemoveAll(go => go == null);
        TryBeginSurvivalWave();
    }

    public void BeginSurvivalWave()
    {
        TryBeginSurvivalWave();
    }

    /// <summary>
    /// 외부에서 강제 클리어 (디버그용)
    /// </summary>
    public void ClearTracking()
    {
        spawnedEnemies.Clear();
    }

    public void StopSpawning()
    {
        isSpawning = false;
    }
}
