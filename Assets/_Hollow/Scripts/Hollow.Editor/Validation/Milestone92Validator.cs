using System.Collections.Generic;
using System.IO;
using System.Linq;
using Hollow.Combat;
using Hollow.Editor.Generation;
using Hollow.Rooms;
using UnityEditor;
using UnityEngine;

namespace Hollow.Editor.Validation
{
    public static class Milestone92Validator
    {
        private const string SampleRoomPath = "Assets/_Hollow/Data/Rooms/Templates/combat_single_sample.hollowruntime.json";

        private static readonly string[] RequiredDocsText =
        {
            "Pathfinding Backend Adapter",
            "RoomGridAStar",
            "0.5m",
            "action envelopes",
            "local steering fallback",
            "Boss runtime behavior remains unchanged"
        };

        [MenuItem("Hollow/Validation/Run Milestone 92 Validation")]
        public static bool Validate()
        {
            AssetDatabase.Refresh();
            var failures = new List<string>();
            ValidateFiles(failures);
            ValidateBackendContract(failures);
            ValidateRosterRouting(failures);
            ValidateSamplePath(failures);

            if (failures.Count == 0)
            {
                Debug.Log("Milestone 92 validation passed.");
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
            ExpectFile(Milestone92AssetGenerator.DocsPath, failures);
            ExpectFile(Milestone92AssetGenerator.ReportPath, failures);
            if (!File.Exists(Milestone92AssetGenerator.DocsPath))
            {
                return;
            }

            var docs = File.ReadAllText(Milestone92AssetGenerator.DocsPath);
            foreach (var required in RequiredDocsText)
            {
                if (!docs.Contains(required))
                {
                    failures.Add($"M92 docs are missing `{required}`.");
                }
            }
        }

        private static void ValidateBackendContract(List<string> failures)
        {
            if (EnemyNavigationAdapter.CurrentBackend != EnemyNavigationBackend.LocalSteering)
            {
                failures.Add("M92 must preserve the M88 LocalSteering compatibility constant.");
            }

            if (RoomGridAStarPathfinder.CellSizeMeters <= 0f)
            {
                failures.Add("RoomGridAStar must expose a positive grid cell size.");
            }

            if (EnemyNavigationAdapter.DefaultModeFor(EnemyMovementMode.Grounded) != EnemyNavigationMode.GroundedLocal ||
                EnemyNavigationAdapter.DefaultModeFor(EnemyMovementMode.Flying) != EnemyNavigationMode.FlyingLocal)
            {
                failures.Add("M92 should not rewrite movement-mode defaults.");
            }
        }

        private static void ValidateRosterRouting(List<string> failures)
        {
            var enemies = EnemyCatalog.CreateRuntimeDefault()
                .Definitions
                .Where(enemy => enemy != null && enemy.SpawnKind != "spawnEnemyBoss")
                .ToArray();
            if (enemies.Length < 20)
            {
                failures.Add("M92 expected the expanded non-boss roster to be present.");
            }

            foreach (var enemy in enemies)
            {
                if (enemy.MovementMode == EnemyMovementMode.Grounded && enemy.SpeedMetersPerSecond > 0f && enemy.SpacingProfile == null)
                {
                    failures.Add($"{enemy.SpawnKind} is grounded/mobile but lacks the M91 spacing profile needed for path goals.");
                }

                if (enemy.MovementMode == EnemyMovementMode.Flying && enemy.SpeedMetersPerSecond <= 0f)
                {
                    failures.Add($"{enemy.SpawnKind} is flying but cannot move.");
                }
            }
        }

        private static void ValidateSamplePath(List<string> failures)
        {
            if (!File.Exists(SampleRoomPath))
            {
                failures.Add($"Missing M92 sample room: {SampleRoomPath}");
                return;
            }

            var roomObject = new GameObject("M92ValidatorRoom");
            try
            {
                var room = roomObject.AddComponent<RoomRuntimeRoot>();
                room.BuildFrom(HollowRuntimeV2Importer.Import(File.ReadAllText(SampleRoomPath)));
                var current = new Vector3(0f, 0f, -2.1f);
                var finalGoal = new Vector3(0f, 0f, -0.8f);
                var desired = new Vector3(0f, 0f, -1.85f);
                var result = EnemyNavigationAdapter.Resolve(new EnemyNavigationRequest(
                    room,
                    current,
                    desired,
                    0.25f,
                    EnemyNavigationMode.GroundedLocal,
                    EnemyNavigationIntent.MoveToPlayer,
                    EnemyIntelligenceLevel.Basic,
                    allowLocalDetour: true,
                    allowPathfinding: true,
                    finalGoalLocalPosition: finalGoal,
                    maxStepDistanceMeters: 0.25f));

                if (result.Backend != EnemyNavigationBackend.RoomGridAStar)
                {
                    failures.Add("M92 sample request did not use RoomGridAStar.");
                }

                if (result.PathStatus is not (EnemyPathStatus.Ready or EnemyPathStatus.Partial))
                {
                    failures.Add($"M92 sample request returned unexpected path status `{result.PathStatus}`.");
                }

                if (result.PathWaypointCount <= 0)
                {
                    failures.Add("M92 sample request returned no waypoints.");
                }

                if (RoomLocalCollision.IntersectsObstacle(room, result.ResolvedLocalPosition, 0.25f))
                {
                    failures.Add("M92 sample path moved into an obstacle.");
                }
            }
            finally
            {
                Object.DestroyImmediate(roomObject);
            }
        }

        private static void ExpectFile(string path, List<string> failures)
        {
            if (!File.Exists(path))
            {
                failures.Add($"Missing M92 file: {path}");
            }
        }
    }
}
