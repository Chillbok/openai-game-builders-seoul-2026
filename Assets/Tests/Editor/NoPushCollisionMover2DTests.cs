#if UNITY_EDITOR
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public sealed class NoPushCollisionMover2DTests
{
    private readonly List<GameObject> createdObjects = new List<GameObject>();

    [TearDown]
    public void TearDown()
    {
        foreach (GameObject createdObject in createdObjects)
        {
            Object.DestroyImmediate(createdObject);
        }

        createdObjects.Clear();
    }

    [Test]
    public void CalculateTargetPosition_StopsBeforeBlockingCollider()
    {
        CreateMover(Vector2.zero, out Rigidbody2D rigidbody, out BoxCollider2D bodyCollider);
        CreateObstacle(new Vector2(1.5f, 0f), new Vector2(1f, 3f));
        Physics2D.SyncTransforms();

        NoPushCollisionMover2D mover = CreateCollisionMover(rigidbody, bodyCollider);
        Vector2 target = mover.CalculateTargetPosition(Vector2.right);

        Assert.That(target.x, Is.InRange(0.49f, 0.5f));
        Assert.That(target.y, Is.EqualTo(0f).Within(0.0001f));
    }

    [Test]
    public void CalculateTargetPosition_PreservesMovementAlongContactSurface()
    {
        CreateMover(Vector2.zero, out Rigidbody2D rigidbody, out BoxCollider2D bodyCollider);
        CreateObstacle(new Vector2(1.5f, 0f), new Vector2(1f, 4f));
        Physics2D.SyncTransforms();

        NoPushCollisionMover2D mover = CreateCollisionMover(rigidbody, bodyCollider);
        Vector2 target = mover.CalculateTargetPosition(new Vector2(1f, 1f));

        Assert.That(target.x, Is.InRange(0.49f, 0.5f));
        Assert.That(target.y, Is.EqualTo(1f).Within(0.001f));
    }

    [Test]
    public void CalculateTargetPosition_AllowsMovementOutOfExistingOverlap()
    {
        CreateMover(Vector2.zero, out Rigidbody2D rigidbody, out BoxCollider2D bodyCollider);
        CreateObstacle(new Vector2(0.25f, 0f), Vector2.one);
        Physics2D.SyncTransforms();

        NoPushCollisionMover2D mover = CreateCollisionMover(rigidbody, bodyCollider);
        Vector2 target = mover.CalculateTargetPosition(Vector2.left * 0.5f);

        Assert.That(target.x, Is.EqualTo(-0.5f).Within(0.001f));
    }

    [Test]
    public void ConfigureNoPushContact_DisablesForcesButKeepsCallbacks()
    {
        CreateMover(Vector2.zero, out _, out BoxCollider2D bodyCollider);
        int enemyLayer = LayerMask.NameToLayer("Enemy");
        Assert.That(enemyLayer, Is.GreaterThanOrEqualTo(0));

        bodyCollider.forceSendLayers = Physics2D.AllLayers;
        bodyCollider.forceReceiveLayers = Physics2D.AllLayers;
        bodyCollider.contactCaptureLayers = 0;
        bodyCollider.callbackLayers = 0;

        NoPushCollisionMover2D.ConfigureNoPushContact(bodyCollider, enemyLayer);

        int enemyMask = 1 << enemyLayer;
        Assert.That(((int)bodyCollider.forceSendLayers & enemyMask), Is.Zero);
        Assert.That(((int)bodyCollider.forceReceiveLayers & enemyMask), Is.Zero);
        Assert.That(((int)bodyCollider.contactCaptureLayers & enemyMask), Is.Not.Zero);
        Assert.That(((int)bodyCollider.callbackLayers & enemyMask), Is.Not.Zero);
    }

    private NoPushCollisionMover2D CreateCollisionMover(Rigidbody2D rigidbody, Collider2D bodyCollider)
    {
        int defaultLayerMask = 1 << 0;
        return new NoPushCollisionMover2D(
            rigidbody,
            bodyCollider,
            rigidbody.transform,
            defaultLayerMask);
    }

    private void CreateMover(
        Vector2 position,
        out Rigidbody2D rigidbody,
        out BoxCollider2D bodyCollider)
    {
        GameObject gameObject = new GameObject("Mover");
        createdObjects.Add(gameObject);
        gameObject.transform.position = position;

        rigidbody = gameObject.AddComponent<Rigidbody2D>();
        rigidbody.bodyType = RigidbodyType2D.Kinematic;
        rigidbody.gravityScale = 0f;

        bodyCollider = gameObject.AddComponent<BoxCollider2D>();
        bodyCollider.size = Vector2.one;
    }

    private void CreateObstacle(Vector2 position, Vector2 size)
    {
        GameObject gameObject = new GameObject("Obstacle");
        createdObjects.Add(gameObject);
        gameObject.transform.position = position;

        BoxCollider2D collider = gameObject.AddComponent<BoxCollider2D>();
        collider.size = size;
    }
}
#endif
