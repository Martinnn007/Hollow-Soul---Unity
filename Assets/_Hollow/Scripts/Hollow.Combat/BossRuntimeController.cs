using System;
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
        private const float ProjectileCleanupIntervalSeconds = 0.25f;

        private readonly List<EnemyProjectileController> activeProjectiles = new();
        private readonly List<EnemyRuntimeController> summonedEnemies = new();
        private readonly Dictionary<string, EnemyAttackProfileDefinition> attackProfileCache = new(StringComparer.Ordinal);
        private EnemyRuntimeController owner;
        private BossDefinition definition;
        private BossPhaseDefinition[] phaseStatusCache = Array.Empty<BossPhaseDefinition>();
        private RoomRuntimeRoot room;
        private PlaceholderPlayerController player;
        private GameObject projectilePrefab;
        private CombatFeelProfileDefinition combatFeelProfile;
        private float nextPrimaryTime;
        private float nextSecondaryTime;
        private float nextSpecialTime;
        private float nextHopTime;
        private float nextProjectileCleanupTime;
        private float rotationAngle;
        private bool spawnedFirstMirrorSplit;
        private bool spawnedSecondMirrorSplit;
        private bool spawnedLarvaMinions;
        private string statusText = "Watching";
        private InspectionEntityMode inspectionMode = InspectionEntityMode.LiveRuntime;

        public BossDefinition Definition => definition;

        public string StatusText => statusText;

        public void SetInspectionMode(InspectionEntityMode mode)
        {
            inspectionMode = mode;
            statusText = mode == InspectionEntityMode.FrozenRuntime ? "Frozen for inspection" : statusText;
        }

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
            activeProjectiles.Clear();
            summonedEnemies.Clear();
            spawnedFirstMirrorSplit = false;
            spawnedSecondMirrorSplit = false;
            spawnedLarvaMinions = false;
            phaseStatusCache = BuildPhaseStatusCache(definition);
            BuildAttackProfileCache(definition);
            nextPrimaryTime = Time.time + 1.2f;
            nextSecondaryTime = Time.time + 2.2f;
            nextSpecialTime = Time.time + 3.4f;
            nextHopTime = Time.time + 1.6f;
            nextProjectileCleanupTime = Time.time + ProjectileCleanupIntervalSeconds;
            rotationAngle = StableHash(definition != null ? definition.BossId : "boss") % 360;
            statusText = "Entering";
        }

        public void Tick(float deltaTime, float timeSeconds)
        {
            CleanupProjectiles(timeSeconds, force: false);
            if (inspectionMode == InspectionEntityMode.FrozenRuntime ||
                owner == null ||
                definition == null ||
                player == null ||
                !owner.IsAlive)
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
                var profile = Profile("stone_charge");
                DashAtPlayer(1.35f, profile, timeSeconds);
                nextPrimaryTime = timeSeconds + profile.CooldownSeconds;
            }

            if (timeSeconds >= nextSecondaryTime)
            {
                statusText = "Stomp burst";
                var profile = Profile("stone_stomp_burst");
                PlayBossPatternCue(profile);
                FireRadial(8, profile, 0f);
                nextSecondaryTime = timeSeconds + profile.CooldownSeconds;
            }

            if (HealthPercent() <= 0.5f && timeSeconds >= nextSpecialTime)
            {
                statusText = "Four-way burst";
                var profile = Profile("stone_four_way_burst");
                PlayBossPatternCue(profile);
                FireCardinal(profile);
                nextSpecialTime = timeSeconds + profile.CooldownSeconds;
            }
        }

        private void TickSplinterSaint(float deltaTime, float timeSeconds)
        {
            if (timeSeconds >= nextHopTime)
            {
                statusText = "Side hop";
                var direction = ((StableHash($"{definition.BossId}{Mathf.FloorToInt(timeSeconds)}") & 1) == 0 ? Vector3.left : Vector3.right);
                Move(direction, definition.SpeedMetersPerSecond * 3.2f, 0.28f);
                FireRadial(6, Profile("splinter_side_hop_radial"), 30f);
                nextHopTime = timeSeconds + Profile("splinter_side_hop_radial").CooldownSeconds;
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
                FireRadial(5, Profile("gravel_rubble_spray"), 18f);
                nextSpecialTime = timeSeconds + Profile("gravel_burrow_summon").CooldownSeconds;
            }
        }

        private void TickCartoucheWidow(float deltaTime, float timeSeconds)
        {
            Strafe(deltaTime, 0.8f);
            if (timeSeconds >= nextPrimaryTime)
            {
                statusText = "Falling marks";
                var profile = Profile("cartouche_falling_marks");
                PlayBossPatternCue(profile);
                FireFanAtPlayer(5, 42f, profile);
                nextPrimaryTime = timeSeconds + profile.CooldownSeconds;
            }

            if (timeSeconds >= nextSecondaryTime)
            {
                statusText = "Lapis volley";
                var profile = Profile("cartouche_lapis_volley");
                PlayBossPatternCue(profile);
                FireFanAtPlayer(3, 26f, profile);
                nextSecondaryTime = timeSeconds + profile.CooldownSeconds;
            }

            if (HealthPercent() <= 0.45f && timeSeconds >= nextSpecialTime)
            {
                statusText = "Sigil mines";
                var profile = Profile("cartouche_sigil_mines");
                PlayBossPatternCue(profile);
                FireRadial(4, profile, rotationAngle);
                rotationAngle += 37f;
                nextSpecialTime = timeSeconds + profile.CooldownSeconds;
            }
        }

        private void TickIronReliquary(float deltaTime, float timeSeconds)
        {
            Strafe(deltaTime, 0.65f);
            if (timeSeconds >= nextPrimaryTime)
            {
                statusText = "Peek shot";
                var profile = Profile("iron_peek_shot");
                FireAtPlayer(profile);
                FireAtPlayer(profile, 10f);
                FireAtPlayer(profile, -10f);
                nextPrimaryTime = timeSeconds + profile.CooldownSeconds;
            }

            if (timeSeconds >= nextSecondaryTime)
            {
                statusText = "Relocate";
                var profile = Profile("iron_relocate_bash");
                DashAtPlayer(-1.1f, profile, timeSeconds);
                nextSecondaryTime = timeSeconds + profile.CooldownSeconds;
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
                var dashProfile = Profile("ash_comet_dash");
                DashAtPlayer(2.2f, dashProfile, timeSeconds);
                FireRadial(8, Profile("ash_fire_radial"), rotationAngle);
                nextPrimaryTime = timeSeconds + dashProfile.CooldownSeconds;
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
                var profile = Profile("choir_rotating_hymn");
                PlayBossPatternCue(profile);
                FireRadial(12, profile, rotationAngle);
                rotationAngle += 17f;
                nextPrimaryTime = timeSeconds + profile.CooldownSeconds;
            }

            if (HealthPercent() <= 0.35f && timeSeconds >= nextSpecialTime)
            {
                statusText = "Tooth storm";
                var profile = Profile("choir_tooth_storm");
                PlayBossPatternCue(profile);
                FireRadial(16, profile, rotationAngle * 0.5f);
                nextSpecialTime = timeSeconds + profile.CooldownSeconds;
            }
        }

        private void TickRustBishop(float deltaTime, float timeSeconds)
        {
            Strafe(deltaTime, 0.55f);
            if (timeSeconds >= nextPrimaryTime)
            {
                statusText = "Beam windup";
                FireFanAtPlayer(3, 14f, Profile("rust_beam"));
                nextPrimaryTime = timeSeconds + Profile("rust_beam").CooldownSeconds;
            }

            if (timeSeconds >= nextSecondaryTime)
            {
                statusText = "Mine pattern";
                FireRadial(6, Profile("rust_mine_pattern"), rotationAngle);
                rotationAngle += 31f;
                nextSecondaryTime = timeSeconds + Profile("rust_mine_pattern").CooldownSeconds;
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
                FireFanAtPlayer(7, 65f, Profile("larva_starfall"));
                nextPrimaryTime = timeSeconds + Profile("larva_starfall").CooldownSeconds;
            }

            if (HealthPercent() <= 0.25f && timeSeconds >= nextSpecialTime)
            {
                statusText = "Desperation";
                FireRadial(18, Profile("larva_desperation"), rotationAngle);
                rotationAngle += 13f;
                nextSpecialTime = timeSeconds + Profile("larva_desperation").CooldownSeconds;
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

        private void DashAtPlayer(float strength, EnemyAttackProfileDefinition profile, float timeSeconds)
        {
            var delta = player.transform.localPosition - owner.transform.localPosition;
            delta.y = 0f;
            var direction = delta.sqrMagnitude > 0.01f ? delta.normalized : Vector3.forward;
            if (strength < 0f)
            {
                direction = -direction;
            }

            Move(direction, definition.SpeedMetersPerSecond * Mathf.Abs(strength) * 2.2f, 0.22f);
            owner.ArmBossActiveContactWindow(profile, timeSeconds);
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

        private void FireAtPlayer(EnemyAttackProfileDefinition profile, float angleOffsetDegrees = 0f)
        {
            var delta = player.transform.localPosition - owner.transform.localPosition;
            delta.y = 0f;
            var direction = delta.sqrMagnitude > 0.01f ? delta.normalized : Vector3.forward;
            FireProjectile(Quaternion.Euler(0f, angleOffsetDegrees, 0f) * direction, profile);
        }

        private void FireFanAtPlayer(int count, float spreadDegrees, EnemyAttackProfileDefinition profile)
        {
            var safeCount = Mathf.Max(1, count);
            var start = safeCount == 1 ? 0f : -spreadDegrees * 0.5f;
            var step = safeCount == 1 ? 0f : spreadDegrees / (safeCount - 1);
            for (var index = 0; index < safeCount; index++)
            {
                FireAtPlayer(profile, start + step * index);
            }
        }

        private void FireCardinal(EnemyAttackProfileDefinition profile)
        {
            FireProjectile(Vector3.forward, profile);
            FireProjectile(Vector3.back, profile);
            FireProjectile(Vector3.left, profile);
            FireProjectile(Vector3.right, profile);
        }

        private void FireRadial(int count, EnemyAttackProfileDefinition profile, float offsetDegrees)
        {
            var safeCount = Mathf.Max(1, count);
            for (var index = 0; index < safeCount; index++)
            {
                var angle = offsetDegrees + 360f * index / safeCount;
                FireProjectile(Quaternion.Euler(0f, angle, 0f) * Vector3.forward, profile);
            }
        }

        private void FireProjectile(Vector3 direction, EnemyAttackProfileDefinition profile)
        {
            CleanupProjectiles(Time.time, force: activeProjectiles.Count >= MaxActiveBossProjectiles);
            if (activeProjectiles.Count >= MaxActiveBossProjectiles)
            {
                return;
            }

            var projectileObject = projectilePrefab != null
                ? Hollow.Core.HollowRuntimePool.Rent(projectilePrefab, owner.transform.parent)
                : Hollow.Core.HollowRuntimePool.RentPrimitive("EnemyProjectile.Boss.Fallback", PrimitiveType.Sphere, owner.transform.parent);
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
            projectile.Configure(
                room,
                player,
                direction,
                profile != null ? profile.Damage : 1,
                profile != null ? profile.ProjectileSpeedMetersPerSecond : definition.ProjectileSpeedMetersPerSecond,
                2.8f);
            projectile.ConfigureCombatFeel(combatFeelProfile);
            if (profile != null)
            {
                projectile.ConfigureAttackProfile(profile);
            }
            else
            {
                projectile.ConfigureThreat(DamageThreatKind.Light);
            }
            activeProjectiles.Add(projectile);
        }

        private void PlayBossPatternCue(EnemyAttackProfileDefinition profile)
        {
            VfxPresenter.Play(VfxCueId.EnemyWindup, owner.transform.position, owner.transform.parent);
            AudioPresenter.Play(AudioCueId.EnemyWindup, owner.transform.position);
        }

        private EnemyAttackProfileDefinition Profile(string attackId)
        {
            if (!string.IsNullOrWhiteSpace(attackId) &&
                attackProfileCache.TryGetValue(attackId, out var cached) &&
                cached != null)
            {
                return cached;
            }

            var profile = definition != null ? definition.ResolveAttackProfile(attackId) : null;
            if (profile == null)
            {
                profile = EnemyAttackProfileDefinition.CreateRuntime(new EnemyAttackProfileSpec(
                definition != null ? definition.BossId : "boss",
                true,
                attackId,
                attackId,
                EnemyAttackRuntimeKind.Projectile,
                1,
                1f,
                0.1f,
                0.1f,
                5f,
                1,
                definition != null ? definition.ProjectileSpeedMetersPerSecond : 4.8f,
                DamageChannel.Physical,
                DamageDelivery.Projectile,
                DamageElement.None,
                ImpactForceClass.Light,
                DamageThreatKind.Light,
                0.35f,
                0.35f,
                "Runtime fallback profile."));
            }

            if (profile != null && !string.IsNullOrWhiteSpace(profile.AttackId))
            {
                attackProfileCache[profile.AttackId] = profile;
            }

            return profile;
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

        private void CleanupProjectiles(float timeSeconds, bool force)
        {
            if (!force && timeSeconds < nextProjectileCleanupTime)
            {
                return;
            }

            nextProjectileCleanupTime = timeSeconds + ProjectileCleanupIntervalSeconds;
            for (var index = activeProjectiles.Count - 1; index >= 0; index--)
            {
                var projectile = activeProjectiles[index];
                if (projectile == null || !projectile.gameObject.activeInHierarchy)
                {
                    activeProjectiles.RemoveAt(index);
                }
            }
        }

        private float HealthPercent()
        {
            var health = owner != null ? owner.Health : null;
            return health == null || health.MaxHealth <= 0 ? 1f : Mathf.Clamp01((float)health.CurrentHealth / health.MaxHealth);
        }

        private void UpdatePhaseStatus()
        {
            if (phaseStatusCache == null || phaseStatusCache.Length == 0)
            {
                return;
            }

            var percent = HealthPercent();
            for (var index = phaseStatusCache.Length - 1; index >= 0; index--)
            {
                var phase = phaseStatusCache[index];
                if (phase != null && percent <= phase.healthThreshold01)
                {
                    if (string.IsNullOrWhiteSpace(statusText))
                    {
                        statusText = phase.statusText;
                    }

                    return;
                }
            }
        }

        private void BuildAttackProfileCache(BossDefinition bossDefinition)
        {
            attackProfileCache.Clear();
            if (bossDefinition == null)
            {
                return;
            }

            var profiles = bossDefinition.AttackProfiles;
            for (var index = 0; index < profiles.Count; index++)
            {
                var profile = profiles[index];
                if (profile != null && !string.IsNullOrWhiteSpace(profile.AttackId))
                {
                    attackProfileCache[profile.AttackId] = profile;
                }
            }
        }

        private static BossPhaseDefinition[] BuildPhaseStatusCache(BossDefinition bossDefinition)
        {
            if (bossDefinition == null || bossDefinition.Phases == null || bossDefinition.Phases.Count == 0)
            {
                return Array.Empty<BossPhaseDefinition>();
            }

            return bossDefinition.Phases
                .Where(phase => phase != null)
                .OrderBy(phase => phase.healthThreshold01)
                .ToArray();
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
