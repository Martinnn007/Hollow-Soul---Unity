using System.Collections.Generic;
using System.IO;
using System.Linq;
using Hollow.Branches;
using Hollow.Combat;
using Hollow.Editor.Generation;
using Hollow.Rooms;
using UnityEditor;

namespace Hollow.Editor.Validation
{
    public static class Milestone53Validator
    {
        public static ContentValidationReport Validate()
        {
            var report = new ContentValidationReport();
            var failures = new List<string>();
            ValidateRuntimeRoster(failures);
            ValidateGeneratedAssets(failures);
            ValidateBossSelection(failures);
            ValidateArenaImports(failures);

            foreach (var failure in failures)
            {
                report.AddFailure(failure);
            }

            return report;
        }

        public static bool ValidateAll()
        {
            var report = Validate();
            if (!report.IsValid)
            {
                UnityEngine.Debug.LogError(string.Join("\n", report.Failures));
            }

            return report.IsValid;
        }

        [MenuItem("Hollow/Validation/Validate Milestone 53")]
        public static void ValidateFromMenu()
        {
            ValidateAll();
        }

        private static void ValidateRuntimeRoster(List<string> failures)
        {
            var bosses = BossCatalogDefinition.CreateRuntimeRoster();
            if (bosses.Length != 10)
            {
                failures.Add($"M53 runtime roster must contain exactly 10 bosses, found {bosses.Length}.");
            }

            ValidateBoss(bosses, "stone_warden", BossWorldBand.World1, 24, failures);
            ValidateBoss(bosses, "splinter_saint", BossWorldBand.World1, 22, failures);
            ValidateBoss(bosses, "gravel_maw", BossWorldBand.World1, 28, failures);
            ValidateBoss(bosses, "cartouche_widow", BossWorldBand.World2, 32, failures);
            ValidateBoss(bosses, "iron_reliquary", BossWorldBand.World2, 36, failures);
            ValidateBoss(bosses, "mirror_husk", BossWorldBand.World2, 34, failures);
            ValidateBoss(bosses, "ash_comet", BossWorldBand.World2, 38, failures);
            ValidateBoss(bosses, "choir_of_teeth", BossWorldBand.World3, 42, failures);
            ValidateBoss(bosses, "rust_bishop", BossWorldBand.World3, 46, failures);
            ValidateBoss(bosses, "hollow_star_larva", BossWorldBand.World3, 50, failures);

            if (bosses.Select(boss => boss.Arena.arenaId).Distinct().Count() != bosses.Length)
            {
                failures.Add("M53 each boss must own a unique arena id.");
            }
        }

        private static void ValidateBoss(IEnumerable<BossDefinition> bosses, string id, BossWorldBand band, int hp, List<string> failures)
        {
            var boss = bosses.FirstOrDefault(candidate => candidate.BossId == id);
            if (boss == null)
            {
                failures.Add($"Missing M53 boss `{id}`.");
                return;
            }

            if (boss.WorldBand != band || boss.MaxHealth != hp)
            {
                failures.Add($"M53 boss `{id}` expected W{(int)band} HP {hp}, found W{(int)boss.WorldBand} HP {boss.MaxHealth}.");
            }

            if (string.IsNullOrWhiteSpace(boss.DisplayName) || string.IsNullOrWhiteSpace(boss.Arena.arenaId) || boss.Attacks.Count == 0)
            {
                failures.Add($"M53 boss `{id}` is missing display name, arena, or attacks.");
            }
        }

        private static void ValidateGeneratedAssets(List<string> failures)
        {
            var catalog = AssetDatabase.LoadAssetAtPath<BossCatalogDefinition>(Milestone53AssetGenerator.BossCatalogPath);
            if (catalog != null && catalog.Bosses.Count != 10)
            {
                failures.Add("Generated M53 boss catalog must contain 10 bosses.");
            }

            if (!File.Exists(Milestone53AssetGenerator.DocsPath))
            {
                failures.Add("Missing M53 docs.");
            }
        }

        private static void ValidateBossSelection(List<string> failures)
        {
            var catalog = BossCatalogDefinition.CreateRuntimeDefault();
            var first = BossSelectionResolver.Resolve(catalog, 53001, 53001, 1, "boss_01", BranchGenerator.DirectedEncounterBranchId);
            var second = BossSelectionResolver.Resolve(catalog, 53001, 53001, 1, "boss_01", BranchGenerator.DirectedEncounterBranchId);
            if (first == null || second == null || first.BossId != second.BossId || first.WorldBand != BossWorldBand.World1)
            {
                failures.Add("M53 boss selection must be deterministic and respect World 1 band.");
            }

            var world3 = BossSelectionResolver.Resolve(catalog, 53001, 53001, 3, "boss_01", BranchGenerator.DirectedEncounterBranchId);
            if (world3 == null || world3.WorldBand != BossWorldBand.World3)
            {
                failures.Add("M53 boss selection must respect World 3 band.");
            }
        }

        private static void ValidateArenaImports(List<string> failures)
        {
            foreach (var arenaId in Milestone53AssetGenerator.ApprovedBossArenaIds)
            {
                var path = $"{Milestone16AssetGenerator.ApprovedRoomDirectory}/{arenaId}.hollowruntime.json";
                if (!File.Exists(path))
                {
                    continue;
                }

                if (!HollowRuntimeV2Importer.TryImport(File.ReadAllText(path), out var asset, out var error))
                {
                    failures.Add($"M53 boss arena `{arenaId}` does not import: {error}");
                    continue;
                }

                if (asset.Id != arenaId || asset.DoorPorts.Count == 0 || asset.SafeStart == null)
                {
                    failures.Add($"M53 boss arena `{arenaId}` is missing canonical id, door ports, or safe start.");
                }
            }
        }
    }
}
