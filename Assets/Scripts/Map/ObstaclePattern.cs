using UnityEngine;

/// <summary>
/// 하나의 장애물 패턴을 정의한다. 셀 마스크와 회전/반전 허용 여부를 담는다.
/// </summary>
[CreateAssetMenu(fileName = "ObstaclePattern", menuName = "Scriptable Objects/Map/ObstaclePattern")]
public sealed class ObstaclePattern : ScriptableObject
{
    [Header("패턴 크기")]
    [Tooltip("패턴의 가로/세로 셀 수")]
    [SerializeField]
    private Vector2Int patternSize = new Vector2Int(3, 2);

    [Tooltip("패턴에서 채울 셀. patternSize.x * patternSize.y 길이. true면 벽 타일을 채운다")]
    [SerializeField]
    private bool[] cells = new bool[6] { true, true, true, true, true, true };

    [Header("변형 허용")]
    [Tooltip("90도 단위 회전을 허용하는지")]
    [SerializeField]
    private bool allowRotation = true;

    [Tooltip("좌우 반전을 허용하는지")]
    [SerializeField]
    private bool allowMirror = true;

    [Header("추첨")]
    [Tooltip("패턴 추첨 가중치. 클수록 자주 선택된다")]
    [SerializeField, Min(0f)]
    private float weight = 1f;

    public Vector2Int PatternSize => patternSize;
    public bool[] Cells => cells;
    public bool AllowRotation => allowRotation;
    public bool AllowMirror => allowMirror;
    public float Weight => Mathf.Max(0f, weight);

    private void OnValidate()
    {
        patternSize.x = Mathf.Clamp(patternSize.x, 1, 8);
        patternSize.y = Mathf.Clamp(patternSize.y, 1, 8);
        int expected = patternSize.x * patternSize.y;
        if (cells == null || cells.Length != expected)
        {
            bool[] newCells = new bool[expected];
            if (cells != null)
            {
                int copyLen = Mathf.Min(cells.Length, expected);
                for (int i = 0; i < copyLen; i++)
                {
                    newCells[i] = cells[i];
                }
            }
            else
            {
                for (int i = 0; i < expected; i++) newCells[i] = true;
            }
            cells = newCells;
        }
        weight = Mathf.Max(0f, weight);
    }

    /// <summary>
    /// 회전/반전을 적용한 셀 좌표 목록을 반환한다.
    /// </summary>
    public Vector2Int[] GetTransformedCells(int rotationSteps, bool mirrored)
    {
        rotationSteps = ((rotationSteps % 4) + 4) % 4;
        if (!allowRotation) rotationSteps = 0;
        if (!allowMirror) mirrored = false;

        int w = patternSize.x;
        int h = patternSize.y;

        // 회전 후 크기
        int outW = (rotationSteps % 2 == 0) ? w : h;
        int outH = (rotationSteps % 2 == 0) ? h : w;

        System.Collections.Generic.List<Vector2Int> result = new System.Collections.Generic.List<Vector2Int>();
        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                int idx = y * w + x;
                if (idx >= cells.Length || !cells[idx]) continue;

                Vector2Int p = TransformPoint(new Vector2Int(x, y), w, h, rotationSteps, mirrored, outW, outH);
                result.Add(p);
            }
        }
        return result.ToArray();
    }

    public Vector2Int GetTransformedSize(int rotationSteps, bool mirrored)
    {
        rotationSteps = ((rotationSteps % 4) + 4) % 4;
        if (!allowRotation) rotationSteps = 0;
        int w = patternSize.x;
        int h = patternSize.y;
        int outW = (rotationSteps % 2 == 0) ? w : h;
        int outH = (rotationSteps % 2 == 0) ? h : w;
        return new Vector2Int(outW, outH);
    }

    private static Vector2Int TransformPoint(Vector2Int p, int w, int h, int rot, bool mirrored, int outW, int outH)
    {
        // mirrored: 좌우 반전 (x -> w-1 - x)
        if (mirrored)
        {
            p.x = w - 1 - p.x;
        }

        // 회전: 0, 90, 180, 270 CW
        Vector2Int r;
        switch (rot)
        {
            case 1: // 90
                r = new Vector2Int(h - 1 - p.y, p.x);
                break;
            case 2: // 180
                r = new Vector2Int(w - 1 - p.x, h - 1 - p.y);
                break;
            case 3: // 270
                r = new Vector2Int(p.y, w - 1 - p.x);
                break;
            default:
                r = p;
                break;
        }
        return r;
    }
}
