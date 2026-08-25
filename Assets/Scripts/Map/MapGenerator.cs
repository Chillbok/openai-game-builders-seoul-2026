using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
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

    [Tooltip("바닥 타일을 배치할 Ground Tilemap. 비어 있으면 자식에서 탐색한다")]
    [SerializeField]
    private Tilemap groundTilemap;

    [Tooltip("외벽/장애물에 사용할 타일. 프로파일 wallTile보다 우선한다. Wall RuleTile 사용")]
    [SerializeField]
    private TileBase wallTile;

    [Tooltip("바닥에 사용할 타일. Ground RuleTile 사용. 비어 있으면 Walls와 구분 없이 동작")]
    [SerializeField]
    private TileBase groundTile;

    [Header("외벽 전용 타일 (방향별, 비어 있으면 wallTile RuleTile 사용)")]
    [Tooltip("외벽 북쪽(상단) 타일")]
    [SerializeField]
    private TileBase outerWallNorthTile;

    [Tooltip("외벽 남쪽(하단) 타일")]
    [SerializeField]
    private TileBase outerWallSouthTile;

    [Tooltip("외벽 서쪽(좌측) 타일")]
    [SerializeField]
    private TileBase outerWallWestTile;

    [Tooltip("외벽 동쪽(우측) 타일")]
    [SerializeField]
    private TileBase outerWallEastTile;

    [Tooltip("외벽 북서 코너")]
    [SerializeField]
    private TileBase outerWallCornerNWTile;

    [Tooltip("외벽 북동 코너")]
    [SerializeField]
    private TileBase outerWallCornerNETile;

    [Tooltip("외벽 남서 코너")]
    [SerializeField]
    private TileBase outerWallCornerSWTile;

    [Tooltip("외벽 남동 코너")]
    [SerializeField]
    private TileBase outerWallCornerSETile;

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
    [Tooltip("방 클리어 후 문 상호작용 시 다음 맵으로 재생성하는지 (기획: 게임 콘텐츠 세부 기획(맵))")]
    [FormerlySerializedAs("regenerateOnBossKill")]
    [SerializeField]
    private bool regenerateOnRoomClear = true;

    private MapLayout currentLayout;
    private SpawnAreaProvider spawnProvider;
    private int mapIndex;

    public MapArenaProfile ArenaProfile => arenaProfile;
    public Tilemap WallsTilemap => wallsTilemap;
    public Tilemap GroundTilemap => groundTilemap;
    public TileBase WallTile => wallTile;
    public TileBase GroundTile => groundTile;
    public TileBase OuterWallNorthTile => outerWallNorthTile;
    public TileBase OuterWallSouthTile => outerWallSouthTile;
    public TileBase OuterWallWestTile => outerWallWestTile;
    public TileBase OuterWallEastTile => outerWallEastTile;
    public bool UseFixedSeed => useFixedSeed;
    public int FixedSeed => fixedSeed;
    public int CurrentSeed => currentSeed;
    public bool RegenerateOnRoomClear => regenerateOnRoomClear;
    public bool RegenerateOnBossKill => regenerateOnRoomClear;
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
        if (wallsTilemap == null)
        {
            wallsTilemap = GetComponentInChildren<Tilemap>();
        }
        if (groundTilemap == null)
        {
            Tilemap[] all = GetComponentsInChildren<Tilemap>(true);
            foreach (var tm in all)
            {
                if (tm.gameObject.name == "Ground") groundTilemap = tm;
            }
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

        if (groundTilemap == null)
        {
            Tilemap[] tilemaps = GetComponentsInChildren<Tilemap>(true);
            foreach (var tm in tilemaps)
            {
                if (tm.gameObject.name == "Ground")
                {
                    groundTilemap = tm;
                    break;
                }
            }
        }

        spawnProvider = GetComponent<SpawnAreaProvider>();
        if (spawnProvider == null) spawnProvider = GetComponentInChildren<SpawnAreaProvider>(true);
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

        System.Random rng = new System.Random(seed);
        Vector2Int size = profile != null ? profile.GetRandomMapSize(rng) : new Vector2Int(30, 20);
        int width = size.x;
        int height = size.y;

        TileBase wallTileResolved = ResolveWallTile();
        TileBase groundTileResolved = ResolveGroundTile();
        if (wallsTilemap == null)
        {
            Debug.LogWarning("MapGenerator: Walls Tilemap이 할당되지 않았습니다.", this);
            return;
        }
        if (wallTileResolved == null)
        {
            Debug.LogWarning("MapGenerator: wallTile이 할당되지 않았습니다. Wall RuleTile을 지정하세요.", this);
            return;
        }

        MapLayout layout = new MapLayout(width, height);
        layout.FillOuterWalls();

        // 단일 직사각형 방: 내부 장애물 생성 제거 (요청: 내부 방 아예 없애기)
        // 외벽 두께 1셀만 유지, 내부는 전체 빈 공간.

        currentLayout = layout;
        ApplyToTilemap(layout, wallTileResolved);
        ApplyGroundTilemap(layout, groundTileResolved);

        if (spawnProvider != null)
        {
            spawnProvider.Bake(layout, wallsTilemap);
        }
    }

    /// <summary>
    /// 방 클리어 후 문 상호작용 시 다음 맵으로 전이. 새 시드로 재생성한다.
    /// 기획: 게임 콘텐츠 세부 기획(맵) - 방 진입 시 1회 일괄 생성, 문 개방 후 전이.
    /// </summary>
    public void RegenerateNextMap()
    {
        if (!regenerateOnRoomClear) return;
        mapIndex++;
        GenerateWithRandomSeed();
    }

    public void RegenerateNextMap(int seed)
    {
        mapIndex++;
        Generate(seed);
    }

    /// <summary>
    /// 외부에서 호출하는 범용 맵 전이 진입점. DoorController/Escape 흐름에서 사용한다.
    /// </summary>
    public bool TryRegenerateNextMap()
    {
        if (!regenerateOnRoomClear) return false;
        RegenerateNextMap();
        return true;
    }

    private TileBase ResolveWallTile()
    {
        if (wallTile != null) return wallTile;
        if (arenaProfile != null && arenaProfile.WallTile != null) return arenaProfile.WallTile;
        return null;
    }

    private TileBase ResolveGroundTile()
    {
        if (groundTile != null) return groundTile;
        return null;
    }

    private void ApplyToTilemap(MapLayout layout, TileBase tile)
    {
        if (wallsTilemap == null || layout == null || tile == null) return;

        wallsTilemap.ClearAllTiles();

        List<Vector3Int> positions = new List<Vector3Int>(layout.Width * layout.Height);
        List<TileBase> tiles = new List<TileBase>(layout.Width * layout.Height);

        int w = layout.Width;
        int h = layout.Height;
        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                if (!layout.IsWall(x, y)) continue;

                TileBase chosen = tile;

                bool isOuter = x == 0 || x == w - 1 || y == 0 || y == h - 1;
                if (isOuter)
                {
                    // 모서리 우선
                    if (x == 0 && y == h - 1 && outerWallCornerNWTile != null) chosen = outerWallCornerNWTile;
                    else if (x == w - 1 && y == h - 1 && outerWallCornerNETile != null) chosen = outerWallCornerNETile;
                    else if (x == 0 && y == 0 && outerWallCornerSWTile != null) chosen = outerWallCornerSWTile;
                    else if (x == w - 1 && y == 0 && outerWallCornerSETile != null) chosen = outerWallCornerSETile;
                    else if (y == h - 1 && outerWallNorthTile != null) chosen = outerWallNorthTile;
                    else if (y == 0 && outerWallSouthTile != null) chosen = outerWallSouthTile;
                    else if (x == 0 && outerWallWestTile != null) chosen = outerWallWestTile;
                    else if (x == w - 1 && outerWallEastTile != null) chosen = outerWallEastTile;
                }

                positions.Add(new Vector3Int(x, y, 0));
                tiles.Add(chosen);
            }
        }

        if (positions.Count > 0)
        {
            wallsTilemap.SetTiles(positions.ToArray(), tiles.ToArray());
        }
        wallsTilemap.CompressBounds();

        // 물리 갱신: TilemapCollider2D는 타일 변경 시 자동 반영되지만, 강제로 리프레시
        // 기획: CompositeCollider2D Synchronous 재생성 (GeometryType Polygons)
        var col = wallsTilemap.GetComponent<TilemapCollider2D>();
        var composite = wallsTilemap.GetComponent<CompositeCollider2D>();
        if (col != null) col.enabled = false;
        if (composite != null) composite.enabled = false;
        if (col != null) col.enabled = true;
        if (composite != null) composite.enabled = true;
    }

    private void ApplyGroundTilemap(MapLayout layout, TileBase tile)
    {
        if (groundTilemap == null || layout == null || tile == null) return;

        groundTilemap.ClearAllTiles();

        List<Vector3Int> positions = new List<Vector3Int>(layout.Width * layout.Height);
        List<TileBase> tiles = new List<TileBase>(layout.Width * layout.Height);

        for (int y = 0; y < layout.Height; y++)
        {
            for (int x = 0; x < layout.Width; x++)
            {
                positions.Add(new Vector3Int(x, y, 0));
                tiles.Add(tile);
            }
        }

        if (positions.Count > 0)
        {
            groundTilemap.SetTiles(positions.ToArray(), tiles.ToArray());
        }
        groundTilemap.CompressBounds();
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
