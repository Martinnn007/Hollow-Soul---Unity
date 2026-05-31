using Hollow.Combat;
using NUnit.Framework;
using UnityEngine;

namespace Hollow.Tests.EditMode
{
    public sealed class PlayerAimShotTelemetryTests
    {
        [SetUp]
        public void SetUp()
        {
            PlayerAimShotTelemetry.Reset();
        }

        [TearDown]
        public void TearDown()
        {
            PlayerAimShotTelemetry.Reset();
        }

        [Test]
        public void RecordShotStoresAimTargetSpeedFrameDeltaAndDirection()
        {
            PlayerAimShotTelemetry.RecordShot(
                PlayerShotAimSource.AutoLock,
                "Enemy.Rat.spawn_a",
                3.25f,
                9f,
                0.016f,
                new Vector2(2f, 0f),
                12.5f);

            var snapshot = PlayerAimShotTelemetry.LastShot;
            Assert.AreEqual(1, PlayerAimShotTelemetry.ShotCount);
            Assert.AreEqual(1, PlayerAimShotTelemetry.LockedShotCount);
            Assert.AreEqual(0, PlayerAimShotTelemetry.UnlockedShotCount);
            Assert.AreEqual(PlayerShotAimSource.AutoLock, snapshot.AimSource);
            Assert.AreEqual("Enemy.Rat.spawn_a", snapshot.LockedTargetName);
            Assert.AreEqual(3.25f, snapshot.LockedTargetDistanceMeters, 0.001f);
            Assert.AreEqual(9f, snapshot.ProjectileSpeedMetersPerSecond, 0.001f);
            Assert.AreEqual(0.016f, snapshot.FrameDeltaSeconds, 0.001f);
            Assert.AreEqual(Vector2.right, snapshot.AimDirection);
            Assert.AreEqual(12.5f, snapshot.ShotTimeSeconds, 0.001f);
        }

        [Test]
        public void RecordShotWithoutTargetCountsUnlockedShotAndNormalizesDistance()
        {
            PlayerAimShotTelemetry.RecordShot(
                PlayerShotAimSource.BodyFacing,
                string.Empty,
                100f,
                9f,
                0.033f,
                Vector2.zero,
                4f);

            var snapshot = PlayerAimShotTelemetry.LastShot;
            Assert.AreEqual(1, PlayerAimShotTelemetry.ShotCount);
            Assert.AreEqual(0, PlayerAimShotTelemetry.LockedShotCount);
            Assert.AreEqual(1, PlayerAimShotTelemetry.UnlockedShotCount);
            Assert.IsFalse(snapshot.HasLockedTarget);
            Assert.AreEqual(-1f, snapshot.LockedTargetDistanceMeters, 0.001f);
            Assert.AreEqual(Vector2.up, snapshot.AimDirection);
        }
    }
}
