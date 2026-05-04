using System.Collections.Generic;
using System.IO;
using System.Linq;
using Hollow.Combat;
using Hollow.Core;
using Hollow.Rooms;
using Hollow.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Hollow.Editor.ArenaMode
{
    public static class ArenaModeAssetGenerator
    {
        public const string PresetFolder = "Assets/_Hollow/Data/ArenaMode/Presets";
        public const string ArenaScenePath = "Assets/_Hollow/Scenes/ArenaMode/ArenaMode.unity";
        public const string SourceGameScenePath = "Assets/_Hollow/Scenes/Game_Windows.unity";
        public const string RatRoomRuntimePath = "Assets/_Hollow/Data/Rooms/DesignerApproved/Room_Small_RatRoom_001.hollowruntime.json";
        public const string RatRoomPresetId = "arena_room_small_ratroom_001";

        [MenuItem("Hollow/Arena Mode/Generate Arena Mode Assets")]
        public static void GenerateAll()
        {
            Directory.CreateDirectory(PresetFolder);
            Directory.CreateDirectory(Path.GetDirectoryName(ArenaScenePath));
            var presetPaths = GenerateStarterPresets()
                .Select(AssetDatabase.GetAssetPath)
                .Where(path => !string.IsNullOrWhiteSpace(path))
                .ToArray();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            var presets = presetPaths
                .Select(AssetDatabase.LoadAssetAtPath<ArenaModePresetDefinition>)
                .Where(preset => preset != null)
                .ToArray();
            GenerateScene(presets);
            AddArenaSceneToBuildSettings();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"Arena Mode generated: {presets.Length} presets and {ArenaScenePath}");
        }

        [MenuItem("Hollow/Arena Mode/Validate Arena Mode Assets")]
        public static void ValidateAll()
        {
            var errors = new List<string>();
            var presets = LoadStarterPresets();
            if (presets.Count < 7)
            {
                errors.Add($"Expected at least 7 Arena Mode presets, found {presets.Count}.");
            }

            if (presets.All(preset => preset.PresetId != "arena_small_skirmish"))
            {
                errors.Add("Missing starter preset 'arena_small_skirmish'.");
            }

            if (presets.All(preset => preset.PresetId != "arena_survival_starter"))
            {
                errors.Add("Missing starter preset 'arena_survival_starter'.");
            }

            var ratRoomPreset = presets.FirstOrDefault(preset => preset.PresetId == RatRoomPresetId);
            if (ratRoomPreset == null)
            {
                errors.Add($"Missing curated preset '{RatRoomPresetId}'.");
            }
            else
            {
                if (!ratRoomPreset.CuratedLocked || !ratRoomPreset.SurvivalMode || ratRoomPreset.CuratedRoomRuntimeJson == null)
                {
                    errors.Add($"{ratRoomPreset.DisplayName}: curated rat room preset must be locked, survival-enabled, and reference a runtime room JSON.");
                }
            }

            if (!File.Exists(RatRoomRuntimePath))
            {
                errors.Add($"Curated rat room JSON is missing at {RatRoomRuntimePath}.");
            }
            else
            {
                var json = File.ReadAllText(RatRoomRuntimePath);
                if (!HollowRuntimeV2Importer.TryImport(json, out var ratRoom, out var importError))
                {
                    errors.Add($"Curated rat room JSON failed to import: {importError}");
                }
                else
                {
                    if (ratRoom.Id != "Room_Small_RatRoom_001" || ratRoom.DisplayName != "Room_Small_RatRoom_001")
                    {
                        errors.Add($"Curated rat room metadata mismatch: {ratRoom.Id} / {ratRoom.DisplayName}.");
                    }

                    if (ratRoom.EnemySpawns.Count == 0 || ratRoom.EnemySpawns.Any(spawn => spawn.kind != "spawnEnemyRat"))
                    {
                        errors.Add("Curated rat room must contain rat enemy spawn anchors only.");
                    }
                }
            }

            foreach (var preset in presets)
            {
                errors.AddRange(preset.ValidateForArena().Select(error => $"{preset.DisplayName}: {error}"));
            }

            if (!File.Exists(ArenaScenePath))
            {
                errors.Add($"Arena scene is missing at {ArenaScenePath}.");
            }

            if (EditorBuildSettings.scenes.All(scene => scene.path != ArenaScenePath || !scene.enabled))
            {
                errors.Add("Arena scene is not enabled in Editor Build Settings.");
            }

            if (File.Exists(ArenaScenePath))
            {
                var arenaSceneText = File.ReadAllText(ArenaScenePath);
                var ratRoomPresetGuid = AssetDatabase.AssetPathToGUID($"{PresetFolder}/ArenaPreset_Room_Small_RatRoom_001.asset");
                if (string.IsNullOrWhiteSpace(ratRoomPresetGuid) ||
                    !arenaSceneText.Contains(ratRoomPresetGuid, System.StringComparison.Ordinal))
                {
                    errors.Add("Arena scene does not reference ArenaPreset_Room_Small_RatRoom_001.");
                }
            }

            if (errors.Count > 0)
            {
                throw new System.InvalidOperationException(string.Join("\n", errors));
            }

            Debug.Log($"Arena Mode validation passed: {presets.Count} presets, scene registered, spawn kinds valid.");
        }

        public static IReadOnlyList<ArenaModePresetDefinition> GenerateStarterPresets()
        {
            return new[]
            {
                CreateOrUpdatePreset(
                    "ArenaPreset_SmallSkirmish",
                    "arena_small_skirmish",
                    "Small Skirmish",
                    ArenaRoomSize.Small,
                    ArenaLayoutStyle.Open,
                    ArenaObstaclePreset.None,
                    survival: false,
                    ArenaModeDefaults.CreateWave(
                        "Wave 1",
                        ArenaModeDefaults.CreateGroup("spawnEnemyNormal", 3, ArenaSpawnPattern.OuterRing, ArenaGroupingMode.LoosePack)),
                    ArenaModeDefaults.CreateWave(
                        "Wave 2",
                        ArenaModeDefaults.CreateGroup("spawnEnemyFast", 2, ArenaSpawnPattern.EdgeLanes, ArenaGroupingMode.Pairs),
                        ArenaModeDefaults.CreateGroup("spawnEnemyRat", 3, ArenaSpawnPattern.Cluster, ArenaGroupingMode.TightPack))),
                CreateOrUpdatePreset(
                    "ArenaPreset_CritterSwarm",
                    "arena_critter_swarm",
                    "Critter Swarm",
                    ArenaRoomSize.Medium,
                    ArenaLayoutStyle.Scramble,
                    ArenaObstaclePreset.LightCover,
                    survival: false,
                    ArenaModeDefaults.CreateWave(
                        "Rats",
                        ArenaModeDefaults.CreateGroup("spawnEnemyRat", 6, ArenaSpawnPattern.Scattered, ArenaGroupingMode.LoosePack)),
                    ArenaModeDefaults.CreateWave(
                        "Spiders",
                        ArenaModeDefaults.CreateGroup("spawnEnemySpider", 8, ArenaSpawnPattern.Cluster, ArenaGroupingMode.TightPack)),
                    ArenaModeDefaults.CreateWave(
                        "Mixed Critters",
                        ArenaModeDefaults.CreateGroup("spawnEnemyRat", 4, ArenaSpawnPattern.EdgeLanes, ArenaGroupingMode.Pairs),
                        ArenaModeDefaults.CreateGroup("spawnEnemySpider", 6, ArenaSpawnPattern.OuterRing, ArenaGroupingMode.LoosePack))),
                CreateOrUpdatePreset(
                    "ArenaPreset_RangedPressure",
                    "arena_ranged_pressure",
                    "Ranged Pressure",
                    ArenaRoomSize.Large,
                    ArenaLayoutStyle.Cover,
                    ArenaObstaclePreset.RockField,
                    survival: false,
                    ArenaModeDefaults.CreateWave(
                        "Backline",
                        ArenaModeDefaults.CreateGroup("spawnEnemyHollowArcher", 3, ArenaSpawnPattern.RangedBackline, ArenaGroupingMode.LoosePack),
                        ArenaModeDefaults.CreateGroup("spawnEnemyNormal", 3, ArenaSpawnPattern.CenterRing, ArenaGroupingMode.LoosePack)),
                    ArenaModeDefaults.CreateWave(
                        "Fireline",
                        ArenaModeDefaults.CreateGroup("spawnEnemyPowderGunner", 2, ArenaSpawnPattern.RangedBackline, ArenaGroupingMode.Pairs),
                        ArenaModeDefaults.CreateGroup("spawnEnemyKnifeThrower", 3, ArenaSpawnPattern.EdgeLanes, ArenaGroupingMode.LoosePack))),
                CreateOrUpdatePreset(
                    "ArenaPreset_WeaponUserDuel",
                    "arena_weapon_user_duel",
                    "Weapon-User Duel",
                    ArenaRoomSize.Medium,
                    ArenaLayoutStyle.Pillars,
                    ArenaObstaclePreset.Pillars,
                    survival: false,
                    ArenaModeDefaults.CreateWave(
                        "Skeleton Patrol",
                        ArenaModeDefaults.CreateGroup("spawnEnemySkeletonSword", 2, ArenaSpawnPattern.PatrolLine, ArenaGroupingMode.Pairs, ArenaPatrolIntent.PatrolLine),
                        ArenaModeDefaults.CreateGroup("spawnEnemySkeletonSpear", 2, ArenaSpawnPattern.EdgeLanes, ArenaGroupingMode.Pairs, ArenaPatrolIntent.GuardPoint)),
                    ArenaModeDefaults.CreateWave(
                        "Knight Line",
                        ArenaModeDefaults.CreateGroup("spawnEnemyKnight", 1, ArenaSpawnPattern.OuterRing, ArenaGroupingMode.Solo, ArenaPatrolIntent.Hold),
                        ArenaModeDefaults.CreateGroup("spawnEnemySkeletonSpear", 2, ArenaSpawnPattern.EdgeLanes, ArenaGroupingMode.Pairs))),
                CreateOrUpdatePreset(
                    "ArenaPreset_MixedChaos",
                    "arena_mixed_chaos",
                    "Mixed Chaos",
                    ArenaRoomSize.Large,
                    ArenaLayoutStyle.Scramble,
                    ArenaObstaclePreset.HazardLanes,
                    survival: false,
                    ArenaModeDefaults.CreateWave(
                        "Creatures",
                        ArenaModeDefaults.CreateGroup("spawnEnemyHollowBeast", 2, ArenaSpawnPattern.OuterRing, ArenaGroupingMode.Pairs),
                        ArenaModeDefaults.CreateGroup("spawnEnemyHollowBird", 3, ArenaSpawnPattern.Scattered, ArenaGroupingMode.LoosePack)),
                    ArenaModeDefaults.CreateWave(
                        "Ranged And Bodies",
                        ArenaModeDefaults.CreateGroup("spawnEnemySpittingPod", 1, ArenaSpawnPattern.RangedBackline, ArenaGroupingMode.Solo, ArenaPatrolIntent.Hold),
                        ArenaModeDefaults.CreateGroup("spawnEnemyFast", 3, ArenaSpawnPattern.CenterRing, ArenaGroupingMode.LoosePack),
                        ArenaModeDefaults.CreateGroup("spawnEnemyHollowArcher", 2, ArenaSpawnPattern.EdgeLanes, ArenaGroupingMode.Pairs))),
                CreateOrUpdatePreset(
                    "ArenaPreset_SurvivalStarter",
                    "arena_survival_starter",
                    "Survival Starter",
                    ArenaRoomSize.Medium,
                    ArenaLayoutStyle.Cover,
                    ArenaObstaclePreset.LightCover,
                    survival: true,
                    ArenaModeDefaults.CreateWave(
                        "Survival A",
                        ArenaModeDefaults.CreateGroup("spawnEnemyNormal", 3, ArenaSpawnPattern.OuterRing, ArenaGroupingMode.LoosePack),
                        ArenaModeDefaults.CreateGroup("spawnEnemyRat", 3, ArenaSpawnPattern.Cluster, ArenaGroupingMode.TightPack)),
                    ArenaModeDefaults.CreateWave(
                        "Survival B",
                        ArenaModeDefaults.CreateGroup("spawnEnemySpider", 5, ArenaSpawnPattern.Scattered, ArenaGroupingMode.LoosePack),
                        ArenaModeDefaults.CreateGroup("spawnEnemyHollowArcher", 1, ArenaSpawnPattern.RangedBackline, ArenaGroupingMode.Solo))),
                CreateOrUpdatePreset(
                    "ArenaPreset_Room_Small_RatRoom_001",
                    RatRoomPresetId,
                    "Room_Small_RatRoom_001",
                    ArenaRoomSize.Small,
                    ArenaLayoutStyle.Cover,
                    ArenaObstaclePreset.None,
                    survival: true,
                    curatedRoomRuntimeJson: AssetDatabase.LoadAssetAtPath<TextAsset>(RatRoomRuntimePath),
                    curatedLocked: true,
                    ArenaModeDefaults.CreateWave(
                        "Rats I",
                        ArenaModeDefaults.CreateGroup("spawnEnemyRat", 3, ArenaSpawnPattern.Scattered, ArenaGroupingMode.LoosePack)),
                    ArenaModeDefaults.CreateWave(
                        "Rats II",
                        ArenaModeDefaults.CreateGroup("spawnEnemyRat", 5, ArenaSpawnPattern.Scattered, ArenaGroupingMode.LoosePack)),
                    ArenaModeDefaults.CreateWave(
                        "Rats III",
                        ArenaModeDefaults.CreateGroup("spawnEnemyRat", 7, ArenaSpawnPattern.Scattered, ArenaGroupingMode.LoosePack)))
            };
        }

        public static void GenerateScene(IReadOnlyList<ArenaModePresetDefinition> presets)
        {
            if (!File.Exists(SourceGameScenePath))
            {
                throw new FileNotFoundException("Arena Mode source scene is missing.", SourceGameScenePath);
            }

            var presetPaths = (presets ?? System.Array.Empty<ArenaModePresetDefinition>())
                .Select(AssetDatabase.GetAssetPath)
                .Where(path => !string.IsNullOrWhiteSpace(path))
                .ToArray();

            var scene = EditorSceneManager.OpenScene(SourceGameScenePath, OpenSceneMode.Single);
            EditorSceneManager.SaveScene(scene, ArenaScenePath, saveAsCopy: true);
            scene = EditorSceneManager.OpenScene(ArenaScenePath, OpenSceneMode.Single);

            var session = Object.FindAnyObjectByType<GameSessionController>();
            if (session != null)
            {
                var serializedSession = new SerializedObject(session);
                serializedSession.FindProperty("sessionMode").enumValueIndex = (int)RuntimeSessionMode.TransientArena;
                serializedSession.FindProperty("sampleRoomRuntimeJson").objectReferenceValue = null;
                serializedSession.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(session);
            }

            var combat = Object.FindAnyObjectByType<RoomCombatController>();
            if (combat != null)
            {
                var serializedCombat = new SerializedObject(combat);
                serializedCombat.FindProperty("autoInitialize").boolValue = false;
                serializedCombat.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(combat);
            }

            var root = GameObject.Find("GameSessionRoot") ?? new GameObject("GameSessionRoot");
            var controller = root.GetComponent<ArenaModeController>() ?? root.AddComponent<ArenaModeController>();
            var resolvedPresets = presetPaths
                .Select(AssetDatabase.LoadAssetAtPath<ArenaModePresetDefinition>)
                .Where(preset => preset != null)
                .ToArray();
            controller.ConfigureArenaPresetsForEditor(resolvedPresets, nextShowSetupOnStart: true);
            EditorUtility.SetDirty(controller);

            EditorSceneManager.SaveScene(scene, ArenaScenePath);
        }

        public static void AddArenaSceneToBuildSettings()
        {
            var scenes = EditorBuildSettings.scenes.ToList();
            if (scenes.Any(scene => scene.path == ArenaScenePath))
            {
                return;
            }

            scenes.Add(new EditorBuildSettingsScene(ArenaScenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }

        private static ArenaModePresetDefinition CreateOrUpdatePreset(
            string assetName,
            string presetId,
            string displayName,
            ArenaRoomSize roomSize,
            ArenaLayoutStyle layoutStyle,
            ArenaObstaclePreset obstaclePreset,
            bool survival,
            params ArenaModeWaveDefinition[] waves)
        {
            return CreateOrUpdatePreset(
                assetName,
                presetId,
                displayName,
                roomSize,
                layoutStyle,
                obstaclePreset,
                survival,
                null,
                false,
                waves);
        }

        private static ArenaModePresetDefinition CreateOrUpdatePreset(
            string assetName,
            string presetId,
            string displayName,
            ArenaRoomSize roomSize,
            ArenaLayoutStyle layoutStyle,
            ArenaObstaclePreset obstaclePreset,
            bool survival,
            TextAsset curatedRoomRuntimeJson,
            bool curatedLocked,
            params ArenaModeWaveDefinition[] waves)
        {
            var path = $"{PresetFolder}/{assetName}.asset";
            var preset = AssetDatabase.LoadAssetAtPath<ArenaModePresetDefinition>(path);
            if (preset == null)
            {
                preset = ScriptableObject.CreateInstance<ArenaModePresetDefinition>();
                AssetDatabase.CreateAsset(preset, path);
            }

            preset.Configure(
                presetId,
                displayName,
                roomSize,
                layoutStyle,
                obstaclePreset,
                survival,
                RoomCombatController.PlayerMaxHealth,
                0,
                PlayerMovementController.DefaultSpeedMetersPerSecond,
                waves,
                curatedRoomRuntimeJson,
                curatedLocked);
            EditorUtility.SetDirty(preset);
            return preset;
        }

        private static IReadOnlyList<ArenaModePresetDefinition> LoadStarterPresets()
        {
            if (!Directory.Exists(PresetFolder))
            {
                return System.Array.Empty<ArenaModePresetDefinition>();
            }

            return AssetDatabase.FindAssets("t:ArenaModePresetDefinition", new[] { PresetFolder })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(AssetDatabase.LoadAssetAtPath<ArenaModePresetDefinition>)
                .Where(preset => preset != null)
                .OrderBy(preset => preset.PresetId)
                .ToArray();
        }
    }
}
