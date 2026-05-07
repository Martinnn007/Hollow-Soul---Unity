using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Hollow.Editor.Generation
{
    public static class Milestone102ReachableTacticalPositioningAssetGenerator
    {
        public const string DocsPath = "Docs/Hollow_M102_Reachable_Tactical_Positioning.md";
        public const string ReportPath = "output/reports/m102_reachable_tactical_positioning.md";

        [MenuItem("Hollow/Generation/Generate Milestone 102 Reachable Tactical Positioning Assets")]
        public static void Generate()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(DocsPath) ?? "Docs");
            Directory.CreateDirectory(Path.GetDirectoryName(ReportPath) ?? "output/reports");
            WriteDocs();
            WriteReport();
            AssetDatabase.Refresh();
            Debug.Log("Generated Hollow M102 reachable tactical positioning docs and report.");
        }

        private static void WriteDocs()
        {
            var builder = new StringBuilder();
            builder.AppendLine("# M102: Reachable Tactical Positioning V1");
            builder.AppendLine();
            builder.AppendLine("M102 upgrades tactical reservations from geometric player rings to NavMesh-validated combat positions.");
            builder.AppendLine();
            builder.AppendLine("## Contract");
            builder.AppendLine();
            builder.AppendLine("- `RoomTacticalDirector` samples candidate combat positions around the player, snaps them to Unity NavMesh, and accepts only complete paths from the enemy to the sampled point.");
            builder.AppendLine("- Reserved positions still respect room collision, enemy radius, clearance scoring, and existing anti-dogpile spacing.");
            builder.AppendLine("- Active threats without a reachable reservation are downgraded into support positioning instead of committing from an impossible slot.");
            builder.AppendLine("- `EnemyTacticalIntent` now records reservation path status, corner count, path length, and reachability for debug overlays/tests.");
            builder.AppendLine("- M101 locomotion ownership remains unchanged: NavMesh owns movement to the reservation, Hollow owns attacks, knockback, stun, death, lunges, charges, and recovery.");
            builder.AppendLine();
            builder.AppendLine("## Runtime Impact");
            builder.AppendLine();
            builder.AppendLine("- Enemies path toward reachable attack starts rather than sliding against rocks to reach mathematically nice circles.");
            builder.AppendLine("- Rooms missing usable NavMesh data cannot produce reachable tactical reservations.");
            builder.AppendLine("- Boss runtime behavior remains unchanged.");
            File.WriteAllText(DocsPath, builder.ToString());
        }

        private static void WriteReport()
        {
            File.WriteAllText(ReportPath, @"# M102 Reachable Tactical Positioning Report

- `RoomTacticalDirector` validates tactical reservation candidates with `NavMesh.SamplePosition` and `NavMesh.CalculatePath`.
- Accepted reservations require `EnemyPathStatus.Ready`.
- Reservation scoring now includes path length, clearance, desired action distance, and existing slot separation.
- `EnemyTacticalIntent` exposes reservation path status, corner count, and length.
- Active tactical slots without reachable positions are downgraded before they can start committed attacks.
");
        }
    }
}
