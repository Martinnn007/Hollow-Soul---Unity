using Hollow.Rooms;
using UnityEngine;

namespace Hollow.Combat
{
    public enum EnemyAttackReachabilityStatus
    {
        Clear = 0,
        Blocked = 1,
        NeedsReposition = 2
    }

    public readonly struct EnemyAttackReachabilityResult
    {
        public EnemyAttackReachabilityResult(
            EnemyAttackReachabilityStatus status,
            EnemyAttackObstructionPolicy policy,
            string reason)
        {
            Status = status;
            Policy = policy;
            Reason = reason ?? string.Empty;
        }

        public EnemyAttackReachabilityStatus Status { get; }

        public EnemyAttackObstructionPolicy Policy { get; }

        public string Reason { get; }

        public bool CanCommit => Status == EnemyAttackReachabilityStatus.Clear;

        public static EnemyAttackReachabilityResult Clear(EnemyAttackObstructionPolicy policy, string reason = "attack_clear")
        {
            return new EnemyAttackReachabilityResult(EnemyAttackReachabilityStatus.Clear, policy, reason);
        }

        public static EnemyAttackReachabilityResult Blocked(EnemyAttackObstructionPolicy policy, string reason)
        {
            return new EnemyAttackReachabilityResult(EnemyAttackReachabilityStatus.Blocked, policy, reason);
        }

        public static EnemyAttackReachabilityResult NeedsReposition(EnemyAttackObstructionPolicy policy, string reason)
        {
            return new EnemyAttackReachabilityResult(EnemyAttackReachabilityStatus.NeedsReposition, policy, reason);
        }
    }

    public static class EnemyAttackReachabilityService
    {
        public static EnemyAttackReachabilityResult Evaluate(
            RoomRuntimeRoot room,
            Vector3 attackerLocalPosition,
            Vector3 targetLocalPosition,
            float attackerRadiusMeters,
            float targetRadiusMeters,
            EnemyAttackProfileDefinition profile,
            EnemyAttackRuntimeKind fallbackRuntimeKind,
            bool canReposition)
        {
            var policy = profile != null
                ? profile.ResolvedObstructionPolicy
                : EnemyAttackProfileDefinition.DefaultObstructionPolicy(fallbackRuntimeKind);
            if (room == null || policy == EnemyAttackObstructionPolicy.IgnoresObstruction)
            {
                return EnemyAttackReachabilityResult.Clear(policy);
            }

            attackerLocalPosition.y = 0f;
            targetLocalPosition.y = 0f;
            switch (policy)
            {
                case EnemyAttackObstructionPolicy.BallisticArc:
                    return EvaluateBallistic(room, targetLocalPosition, targetRadiusMeters, policy, canReposition);
                case EnemyAttackObstructionPolicy.SelfArea:
                    return EnemyAttackReachabilityResult.Clear(policy, "self_area_clear");
                case EnemyAttackObstructionPolicy.BodyLane:
                    return RoomLocalCollision.HasClearMovementLane(
                            room,
                            attackerLocalPosition,
                            targetLocalPosition,
                            Mathf.Max(RoomLocalCollision.MinimumRadiusMeters, attackerRadiusMeters * 0.85f),
                            out var bodyReason)
                        ? EnemyAttackReachabilityResult.Clear(policy)
                        : BlockedOrReposition(policy, bodyReason, canReposition);
                case EnemyAttackObstructionPolicy.ClearLine:
                    if (IsProjectileLike(profile, fallbackRuntimeKind))
                    {
                        return RoomLocalCollision.HasClearProjectileLine(
                                room,
                                attackerLocalPosition,
                                targetLocalPosition,
                                Mathf.Max(0.05f, targetRadiusMeters * 0.25f),
                                out var projectileReason)
                            ? EnemyAttackReachabilityResult.Clear(policy)
                            : BlockedOrReposition(policy, projectileReason, canReposition);
                    }

                    return RoomLocalCollision.HasClearMovementLane(
                            room,
                            attackerLocalPosition,
                            targetLocalPosition,
                            Mathf.Max(RoomLocalCollision.MinimumRadiusMeters, Mathf.Min(attackerRadiusMeters, 0.28f)),
                            out var lineReason)
                        ? EnemyAttackReachabilityResult.Clear(policy)
                        : BlockedOrReposition(policy, lineReason, canReposition);
                default:
                    return EnemyAttackReachabilityResult.Clear(policy);
            }
        }

        public static bool CanCommit(
            RoomRuntimeRoot room,
            Vector3 attackerLocalPosition,
            Vector3 targetLocalPosition,
            float attackerRadiusMeters,
            float targetRadiusMeters,
            EnemyAttackProfileDefinition profile,
            EnemyAttackRuntimeKind fallbackRuntimeKind,
            out string reason)
        {
            var result = Evaluate(
                room,
                attackerLocalPosition,
                targetLocalPosition,
                attackerRadiusMeters,
                targetRadiusMeters,
                profile,
                fallbackRuntimeKind,
                canReposition: false);
            reason = result.Reason;
            return result.CanCommit;
        }

        private static EnemyAttackReachabilityResult EvaluateBallistic(
            RoomRuntimeRoot room,
            Vector3 targetLocalPosition,
            float targetRadiusMeters,
            EnemyAttackObstructionPolicy policy,
            bool canReposition)
        {
            var landingRadius = Mathf.Max(0.08f, Mathf.Min(0.35f, targetRadiusMeters));
            if (RoomLocalCollision.IsOutsideBounds(room, targetLocalPosition, landingRadius))
            {
                return BlockedOrReposition(policy, "blocked_landing_bounds", canReposition);
            }

            if (RoomLocalCollision.IntersectsObstacle(room, targetLocalPosition, landingRadius))
            {
                return BlockedOrReposition(policy, "blocked_landing_obstacle", canReposition);
            }

            return EnemyAttackReachabilityResult.Clear(policy, "ballistic_arc_clear");
        }

        private static EnemyAttackReachabilityResult BlockedOrReposition(
            EnemyAttackObstructionPolicy policy,
            string reason,
            bool canReposition)
        {
            var normalizedReason = string.IsNullOrWhiteSpace(reason) ? "blocked_by_obstacle" : reason;
            return canReposition
                ? EnemyAttackReachabilityResult.NeedsReposition(policy, normalizedReason)
                : EnemyAttackReachabilityResult.Blocked(policy, normalizedReason);
        }

        private static bool IsProjectileLike(EnemyAttackProfileDefinition profile, EnemyAttackRuntimeKind fallbackRuntimeKind)
        {
            var runtimeKind = profile != null ? profile.RuntimeKind : fallbackRuntimeKind;
            return runtimeKind is EnemyAttackRuntimeKind.Projectile
                or EnemyAttackRuntimeKind.FanProjectile
                or EnemyAttackRuntimeKind.RadialProjectile
                or EnemyAttackRuntimeKind.SequentialRadialProjectile
                or EnemyAttackRuntimeKind.Beam
                or EnemyAttackRuntimeKind.LockingBeam;
        }
    }
}
