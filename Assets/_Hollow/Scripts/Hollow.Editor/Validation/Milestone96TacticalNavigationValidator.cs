using System.Collections.Generic;
using System.IO;
using System.Linq;
using Hollow.Combat;
using Hollow.Editor.Generation;
using Hollow.Entities;
using Hollow.Rooms;
using UnityEditor;
using UnityEngine;

namespace Hollow.Editor.Validation
{
    public static class Milestone96TacticalNavigationValidator
    {
        private const string SampleRoomPath = "Assets/_Hollow/Data/Rooms/Templates/combat_single_sample.hollowruntime.json";

        private static readonly string[] RequiredDocsText =
        {
            "Tactical Navigation",
            "RoomTacticalDirector",
            "EnemyTacticalIntent",
            "EnemyLocomotionAgent",
            "A* Pathfinding Project Pro",
            "Behavior Designer Pro 3",
            "Hollow data as the source of truth"
        };

        [MenuItem("Hollow/Validation/Run Milestone 96 Tactical Navigation Validation")]
        public static bool Validate()
        {
            AssetDatabase.Refresh();
            var failures = new List<string>();
            ValidateFiles(failures);
            ValidateBakeOffContract(failures);
            ValidateTacticalDirectorSample(failures);

            if (failures.Count == 0)
            {
                Debug.Log("Milestone 96 tactical navigation validation passed.");
                return true;
            }

            foreach (var failure in failures)
            {
                Debug.LogError(failure);
            }

            return false;
        }

        private static void ValidateFiles(List<string> failures)
        {
            ExpectFile(Milestone96TacticalNavigationAssetGenerator.DocsPath, failures);
            ExpectFile(Milestone96TacticalNavigationAssetGenerator.ReportPath, failures);
            if (!File.Exists(Milestone96TacticalNavigationAssetGenerator.DocsPath))
            {
                return;
            }

            var docs = File.ReadAllText(Milestone96TacticalNavigationAssetGenerator.DocsPath);
            foreach (var required in RequiredDocsText)
            {
                if (!docs.Contains(required))
                {
                    failures.Add($"M96 tactical docs are missing `{required}`.");
                }
            }
        }

        private static void ValidateBakeOffContract(List<string> failures)
        {
            if (RoomTacticalDirector.MinActiveThreatSlots != 2 || RoomTacticalDirector.MaxActiveThreatSlots != 4)
            {
                failures.Add("M96 tactical slots must remain 2-4 for the Pure Souls leaning target.");
            }

            if (!EnemyAiToolBakeOffEvaluation.HollowSourceOfTruth.Contains("Hollow"))
            {
                failures.Add("M96 must keep Hollow data as the source of truth.");
            }

            var optionNames = EnemyAiToolBakeOffEvaluation.Options.Select(option => option.Name).ToArray();
            if (!optionNames.Any(name => name.Contains("A* Pathfinding Project Pro")) ||
                !optionNames.Any(name => name.Contains("Behavior Designer Pro 3")) ||
                !optionNames.Any(name => name.Contains("Unity AI Navigation")))
            {
                failures.Add("M96 bake-off options must include A* Pro, Behavior Designer Pro 3, and Unity AI Navigation.");
            }
        }

        private static void ValidateTacticalDirectorSample(List<string> failures)
        {
            if (!File.Exists(SampleRoomPath))
            {
                failures.Add($"Missing sample room: {SampleRoomPath}");
                return;
            }

            var root = new GameObject("M96TacticalValidator");
            try
            {
                var roomObject = new GameObject("RoomRuntimeRoot");
                roomObject.transform.SetParent(root.transform, false);
                var room = roomObject.AddComponent<RoomRuntimeRoot>();
                room.BuildFrom(HollowRuntimeV2Importer.Import(File.ReadAllText(SampleRoomPath)));

                var playerObject = new GameObject("PlayerCharacter");
                playerObject.transform.SetParent(root.transform, false);
                var player = playerObject.AddComponent<PlaceholderPlayerController>();
                player.ConfigureDefault();
                player.transform.localPosition = Vector3.zero;

                var catalog = EnemyCatalog.CreateRuntimeDefault();
                var enemies = new List<EnemyRuntimeController>();
                for (var index = 0; index < 7; index++)
                {
                    var enemyObject = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                    enemyObject.transform.SetParent(root.transform, false);
                    enemyObject.transform.localPosition = Quaternion.Euler(0f, index * 52f, 0f) * Vector3.forward * 3f;
                    var enemy = enemyObject.AddComponent<EnemyRuntimeController>();
                    enemy.Configure(room, player, catalog.Resolve(index % 2 == 0 ? "spawnEnemyNormal" : "spawnEnemySkeletonSword"), DifficultyTierDefinition.CreateRuntimeDeveloperSample());
                    enemy.ConfigureSpawnContext(null, null, catalog, DifficultyTierDefinition.CreateRuntimeDeveloperSample(), new CombatDiagnosticsModel(), index);
                    enemies.Add(enemy);
                }

                var director = new RoomTacticalDirector();
                director.Tick(enemies, room, player, 4f);
                if (director.ActiveThreatCount < RoomTacticalDirector.MinActiveThreatSlots ||
                    director.ActiveThreatCount > RoomTacticalDirector.MaxActiveThreatSlots)
                {
                    failures.Add($"M96 tactical director produced invalid active threat count `{director.ActiveThreatCount}`.");
                }

                var activeReservations = enemies
                    .Select(enemy => director.ResolveIntent(enemy))
                    .Where(intent => intent.Role == EnemyTacticalRole.ActiveThreat)
                    .ToArray();
                if (activeReservations.Length != director.ActiveThreatCount)
                {
                    failures.Add("M96 tactical director active intents do not match active threat count.");
                }

                if (activeReservations.Any(intent => !intent.HasReservedPosition))
                {
                    failures.Add("M96 tactical active threats should receive reserved positions.");
                }
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        private static void ExpectFile(string path, List<string> failures)
        {
            if (!File.Exists(path))
            {
                failures.Add($"Missing M96 tactical file: {path}");
            }
        }
    }
}
