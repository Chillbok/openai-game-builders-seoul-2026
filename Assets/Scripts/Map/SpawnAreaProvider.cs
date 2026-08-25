using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// 맵 생성 직후 빈 셀을 캐시하고 스폰 좌표를 제공한다.
/// 지형 내부/겹침 제외 규칙을 적용한다.
/// </summary>
[DisallowMultipleComponent]
public sealed class SpawnAreaProvider : MonoBehaviour
{
    private readonly List<Vector2Int> emptyCells = new List<Vector2Int>();
    private readonly List<Vector3> worldPositions = new List<Vector3>();
    private Tilemap wallsTilemap;
    private MapLayout layout;
    private int obstacleLayerMask;
    private int enemyLayerMask;

    public IReadOnlyList<Vector2Int> EmptyCells => emptyCells;
    public IReadOnlyList<Vector3> WorldPositions => worldPositions;
    public bool HasBakedData => emptyCells.Count > 0;

    private void Awake()
    {
        CacheLayerMasks();
    }

    private void CacheLayerMasks()
    {
        int obstacleLayer = LayerMask.NameToLayer("Obstacle");
        int enemyLayer = LayerMask.NameToLayer("Enemy");
        obstacleLayerMask = obstacleLayer >= 0 ? 1 << obstacleLayer : 0;
        enemyLayerMask = enemyLayer >= 0 ? 1 << enemyLayer : 0;
    }

    public void Bake(MapLayout mapLayout, Tilemap tilemap)
    {
        layout = mapLayout;
        wallsTilemap = tilemap;
        emptyCells.Clear();
        worldPositions.Clear();

        if (mapLayout == null || tilemap == null) return;

        List<Vector2Int> empties = mapLayout.GetEmptyCells();
        emptyCells.AddRange(empties);

        foreach (var cell in empties)
        {
            // 셀 중심 월드 좌표
            Vector3 world = tilemap.GetCellCenterWorld(new Vector3Int(cell.x, cell.y, 0));
            worldPositions.Add(world);
        }
    }

    /// <summary>
    /// 빈 셀 중 무작위로 하나를 선택해 월드 좌표를 반환한다. 실패 시 중앙을 반환한다.
    /// </summary>
    public Vector3 GetRandomSpawnPosition(System.Random rng)
    {
        if (rng == null) rng = new System.Random();

        // 겹침 검사 포함 재시도
        for (int attempt = 0; attempt < 10; attempt++)
        {
            if (worldPositions.Count == 0) break;
            int idx = rng.Next(worldPositions.Count);
            Vector3 candidate = worldPositions[idx];
            if (!IsOverlapping(candidate))
            {
                return candidate;
            }
        }

        // 재시도 실패 시 겹침 무시하고 랜덤 반환
        if (worldPositions.Count > 0)
        {
            return worldPositions[rng.Next(worldPositions.Count)];
        }

        // Bake 전이면 타일맵 중앙
        if (wallsTilemap != null)
        {
            return wallsTilemap.GetCellCenterWorld(Vector3Int.zero);
        }
        return transform.position;
    }

    public Vector3 GetRandomSpawnPosition(int seed)
    {
        return GetRandomSpawnPosition(new System.Random(seed));
    }

    private bool IsOverlapping(Vector3 worldPos)
    {
        // 반경 0.4로 Obstacle/Enemy 겹침 검사
        Vector2 pos2 = new Vector2(worldPos.x, worldPos.y);
        int combinedMask = obstacleLayerMask | enemyLayerMask;
        if (combinedMask == 0) return false;
        Collider2D hit = Physics2D.OverlapCircle(pos2, 0.4f, combinedMask);
        return hit != null;
    }

    /// <summary>
    /// 지정한 월드 반경 내 빈 셀을 찾는다.
    /// </summary>
    public Vector3 GetRandomSpawnPositionInRadius(Vector3 center, float radius, System.Random rng)
    {
        if (rng == null) rng = new System.Random();
        List<Vector3> candidates = new List<Vector3>();
        foreach (var wp in worldPositions)
        {
            if ((wp - center).sqrMagnitude <= radius * radius)
            {
                if (!IsOverlapping(wp)) candidates.Add(wp);
            }
        }
        if (candidates.Count > 0) return candidates[rng.Next(candidates.Count)];
        return GetRandomSpawnPosition(rng);
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (worldPositions == null) return;
        Gizmos.color = new Color(0f, 1f, 0.3f, 0.35f);
        foreach (var wp in worldPositions)
        {
            Gizmos.DrawWireCube(wp, Vector3.one * 0.8f);
        }
    }
#endif
}
