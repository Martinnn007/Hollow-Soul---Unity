using UnityEngine;

namespace Hollow.Combat
{
    public enum PlayerShotAimSource
    {
        Unknown = 0,
        BodyFacing = 1,
        ManualAim = 2,
        AutoLock = 3,
        ManualLock = 4
    }

    public readonly struct PlayerAimShotTelemetrySnapshot
    {
        public PlayerAimShotTelemetrySnapshot(
            int shotSequence,
            PlayerShotAimSource aimSource,
            string lockedTargetName,
            float lockedTargetDistanceMeters,
            float projectileSpeedMetersPerSecond,
            float frameDeltaSeconds,
            Vector2 aimDirection,
            float shotTimeSeconds)
        {
            ShotSequence = shotSequence;
            AimSource = aimSource;
            LockedTargetName = lockedTargetName ?? string.Empty;
            LockedTargetDistanceMeters = lockedTargetDistanceMeters;
            ProjectileSpeedMetersPerSecond = projectileSpeedMetersPerSecond;
            FrameDeltaSeconds = frameDeltaSeconds;
            AimDirection = aimDirection;
            ShotTimeSeconds = shotTimeSeconds;
        }

        public int ShotSequence { get; }

        public PlayerShotAimSource AimSource { get; }

        public string LockedTargetName { get; }

        public float LockedTargetDistanceMeters { get; }

        public float ProjectileSpeedMetersPerSecond { get; }

        public float FrameDeltaSeconds { get; }

        public Vector2 AimDirection { get; }

        public float ShotTimeSeconds { get; }

        public bool HasLockedTarget => !string.IsNullOrWhiteSpace(LockedTargetName);
    }

    public static class PlayerAimShotTelemetry
    {
        private static PlayerAimShotTelemetrySnapshot lastShot;
        private static int shotCount;
        private static int lockedShotCount;
        private static int unlockedShotCount;

        public static bool ConsoleLoggingEnabled { get; set; }

        public static int ShotCount => shotCount;

        public static int LockedShotCount => lockedShotCount;

        public static int UnlockedShotCount => unlockedShotCount;

        public static PlayerAimShotTelemetrySnapshot LastShot => lastShot;

        public static void RecordShot(
            PlayerShotAimSource aimSource,
            string lockedTargetName,
            float lockedTargetDistanceMeters,
            float projectileSpeedMetersPerSecond,
            float frameDeltaSeconds,
            Vector2 aimDirection,
            float shotTimeSeconds)
        {
            shotCount++;
            if (string.IsNullOrWhiteSpace(lockedTargetName))
            {
                unlockedShotCount++;
                lockedTargetDistanceMeters = -1f;
            }
            else
            {
                lockedShotCount++;
            }

            lastShot = new PlayerAimShotTelemetrySnapshot(
                shotCount,
                aimSource,
                lockedTargetName,
                lockedTargetDistanceMeters,
                projectileSpeedMetersPerSecond,
                frameDeltaSeconds,
                aimDirection.sqrMagnitude > 0.001f ? aimDirection.normalized : Vector2.up,
                shotTimeSeconds);

            if (ConsoleLoggingEnabled)
            {
                Debug.Log(
                    $"Player shot #{lastShot.ShotSequence}: aim={lastShot.AimSource}, target={(lastShot.HasLockedTarget ? lastShot.LockedTargetName : "none")}, distance={lastShot.LockedTargetDistanceMeters:0.00}m, speed={lastShot.ProjectileSpeedMetersPerSecond:0.00}m/s, dt={lastShot.FrameDeltaSeconds * 1000f:0.00}ms, dir={lastShot.AimDirection}");
            }
        }

        public static void Reset()
        {
            lastShot = default;
            shotCount = 0;
            lockedShotCount = 0;
            unlockedShotCount = 0;
            ConsoleLoggingEnabled = false;
        }
    }
}
