using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 실제 Collider2D 형상을 투사해 다른 본체를 밀지 않고 이동을 제한하고 슬라이딩한다.
/// </summary>
public sealed class NoPushCollisionMover2D
{
    private const float SkinWidth = 0.001f;
    private const float DirectionEpsilon = 0.000001f;
    private const int MaxSlideIterations = 3;

    private readonly Rigidbody2D rigidbody;
    private readonly Collider2D bodyCollider;
    private readonly Transform ownerTransform;
    private readonly ContactFilter2D contactFilter;
    private readonly List<RaycastHit2D> castResults = new List<RaycastHit2D>(8);

    public NoPushCollisionMover2D(
        Rigidbody2D rigidbody,
        Collider2D bodyCollider,
        Transform ownerTransform,
        int blockingLayerMask)
    {
        this.rigidbody = rigidbody;
        this.bodyCollider = bodyCollider;
        this.ownerTransform = ownerTransform;

        contactFilter = new ContactFilter2D
        {
            useTriggers = false
        };
        contactFilter.SetLayerMask(blockingLayerMask);
    }

    /// <summary>
    /// 상대 레이어와의 접촉·콜백은 유지하되 양방향 물리 힘 전달에서 상대를 제외한다.
    /// </summary>
    public static void ConfigureNoPushContact(Collider2D collider, int counterpartLayer)
    {
        if (collider == null || counterpartLayer < 0)
        {
            return;
        }

        int counterpartMask = 1 << counterpartLayer;
        int forceSendLayers = collider.forceSendLayers;
        int forceReceiveLayers = collider.forceReceiveLayers;
        int contactCaptureLayers = collider.contactCaptureLayers;
        int callbackLayers = collider.callbackLayers;

        collider.forceSendLayers = forceSendLayers & ~counterpartMask;
        collider.forceReceiveLayers = forceReceiveLayers & ~counterpartMask;
        collider.contactCaptureLayers = contactCaptureLayers | counterpartMask;
        collider.callbackLayers = callbackLayers | counterpartMask;
    }

    public void Move(Vector2 delta)
    {
        if (rigidbody == null || bodyCollider == null || delta.sqrMagnitude < DirectionEpsilon)
        {
            return;
        }

        rigidbody.MovePosition(CalculateTargetPosition(delta));
    }

    /// <summary>
    /// 충돌 접선 방향의 이동을 보존하면서 이번 물리 틱의 최종 위치를 계산한다.
    /// </summary>
    public Vector2 CalculateTargetPosition(Vector2 delta)
    {
        if (rigidbody == null || bodyCollider == null || delta.sqrMagnitude < DirectionEpsilon)
        {
            return rigidbody != null ? rigidbody.position : Vector2.zero;
        }

        Vector2 position = rigidbody.position;
        Vector2 remainingDelta = delta;
        float angle = rigidbody.rotation;

        for (int iteration = 0; iteration < MaxSlideIterations; iteration++)
        {
            float distance = remainingDelta.magnitude;
            if (distance < DirectionEpsilon)
            {
                break;
            }

            Vector2 direction = remainingDelta / distance;
            if (!TryGetNearestBlockingHit(position, angle, direction, distance, out RaycastHit2D hit))
            {
                position += remainingDelta;
                break;
            }

            float travelDistance = Mathf.Clamp(hit.distance - SkinWidth, 0f, distance);
            Vector2 traveledDelta = direction * travelDistance;
            position += traveledDelta;
            remainingDelta -= traveledDelta;

            float intoSurface = Vector2.Dot(remainingDelta, hit.normal);
            if (intoSurface >= 0f)
            {
                position += remainingDelta;
                break;
            }

            // 표면을 파고드는 성분만 제거하고 접선 성분은 다음 반복에서 계속 이동한다.
            remainingDelta -= hit.normal * intoSurface;
        }

        return position;
    }

    private bool TryGetNearestBlockingHit(
        Vector2 position,
        float angle,
        Vector2 direction,
        float distance,
        out RaycastHit2D nearestHit)
    {
        castResults.Clear();
        bodyCollider.Cast(
            position,
            angle,
            direction,
            contactFilter,
            castResults,
            distance + SkinWidth,
            true);

        nearestHit = default;
        float nearestDistance = float.PositiveInfinity;

        foreach (RaycastHit2D hit in castResults)
        {
            Collider2D hitCollider = hit.collider;
            if (hitCollider == null || hitCollider.isTrigger || IsOwnedCollider(hitCollider))
            {
                continue;
            }

            // 접촉면에서 멀어지거나 접선으로 이동하는 경우에는 기존 접촉에 가로막히지 않는다.
            if (Vector2.Dot(direction, hit.normal) >= -DirectionEpsilon)
            {
                continue;
            }

            if (hit.distance <= SkinWidth && IsMovingOutOfCurrentOverlap(hitCollider, direction))
            {
                continue;
            }

            if (hit.distance < nearestDistance)
            {
                nearestDistance = hit.distance;
                nearestHit = hit;
            }
        }

        return nearestDistance < float.PositiveInfinity;
    }

    private bool IsOwnedCollider(Collider2D collider)
    {
        return collider == bodyCollider ||
               collider.attachedRigidbody == rigidbody ||
               (ownerTransform != null && collider.transform.IsChildOf(ownerTransform));
    }

    private bool IsMovingOutOfCurrentOverlap(Collider2D otherCollider, Vector2 direction)
    {
        ColliderDistance2D distance = bodyCollider.Distance(otherCollider);
        if (!distance.isValid || !distance.isOverlapped)
        {
            return false;
        }

        // Unity가 제공하는 최소 분리 벡터 방향과 같은 방향으로 움직이면 탈출 이동이다.
        Vector2 separation = distance.normal * distance.distance;
        return Vector2.Dot(direction, separation) > DirectionEpsilon;
    }
}
