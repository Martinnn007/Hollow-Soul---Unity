using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Hollow.Combat;
using Hollow.Data.Definitions;
using Hollow.Editor.DesignerRooms;
using Hollow.RoomDesigner;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Hollow.Editor.CombatEncounterSimulator
{
    public static class CombatEncounterSimulatorEngine
    {
        public const string ReportDirectory = "output/reports/combat_encounter_simulator";

        private const float PlayerRadiusMeters = 0.32f;
        private const float DefaultPlayerMaxHealth = 6f;

        public static CombatEncounterSimulationResult Run(CombatEncounterScenario scenario, EnemyCatalog catalog = null)
        {
            var safeScenario = (scenario ?? new CombatEncounterScenario()).Clone();
            catalog ??= ResolveCatalog();
            var random = new System.Random(safeScenario.seed);
            var loadout = PlayerLoadout.FromId(safeScenario.playerLoadoutId, safeScenario.difficulty);
            var roomSize = safeScenario.RoomSizeMeters;
            var enemies = SpawnEnemies(safeScenario, catalog, random).ToList();
            var result = new CombatEncounterSimulationResult
            {
                scenario = safeScenario.Clone(),
                seed = safeScenario.seed,
                totalEnemies = enemies.Count
            };

            var playerHealth = loadout.maxHealth;
            var playerAttackTimer = 0f;
            var playerPosition = Vector2.zero;
            var elapsed = 0f;
            var totalPressureSamples = 0;
            var pressureSum = 0f;
            var maxFrameSolveMs = 0f;
            var totalSolveMs = 0f;
            var totalPathRequests = 0;

            while (elapsed <= safeScenario.durationSeconds + 0.001f)
            {
                var frame = new CombatEncounterFrame
                {
                    timeSeconds = elapsed,
                    playerPosition = PlayerPositionFor(elapsed, roomSize, loadout)
                };
                playerPosition = frame.playerPosition;

                var requestBudget = PathRequestBudgetFor(safeScenario, enemies.Count, safeScenario.tickSeconds);
                var framePathRequests = 0;
                var frameSolveMs = 0f;
                var attackPressureThisFrame = 0f;
                var aliveEnemies = enemies.Where(enemy => enemy.alive).ToArray();

                foreach (var enemy in aliveEnemies)
                {
                    enemy.attackFlashTimer = Mathf.Max(0f, enemy.attackFlashTimer - safeScenario.tickSeconds);
                    enemy.cooldownSeconds = Mathf.Max(0f, enemy.cooldownSeconds - safeScenario.tickSeconds);
                    enemy.playerDistance = Vector2.Distance(enemy.position, playerPosition);
                    SimulateEnemyMovement(enemy, playerPosition, safeScenario, random, elapsed, frame);
                    SimulatePathRequest(
                        enemy,
                        safeScenario,
                        elapsed,
                        requestBudget,
                        ref framePathRequests,
                        ref frameSolveMs,
                        frame);

                    if (enemy.aiEnabled && enemy.cooldownSeconds <= 0f && CanCommitAttack(enemy, safeScenario))
                    {
                        var lane = LaneFor(enemy.attackProfile);
                        var lanePressure = PressureFor(enemy.attackProfile, enemy.definition);
                        var pressurePenalty = safeScenario.includeRuntimePressureBudgets ? PressurePenalty(lane, frame) : 0f;
                        var commitChance = Mathf.Clamp01(0.92f - pressurePenalty + IntelligenceCommitBonus(enemy.definition));
                        if (random.NextDouble() <= commitChance)
                        {
                            enemy.attackStarts++;
                            enemy.attackFlashTimer = Mathf.Max(0.12f, enemy.attackProfile.ActiveSeconds + enemy.attackProfile.RecoverySeconds * 0.35f);
                            enemy.cooldownSeconds = CooldownFor(enemy.attackProfile, safeScenario.difficulty, enemy.definition);
                            attackPressureThisFrame += lanePressure;
                            AddLanePressure(frame, lane, lanePressure);

                            var hitChance = HitChanceFor(enemy, safeScenario, loadout, frame);
                            if (random.NextDouble() <= hitChance)
                            {
                                var damage = DamageFor(enemy.attackProfile, safeScenario.difficulty);
                                enemy.hits++;
                                enemy.damageDealt += damage;
                                playerHealth = Mathf.Max(0f, playerHealth - damage);
                            }
                        }
                        else
                        {
                            enemy.cooldownSeconds = Mathf.Max(0.18f, enemy.attackProfile.CooldownSeconds * 0.28f);
                        }
                    }
                }

                SimulatePlayerDamage(enemies, playerPosition, loadout, random, safeScenario.tickSeconds, ref playerAttackTimer);
                aliveEnemies = enemies.Where(enemy => enemy.alive).ToArray();
                frame.aliveEnemies = aliveEnemies.Length;
                frame.playerHealth = playerHealth;
                frame.pathRequests = framePathRequests;
                frame.stuckEnemies = aliveEnemies.Count(enemy => enemy.isStuck);
                frame.entities = enemies.Select(enemy => new CombatEncounterEntitySnapshot
                {
                    displayName = enemy.definition.DisplayName,
                    spawnKind = enemy.definition.SpawnKind,
                    position = enemy.position,
                    alive = enemy.alive,
                    attacking = enemy.attackFlashTimer > 0f,
                    stuck = enemy.isStuck,
                    lane = LaneFor(enemy.attackProfile)
                }).ToList();

                var framePressure = frame.meleePressure + frame.rangedPressure + frame.areaPressure + frame.chargePressure + attackPressureThisFrame * 0.4f;
                result.peakPressure = Mathf.Max(result.peakPressure, framePressure);
                pressureSum += framePressure;
                totalPressureSamples++;
                totalPathRequests += framePathRequests;
                totalSolveMs += frameSolveMs;
                maxFrameSolveMs = Mathf.Max(maxFrameSolveMs, frameSolveMs);
                result.frames.Add(frame);

                if (playerHealth <= 0f)
                {
                    result.playerDied = true;
                    break;
                }

                if (aliveEnemies.Length == 0)
                {
                    break;
                }

                elapsed += safeScenario.tickSeconds;
            }

            result.durationSeconds = Mathf.Max(0.01f, result.frames.LastOrDefault()?.timeSeconds ?? elapsed);
            result.playerFinalHealth = playerHealth;
            result.playerSurvived = playerHealth > 0f;
            result.enemyDeaths = enemies.Count(enemy => !enemy.alive);
            result.totalAttackStarts = enemies.Sum(enemy => enemy.attackStarts);
            result.totalHits = enemies.Sum(enemy => enemy.hits);
            result.totalDamageTaken = enemies.Sum(enemy => enemy.damageDealt);
            result.stuckSeconds = enemies.Sum(enemy => enemy.stuckSeconds);
            result.totalDeferredPathRequests = enemies.Sum(enemy => enemy.deferredPathRequests);
            result.pathRequestsPerSecond = totalPathRequests / result.durationSeconds;
            result.averagePathSolveMs = totalPathRequests <= 0 ? 0f : totalSolveMs / totalPathRequests;
            result.maxPathSolveMs = maxFrameSolveMs;
            result.averagePressure = totalPressureSamples <= 0 ? 0f : pressureSum / totalPressureSamples;
            var totalLanePressure = result.frames.Sum(frame => frame.meleePressure + frame.rangedPressure + frame.areaPressure + frame.chargePressure);
            result.rangedPressureShare = totalLanePressure <= 0f ? 0f : result.frames.Sum(frame => frame.rangedPressure) / totalLanePressure;
            result.areaPressureShare = totalLanePressure <= 0f ? 0f : result.frames.Sum(frame => frame.areaPressure) / totalLanePressure;
            result.enemyMetrics = BuildEnemyMetrics(enemies, result.durationSeconds);
            PopulateWarningsAndRecommendations(result);
            return result;
        }

        public static CombatEncounterBatchResult RunBatch(CombatEncounterScenario scenario, int runCount, EnemyCatalog catalog = null)
        {
            var safeScenario = (scenario ?? new CombatEncounterScenario()).Clone();
            catalog ??= ResolveCatalog();
            var batch = new CombatEncounterBatchResult
            {
                scenario = safeScenario.Clone()
            };

            var count = Mathf.Clamp(runCount, 1, 250);
            for (var index = 0; index < count; index++)
            {
                var runScenario = safeScenario.Clone();
                runScenario.seed = safeScenario.seed + index * 37;
                batch.results.Add(Run(runScenario, catalog));
            }

            PopulateBatchRecommendations(batch);
            return batch;
        }

        public static EnemyCatalog ResolveCatalog()
        {
            var assetCatalog = AssetDatabase.LoadAssetAtPath<EnemyCatalog>(EnemyPreviewLabController.DefaultEnemyCatalogPath);
            return assetCatalog != null ? assetCatalog : EnemyCatalog.CreateRuntimeDefault();
        }

        public static string ExportMarkdownReport(CombatEncounterSimulationResult result)
        {
            if (result == null)
            {
                throw new ArgumentNullException(nameof(result));
            }

            Directory.CreateDirectory(ReportDirectory);
            var path = $"{ReportDirectory}/{Sanitize(result.scenario.scenarioName)}_{result.seed}.md";
            File.WriteAllText(path, BuildMarkdownReport(result));
            return path;
        }

        public static string ExportBatchCsv(CombatEncounterBatchResult batch)
        {
            if (batch == null)
            {
                throw new ArgumentNullException(nameof(batch));
            }

            Directory.CreateDirectory(ReportDirectory);
            var path = $"{ReportDirectory}/{Sanitize(batch.scenario.scenarioName)}_batch_{batch.Runs}.csv";
            var builder = new StringBuilder();
            builder.AppendLine("seed,survived,duration,final_health,enemy_deaths,attacks,hits,damage_taken,peak_pressure,path_requests_per_second,avg_path_solve_ms,stuck_seconds");
            foreach (var result in batch.results)
            {
                builder.AppendLine(string.Join(",",
                    result.seed,
                    result.playerSurvived ? "1" : "0",
                    result.durationSeconds.ToString("0.00"),
                    result.playerFinalHealth.ToString("0.00"),
                    result.enemyDeaths,
                    result.totalAttackStarts,
                    result.totalHits,
                    result.totalDamageTaken,
                    result.peakPressure.ToString("0.00"),
                    result.pathRequestsPerSecond.ToString("0.00"),
                    result.averagePathSolveMs.ToString("0.000"),
                    result.stuckSeconds.ToString("0.00")));
            }

            File.WriteAllText(path, builder.ToString());
            return path;
        }

        public static CombatEncounterScenario ScenarioFromActiveDesignerRoom(CombatEncounterScenario baseScenario)
        {
            var scene = SceneManager.GetActiveScene();
            var project = DesignerRoomSceneAuthoringUtility.BuildRoomDesignerProject(scene);
            var scenario = (baseScenario ?? new CombatEncounterScenario()).Clone();
            scenario.scenarioName = string.IsNullOrWhiteSpace(scene.name) ? "Designer Room Simulation" : scene.name;
            scenario.roomPreset = CombatEncounterRoomPreset.ActiveDesignerRoom;
            scenario.enemyGroups.Clear();

            var enemyGroups = (project.markers ?? new List<RoomDesignerMarker>())
                .Where(marker => marker != null && RoomDesignerMarkerKinds.IsEnemy(marker.kind))
                .GroupBy(marker => marker.kind, StringComparer.Ordinal)
                .OrderBy(group => group.Key);
            foreach (var group in enemyGroups)
            {
                scenario.enemyGroups.Add(new CombatEncounterEnemyGroup
                {
                    spawnKind = group.Key,
                    count = group.Count(),
                    spawnPattern = CombatEncounterSpawnPattern.CustomMarkers,
                    aiEnabled = true,
                    notes = "Loaded from active Designer Room scene."
                });
            }

            if (scenario.enemyGroups.Count == 0)
            {
                scenario.enemyGroups.Add(new CombatEncounterEnemyGroup());
            }

            var cells = project.cells ?? new List<RoomDesignerCell>();
            var ground = Mathf.Max(1, cells.Count(cell => cell.kind == RoomDesignerCellKinds.Ground));
            var blockers = cells.Count(cell => cell.kind is RoomDesignerCellKinds.Rock or RoomDesignerCellKinds.Hole);
            scenario.obstacleDensity = Mathf.Clamp01(blockers / (float)ground);
            var occupied = RoomDesignerFootprintUtility.OccupiedCells(project.footprintPreset);
            scenario.customRoomSizeMeters = occupied.Count <= 0
                ? new Vector2(16f, 12f)
                : new Vector2(
                    Mathf.Clamp((occupied.Max(cell => cell.x) - occupied.Min(cell => cell.x) + 1) * 8f, 8f, 40f),
                    Mathf.Clamp((occupied.Max(cell => cell.y) - occupied.Min(cell => cell.y) + 1) * 8f, 8f, 40f));
            return scenario;
        }

        public static string BuildMarkdownReport(CombatEncounterSimulationResult result)
        {
            var builder = new StringBuilder();
            builder.AppendLine("# Combat Encounter Simulator Report");
            builder.AppendLine();
            builder.AppendLine($"- Scenario: `{result.scenario.scenarioName}`");
            builder.AppendLine($"- Seed: `{result.seed}`");
            builder.AppendLine($"- Difficulty: `{result.scenario.difficulty}`");
            builder.AppendLine($"- Loadout: `{result.scenario.playerLoadoutId}`");
            builder.AppendLine($"- Duration: `{result.durationSeconds:0.00}s`");
            builder.AppendLine($"- Player survived: `{result.playerSurvived}`");
            builder.AppendLine($"- Final HP: `{result.playerFinalHealth:0.0}`");
            builder.AppendLine($"- Enemy deaths: `{result.enemyDeaths}/{result.totalEnemies}`");
            builder.AppendLine($"- Attacks / hits: `{result.totalAttackStarts}` / `{result.totalHits}`");
            builder.AppendLine($"- Peak pressure: `{result.peakPressure:0.00}`");
            builder.AppendLine($"- Path requests/sec: `{result.pathRequestsPerSecond:0.00}`");
            builder.AppendLine($"- Avg path solve ms: `{result.averagePathSolveMs:0.000}`");
            builder.AppendLine($"- Stuck enemy seconds: `{result.stuckSeconds:0.00}`");
            builder.AppendLine();
            builder.AppendLine("## Enemy Metrics");
            builder.AppendLine();
            builder.AppendLine("| Enemy | Count | Deaths | Attacks | Hits | Hit % | Damage | Stuck s | Path req | Solve ms |");
            builder.AppendLine("| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |");
            foreach (var enemy in result.enemyMetrics)
            {
                builder.AppendLine($"| {enemy.displayName} | {enemy.count} | {enemy.deaths} | {enemy.attackStarts} | {enemy.hits} | {enemy.HitRate:P0} | {enemy.damageDealt} | {enemy.stuckSeconds:0.0} | {enemy.pathRequests} | {enemy.estimatedPathSolveMs:0.00} |");
            }

            builder.AppendLine();
            builder.AppendLine("## Recommendations");
            builder.AppendLine();
            foreach (var recommendation in result.recommendations.DefaultIfEmpty("No major balance warnings."))
            {
                builder.AppendLine($"- {recommendation}");
            }

            builder.AppendLine();
            builder.AppendLine("## Warnings");
            builder.AppendLine();
            foreach (var warning in result.warnings.DefaultIfEmpty("No warnings."))
            {
                builder.AppendLine($"- {warning}");
            }

            return builder.ToString();
        }

        private static IEnumerable<SimEnemy> SpawnEnemies(CombatEncounterScenario scenario, EnemyCatalog catalog, System.Random random)
        {
            var room = scenario.RoomSizeMeters;
            var globalIndex = 0;
            foreach (var group in scenario.enemyGroups ?? new List<CombatEncounterEnemyGroup>())
            {
                var definition = catalog.Resolve(group.spawnKind);
                if (definition == null)
                {
                    continue;
                }

                for (var index = 0; index < group.count; index++)
                {
                    var attack = ResolvePrimaryAttack(definition);
                    yield return new SimEnemy
                    {
                        definition = definition,
                        group = group,
                        position = SpawnPosition(group.spawnPattern, index, group.count, room, random),
                        alive = true,
                        health = definition.MaxHealth,
                        aiEnabled = group.aiEnabled,
                        attackProfile = attack,
                        cooldownSeconds = InitialCooldown(definition, attack, index, scenario),
                        nextPathTime = 0.15f + (globalIndex % 7) * 0.09f
                    };
                    globalIndex++;
                }
            }
        }

        private static EnemyAttackProfileDefinition ResolvePrimaryAttack(EnemyDefinition definition)
        {
            var actionAttackIds = definition.ActionProfiles
                .Where(action => action != null &&
                                 action.UsageState == EnemyActionUsageState.CurrentRuntime &&
                                 action.Intent is EnemyActionIntent.Damage or EnemyActionIntent.Pressure or EnemyActionIntent.HazardSetup &&
                                 action.HasLinkedAttack)
                .Select(action => action.LinkedAttackId)
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .ToHashSet(StringComparer.Ordinal);

            var attacks = definition.AttackProfiles
                .Where(attack => attack != null && attack.Damage > 0 && IsRuntimeDamageAttack(attack))
                .OrderByDescending(attack => actionAttackIds.Contains(attack.AttackId))
                .ThenByDescending(attack => attack.Damage * ForceMultiplier(attack.ForceClass))
                .ThenBy(attack => attack.CooldownSeconds)
                .ToArray();
            return attacks.FirstOrDefault() ?? EnemyAttackProfileDefaults.CreateEnemyProfiles(definition.SpawnKind).FirstOrDefault(profile => profile.Damage > 0) ?? definition.AttackProfiles.FirstOrDefault();
        }

        private static bool IsRuntimeDamageAttack(EnemyAttackProfileDefinition attack)
        {
            return attack.RuntimeKind is not EnemyAttackRuntimeKind.Defense
                and not EnemyAttackRuntimeKind.Movement
                and not EnemyAttackRuntimeKind.CreatureMove
                and not EnemyAttackRuntimeKind.CreatureSignal
                and not EnemyAttackRuntimeKind.PhaseMove
                and not EnemyAttackRuntimeKind.Summon
                and not EnemyAttackRuntimeKind.Split;
        }

        private static float InitialCooldown(EnemyDefinition definition, EnemyAttackProfileDefinition attack, int index, CombatEncounterScenario scenario)
        {
            if (attack == null)
            {
                return 999f;
            }

            var baseDelay = definition.Disposition switch
            {
                EnemyInstinctDisposition.Prey => 1.25f,
                EnemyInstinctDisposition.Territorial => 0.75f,
                EnemyInstinctDisposition.Sentinel => 0.45f,
                _ => 0.2f
            };
            return baseDelay + (index % 5) * scenario.tickSeconds * 0.7f;
        }

        private static Vector2 SpawnPosition(CombatEncounterSpawnPattern pattern, int index, int count, Vector2 room, System.Random random)
        {
            var half = room * 0.5f;
            var t = count <= 1 ? 0f : index / (float)count;
            var angle = Mathf.PI * 2f * t;
            return pattern switch
            {
                CombatEncounterSpawnPattern.AroundPlayer => new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * Mathf.Min(half.x, half.y) * 0.42f,
                CombatEncounterSpawnPattern.DoorAmbush => new Vector2(-half.x + 1.2f, Mathf.Lerp(-half.y + 1f, half.y - 1f, t)),
                CombatEncounterSpawnPattern.Corners => new Vector2(index % 2 == 0 ? -half.x + 1.1f : half.x - 1.1f, index % 4 < 2 ? -half.y + 1.1f : half.y - 1.1f),
                CombatEncounterSpawnPattern.ClusteredGroup => new Vector2(
                    Mathf.Clamp((float)(random.NextDouble() - 0.5) * 2.4f, -half.x + 1f, half.x - 1f),
                    Mathf.Clamp((float)(random.NextDouble() - 0.5) * 2.4f, -half.y + 1f, half.y - 1f)),
                CombatEncounterSpawnPattern.RangedBackline => new Vector2(half.x - 1.8f, Mathf.Lerp(-half.y + 1.2f, half.y - 1.2f, t)),
                CombatEncounterSpawnPattern.CustomMarkers => new Vector2(Mathf.Lerp(-half.x + 1f, half.x - 1f, t), index % 2 == 0 ? -half.y * 0.4f : half.y * 0.4f),
                _ => new Vector2(
                    Mathf.Lerp(-half.x + 1.2f, half.x - 1.2f, Mathf.Repeat(t * 1.7f, 1f)),
                    Mathf.Lerp(-half.y + 1.2f, half.y - 1.2f, Mathf.Repeat(t * 2.3f, 1f)))
            };
        }

        private static Vector2 PlayerPositionFor(float timeSeconds, Vector2 roomSize, PlayerLoadout loadout)
        {
            var radius = Mathf.Min(roomSize.x, roomSize.y) * 0.18f;
            var speed = loadout.movementSkill * 0.8f;
            return new Vector2(Mathf.Cos(timeSeconds * speed), Mathf.Sin(timeSeconds * speed * 0.73f)) * radius;
        }

        private static void SimulateEnemyMovement(SimEnemy enemy, Vector2 playerPosition, CombatEncounterScenario scenario, System.Random random, float elapsed, CombatEncounterFrame frame)
        {
            if (enemy.definition.SpeedMetersPerSecond <= 0.01f || enemy.definition.MovementMode == EnemyMovementMode.Flying)
            {
                enemy.isStuck = false;
                return;
            }

            var desired = DesiredDistance(enemy);
            var delta = playerPosition - enemy.position;
            var distance = Mathf.Max(0.001f, delta.magnitude);
            var direction = delta / distance;
            var shouldApproach = distance > desired + 0.25f;
            var shouldRetreat = distance < Mathf.Max(PlayerRadiusMeters + enemy.definition.RadiusMeters + 0.08f, desired - 0.35f) &&
                                enemy.definition.Disposition is EnemyInstinctDisposition.Prey or EnemyInstinctDisposition.Sentinel;
            var moveSign = shouldApproach ? 1f : shouldRetreat ? -0.55f : 0.12f;
            var clutter = scenario.obstacleDensity + Mathf.Clamp01(frame.entities.Count(entity => entity.alive) / 30f) * 0.35f;
            var stuckRisk = enemy.definition.MovementMode == EnemyMovementMode.Grounded
                ? clutter * (scenario.usePathfinding ? 0.12f : 0.42f) * Mathf.Clamp01(distance / 8f)
                : 0f;
            enemy.isStuck = shouldApproach && random.NextDouble() < stuckRisk * scenario.tickSeconds;
            if (enemy.isStuck)
            {
                enemy.stuckSeconds += scenario.tickSeconds;
                return;
            }

            var side = new Vector2(-direction.y, direction.x) * Mathf.Sin(elapsed * 1.7f + enemy.position.x * 0.43f) * 0.2f;
            var move = (direction * moveSign + side).normalized * enemy.definition.SpeedMetersPerSecond * scenario.tickSeconds;
            enemy.position += move;
            var half = scenario.RoomSizeMeters * 0.5f;
            enemy.position.x = Mathf.Clamp(enemy.position.x, -half.x + enemy.definition.RadiusMeters, half.x - enemy.definition.RadiusMeters);
            enemy.position.y = Mathf.Clamp(enemy.position.y, -half.y + enemy.definition.RadiusMeters, half.y - enemy.definition.RadiusMeters);
        }

        private static void SimulatePathRequest(
            SimEnemy enemy,
            CombatEncounterScenario scenario,
            float elapsed,
            int requestBudget,
            ref int framePathRequests,
            ref float frameSolveMs,
            CombatEncounterFrame frame)
        {
            if (!scenario.usePathfinding ||
                enemy.definition.MovementMode != EnemyMovementMode.Grounded ||
                enemy.definition.SpeedMetersPerSecond <= 0.01f ||
                elapsed < enemy.nextPathTime)
            {
                return;
            }

            var priority = (int)enemy.definition.Intelligence + (enemy.isStuck ? 3 : 0) + (enemy.playerDistance < 4f ? 2 : 0);
            if (framePathRequests >= requestBudget && priority < 6)
            {
                enemy.deferredPathRequests++;
                frame.deferredPathRequests++;
                enemy.nextPathTime = elapsed + 0.18f;
                return;
            }

            framePathRequests++;
            enemy.pathRequests++;
            var solveMs = EstimatePathSolveMs(enemy, scenario);
            enemy.estimatedPathSolveMs += solveMs;
            frameSolveMs += solveMs;
            enemy.nextPathTime = elapsed + RepathCadence(enemy, scenario);
        }

        private static bool CanCommitAttack(SimEnemy enemy, CombatEncounterScenario scenario)
        {
            if (enemy.attackProfile == null)
            {
                return false;
            }

            var range = AttackCommitRange(enemy);
            if (enemy.definition.Disposition == EnemyInstinctDisposition.Prey && enemy.attackStarts == 0 && enemy.playerDistance > range * 0.85f)
            {
                return false;
            }

            return enemy.playerDistance <= range;
        }

        private static float AttackCommitRange(SimEnemy enemy)
        {
            if (enemy.attackProfile == null)
            {
                return enemy.definition.AttackRangeMeters;
            }

            var range = enemy.attackProfile.RangeMeters > 0.05f ? enemy.attackProfile.RangeMeters : enemy.definition.AttackRangeMeters;
            if (enemy.attackProfile.RuntimeKind is EnemyAttackRuntimeKind.Projectile or EnemyAttackRuntimeKind.FanProjectile or EnemyAttackRuntimeKind.RadialProjectile or EnemyAttackRuntimeKind.Beam)
            {
                return Mathf.Max(range, enemy.definition.AttackRangeMeters);
            }

            return Mathf.Max(range, enemy.definition.LungeTriggerRangeMeters, enemy.definition.PreferredRangeMinMeters + 0.25f);
        }

        private static float DesiredDistance(SimEnemy enemy)
        {
            var actionRange = AttackCommitRange(enemy);
            if (enemy.attackProfile?.RuntimeKind is EnemyAttackRuntimeKind.Projectile or EnemyAttackRuntimeKind.FanProjectile or EnemyAttackRuntimeKind.RadialProjectile or EnemyAttackRuntimeKind.Beam)
            {
                return Mathf.Clamp(actionRange * 0.8f, enemy.definition.PreferredRangeMinMeters, enemy.definition.PreferredRangeMaxMeters);
            }

            return Mathf.Clamp(actionRange * 0.78f, 0.65f, enemy.definition.PreferredRangeMaxMeters);
        }

        private static void SimulatePlayerDamage(
            IReadOnlyList<SimEnemy> enemies,
            Vector2 playerPosition,
            PlayerLoadout loadout,
            System.Random random,
            float deltaSeconds,
            ref float playerAttackTimer)
        {
            playerAttackTimer -= deltaSeconds;
            if (playerAttackTimer > 0f)
            {
                return;
            }

            var target = enemies
                .Where(enemy => enemy.alive)
                .OrderBy(enemy => Vector2.Distance(enemy.position, playerPosition))
                .FirstOrDefault();
            if (target == null)
            {
                return;
            }

            playerAttackTimer = loadout.attackCooldownSeconds;
            var distance = Vector2.Distance(target.position, playerPosition);
            var hitChance = Mathf.Clamp01(loadout.hitChance - Mathf.Max(0f, distance - loadout.attackReachMeters) * 0.12f);
            if (random.NextDouble() > hitChance)
            {
                return;
            }

            target.health -= loadout.damagePerHit;
            if (target.health <= 0f)
            {
                target.alive = false;
            }
        }

        private static void AddLanePressure(CombatEncounterFrame frame, CombatEncounterPressureLane lane, float pressure)
        {
            switch (lane)
            {
                case CombatEncounterPressureLane.Ranged:
                    frame.rangedPressure += pressure;
                    break;
                case CombatEncounterPressureLane.Area:
                    frame.areaPressure += pressure;
                    break;
                case CombatEncounterPressureLane.Charge:
                    frame.chargePressure += pressure;
                    break;
                default:
                    frame.meleePressure += pressure;
                    break;
            }
        }

        private static CombatEncounterPressureLane LaneFor(EnemyAttackProfileDefinition attack)
        {
            if (attack == null)
            {
                return CombatEncounterPressureLane.Melee;
            }

            return attack.RuntimeKind switch
            {
                EnemyAttackRuntimeKind.Projectile or EnemyAttackRuntimeKind.FanProjectile or EnemyAttackRuntimeKind.RadialProjectile or EnemyAttackRuntimeKind.Beam => CombatEncounterPressureLane.Ranged,
                EnemyAttackRuntimeKind.Area => CombatEncounterPressureLane.Area,
                EnemyAttackRuntimeKind.Charge => CombatEncounterPressureLane.Charge,
                _ => CombatEncounterPressureLane.Melee
            };
        }

        private static float PressureFor(EnemyAttackProfileDefinition attack, EnemyDefinition definition)
        {
            if (attack == null)
            {
                return 0f;
            }

            var timing = Mathf.Max(0.2f, attack.WindupSeconds + attack.ActiveSeconds + attack.RecoverySeconds);
            var projectileBonus = attack.ProjectileCount > 1 ? 1f + (attack.ProjectileCount - 1) * 0.18f : 1f;
            var force = ForceMultiplier(attack.ForceClass);
            var intelligence = 1f + (int)definition.Intelligence * 0.035f;
            return (attack.Damage + 0.35f) * force * projectileBonus * intelligence / timing;
        }

        private static float PressurePenalty(CombatEncounterPressureLane lane, CombatEncounterFrame frame)
        {
            var current = lane switch
            {
                CombatEncounterPressureLane.Ranged => frame.rangedPressure,
                CombatEncounterPressureLane.Area => frame.areaPressure,
                CombatEncounterPressureLane.Charge => frame.chargePressure,
                _ => frame.meleePressure
            };
            var softCap = lane switch
            {
                CombatEncounterPressureLane.Ranged => 3.2f,
                CombatEncounterPressureLane.Area => 2.2f,
                CombatEncounterPressureLane.Charge => 1.7f,
                _ => 4.5f
            };
            return Mathf.Clamp01((current - softCap) / Mathf.Max(softCap, 0.01f)) * 0.65f;
        }

        private static float IntelligenceCommitBonus(EnemyDefinition definition)
        {
            return definition.Intelligence switch
            {
                EnemyIntelligenceLevel.Tactical => 0.04f,
                EnemyIntelligenceLevel.Cunning => 0.07f,
                EnemyIntelligenceLevel.Instinctive => -0.06f,
                _ => 0f
            };
        }

        private static float HitChanceFor(SimEnemy enemy, CombatEncounterScenario scenario, PlayerLoadout loadout, CombatEncounterFrame frame)
        {
            var attack = enemy.attackProfile;
            var telegraph = attack.WindupSeconds + attack.RecoverySeconds * 0.5f;
            var baseChance = LaneFor(attack) switch
            {
                CombatEncounterPressureLane.Ranged => 0.28f,
                CombatEncounterPressureLane.Area => 0.36f,
                CombatEncounterPressureLane.Charge => 0.33f,
                _ => 0.42f
            };
            var pressureBonus = Mathf.Clamp01((frame.meleePressure + frame.rangedPressure + frame.areaPressure + frame.chargePressure) / 7f) * 0.16f;
            var difficultyBonus = scenario.difficulty switch
            {
                CombatEncounterDifficulty.Easy => -0.08f,
                CombatEncounterDifficulty.Hard => 0.08f,
                CombatEncounterDifficulty.StressTest => 0.14f,
                _ => 0f
            };
            var telegraphPenalty = Mathf.Clamp01(telegraph / 1.2f) * 0.18f;
            var movementPenalty = loadout.movementSkill * 0.06f;
            return Mathf.Clamp01(baseChance + pressureBonus + difficultyBonus - telegraphPenalty - movementPenalty);
        }

        private static int DamageFor(EnemyAttackProfileDefinition attack, CombatEncounterDifficulty difficulty)
        {
            var damage = attack != null ? attack.Damage : 1;
            if (difficulty == CombatEncounterDifficulty.StressTest && damage > 0)
            {
                damage += 1;
            }

            return Mathf.Max(0, damage);
        }

        private static float CooldownFor(EnemyAttackProfileDefinition attack, CombatEncounterDifficulty difficulty, EnemyDefinition definition)
        {
            var cooldown = attack.CooldownSeconds;
            cooldown *= difficulty switch
            {
                CombatEncounterDifficulty.Easy => 1.15f,
                CombatEncounterDifficulty.Hard => 0.92f,
                CombatEncounterDifficulty.StressTest => 0.78f,
                _ => 1f
            };
            if (definition.Disposition == EnemyInstinctDisposition.Prey)
            {
                cooldown *= 1.15f;
            }

            return Mathf.Max(0.15f, cooldown);
        }

        private static int PathRequestBudgetFor(CombatEncounterScenario scenario, int enemyCount, float tickSeconds)
        {
            var perSecond = scenario.difficulty == CombatEncounterDifficulty.StressTest ? 90f : 64f;
            perSecond += Mathf.Clamp(enemyCount - 8, 0, 24) * 1.3f;
            return Mathf.Max(1, Mathf.RoundToInt(perSecond * tickSeconds));
        }

        private static float RepathCadence(SimEnemy enemy, CombatEncounterScenario scenario)
        {
            var baseCadence = enemy.definition.Intelligence switch
            {
                EnemyIntelligenceLevel.Cunning => 0.26f,
                EnemyIntelligenceLevel.Tactical => 0.32f,
                EnemyIntelligenceLevel.Trained => 0.42f,
                EnemyIntelligenceLevel.Basic => 0.55f,
                EnemyIntelligenceLevel.Simple => 0.72f,
                _ => 0.9f
            };
            if (enemy.isStuck)
            {
                baseCadence *= 0.45f;
            }

            return Mathf.Clamp(baseCadence + scenario.obstacleDensity * 0.12f, 0.18f, 1.2f);
        }

        private static float EstimatePathSolveMs(SimEnemy enemy, CombatEncounterScenario scenario)
        {
            var room = scenario.RoomSizeMeters;
            var nodes = Mathf.Clamp((room.x * room.y) / 0.25f, 120f, 3200f);
            var clutter = 1f + scenario.obstacleDensity * 2.8f;
            var intelligence = 0.8f + (int)enemy.definition.Intelligence * 0.08f;
            var distance = 0.75f + Mathf.Clamp01(enemy.playerDistance / Mathf.Max(room.x, room.y));
            return nodes * 0.00022f * clutter * intelligence * distance;
        }

        private static float ForceMultiplier(ImpactForceClass forceClass)
        {
            return forceClass switch
            {
                ImpactForceClass.Medium => 1.25f,
                ImpactForceClass.Heavy => 1.65f,
                ImpactForceClass.Massive => 2.1f,
                _ => 1f
            };
        }

        private static List<CombatEncounterEnemyTypeMetrics> BuildEnemyMetrics(IReadOnlyList<SimEnemy> enemies, float durationSeconds)
        {
            return enemies
                .GroupBy(enemy => enemy.definition.SpawnKind)
                .Select(group => new CombatEncounterEnemyTypeMetrics
                {
                    spawnKind = group.Key,
                    displayName = group.First().definition.DisplayName,
                    count = group.Count(),
                    deaths = group.Count(enemy => !enemy.alive),
                    attackStarts = group.Sum(enemy => enemy.attackStarts),
                    hits = group.Sum(enemy => enemy.hits),
                    damageDealt = group.Sum(enemy => enemy.damageDealt),
                    stuckSeconds = group.Sum(enemy => enemy.stuckSeconds),
                    pathRequests = group.Sum(enemy => enemy.pathRequests),
                    deferredPathRequests = group.Sum(enemy => enemy.deferredPathRequests),
                    estimatedPathSolveMs = group.Sum(enemy => enemy.estimatedPathSolveMs)
                })
                .OrderByDescending(metric => metric.damageDealt)
                .ThenByDescending(metric => metric.attackStarts)
                .ThenBy(metric => metric.displayName)
                .ToList();
        }

        private static void PopulateWarningsAndRecommendations(CombatEncounterSimulationResult result)
        {
            if (result.totalEnemies <= 0)
            {
                result.warnings.Add("No enemies were included in the simulation.");
                result.recommendations.Add("Add at least one enemy group before using this scenario for balancing.");
                return;
            }

            var attacksPerSecond = result.durationSeconds <= 0f ? 0f : result.totalAttackStarts / result.durationSeconds;
            if (result.playerDied && result.durationSeconds < result.scenario.durationSeconds * 0.65f)
            {
                result.warnings.Add("The player died early in the simulation.");
                var top = result.enemyMetrics.OrderByDescending(metric => metric.damageDealt).FirstOrDefault();
                if (top != null)
                {
                    result.recommendations.Add($"Reduce `{top.displayName}` by 1-2 enemies, delay its first attack, or increase its recovery/cooldown before adding more pressure.");
                }
            }

            if (attacksPerSecond < 0.35f && result.totalEnemies >= 3)
            {
                result.warnings.Add("Enemies started very few attacks for the encounter size.");
                result.recommendations.Add("Check action spacing and behavior-tree gates: enemies may still be hovering outside commit range instead of entering active windows.");
            }
            else if (attacksPerSecond > 3.25f)
            {
                result.warnings.Add("Attack frequency is very high.");
                result.recommendations.Add("Use softer pressure caps, larger spawn spread, or longer recovery on the top attacking enemy to preserve Souls-like readability.");
            }

            if (result.rangedPressureShare > 0.62f && result.totalAttackStarts > 6)
            {
                result.warnings.Add("Ranged pressure dominates the fight.");
                result.recommendations.Add("Move ranged enemies farther from doors but give them clearer cover gaps, or reduce simultaneous ranged groups by one unit.");
            }

            if (result.areaPressureShare > 0.38f)
            {
                result.warnings.Add("Area pressure is unusually high.");
                result.recommendations.Add("Add longer windups/recovery on area actions or reduce overlap between area enemies and melee swarms.");
            }

            if (result.stuckSeconds > result.totalEnemies * 1.5f)
            {
                result.warnings.Add("Enemies spent significant time stuck or blocked.");
                result.recommendations.Add("Spread spawn points, widen lanes around rocks, or review M92 path goals for this room before tuning damage.");
            }

            if (result.pathRequestsPerSecond > 55f || result.averagePathSolveMs > 0.42f || result.totalDeferredPathRequests > 0)
            {
                result.warnings.Add("Pathfinding load is high for an encounter simulation.");
                result.recommendations.Add("Lower low-intelligence repath cadence, reduce obstacle density near spawn clusters, or split the room into smaller pressure waves.");
            }

            foreach (var metric in result.enemyMetrics)
            {
                if (metric.count >= 3 && metric.attackStarts == 0)
                {
                    result.recommendations.Add($"`{metric.displayName}` never attacked. Verify its action profile, awareness gate, minimum range, and behavior tree attack branch.");
                }

                if (metric.count >= 2 && metric.HitRate > 0.55f)
                {
                    result.recommendations.Add($"`{metric.displayName}` hit rate is high ({metric.HitRate:P0}). Increase telegraph, recovery, or player dodge space.");
                }

                if (metric.stuckSeconds > metric.count * 2.2f)
                {
                    result.recommendations.Add($"`{metric.displayName}` is the main stuck source. Try wider spawn spread or less blocker-adjacent placement.");
                }
            }

            if (result.recommendations.Count == 0)
            {
                result.recommendations.Add("Encounter looks readable in this deterministic pass. Watch one sandbox playback seed before approving.");
            }
        }

        private static void PopulateBatchRecommendations(CombatEncounterBatchResult batch)
        {
            if (batch.Runs == 0)
            {
                return;
            }

            if (batch.SurvivalRate < 0.35f)
            {
                batch.recommendations.Add("Survival rate is low across seeds. Reduce the highest-damage group count or delay initial engagement.");
            }
            else if (batch.SurvivalRate > 0.92f && batch.AverageEnemyDeaths >= batch.scenario.enemyGroups.Sum(group => group.count) * 0.8f)
            {
                batch.recommendations.Add("Encounter is consistently safe. Add one support enemy, improve ranged angles, or shorten recovery on a low-threat action.");
            }

            if (batch.AverageStuckSeconds > batch.scenario.enemyGroups.Sum(group => group.count) * 1.2f)
            {
                batch.recommendations.Add("Stuck time is consistently high. Fix room geometry/path envelopes before adjusting damage.");
            }

            if (batch.AveragePathRequestsPerSecond > 50f || batch.AveragePathSolveMs > 0.4f)
            {
                batch.recommendations.Add("Pathfinding cost is high in batch. Use staggered waves, lower repath cadence, or reduce blocker density.");
            }

            var topDamage = batch.results
                .SelectMany(result => result.enemyMetrics)
                .GroupBy(metric => metric.spawnKind)
                .Select(group => new
                {
                    SpawnKind = group.Key,
                    Name = group.First().displayName,
                    Damage = group.Average(metric => metric.damageDealt),
                    Stuck = group.Average(metric => metric.stuckSeconds)
                })
                .OrderByDescending(row => row.Damage)
                .FirstOrDefault();
            if (topDamage != null && topDamage.Damage > batch.AverageDamageTaken * 0.55f)
            {
                batch.recommendations.Add($"`{topDamage.Name}` is carrying most damage. Tune it first before changing the whole encounter.");
            }

            if (batch.recommendations.Count == 0)
            {
                batch.recommendations.Add("Batch variance looks stable. Use visible sandbox playback to confirm readability and spawn placement.");
            }
        }

        private static string Sanitize(string value)
        {
            var safe = string.IsNullOrWhiteSpace(value) ? "encounter_simulation" : value.Trim().ToLowerInvariant();
            foreach (var invalid in Path.GetInvalidFileNameChars())
            {
                safe = safe.Replace(invalid, '_');
            }

            return safe.Replace(' ', '_');
        }

        private sealed class SimEnemy
        {
            public EnemyDefinition definition;
            public CombatEncounterEnemyGroup group;
            public EnemyAttackProfileDefinition attackProfile;
            public Vector2 position;
            public float health;
            public bool alive;
            public bool aiEnabled;
            public float cooldownSeconds;
            public float attackFlashTimer;
            public float nextPathTime;
            public float playerDistance;
            public bool isStuck;
            public float stuckSeconds;
            public int attackStarts;
            public int hits;
            public int damageDealt;
            public int pathRequests;
            public int deferredPathRequests;
            public float estimatedPathSolveMs;
        }

        private readonly struct PlayerLoadout
        {
            public readonly float maxHealth;
            public readonly float damagePerHit;
            public readonly float attackCooldownSeconds;
            public readonly float attackReachMeters;
            public readonly float hitChance;
            public readonly float movementSkill;

            private PlayerLoadout(float maxHealth, float damagePerHit, float attackCooldownSeconds, float attackReachMeters, float hitChance, float movementSkill)
            {
                this.maxHealth = maxHealth;
                this.damagePerHit = damagePerHit;
                this.attackCooldownSeconds = attackCooldownSeconds;
                this.attackReachMeters = attackReachMeters;
                this.hitChance = hitChance;
                this.movementSkill = movementSkill;
            }

            public static PlayerLoadout FromId(string id, CombatEncounterDifficulty difficulty)
            {
                var loadout = id == "heavy"
                    ? new PlayerLoadout(DefaultPlayerMaxHealth + 2f, 2f, 0.82f, 1.35f, 0.78f, 0.75f)
                    : new PlayerLoadout(DefaultPlayerMaxHealth, 1f, 0.48f, 1.18f, 0.84f, 1f);
                return difficulty switch
                {
                    CombatEncounterDifficulty.Easy => new PlayerLoadout(loadout.maxHealth + 1f, loadout.damagePerHit, loadout.attackCooldownSeconds * 0.92f, loadout.attackReachMeters, Mathf.Min(0.95f, loadout.hitChance + 0.06f), loadout.movementSkill + 0.1f),
                    CombatEncounterDifficulty.Hard => new PlayerLoadout(loadout.maxHealth, loadout.damagePerHit, loadout.attackCooldownSeconds * 1.05f, loadout.attackReachMeters, loadout.hitChance - 0.06f, loadout.movementSkill - 0.05f),
                    CombatEncounterDifficulty.StressTest => new PlayerLoadout(loadout.maxHealth - 1f, loadout.damagePerHit, loadout.attackCooldownSeconds * 1.12f, loadout.attackReachMeters, loadout.hitChance - 0.12f, loadout.movementSkill - 0.12f),
                    _ => loadout
                };
            }
        }
    }
}
