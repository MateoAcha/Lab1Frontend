using UnityEngine;

public static class EnemyObstacleAvoidance
{
    public static Vector2 GetSteeredDirection(
        Transform self,
        Rigidbody2D body,
        Vector2 desiredDirection,
        float probeRadius,
        float probeDistance,
        float turnAngle)
    {
        if (desiredDirection.sqrMagnitude < 0.001f)
        {
            return Vector2.zero;
        }

        Vector2 forward = desiredDirection.normalized;
        float maxDistance = Mathf.Max(0.1f, probeDistance);
        float radius = Mathf.Max(0.05f, probeRadius);

        bool touchingBlocker = IsTouchingBlocker(self, body, radius * 0.9f);
        float forwardClearance = GetDirectionClearance(self, body, forward, radius, maxDistance);
        if (!touchingBlocker && forwardClearance >= maxDistance)
        {
            return forward;
        }

        Vector2[] candidates =
        {
            Rotate(forward, turnAngle),
            Rotate(forward, -turnAngle),
            Rotate(forward, turnAngle * 2f),
            Rotate(forward, -turnAngle * 2f),
            Rotate(forward, 90f),
            Rotate(forward, -90f)
        };

        Vector2 bestDirection = candidates[0];
        float bestClearance = -1f;
        for (int i = 0; i < candidates.Length; i++)
        {
            float clearance = GetDirectionClearance(self, body, candidates[i], radius, maxDistance);
            if (clearance > bestClearance)
            {
                bestClearance = clearance;
                bestDirection = candidates[i];
            }
        }

        return bestDirection;
    }

    private static float GetDirectionClearance(
        Transform self,
        Rigidbody2D body,
        Vector2 direction,
        float probeRadius,
        float probeDistance)
    {
        RaycastHit2D[] hits = Physics2D.CircleCastAll(self.position, probeRadius, direction, probeDistance);

        float closest = probeDistance;
        bool blocked = false;
        for (int i = 0; i < hits.Length; i++)
        {
            Collider2D col = hits[i].collider;
            if (col == null || !IsBlockingCollider(self, body, col))
            {
                continue;
            }

            blocked = true;
            closest = Mathf.Min(closest, hits[i].distance);
        }

        return blocked ? closest : probeDistance;
    }

    private static bool IsBlockingCollider(Transform self, Rigidbody2D body, Collider2D col)
    {
        if (col.isTrigger)
        {
            return false;
        }

        if (col.transform == self || col.attachedRigidbody == body)
        {
            return false;
        }

        if (col.GetComponent<PlayerController>() != null)
        {
            return false;
        }

        if (col.GetComponent<EnemyController>() != null || col.GetComponent<RangedEnemyController>() != null)
        {
            return false;
        }

        return true;
    }

    private static bool IsTouchingBlocker(Transform self, Rigidbody2D body, float radius)
    {
        Collider2D[] overlaps = Physics2D.OverlapCircleAll(self.position, radius);
        for (int i = 0; i < overlaps.Length; i++)
        {
            Collider2D col = overlaps[i];
            if (col != null && IsBlockingCollider(self, body, col))
            {
                return true;
            }
        }

        return false;
    }

    private static Vector2 Rotate(Vector2 v, float degrees)
    {
        float r = degrees * Mathf.Deg2Rad;
        float cos = Mathf.Cos(r);
        float sin = Mathf.Sin(r);
        return new Vector2(v.x * cos - v.y * sin, v.x * sin + v.y * cos);
    }
}
