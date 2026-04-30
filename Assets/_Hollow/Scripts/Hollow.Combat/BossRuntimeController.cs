using System.Collections.Generic;
using System.Linq;
using Hollow.Data.Definitions;
using Hollow.Entities;
using Hollow.Presentation;
using Hollow.Rooms;
using UnityEngine;

namespace Hollow.Combat
{
    public sealed class BossRuntimeController : MonoBehaviour
    {
        public const int MaxActiveBossProjectiles = 24;

        private readonly List<EnemyProjectileController> activeProjectiles = new();
        private readonly List<EnemyRuntimeController> summonedEnemies = new();
        private EnemyRuntimeController owner;
        private BossDefinition definition;
        private RoomRuntimeRoot room;
        private PlaceholderPlayerController player;
        private GameObject projectilePrefab;
        private CombatFeelProfileDefinition combatFeelProfile;
        private float nextPrimaryTime;
        private float nextSecondaryTime;
        private float nextSpecialTime;
        private float nextHopTime;
        private float rotationAngle;
        private bool spawnedFirstMirrorSplit;
        private bool spawnedSecondMirrorSplit;
        private bool spawnedLarvaMinions;
        private string statusText = "Watching";

        public BossDefinition Definition => definition;

        public string StatusText => statusText;

        public void Configure(
            EnemyRuntimeController nextOwner,
            BossDefinition nextDefinition,
            RoomRuntimeRoot nextRoom,
            PlaceholderPlayerController nextPlayer,
            GameObject nextProjectilePrefab,
            CombatFeelProfileDefinition nextCombatFeelProfile)
        {
            owner = nextOwner;
            definition = nextDefinition;
            room = nextRoom;
            player = nextPlayer;
            projectilePrefab = nextProjectilePrefab;
            combatFeelProfile = CombatFeelProfileDefinition.Resolve(nextCombatFeelProfile);
            nextPrimaryTime = Time.time + 1.2f;
            nextSecondaryTime = Time.time + 2.2f;
            nextSpecialTime = Time.time + 3.4f;
            nextHopTime = Time.time + 1.6f;
            rotationAngle = StableHash(definition != null ? definition.BossId : "boss") % 360;
            statusText = "Entering";
        }

        public void Tick(float deltaTime, float timeSeconds)
        {
            CleanupProjectiles();
            if (owner == null || definition == null || player == null || !owner.IsAlive)
            {
                return;
            }

            UpdatePhaseStatus();
            switch (definition.BehaviorId)
            {
                case BossBehaviorId.SplinterSaint:
                    TickSplinterSaint(deltaTime, timeSeconds);
                    break;
                case BossBehaviorId.GravelMaw:
                    TickGravelMaw(deltaTime, timeSeconds);
                    break;
                case BossBehaviorId.CartoucheWidow:
                    TickCartoucheWidow(deltaTime, timeSeconds);
                    break;
                case BossBehaviorId.IronReliquary:
                    TickIronReliquary(deltaTime, timeSeconds);
                    break;
                case BossBehaviorId.MirrorHusk:
                    TickMirrorHusk(deltaTime, timeSeconds);
                    break;
                case BossBehaviorId.AshComet:
                    TickAshComet(deltaTime, timeSeconds);
                    break;
                case BossBehaviorId.ChoirOfTeeth:
                    TickChoirOfTeeth(deltaTime, timeSeconds);
                    break;
                case BossBehaviorId.RustBishop:
                    TickRustBishop(deltaTime, timeSeconds);
                    break;
                case BossBehaviorId.HollowStarLarva:
                    TickHollowStarLarva(deltaTime, timeSeconds);
                    break;
                default:
                    TickStoneWarden(deltaTime, timeSeconds);
                    break;
            }
        }

        private void TickStoneWarden(float deltaTime, float timeSeconds)
        {
            Chase(deltaTime, definition.SpeedMetersPerSecond);
            if (timeSeconds >= nextPrimaryTime)
            {
                statusText = "Stone charge";
                DashAtPlayer(1.35f);
                nextPrimaryTime = timeSeconds + 3.2f;
            }

            if (timeSeconds >= nextSecondaryTime)
            {
                statusText = "Stomp burst";
                FireRadial(8, DamageThreatKind.Boss, 2, 0f);
                nextSecondaryTime = timeSeconds + 4.6f;
            }

            if (HealthPercent() <= 0.5f && timeSeconds >= nextSpecialTime)
            {
                statusText = "Four-way burst";
                FireCardinal(DamageThreatKind.StrongProjectile, 1);
                nextSpecialTime = timeSeconds + 3.8f;
            }
        }

        private void TickSplinterSaint(float deltaTime, float timeSeconds)
        {
            if (timeSeconds >= nextHopTime)
            {
                statusText = "Side hop";
                var direction = ((StableHash($"{definition.BossId}{Mathf.FloorToInt(timeSeconds)}") & 1) == 0 ? Vector3.left : Vector3.right);
                Move(direction, definition.SpeedMetersPerSecond * 3.2f, 0.28f);
                FireRadial(6, DamageThreatKind.Light, 1, 30f);
                nextHopTime = timeSeconds + 1.75f;
            }
            else
            {
                Chase(deltaTime, definition.SpeedMetersPerSecond * 0.75f);
            }
        }

        private void TickGravelMaw(float deltaTime, float timeSeconds)
        {
            Chase(deltaTime, definition.SpeedMetersPerSecond);
            if (timeSeconds >= nextSpecialTime)
            {
                statusText = "Burrow summon";
                SpawnMinions(new[] { "spawnEnemyNormal", "spawnEnemyFast", "spawnEnemyNormal" });
                FireRadial(5, DamageThreatKind.Light, 1, 18f);
                nextSpecialTime = timeSeconds + 7f;
            }
        }

        private void TickCartoucheWidow(float deltaTime, float timeSeconds)
        {
            Strafe(deltaTime, 0.8f);
            if (timeSeconds >= nextPrimaryTime)
            {
                statusText = "Falling marks";
                FireFanAtPlayer(5, 42f, DamageThreatKind.Light, 1);
                nextPrimaryTime = timeSeconds + 1.65f;
            }
        }

        private void TickIronReliquary(float deltaTime, float timeSeconds)
        {
            Strafe(deltaTime, 0.65f);
            if (timeSeconds >= nextPrimaryTime)
            {
                statusText = "Peek shot";
                FireAtPlayer(DamageThreatKind.Light, 1);
                FireAtPlayer(DamageThreatKind.Light, 1, 10f);
                FireAtPlayer(DamageThreatKind.Light, 1, -10f);
                nextPrimaryTime = timeSeconds + 1.9f;
            }

            if (timeSeconds >= nextSecondaryTime)
            {
                statusText = "Relocate";
                DashAtPlayer(-1.1f);
                nextSecondaryTime = timeSeconds + 4.2f;
            }
        }

        private void TickMirrorHusk(float deltaTime, float timeSeconds)
        {
            Chase(deltaTime, definition.SpeedMetersPerSecond);
            var percent = HealthPercent();
            if (!spawnedFirstMirrorSplit && percent <= 0.75f)
            {
                spawnedFirstMirrorSplit = true;
                statusText = "Split x2";
                SpawnMinions(new[] { "spawnEnemyFast", "spawnEnemyFast" });
            }

            if (!spawnedSecondMirrorSplit && percent <= 0.5f)
            {
                spawnedSecondMirrorSplit = true;
                statusText = "Split x4";
                SpawnMinions(new[] { "spawnEnemyNormal", "spawnEnemyNormal", "spawnEnemyFast", "spawnEnemyFast" });
            }
        }

        private void TickAshComet(float deltaTime, float timeSeconds)
        {
            if (timeSeconds >= nextPrimaryTime)
            {
                statusText = "Comet dash";
                DashAtPlayer(2.2f);
                FireRadial(8, DamageThreatKind.Boss, 2, rotationAngle);
                nextPrimaryTime = timeSeconds + 2.6f;
                rotationAngle += 24f;
            }
            else
            {
                Chase(deltaTime, definition.SpeedMetersPerSecond * 0.5f);
            }
        }

        private void TickChoirOfTeeth(float deltaTime, float timeSeconds)
        {
            Strafe(deltaTime, 0.45f);
            if (timeSeconds >= nextPrimaryTime)
            {
                statusText = "Rotating hymn";
                FireRadial(12, DamageThreatKind.Light, 1, rotationAngle);
                rotationAngle += 17f;
                nextPrimaryTime = timeSeconds + 2.2f;
            }

            if (HealthPercent() <= 0.35f && timeSeconds >= nextSpecialTime)
            {
                statusText = "Tooth storm";
                FireRadial(16, DamageThreatKind.StrongProjectile, 2, rotationAngle * 0.5f);
                nextSpecialTime = timeSeconds + 4.2f;
            }
        }

        private void TickRustBishop(float deltaTime, float timeSeconds)
        {
            Strafe(deltaTime, 0.55f);
            if (timeSeconds >= nextPrimaryTime)
            {
                statusText = "Beam windup";
                FireFanAtPlayer(3, 14f, DamageThreatKind.StrongProjectile, 2);
                nextPrimaryTime = timeSeconds + 2.8f;
            }

            if (timeSeconds >= nextSecondaryTime)
            {
                statusText = "Mine pattern";
                FireRadial(6, DamageThreatKind.Light, 1, rotationAngle);
                rotationAngle += 31f;
                nextSecondaryTime = timeSeconds + 3.6f;
            }
        }

        private void TickHollowStarLarva(float deltaTime, float timeSeconds)
        {
            Chase(deltaTime, definition.SpeedMetersPerSecond * (HealthPercent() <= 0.35f ? 1.25f : 0.9f));
            if (!spawnedLarvaMinions && HealthPercent() <= 0.6f)
            {
                spawnedLarvaMinions = true;
                statusText = "Abyss call";
                SpawnMinions(new[] { "spawnEnemyFlying", "spawnEnemyNormal", "spawnEnemyCharger" });
            }

            if (timeSeconds >= nextPrimaryTime)
            {
                statusText = "Starfall";
                FireFanAtPlayer(7, 65f, DamageThreatKind.Light, 1);
                nextPrimaryTime = timeSeconds + 2.1f;
            }

            if (HealthPercent() <= 0.25f && timeSeconds >= nextSpecialTime)
            {
                statusText = "Desperation";
                FireRadial(18, DamageThreatKind.StrongProjectile, 2, rotationAngle);
                rotationAngle += 13f;
                nextSpecialTime = timeSeconds + 3.1f;
            }
        }

        private void Chase(float deltaTime, float speed)
        {
            var delta = player.transform.localPosition - owner.transform.localPosition;
            delta.y = 0f;
            if (delta.sqrMagnitude <= 0.01f)
            {
                return;
            }

            Move(delta.normalized, speed, deltaTime);
        }

        private void Strafe(float deltaTime, float speedMultiplier)
        {
            var delta = player.transform.localPosition - owner.transform.localPosition;
            delta.y = 0f;
            var side = delta.sqrMagnitude > 0.01f ? Vector3.Cross(Vector3.up, delta.normalized) : Vector3.right;
            Move(side, definition.SpeedMetersPerSecond * speedMultiplier, deltaTime);
        }

        private void DashAtPlayer(float strength)
        {
            var delta = player.transform.localPosition - owner.transform.localPosition;
            delta.y = 0f;
            var direction = delta.sqrMagnitude > 0.01f ? delta.normalized : Vector3.forward;
            if (strength < 0f)
            {
                direction = -direction;
            }

            Move(direction, definition.SpeedMetersPerSecond * Mathf.Abs(strength) * 2.2f, 0.22f);
            VfxPresenter.Play(VfxCueId.EnemyWindup, owner.transform.position, owner.transform.parent);
            AudioPresenter.Play(AudioCueId.EnemyWindup, owner.transform.position);
        }

        private void Move(Vector3 direction, float speed, float deltaTime)
        {
            if (room == null || direction.sqrMagnitude <= 0.001f)
            {
                return;
            }

            var desired = owner.transform.localPosition + direction.normalized * speed * Mathf.Max(0f, deltaTime);
            owner.transform.localPosition = RoomLocalCollision.ResolveMove(room, owner.transform.localPosition, desired, owner.RadiusMeters);
        }

        private void FireAtPlayer(DamageThreatKind threatKind, int damage, float angleOffsetDegrees = 0f)
        {
            var delta = player.transform.localPosition - owner.transform.localPosition;
            delta.y = 0f;
            var direction = delta.sqrMagnitude > 0.01f ? delta.normalized : Vector3.forward;
            FireProjectile(Quaternion.Euler(0f, angleOffsetDegrees, 0f) * direction, threatKind, damage);
        }

        private void FireFanAtPlayer(int count, float spreadDegrees, DamageThreatKind threatKind, int damage)
        {
            var safeCount = Mathf.Max(1, count);
            var start = safeCount == 1 ? 0f : -spreadDegrees * 0.5f;
            var step = safeCount == 1 ? 0f : spreadDegrees / (safeCount - 1);
            for (var index = 0; index < safeCount; index++)
            {
                FireAtPlayer(threatKind, damage, start + step * index);
            }
        }

        private void FireCardinal(DamageThreatKind threatKind, int damage)
        {
            FireProjectile(Vector3.forward, threatKind, damage);
            FireProjectile(Vector3.back, threatKind, damage);
            FireProjectile(Vector3.left, threatKind, damage);
            FireProjectile(Vector3.right, threatKind, damage);
        }

        private void FireRadial(int count, DamageThreatKind threatKind, int damage, float offsetDegrees)
        {
            var safeCount = Mathf.Max(1, count);
            for (var index = 0; index < safeCount; index++)
            {
                var angle = offsetDegrees + 360f * index / safeCount;
                FireProjectile(Quaternion.Euler(0f, angle, 0f) * Vector3.forward, threatKind, damage);
            }
        }

        private void FireProjectile(Vector3 direction, DamageThreatKind threatKind, int damage)
        {
            CleanupProjectiles();
            if (activeProjectiles.Count >= MaxActiveBossProjectiles)
            {
                return;
            }

            var projectileObject = projectilePrefab != null
                ? Instantiate(projectilePrefab, owner.transform.parent)
                : GameObject.CreatePrimitive(PrimitiveType.Sphere);
            projectileObject.name = $"EnemyProjectile.Boss.{definition.BossId}";
            projectileObject.transform.SetParent(owner.transform.parent, worldPositionStays: false);
            projectileObject.transform.localPosition = owner.transform.localPosition + direction.normalized * (owner.RadiusMeters + 0.32f) + new Vector3(0f, 0.42f, 0f);
            projectileObject.transform.localScale = Vector3.one * 0.26f;
            var playerProjectile = projectileObject.GetComponent<ProjectileController>();
            if (playerProjectile != null)
            {
                DestroyRuntime(playerProjectile);
            }

            var collider = projectileObject.GetComponent<Collider>();
            if (collider != null)
            {
                collider.enabled = false;
            }

            var projectile = projectileObject.GetComponent<EnemyProjectileController>() ?? projectileObject.AddComponent<EnemyProjectileController>();
            projectile.Configure(room, player, direction, Mathf.Clamp(damage, 1, 2), definition.ProjectileSpeedMetersPerSecond, 2.8f);
            projectile.ConfigureCombatFeel(combatFeelProfile);
            projectile.ConfigureThreat(threatKind);
            activeProjectiles.Add(projectile);
        }

        private void SpawnMinions(IEnumerable<string> spawnKinds)
        {
            var index = 0;
            foreach (var spawnKind in spawnKinds ?? Enumerable.Empty<string>())
            {
                var angle = 360f * index / 3f + rotationAngle;
                var offset = Quaternion.Euler(0f, angle, 0f) * Vector3.forward * 1.2f;
                var child = owner.SpawnChildEnemy(spawnKind, owner.transform.localPosition + offset);
                if (child != null)
                {
                    summonedEnemies.Add(child);
                }

                index++;
            }

            rotationAngle += 41f;
        }

        private void CleanupProjectiles()
        {
            activeProjectiles.RemoveAll(projectile => projectile == null || !projectile.gameObject.activeInHierarchy);
        }

        private float HealthPercent()
        {
            var health = owner != null ? owner.Health : null;
            return health == null || health.MaxHealth <= 0 ? 1f : Mathf.Clamp01((float)health.CurrentHealth / health.MaxHealth);
        }

        private void UpdatePhaseStatus()
        {
            if (definition == null || definition.Phases.Count == 0)
            {
                return;
            }

            var percent = HealthPercent();
            var phase = definition.Phases
                .OrderBy(phase => phase.healthThreshold01)
                .LastOrDefault(phase => percent <= phase.healthThreshold01);
            if (phase != null && string.IsNullOrWhiteSpace(statusText))
            {
                statusText = phase.statusText;
            }
        }

        private static void DestroyRuntime(Component component)
        {
            if (component == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(component);
            }
            else
            {
                DestroyImmediate(component);
            }
        }

        private static int StableHash(string value)
        {
            unchecked
            {
                var hash = 2166136261u;
                foreach (var character in value ?? string.Empty)
                {
                    hash ^= character;
                    hash *= 16777619u;
                }

                return (int)(hash & 0x7fffffff);
            }
        }
    }
}
