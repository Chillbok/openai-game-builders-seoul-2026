using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 순수 C# 맵 레이아웃 모델. Tilemap 의존성 없이 FloodFill 연결성을 검사한다.
/// </summary>
public sealed class MapLayout
{
    private readonly int width;
    private readonly int height;
    private readonly bool[,] walls;

    public int Width => width;
    public int Height => height;

    public MapLayout(int width, int height)
    {
        this.width = Mathf.Max(1, width);
        this.height = Mathf.Max(1, height);
        walls = new bool[this.width, this.height];
    }

    public bool IsInside(int x, int y)
    {
        return x >= 0 && x < width && y >= 0 && y < height;
    }

    public bool IsWall(int x, int y)
    {
        if (!IsInside(x, y)) return true;
        return walls[x, y];
    }

    public void SetWall(int x, int y, bool isWall)
    {
        if (!IsInside(x, y)) return;
        walls[x, y] = isWall;
    }

    public void Clear()
    {
        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
                walls[x, y] = false;
    }

    public void FillOuterWalls()
    {
        for (int x = 0; x < width; x++)
        {
            walls[x, 0] = true;
            walls[x, height - 1] = true;
        }
        for (int y = 0; y < height; y++)
        {
            walls[0, y] = true;
            walls[width - 1, y] = true;
        }
    }

    public int CountEmptyCells()
    {
        int cnt = 0;
        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
                if (!walls[x, y]) cnt++;
        return cnt;
    }

    public List<Vector2Int> GetEmptyCells()
    {
        List<Vector2Int> list = new List<Vector2Int>(width * height);
        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
                if (!walls[x, y]) list.Add(new Vector2Int(x, y));
        return list;
    }

    /// <summary>
    /// 시작점에서 4방향 BFS로 도달 가능한 빈 셀 수를 센다.
    /// </summary>
    public int CountReachableFrom(Vector2Int start)
    {
        if (!IsInside(start.x, start.y) || IsWall(start.x, start.y)) return 0;

        bool[,] visited = new bool[width, height];
        Queue<Vector2Int> q = new Queue<Vector2Int>();
        q.Enqueue(start);
        visited[start.x, start.y] = true;
        int reachable = 0;

        int[] dx = { 1, -1, 0, 0 };
        int[] dy = { 0, 0, 1, -1 };

        while (q.Count > 0)
        {
            Vector2Int cur = q.Dequeue();
            reachable++;

            for (int i = 0; i < 4; i++)
            {
                int nx = cur.x + dx[i];
                int ny = cur.y + dy[i];
                if (!IsInside(nx, ny) || visited[nx, ny] || IsWall(nx, ny)) continue;
                visited[nx, ny] = true;
                q.Enqueue(new Vector2Int(nx, ny));
            }
        }
        return reachable;
    }

    public bool IsFullyConnected(Vector2Int start)
    {
        int empty = CountEmptyCells();
        if (empty == 0) return false;
        int reachable = CountReachableFrom(start);
        return reachable == empty;
    }

    /// <summary>
    /// 원형 반경 내 모든 셀을 벽으로 간주해 검사한다.
    /// </summary>
    public bool HasWallInRadius(Vector2Int center, float radius)
    {
        int r = Mathf.CeilToInt(radius);
        float rSq = radius * radius;
        for (int y = center.y - r; y <= center.y + r; y++)
        {
            for (int x = center.x - r; x <= center.x + r; x++)
            {
                if (!IsInside(x, y)) continue;
                float dx = x - center.x;
                float dy = y - center.y;
                if (dx * dx + dy * dy <= rSq + 0.001f)
                {
                    if (IsWall(x, y)) return true;
                }
            }
        }
        return false;
    }

    public bool HasWallInRect(Vector2Int origin, Vector2Int size)
    {
        for (int y = 0; y < size.y; y++)
            for (int x = 0; x < size.x; x++)
            {
                int wx = origin.x + x;
                int wy = origin.y + y;
                if (!IsInside(wx, wy)) return true;
                if (IsWall(wx, wy)) return true;
            }
        return false;
    }

    public Vector2Int GetCenter()
    {
        return new Vector2Int(width / 2, height / 2);
    }

    /// <summary>
    /// 패턴 셀을 맵에 배치한다. 경계 밖이면 false.
    /// </summary>
    public bool TryPlacePattern(Vector2Int origin, Vector2Int[] cells)
    {
        if (cells == null || cells.Length == 0) return false;
        // 경계 검사
        foreach (var c in cells)
        {
            int wx = origin.x + c.x;
            int wy = origin.y + c.y;
            if (!IsInside(wx, wy)) return false;
            if (IsWall(wx, wy)) return false;
        }
        foreach (var c in cells)
        {
            walls[origin.x + c.x, origin.y + c.y] = true;
        }
        return true;
    }

    public void RemovePattern(Vector2Int origin, Vector2Int[] cells)
    {
        if (cells == null) return;
        foreach (var c in cells)
        {
            int wx = origin.x + c.x;
            int wy = origin.y + c.y;
            if (IsInside(wx, wy)) walls[wx, wy] = false;
        }
    }
}
