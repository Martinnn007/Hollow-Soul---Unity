using System.Collections.Generic;
using System.IO;
using Hollow.Editor.Generation;
using UnityEditor;
using UnityEngine;

namespace Hollow.Editor.Validation
{
    public static class Milestone102ReachableTacticalPositioningValidator
    {
        [MenuItem("Hollow/Validation/Validate Milestone 102 Reachable Tactical Positioning")]
        public static void Validate()
        {
            var failures = CollectFailures();
            if (failures.Count > 0)
            {
                throw new System.InvalidOperationException("M102 validation failed:\n- " + string.Join("\n- ", failures));
            }

            Debug.Log("M102 reachable tactical positioning validation passed.");
        }

        public static List<string> CollectFailures()
        {
            var failures = new List<string>();
            if (!File.Exists(Milestone102ReachableTacticalPositioningAssetGenerator.DocsPath))
            {
                failures.Add($"Missing docs: {Milestone102ReachableTacticalPositioningAssetGenerator.DocsPath}.");
            }

            if (!File.Exists(Milestone102ReachableTacticalPositioningAssetGenerator.ReportPath))
            {
                failures.Add($"Missing report: {Milestone102ReachableTacticalPositioningAssetGenerator.ReportPath}.");
            }

            var directorPath = "Assets/_Hollow/Scripts/Hollow.Combat/RoomTacticalDirector.cs";
            if (File.Exists(directorPath))
            {
                var director = File.ReadAllText(directorPath);
                if (!director.Contains("TryResolveReachableReservation", System.StringComparison.Ordinal) ||
                    !director.Contains("NavMesh.SamplePosition", System.StringComparison.Ordinal) ||
                    !director.Contains("NavMesh.CalculatePath", System.StringComparison.Ordinal))
                {
                    failures.Add("RoomTacticalDirector must sample and path-validate tactical reservations on Unity NavMesh.");
                }

                if (!director.Contains("active_slot_missing_reachable_reservation", System.StringComparison.Ordinal))
                {
                    failures.Add("RoomTacticalDirector must downgrade active slots that cannot find reachable reservations.");
                }
            }

            var intentPath = "Assets/_Hollow/Scripts/Hollow.Combat/EnemyTacticalIntent.cs";
            if (File.Exists(intentPath))
            {
                var intent = File.ReadAllText(intentPath);
                if (!intent.Contains("ReservationPathStatus", System.StringComparison.Ordinal) ||
                    !intent.Contains("HasReachableReservedPosition", System.StringComparison.Ordinal))
                {
                    failures.Add("EnemyTacticalIntent must expose reachable reservation diagnostics.");
                }
            }

            return failures;
        }
    }
}
