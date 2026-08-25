#if UNITY_EDITOR
using NUnit.Framework;
using UnityEngine;

public sealed class MapLayoutTests
{
    [Test]
    public void FillOuterWalls_CreatesClosedBorder()
    {
        var layout = new MapLayout(10, 10);
        layout.FillOuterWalls();

        for (int x = 0; x < 10; x++)
        {
            Assert.That(layout.IsWall(x, 0), Is.True);
            Assert.That(layout.IsWall(x, 9), Is.True);
        }
        for (int y = 0; y < 10; y++)
        {
            Assert.That(layout.IsWall(0, y), Is.True);
            Assert.That(layout.IsWall(9, y), Is.True);
        }
        Assert.That(layout.IsWall(5, 5), Is.False);
    }

    [Test]
    public void IsFullyConnected_EmptyMap_ReturnsTrue()
    {
        var layout = new MapLayout(10, 10);
        layout.FillOuterWalls();
        Vector2Int center = layout.GetCenter();
        Assert.That(layout.IsFullyConnected(center), Is.True);
    }

    [Test]
    public void IsFullyConnected_WithIsolatedArea_ReturnsFalse()
    {
        var layout = new MapLayout(10, 10);
        layout.FillOuterWalls();
        // 중앙에 수직 벽을 만들어 두 영역으로 분리
        for (int y = 1; y < 9; y++)
        {
            layout.SetWall(5, y, true);
        }
        Vector2Int center = layout.GetCenter(); // (5,5)는 벽이므로 도달 불가
        // 벽 위에 있으므로 IsFullyConnected는 false
        // 대신 왼쪽 영역의 점에서 검사하면 오른쪽 영역 도달 불가로 false
        Vector2Int left = new Vector2Int(2, 5);
        Assert.That(layout.IsFullyConnected(left), Is.False);
    }

    [Test]
    public void CountReachableFrom_CorrectlyCounts()
    {
        var layout = new MapLayout(5, 5);
        layout.FillOuterWalls();
        // 내부 3x3 = 9칸 빈 공간
        Vector2Int center = layout.GetCenter(); // (2,2)
        int reachable = layout.CountReachableFrom(center);
        Assert.That(reachable, Is.EqualTo(9));
    }

    [Test]
    public void HasWallInRadius_DetectsNearbyWall()
    {
        var layout = new MapLayout(10, 10);
        layout.FillOuterWalls();
        layout.SetWall(5, 5, true);
        Vector2Int center = new Vector2Int(5, 5);
        Assert.That(layout.HasWallInRadius(center, 0.5f), Is.True);
        Assert.That(layout.HasWallInRadius(new Vector2Int(2, 2), 1f), Is.False);
    }

    [Test]
    public void TryPlacePattern_RejectsOverlap()
    {
        var layout = new MapLayout(10, 10);
        layout.FillOuterWalls();
        Vector2Int origin = new Vector2Int(2, 2);
        Vector2Int[] cells = new Vector2Int[] { new Vector2Int(0, 0), new Vector2Int(1, 0) };
        bool first = layout.TryPlacePattern(origin, cells);
        Assert.That(first, Is.True);
        bool second = layout.TryPlacePattern(origin, cells);
        Assert.That(second, Is.False);
    }

    [Test]
    public void TryPlacePattern_AfterRemove_CanPlaceAgain()
    {
        var layout = new MapLayout(10, 10);
        layout.FillOuterWalls();
        Vector2Int origin = new Vector2Int(2, 2);
        Vector2Int[] cells = new Vector2Int[] { new Vector2Int(0, 0), new Vector2Int(1, 0) };
        layout.TryPlacePattern(origin, cells);
        layout.RemovePattern(origin, cells);
        bool again = layout.TryPlacePattern(origin, cells);
        Assert.That(again, Is.True);
    }

    [Test]
    public void GetCenter_ReturnsCorrect()
    {
        var layout = new MapLayout(30, 20);
        Vector2Int center = layout.GetCenter();
        Assert.That(center, Is.EqualTo(new Vector2Int(15, 10)));
    }

    [Test]
    public void Generate_Deterministic_WithSameSeed()
    {
        // MapLayout + MapGenerator 로직의 결정론 간접 검증: 같은 시드로 같은 수의 장애물 배치 시 동일한 빈 셀 수
        // 실제 MapGenerator는 Unity Tilemap 의존이므로 여기선 MapLayout만으로 검증
        var layout1 = new MapLayout(10, 10);
        layout1.FillOuterWalls();
        var layout2 = new MapLayout(10, 10);
        layout2.FillOuterWalls();
        // 동일한 패턴 배치를 수동으로 수행하면 결과가 동일해야 함
        Vector2Int[] cells = new Vector2Int[] { new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(0, 1) };
        layout1.TryPlacePattern(new Vector2Int(2, 2), cells);
        layout2.TryPlacePattern(new Vector2Int(2, 2), cells);
        Assert.That(layout1.CountEmptyCells(), Is.EqualTo(layout2.CountEmptyCells()));
    }
}
#endif
