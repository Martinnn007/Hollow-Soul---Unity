using System.Collections.Generic;
using Hollow.Rooms;
using UnityEngine;

namespace Hollow.Combat
{
    public readonly struct PlayerEnemyBodyCollisionResult
    {
        public PlayerEnemyBodyCollisionResult(Vector3 position, EnemyRuntimeController blockingEnemy, bool wasBlocked)
        {
            Position = position;
            BlockingEnemy = blockingEnemy;
            WasBlocked = wasBlocked;
        }

        public Vector3 Position { get; }

        public EnemyRuntimeController BlockingEnemy { get; }

        public bool WasBlocked { get; }
    }

    public static class PlayerEnemyBodyCollision
    {
        public const float BodySkinMeters = 0.03f;
        public const float TangentSlideMultiplier = 0.32f;
        public const float LightWalkShoveMaxMeters = 0.004f;
        public const float LightRollShoveMaxMeters = 0.008f;
        public const float LightWalkShoveMovementMultiplier = 0.08f;
        public const float LightRollShoveMovementMultiplier = 0.12f;
        public const float RollBodyDeflectMultiplier = 0.72f;
        public const float RollBodyDeflectMaxMetersPerSubstep = 0.11f;
        public const float RollBodyDeflectPressureThreshold = 0.35f;
        private const int MaxDepenetrationPasses = 3;

        public static PlayerEnemyBodyCollisionResult Resolve(
            RoomRuntimeRoot room,
            IReadOnlyList<EnemyRuntimeController> enemies,
            Vector3 currentLocal,
            Vector3 desiredLocal,
            float playerRadiusMeters,
            bool isRollTraveling = false,
            Vector3 rollDirectionLocal = default)
        {
            if (enemies == null || enemies.Count == 0)
            {
                return new PlayerEnemyBodyCollisionResult(desiredLocal, null, false);
            }

            var resolved = desiredLocal;
            var firstBlockingEnemy = default(EnemyRuntimeController);
            var wasBlocked = false;
            var safePlayerRadius = Mathf.Max(RoomLocalCollision.MinimumRadiusMeters, playerRadiusMeters);

            for (var pass = 0; pass < MaxDepenetrationPasses; pass++)
            {
                var movedThisPass = false;
                foreach (var enemy in enemies)
                {
                    if (!IsBlockingEnemy(enemy))
                    {
                        continue;
                    }

                    var enemyPosition = enemy.transform.localPosition;
                    enemyPosition.y = resolved.y;
                    var minimumDistance = safePlayerRadius + Mathf.Max(RoomLocalCollision.MinimumRadiusMeters, enemy.RadiusMeters) + BodySkinMeters;
                    var delta = resolved - enemyPosition;
                    delta.y = 0f;
                    if (delta.sqrMagnitude >= minimumDistance * minimumDistance)
                    {
                        continue;
                    }

                    wasBlocked = true;
                    firstBlockingEnemy ??= enemy;
                    var surfaceNormal = ResolveSurfaceNormal(currentLocal, desiredLocal, enemyPosition, delta);
                    if (pass == 0 && TryNudgeLightEnemy(room, enemy, enemyPosition, surfaceNormal, desiredLocal - currentLocal, isRollTraveling))
                    {
                        enemyPosition = enemy.transform.localPosition;
                        enemyPosition.y = resolved.y;
                        delta = resolved - enemyPosition;
                        delta.y = 0f;
                        surfaceNormal = ResolveSurfaceNormal(currentLocal, desiredLocal, enemyPosition, delta);
                    }

                    var candidate = TryResolveRollDeflectionCandidate(
                        currentLocal,
                        desiredLocal,
                        enemyPosition,
                        surfaceNormal,
                        minimumDistance,
                        isRollTraveling,
                        rollDirectionLocal,
                        out var rollDeflectionCandidate)
                        ? rollDeflectionCandidate
                        : ResolveFrictionCandidate(currentLocal, desiredLocal, enemyPosition, surfaceNormal, minimumDistance);
                    candidate.y = resolved.y;
                    candidate = RoomLocalCollision.ResolveMove(room, currentLocal, candidate, safePlayerRadius);

                    if (StillOverlaps(candidate, enemyPosition, minimumDistance))
                    {
                        candidate = RoomLocalCollision.ResolveNearestOccupiablePosition(
                            room,
                            candidate,
                            safePlayerRadius,
                            surfaceNormal,
                            1.25f);
                    }

                    if ((candidate - resolved).sqrMagnitude > 0.000001f)
                    {
                        resolved = candidate;
                        movedThisPass = true;
                    }
                }

                if (!movedThisPass)
                {
                    break;
                }
            }

            return new PlayerEnemyBodyCollisionResult(resolved, firstBlockingEnemy, wasBlocked);
        }

        public static bool IsBlockingEnemy(EnemyRuntimeController enemy)
        {
            return enemy != null &&
                enemy.IsAlive &&
                enemy.gameObject.activeInHierarchy &&
                enemy.BossDefinition == null &&
                enemy.MovementMode == EnemyMovementMode.Grounded;
        }

        private static Vector3 ResolveSurfaceNormal(Vector3 currentLocal, Vector3 desiredLocal, Vector3 enemyPosition, Vector3 desiredDelta)
        {
            var currentDelta = currentLocal - enemyPosition;
            currentDelta.y = 0f;
            if (currentDelta.sqrMagnitude > 0.0001f)
            {
                return currentDelta.normalized;
            }

            if (desiredDelta.sqrMagnitude > 0.0001f)
            {
                return desiredDelta.normalized;
            }

            var movementDelta = desiredLocal - currentLocal;
            movementDelta.y = 0f;
            if (movementDelta.sqrMagnitude > 0.0001f)
            {
                return movementDelta.normalized;
            }

            return Vector3.back;
        }

        private static Vector3 ResolveFrictionCandidate(
            Vector3 currentLocal,
            Vector3 desiredLocal,
            Vector3 enemyPosition,
            Vector3 surfaceNormal,
            float minimumDistance)
        {
            var movementDelta = desiredLocal - currentLocal;
            movementDelta.y = 0f;
            var intoEnemy = Vector3.Dot(movementDelta, -surfaceNormal);
            var candidate = desiredLocal;
            candidate.y = currentLocal.y;

            if (intoEnemy > 0f)
            {
                var tangent = movementDelta + surfaceNormal * intoEnemy;
                candidate = currentLocal + tangent * TangentSlideMultiplier;
                candidate.y = currentLocal.y;
            }

            var candidateDelta = candidate - enemyPosition;
            candidateDelta.y = 0f;
            if (candidateDelta.sqrMagnitude < minimumDistance * minimumDistance)
            {
                var outward = candidateDelta.sqrMagnitude > 0.0001f ? candidateDelta.normalized : surfaceNormal;
                candidate = enemyPosition + outward * minimumDistance;
                candidate.y = currentLocal.y;
            }

            return candidate;
        }

        private static bool TryResolveRollDeflectionCandidate(
            Vector3 currentLocal,
            Vector3 desiredLocal,
            Vector3 enemyPosition,
            Vector3 surfaceNormal,
            float minimumDistance,
            bool isRollTraveling,
            Vector3 rollDirectionLocal,
            out Vector3 candidate)
        {
            candidate = currentLocal;
            if (!isRollTraveling)
            {
                return false;
            }

            var movementDelta = desiredLocal - currentLocal;
            movementDelta.y = 0f;
            rollDirectionLocal.y = 0f;
            var rollDirection = rollDirectionLocal.sqrMagnitude > 0.0001f
                ? rollDirectionLocal.normalized
                : movementDelta.sqrMagnitude > 0.0001f
                    ? movementDelta.normalized
                    : Vector3.zero;
            if (rollDirection.sqrMagnitude < 0.0001f)
            {
                return false;
            }

            var pressure = Mathf.Clamp01(Vector3.Dot(rollDirection, -surfaceNormal));
            if (pressure < RollBodyDeflectPressureThreshold)
            {
                return false;
            }

            var rightTangent = new Vector3(-surfaceNormal.z, 0f, surfaceNormal.x);
            if (rightTangent.sqrMagnitude < 0.0001f)
            {
                return false;
            }

            rightTangent.Normalize();
            var lateralIntent = Vector3.Dot(rollDirection, rightTangent);
            var tangent = lateralIntent < -0.05f ? -rightTangent : rightTangent;
            var deflectDistance = Mathf.Min(
                RollBodyDeflectMaxMetersPerSubstep,
                Mathf.Max(CombatFeelTuning.MinimumCollisionRadiusMeters, movementDelta.magnitude) * RollBodyDeflectMultiplier * pressure);

            candidate = currentLocal + tangent * deflectDistance;
            candidate.y = currentLocal.y;

            var candidateDelta = candidate - enemyPosition;
            candidateDelta.y = 0f;
            if (candidateDelta.sqrMagnitude < minimumDistance * minimumDistance)
            {
                var outward = candidateDelta.sqrMagnitude > 0.0001f ? candidateDelta.normalized : surfaceNormal;
                candidate = enemyPosition + outward * minimumDistance;
                candidate.y = currentLocal.y;
            }

            return true;
        }

        private static bool TryNudgeLightEnemy(
            RoomRuntimeRoot room,
            EnemyRuntimeController enemy,
            Vector3 enemyPosition,
            Vector3 surfaceNormal,
            Vector3 movementDelta,
            bool isRollTraveling)
        {
            if (enemy.BodyClass != EnemyBodyClass.Light)
            {
                return false;
            }

            movementDelta.y = 0f;
            if (movementDelta.sqrMagnitude < 0.0001f)
            {
                return false;
            }

            var movementDirection = movementDelta.normalized;
            var pressure = Mathf.Clamp01(Vector3.Dot(movementDirection, -surfaceNormal));
            if (pressure <= 0.05f)
            {
                return false;
            }

            var maxShove = isRollTraveling ? LightRollShoveMaxMeters : LightWalkShoveMaxMeters;
            var movementMultiplier = isRollTraveling ? LightRollShoveMovementMultiplier : LightWalkShoveMovementMultiplier;
            var shoveDistance = Mathf.Min(maxShove, movementDelta.magnitude * movementMultiplier * pressure);
            if (shoveDistance <= 0.0001f)
            {
                return false;
            }

            var target = enemyPosition + movementDirection * shoveDistance;
            target.y = enemy.transform.localPosition.y;
            var resolved = RoomLocalCollision.ResolveMove(room, enemy.transform.localPosition, target, enemy.RadiusMeters);
            if ((resolved - enemy.transform.localPosition).sqrMagnitude <= 0.000001f)
            {
                return false;
            }

            enemy.transform.localPosition = resolved;
            enemy.SyncNavMeshAgentAfterExternalDisplacement("player_body_light_nudge");
            return true;
        }

        private static bool StillOverlaps(Vector3 candidate, Vector3 enemyPosition, float minimumDistance)
        {
            var delta = candidate - enemyPosition;
            delta.y = 0f;
            return delta.sqrMagnitude < minimumDistance * minimumDistance;
        }
    }
}
