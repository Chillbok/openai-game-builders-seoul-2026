using UnityEngine;

/// <summary>
/// 물리 레이어 충돌 매트릭스를 런타임에 보장한다.
/// Player(6)-Enemy(7) 충돌은 비활성화해 이동 밀림을 제거하고,
/// Player-Obstacle(8)/Enemy-Obstacle 충돌은 활성화해 벽 막힘을 유지한다.
/// ProjectSettings/Physics2DSettings.asset 의 m_LayerCollisionMatrix 편집과 이중 보장한다. (웹 빌드 포함)
/// </summary>
public static class PhysicsLayerSetup
{
    private const string PlayerLayerName = "Player";
    private const string EnemyLayerName = "Enemy";
    private const string ObstacleLayerName = "Obstacle";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Setup()
    {
        int playerLayer = LayerMask.NameToLayer(PlayerLayerName);
        int enemyLayer = LayerMask.NameToLayer(EnemyLayerName);
        int obstacleLayer = LayerMask.NameToLayer(ObstacleLayerName);

        if (playerLayer >= 0 && enemyLayer >= 0)
        {
            Physics2D.IgnoreLayerCollision(playerLayer, enemyLayer, true);
        }

        if (playerLayer >= 0 && obstacleLayer >= 0)
        {
            Physics2D.IgnoreLayerCollision(playerLayer, obstacleLayer, false);
        }

        if (enemyLayer >= 0 && obstacleLayer >= 0)
        {
            Physics2D.IgnoreLayerCollision(enemyLayer, obstacleLayer, false);
        }
    }
}
