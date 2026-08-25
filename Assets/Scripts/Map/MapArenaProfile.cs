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
    [Tooltip("외벽 포함 가로 셀 수")]
    [SerializeField, Min(10)]
    private int mapWidth = 30;

    [Tooltip("외벽 포함 세로 셀 수")]
    [SerializeField, Min(10)]
    private int mapHeight = 20;

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
    public int ObstacleCount => Mathf.Max(0, obstacleCount);
    public int ObstacleCountVariance => Mathf.Max(0, obstacleCountVariance);
    public float MinObstacleDistance => Mathf.Max(0f, minObstacleDistance);
    public float PlayerClearRadius => Mathf.Max(0f, playerClearRadius);
    public int WallMargin => Mathf.Max(0, wallMargin);
    public ObstaclePattern[] ObstaclePatterns => obstaclePatterns;
    public TileBase WallTile => wallTile;
    public int MaxRetries => Mathf.Max(5, maxRetries);

    private void OnValidate()
    {
        mapWidth = Mathf.Max(10, mapWidth);
        mapHeight = Mathf.Max(10, mapHeight);
        obstacleCount = Mathf.Max(0, obstacleCount);
        obstacleCountVariance = Mathf.Max(0, obstacleCountVariance);
        minObstacleDistance = Mathf.Max(0f, minObstacleDistance);
        playerClearRadius = Mathf.Clamp(playerClearRadius, 0f, Mathf.Min(mapWidth, mapHeight) * 0.4f);
        wallMargin = Mathf.Max(0, wallMargin);
        maxRetries = Mathf.Max(5, maxRetries);

        if (obstacleCountVariance > obstacleCount)
        {
            obstacleCountVariance = obstacleCount;
        }
    }
}
