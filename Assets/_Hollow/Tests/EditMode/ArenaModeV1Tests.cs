using System.Linq;
using Hollow.Combat;
using Hollow.Core.App;
using Hollow.Rooms;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Hollow.Tests.EditMode
{
    public sealed class ArenaModeV1Tests
    {
        [Test]
        public void ArenaRouteLoadsDedicatedSceneName()
        {
            Assert.AreEqual("ArenaMode", SceneLoaderService.SceneNameForRoute(AppShellRoute.ArenaMode));
        }

        [Test]
        public void StarterPresetsExistAndValidate()
        {
            var presets = AssetDatabase.FindAssets("t:ArenaModePresetDefinition", new[] { "Assets/_Hollow/Data/ArenaMode/Presets" })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(AssetDatabase.LoadAssetAtPath<ArenaModePresetDefinition>)
                .Where(preset => preset != null)
                .ToArray();

            Assert.GreaterOrEqual(presets.Length, 7);
            Assert.That(presets.Select(preset => preset.PresetId), Does.Contain("arena_small_skirmish"));
            Assert.That(presets.Select(preset => preset.PresetId), Does.Contain("arena_survival_starter"));
            Assert.That(presets.Select(preset => preset.PresetId), Does.Contain("arena_room_small_ratroom_001"));
            foreach (var preset in presets)
            {
                Assert.IsEmpty(preset.ValidateForArena(), preset.DisplayName);
                Assert.That(preset.PlayerHp, Is.InRange(ArenaModeRuntimeSettings.MinPlayerHp, ArenaModeRuntimeSettings.MaxPlayerHp));
                Assert.That(preset.PlayerDamageBonus, Is.InRange(ArenaModeRuntimeSettings.MinDamageBonus, ArenaModeRuntimeSettings.MaxDamageBonus));
                Assert.That(preset.PlayerSpeedMetersPerSecond, Is.InRange(ArenaModeRuntimeSettings.MinPlayerSpeed, ArenaModeRuntimeSettings.MaxPlayerSpeed));
            }
        }

        [Test]
        public void CuratedRatRoomPresetUsesApprovedRuntimeRoomAndRatAnchors()
        {
            var preset = AssetDatabase.LoadAssetAtPath<ArenaModePresetDefinition>("Assets/_Hollow/Data/ArenaMode/Presets/ArenaPreset_Room_Small_RatRoom_001.asset");
            Assert.NotNull(preset);
            Assert.AreEqual("arena_room_small_ratroom_001", preset.PresetId);
            Assert.AreEqual("Room_Small_RatRoom_001", preset.DisplayName);
            Assert.IsTrue(preset.SurvivalMode);
            Assert.IsTrue(preset.CuratedLocked);
            Assert.NotNull(preset.CuratedRoomRuntimeJson);

            Assert.IsTrue(HollowRuntimeV2Importer.TryImport(preset.CuratedRoomRuntimeJson.text, out var room, out var error), error);
            Assert.AreEqual("Room_Small_RatRoom_001", room.Id);
            Assert.AreEqual("Room_Small_RatRoom_001", room.DisplayName);
            Assert.GreaterOrEqual(room.EnemySpawns.Count, 8);
            Assert.That(room.EnemySpawns.Select(spawn => spawn.kind), Is.All.EqualTo("spawnEnemyRat"));

            var settings = preset.CreateRuntimeSettings();
            var spawns = ArenaModeRuntimeRoomBuilder.BuildCuratedSpawnPoints(room, settings.Waves[2].Groups, 2);
            Assert.AreEqual(7, spawns.Count);
            Assert.That(ArenaModeRuntimeRoomBuilder.SpawnKindsFor(spawns), Is.All.EqualTo("spawnEnemyRat"));
        }

        [Test]
        public void RuntimeRoomBuilderCreatesCenteredArenaWithRealSpawnAnchors()
        {
            var preset = AssetDatabase.LoadAssetAtPath<ArenaModePresetDefinition>("Assets/_Hollow/Data/ArenaMode/Presets/ArenaPreset_SmallSkirmish.asset");
            var settings = preset.CreateRuntimeSettings();
            var room = ArenaModeRuntimeRoomBuilder.BuildRoom(settings);
            var spawns = ArenaModeRuntimeRoomBuilder.BuildSpawnPoints(settings, settings.Waves[0].Groups, 0);

            Assert.NotNull(room);
            Assert.AreEqual(Vector3.zero, room.SafeStart.position.ToUnityVector3());
            Assert.Greater(room.Layout.FloorRegions.Count, 0);
            Assert.Greater(room.Layout.WalkableTiles.Count, 0);
            Assert.AreEqual(3, spawns.Count);
            Assert.That(ArenaModeRuntimeRoomBuilder.SpawnKindsFor(spawns), Is.All.EqualTo("spawnEnemyNormal"));
            Assert.That(spawns.Select(spawn => spawn.position.ToUnityVector3().magnitude), Is.All.GreaterThan(2.5f));
        }

        [Test]
        public void ArenaSceneContainsArenaControllerAndIsInBuildSettings()
        {
            const string scenePath = "Assets/_Hollow/Scenes/ArenaMode/ArenaMode.unity";
            Assert.IsTrue(System.IO.File.Exists(scenePath));
            Assert.IsTrue(EditorBuildSettings.scenes.Any(scene => scene.enabled && scene.path == scenePath));

            var activeScene = EditorSceneManager.GetActiveScene();
            var activePath = activeScene.path;
            var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            var controller = Object.FindAnyObjectByType<ArenaModeController>();
            Assert.NotNull(controller);
            Assert.GreaterOrEqual(controller.Presets.Count, 7);
            Assert.That(controller.Presets.Select(preset => preset.PresetId), Does.Contain("arena_room_small_ratroom_001"));
            if (!string.IsNullOrWhiteSpace(activePath) && activePath != scene.path)
            {
                EditorSceneManager.OpenScene(activePath, OpenSceneMode.Single);
            }
        }

        [Test]
        public void DamageAppliedEventReportsAppliedAmountForArenaScoring()
        {
            var targetObject = new GameObject("ArenaDamageTarget");
            try
            {
                var health = targetObject.AddComponent<CombatantHealth>();
                health.Configure(3);
                var applied = 0;
                health.DamageApplied += (_, _, amount) => applied += amount;

                Assert.IsTrue(health.ApplyDamage(new DamageRequest(5, targetObject)));
                Assert.AreEqual(3, applied);
            }
            finally
            {
                Object.DestroyImmediate(targetObject);
            }
        }
    }
}
