using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// 시드 기반 절차적 아레나 맵 생성기. Tilemap에 외벽과 내부 장애물을 일괄 배치한다.
/// 지형지물 수치는 MapArenaProfile에서 조절한다.
/// </summary>
[DisallowMultipleComponent]
public sealed class MapGenerator : MonoBehaviour
{
    [Header("프로파일")]
    [Tooltip("맵 크기, 장애물 수, 거리 규칙을 담은 프로파일")]
    [SerializeField]
    private MapArenaProfile arenaProfile;

    [Header("타일")]
    [Tooltip("타일을 배치할 Walls Tilemap. 비어 있으면 자식에서 탐색한다")]
    [SerializeField]
    private Tilemap wallsTilemap;

    [Tooltip("외벽/장애물에 사용할 타일. 프로파일 wallTile보다 우선한다")]
    [SerializeField]
    private TileBase wallTile;

    [Header("시드")]
    [Tooltip("true면 fixedSeed를 사용해 같은 맵을 재현한다")]
    [SerializeField]
    private bool useFixedSeed;

    [Tooltip("고정 시드 값")]
    [SerializeField]
    private int fixedSeed;

    [SerializeField, Tooltip("이번 생성에 사용된 시드 (읽기 전용)")]
    private int currentSeed;

    [Header("맵 전이")]
    [Tooltip("보스 처치 시 다음 맵으로 재생성하는지")]
    [SerializeField]
    private bool regenerateOnBossKill = true;

    private MapLayout currentLayout;
    private SpawnAreaProvider spawnProvider;
    private int mapIndex;
    private Tilemap groundTilemap;

    public MapArenaProfile ArenaProfile => arenaProfile;
    public Tilemap WallsTilemap => wallsTilemap;
    public TileBase WallTile => wallTile;
    public bool UseFixedSeed => useFixedSeed;
    public int FixedSeed => fixedSeed;
    public int CurrentSeed => currentSeed;
    public bool RegenerateOnBossKill => regenerateOnBossKill;
    public MapLayout CurrentLayout => currentLayout;
    public SpawnAreaProvider SpawnProvider => spawnProvider;
    public int MapIndex => mapIndex;

    private void Awake()
    {
        CacheReferences();
        if (currentSeed == 0 && !useFixedSeed)
        {
            GenerateWithRandomSeed();
        }
        else
        {
            int seed = useFixedSeed ? fixedSeed : currentSeed;
            if (seed == 0) seed = System.Environment.TickCount;
            Generate(seed);
        }
    }

    private void OnValidate()
    {
        if (arenaProfile == null) return;
        // 프로파일 값은 OnValidate에서 클램프됨
        if (wallsTilemap == null)
        {
            wallsTilemap = GetComponentInChildren<Tilemap>();
        }
    }

    private void CacheReferences()
    {
        if (wallsTilemap == null)
        {
            // Walls 이름의 Tilemap 우선 탐색
            Tilemap[] tilemaps = GetComponentsInChildren<Tilemap>(true);
            foreach (var tm in tilemaps)
            {
                if (tm.gameObject.name == "Walls")
                {
                    wallsTilemap = tm;
                    break;
                }
            }
            if (wallsTilemap == null && tilemaps.Length > 0) wallsTilemap = tilemaps[0];
        }

        spawnProvider = GetComponent<SpawnAreaProvider>();
        if (spawnProvider == null) spawnProvider = GetComponentInChildren<SpawnAreaProvider>(true);

        // Ground 타일맵은 참고용으로 캐시 (선택)
        Tilemap[] all = GetComponentsInChildren<Tilemap>(true);
        foreach (var tm in all)
        {
            if (tm.gameObject.name == "Ground") groundTilemap = tm;
        }
    }

    public void GenerateWithRandomSeed()
    {
        int seed = useFixedSeed ? fixedSeed + mapIndex : System.Environment.TickCount + mapIndex * 7919;
        // TickCount가 0일 수 있으므로 보정
        if (seed == 0) seed = 12345 + mapIndex;
        Generate(seed);
    }

    public void Generate(int seed)
    {
        currentSeed = seed;
        MapArenaProfile profile = arenaProfile;

        int width = profile != null ? profile.MapWidth : 30;
        int height = profile != null ? profile.MapHeight : 20;

        TileBase tile = ResolveWallTile();
        if (wallsTilemap == null)
        {
            Debug.LogWarning("MapGenerator: Walls Tilemap이 할당되지 않았습니다.", this);
            return;
        }
        if (tile == null)
        {
            Debug.LogWarning("MapGenerator: wallTile이 할당되지 않았습니다. 기본 타일을 지정하세요.", this);
            return;
        }

        System.Random rng = new System.Random(seed);
        MapLayout layout = new MapLayout(width, height);
        layout.FillOuterWalls();

        // 장애물 수량 결정 (variance 적용)
        int baseCount = profile != null ? profile.ObstacleCount : 6;
        int variance = profile != null ? profile.ObstacleCountVariance : 1;
        int obstacleCount = baseCount;
        if (variance > 0)
        {
            int delta = rng.Next(-variance, variance + 1);
            obstacleCount = Mathf.Clamp(baseCount + delta, 0, 32);
        }

        float minDist = profile != null ? profile.MinObstacleDistance : 2f;
        float clearRadius = profile != null ? profile.PlayerClearRadius : 3f;
        int wallMargin = profile != null ? profile.WallMargin : 1;
        int maxRetries = profile != null ? profile.MaxRetries : 30;

        ObstaclePattern[] patterns = profile != null ? profile.ObstaclePatterns : null;
        List<Vector2Int> placedOrigins = new List<Vector2Int>();
        List<Vector2Int[]> placedCellsList = new List<Vector2Int[]>();
        Vector2Int center = layout.GetCenter();

        for (int i = 0; i < obstacleCount; i++)
        {
            bool placed = false;
            for (int attempt = 0; attempt < maxRetries; attempt++)
            {
                ObstaclePattern pattern = PickPattern(patterns, rng);
                int rot = pattern != null && pattern.AllowRotation ? rng.Next(0, 4) : 0;
                bool mirrored = pattern != null && pattern.AllowMirror && rng.Next(0, 2) == 1;

                Vector2Int[] cells;
                Vector2Int size;
                if (pattern != null)
                {
                    cells = pattern.GetTransformedCells(rot, mirrored);
                    size = pattern.GetTransformedSize(rot, mirrored);
                }
                else
                {
                    // 기본 폴백: 3x2 직사각형
                    size = new Vector2Int(3, 2);
                    cells = CreateRectCells(size);
                }

                // 랜덤 위치 샘플링 (wallMargin 이격)
                int minX = 1 + wallMargin;
                int maxX = width - 1 - wallMargin - size.x;
                int minY = 1 + wallMargin;
                int maxY = height - 1 - wallMargin - size.y;
                if (maxX < minX || maxY < minY) break;

                int ox = rng.Next(minX, maxX + 1);
                int oy = rng.Next(minY, maxY + 1);
                Vector2Int origin = new Vector2Int(ox, oy);

                // 시작점 금지구역 검사
                bool inClear = false;
                foreach (var c in cells)
                {
                    Vector2Int wp = origin + c;
                    float dx = wp.x - center.x;
                    float dy = wp.y - center.y;
                    if (dx * dx + dy * dy <= clearRadius * clearRadius + 0.001f)
                    {
                        inClear = true;
                        break;
                    }
                }
                if (inClear) continue;

                // 기존 장애물과 최소거리 검사 (원점 간 거리로 근사)
                bool tooClose = false;
                foreach (var prev in placedOrigins)
                {
                    float dx = origin.x - prev.x;
                    float dy = origin.y - prev.y;
                    if (Mathf.Sqrt(dx * dx + dy * dy) < minDist)
                    {
                        tooClose = true;
                        break;
                    }
                    // 셀 단위 겹침도 추가 검사
                    // 기존 패턴의 bounding box와 겹치면 tooClose로 간주 (셀 0거리)
                    // 이미 IsWall로도 걸리지만 minDist가 0이어도 겹침은 방지
                }
                if (tooClose) continue;

                // 셀 겹침 검사
                bool overlap = false;
                foreach (var c in cells)
                {
                    int wx = origin.x + c.x;
                    int wy = origin.y + c.y;
                    if (layout.IsWall(wx, wy))
                    {
                        overlap = true;
                        break;
                    }
                }
                if (overlap) continue;

                // 임시 배치 후 연결성 검사
                layout.TryPlacePattern(origin, cells);
                bool connected = layout.IsFullyConnected(center);
                if (!connected)
                {
                    layout.RemovePattern(origin, cells);
                    continue;
                }

                placedOrigins.Add(origin);
                placedCellsList.Add(cells);
                placed = true;
                break;
            }

            if (!placed)
            {
                // 이번 장애물은 배치 실패, 다음으로 넘어감
                // 재시도 초과 시 장애물 수 1개 감소 효과와 동일
                continue;
            }
        }

        currentLayout = layout;
        ApplyToTilemap(layout, tile);

        if (spawnProvider != null)
        {
            spawnProvider.Bake(layout, wallsTilemap);
        }
    }

    /// <summary>
    /// 보스 처치 시 다음 맵으로 전이. 새 시드로 재생성한다.
    /// </summary>
    public void RegenerateNextMap()
    {
        if (!regenerateOnBossKill) return;
        mapIndex++;
        GenerateWithRandomSeed();
    }

    public void RegenerateNextMap(int seed)
    {
        mapIndex++;
        Generate(seed);
    }

    private TileBase ResolveWallTile()
    {
        if (wallTile != null) return wallTile;
        if (arenaProfile != null && arenaProfile.WallTile != null) return arenaProfile.WallTile;
        return null;
    }

    private void ApplyToTilemap(MapLayout layout, TileBase tile)
    {
        if (wallsTilemap == null || layout == null || tile == null) return;

        wallsTilemap.ClearAllTiles();

        List<Vector3Int> positions = new List<Vector3Int>(layout.Width * layout.Height);
        List<TileBase> tiles = new List<TileBase>(layout.Width * layout.Height);

        for (int y = 0; y < layout.Height; y++)
        {
            for (int x = 0; x < layout.Width; x++)
            {
                if (layout.IsWall(x, y))
                {
                    positions.Add(new Vector3Int(x, y, 0));
                    tiles.Add(tile);
                }
            }
        }

        if (positions.Count > 0)
        {
            wallsTilemap.SetTiles(positions.ToArray(), tiles.ToArray());
        }
        wallsTilemap.CompressBounds();

        // 물리 갱신: TilemapCollider2D는 타일 변경 시 자동 반영되지만, 강제로 리프레시
        var col = wallsTilemap.GetComponent<TilemapCollider2D>();
        if (col != null) col.enabled = false;
        if (col != null) col.enabled = true;
    }

    private static ObstaclePattern PickPattern(ObstaclePattern[] patterns, System.Random rng)
    {
        if (patterns == null || patterns.Length == 0) return null;
        // 가중치 추첨
        float total = 0f;
        foreach (var p in patterns)
        {
            if (p == null) continue;
            total += Mathf.Max(0.01f, p.Weight);
        }
        if (total <= 0f) return patterns[0];
        float pick = (float)rng.NextDouble() * total;
        float acc = 0f;
        foreach (var p in patterns)
        {
            if (p == null) continue;
            acc += Mathf.Max(0.01f, p.Weight);
            if (pick <= acc) return p;
        }
        return patterns[patterns.Length - 1];
    }

    private static Vector2Int[] CreateRectCells(Vector2Int size)
    {
        List<Vector2Int> list = new List<Vector2Int>(size.x * size.y);
        for (int y = 0; y < size.y; y++)
            for (int x = 0; x < size.x; x++)
                list.Add(new Vector2Int(x, y));
        return list.ToArray();
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (currentLayout == null) return;
        // 시작점 표시
        Vector2Int center = currentLayout.GetCenter();
        if (wallsTilemap != null)
        {
            Vector3 wCenter = wallsTilemap.GetCellCenterWorld(new Vector3Int(center.x, center.y, 0));
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(wCenter, 0.5f);
        }
    }
#endif
}
