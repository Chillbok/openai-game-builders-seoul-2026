using UnityEngine;

/// <summary>
/// 물리 레이어 충돌 매트릭스를 런타임에 보장한다.
/// Player(6)-Enemy(7) 접촉은 활성화해 충돌 콜백을 유지하고,
/// Player-Obstacle(8)/Enemy-Obstacle 충돌은 활성화해 벽 막힘을 유지한다.
/// ProjectSettings 설정과 런타임에서 이중 보장하며,
/// Player-Enemy 사이의 힘 전달과 이동 차단은 NoPushCollisionMover2D가 담당한다. (웹 빌드 포함)
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
            Physics2D.IgnoreLayerCollision(playerLayer, enemyLayer, false);
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
