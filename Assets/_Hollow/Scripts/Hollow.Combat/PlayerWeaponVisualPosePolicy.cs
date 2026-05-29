using Hollow.Data.Definitions;
using UnityEngine;

namespace Hollow.Combat
{
    public static class PlayerWeaponVisualPosePolicy
    {
        public const float MuzzleForwardOffsetMeters = 0.44f;

        private const float MeleeHeightMeters = 0.78f;
        private const float RangedHeightMeters = 0.74f;
        private const float MeleeForwardOffsetMeters = 0.34f;
        private const float RangedForwardOffsetMeters = 0.44f;
        private const float MeleeSideOffsetMeters = 0.24f;
        private const float RangedSideOffsetMeters = 0.28f;

        public static Vector2 SafeAim(Vector2 direction)
        {
            return direction.sqrMagnitude > 0.001f ? direction.normalized : Vector2.up;
        }

        public static Vector3 PlanarForward(Vector2 direction)
        {
            var aim = SafeAim(direction);
            return new Vector3(aim.x, 0f, aim.y).normalized;
        }

        public static Quaternion AimRotation(Vector2 direction)
        {
            return Quaternion.LookRotation(PlanarForward(direction), Vector3.up);
        }

        public static Vector3 HeldLocalPosition(WeaponSlot slot, Vector2 direction)
        {
            var forward = PlanarForward(direction);
            var side = new Vector3(-forward.z, 0f, forward.x);
            return forward * ForwardOffset(slot) + side * SideOffset(slot) + Vector3.up * Height(slot);
        }

        public static Vector3 AttackLocalPositionOffset(WeaponSlot slot, AttackKind attackKind, Vector2 direction, float progress01)
        {
            var progress = Mathf.Clamp01(progress01);
            if (progress <= 0f)
            {
                return Vector3.zero;
            }

            var punch = Mathf.Sin(progress * Mathf.PI);
            var forward = PlanarForward(direction);
            if (slot == WeaponSlot.Melee)
            {
                return forward * (attackKind == AttackKind.Heavy ? 0.22f : 0.13f) * punch;
            }

            return -forward * (attackKind == AttackKind.Heavy ? 0.18f : 0.09f) * punch;
        }

        public static Quaternion AttackLocalRotationOffset(WeaponSlot slot, AttackKind attackKind, float progress01)
        {
            var progress = Mathf.Clamp01(progress01);
            if (progress <= 0f)
            {
                return Quaternion.identity;
            }

            var punch = Mathf.Sin(progress * Mathf.PI);
            if (slot == WeaponSlot.Melee)
            {
                var arcDegrees = attackKind == AttackKind.Heavy ? 92f : 62f;
                var startDegrees = attackKind == AttackKind.Heavy ? -54f : -34f;
                return Quaternion.Euler(0f, startDegrees + arcDegrees * EaseOut(progress), 0f);
            }

            return Quaternion.Euler(attackKind == AttackKind.Heavy ? -9f * punch : -5f * punch, 0f, 0f);
        }

        public static Quaternion ModelCanonicalLocalRotation(WeaponSlot slot)
        {
            return Quaternion.Euler(90f, 0f, 0f);
        }

        public static Vector3 MuzzleLocalPosition()
        {
            return new Vector3(0f, 0f, MuzzleForwardOffsetMeters);
        }

        private static float Height(WeaponSlot slot)
        {
            return slot == WeaponSlot.Melee ? MeleeHeightMeters : RangedHeightMeters;
        }

        private static float ForwardOffset(WeaponSlot slot)
        {
            return slot == WeaponSlot.Melee ? MeleeForwardOffsetMeters : RangedForwardOffsetMeters;
        }

        private static float SideOffset(WeaponSlot slot)
        {
            return slot == WeaponSlot.Melee ? MeleeSideOffsetMeters : RangedSideOffsetMeters;
        }

        private static float EaseOut(float value)
        {
            value = Mathf.Clamp01(value);
            return 1f - (1f - value) * (1f - value);
        }
    }
}
