using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// 절차적 아레나 맵의 크기, 장애물 수, 거리 규칙을 담은 프로파일.
/// Inspector에서 수치를 조절해 맵 밀도를 바꾼다.
/// </summary>
[CreateAssetMenu(fileName = "MapArenaProfile", menuName = "Scriptable Objects/Map/MapArenaProfile")]
public sealed class MapArenaProfile : ScriptableObject
{
    [Header("맵 크기")]
    [Tooltip("외벽 포함 가로 셀 수 (단일 크기 모드에서 사용)")]
    [SerializeField, Min(10)]
    private int mapWidth = 30;

    [Tooltip("외벽 포함 세로 셀 수 (단일 크기 모드에서 사용)")]
    [SerializeField, Min(10)]
    private int mapHeight = 20;

    [Header("가변 크기 (단일 직사각형)")]
    [Tooltip("가변 크기 사용 여부. true면 min/max 범위에서 시드 난수로 크기를 결정한다")]
    [SerializeField]
    private bool useVariableSize = true;

    [Tooltip("가변 가로 최소값")]
    [SerializeField, Min(10)]
    private int minMapWidth = 24;

    [Tooltip("가변 가로 최대값")]
    [SerializeField, Min(10)]
    private int maxMapWidth = 36;

    [Tooltip("가변 세로 최소값")]
    [SerializeField, Min(10)]
    private int minMapHeight = 16;

    [Tooltip("가변 세로 최대값")]
    [SerializeField, Min(10)]
    private int maxMapHeight = 24;

    [Header("장애물 수량")]
    [Tooltip("내부 장애물 개수")]
    [SerializeField, Min(0)]
    private int obstacleCount = 6;

    [Tooltip("개수 랜덤 편차 ±")]
    [SerializeField, Min(0)]
    private int obstacleCountVariance = 1;

    [Header("거리 규칙")]
    [Tooltip("장애물 간 최소 거리(셀)")]
    [SerializeField, Min(0f)]
    private float minObstacleDistance = 2f;

    [Tooltip("플레이어 시작점 금지 반경(셀)")]
    [SerializeField, Min(0f)]
    private float playerClearRadius = 3f;

    [Tooltip("외벽과 장애물 최소 이격(셀)")]
    [SerializeField, Min(0)]
    private int wallMargin = 1;

    [Header("패턴")]
    [Tooltip("사용할 장애물 패턴 목록. 비어 있으면 기본 3종을 코드에서 생성한다")]
    [SerializeField]
    private ObstaclePattern[] obstaclePatterns;

    [Header("타일")]
    [Tooltip("외벽/장애물에 사용할 타일. 비어 있으면 MapGenerator의 wallTile을 사용한다")]
    [SerializeField]
    private TileBase wallTile;

    [Header("생성")]
    [Tooltip("배치 재시도 횟수")]
    [SerializeField, Min(5)]
    private int maxRetries = 30;

    public int MapWidth => Mathf.Max(10, mapWidth);
    public int MapHeight => Mathf.Max(10, mapHeight);
    public bool UseVariableSize => useVariableSize;
    public int MinMapWidth => Mathf.Max(10, minMapWidth);
    public int MaxMapWidth => Mathf.Max(MinMapWidth, maxMapWidth);
    public int MinMapHeight => Mathf.Max(10, minMapHeight);
    public int MaxMapHeight => Mathf.Max(MinMapHeight, maxMapHeight);
    public int ObstacleCount => Mathf.Max(0, obstacleCount);
    public int ObstacleCountVariance => Mathf.Max(0, obstacleCountVariance);
    public float MinObstacleDistance => Mathf.Max(0f, minObstacleDistance);
    public float PlayerClearRadius => Mathf.Max(0f, playerClearRadius);
    public int WallMargin => Mathf.Max(0, wallMargin);
    public ObstaclePattern[] ObstaclePatterns => obstaclePatterns;
    public TileBase WallTile => wallTile;
    public int MaxRetries => Mathf.Max(5, maxRetries);

    public Vector2Int GetRandomMapSize(System.Random rng)
    {
        if (!useVariableSize || rng == null) return new Vector2Int(MapWidth, MapHeight);
        int w = rng.Next(MinMapWidth, MaxMapWidth + 1);
        // 2 단위 스텝으로 맞추어 타일 정렬 유지 (선택)
        if (w % 2 == 1) w = Mathf.Clamp(w + 1, MinMapWidth, MaxMapWidth);
        int h = rng.Next(MinMapHeight, MaxMapHeight + 1);
        if (h % 2 == 1) h = Mathf.Clamp(h + 1, MinMapHeight, MaxMapHeight);
        return new Vector2Int(Mathf.Max(10, w), Mathf.Max(10, h));
    }

    private void OnValidate()
    {
        mapWidth = Mathf.Max(10, mapWidth);
        mapHeight = Mathf.Max(10, mapHeight);
        minMapWidth = Mathf.Clamp(minMapWidth, 10, 64);
        maxMapWidth = Mathf.Clamp(maxMapWidth, minMapWidth, 64);
        minMapHeight = Mathf.Clamp(minMapHeight, 10, 64);
        maxMapHeight = Mathf.Clamp(maxMapHeight, minMapHeight, 64);
        if (!useVariableSize)
        {
            // 단일 크기 모드에서는 기존 값 유지
        }
        obstacleCount = Mathf.Max(0, obstacleCount);
        obstacleCountVariance = Mathf.Max(0, obstacleCountVariance);
        minObstacleDistance = Mathf.Max(0f, minObstacleDistance);
        int refW = useVariableSize ? minMapWidth : mapWidth;
        int refH = useVariableSize ? minMapHeight : mapHeight;
        playerClearRadius = Mathf.Clamp(playerClearRadius, 0f, Mathf.Min(refW, refH) * 0.4f);
        wallMargin = Mathf.Max(0, wallMargin);
        maxRetries = Mathf.Max(5, maxRetries);

        if (obstacleCountVariance > obstacleCount)
        {
            obstacleCountVariance = obstacleCount;
        }
    }
}
