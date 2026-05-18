using Hollow.Data.Definitions;

namespace Hollow.Branches
{
    public static class RunFramingService
    {
        public static RunFramingSnapshot Create(
            RunFramingCatalogDefinition catalog,
            int worldIndex,
            RunWorldPhase phase,
            int runSeed,
            int branchSeed,
            bool bossRoomActive)
        {
            var normalizedWorld = NormalizedWorld(worldIndex);
            var title = $"World {normalizedWorld}";
            var subtitle = "The Hollow keeps rearranging itself.";
            var message = "Clear rooms, keep what helps, and choose how deep to go.";
            var identityId = string.Empty;
            var identityName = string.Empty;
            var biomeId = RoomBiomeIds.HollowThreshold;
            var definition = RunWorldItineraryService.Resolve(catalog, runSeed, normalizedWorld);
            if (definition != null)
            {
                title = $"World {normalizedWorld}: {definition.DisplayName}";
                subtitle = definition.Subtitle;
                message = MessageFor(definition, phase, bossRoomActive);
                identityId = definition.IdentityId;
                identityName = definition.DisplayName;
                biomeId = definition.BiomeId;
            }
            else
            {
                message = FallbackMessageFor(phase, bossRoomActive);
            }

            return new RunFramingSnapshot(
                title,
                subtitle,
                PhaseLabelFor(phase, bossRoomActive),
                message,
                $"Run Seed {runSeed} | Branch {branchSeed}",
                identityId,
                identityName,
                biomeId);
        }

        public static string PhaseLabelFor(RunWorldPhase phase, bool bossRoomActive)
        {
            if (bossRoomActive)
            {
                return "Boss Threshold";
            }

            return phase switch
            {
                RunWorldPhase.Prologue => "Prologue Branch",
                RunWorldPhase.Branch => "Hub Branch",
                RunWorldPhase.Hub => "Inter-Branch Hub",
                RunWorldPhase.Completed => "Extraction",
                _ => "Legacy Branch"
            };
        }

        private static string MessageFor(RunFramingDefinition definition, RunWorldPhase phase, bool bossRoomActive)
        {
            if (bossRoomActive && !string.IsNullOrWhiteSpace(definition.BossLine))
            {
                return definition.BossLine;
            }

            return phase switch
            {
                RunWorldPhase.Prologue when !string.IsNullOrWhiteSpace(definition.PrologueLine) => definition.PrologueLine,
                RunWorldPhase.Branch when !string.IsNullOrWhiteSpace(definition.BranchLine) => definition.BranchLine,
                RunWorldPhase.Hub when !string.IsNullOrWhiteSpace(definition.HubLine) => definition.HubLine,
                RunWorldPhase.Completed when !string.IsNullOrWhiteSpace(definition.ExtractionLine) => definition.ExtractionLine,
                _ => !string.IsNullOrWhiteSpace(definition.BranchLine) ? definition.BranchLine : FallbackMessageFor(phase, bossRoomActive)
            };
        }

        private static string FallbackMessageFor(RunWorldPhase phase, bool bossRoomActive)
        {
            if (bossRoomActive)
            {
                return "A warden blocks the end of this branch.";
            }

            return phase switch
            {
                RunWorldPhase.Prologue => "The first branch opens before the hub has a shape.",
                RunWorldPhase.Hub => "The hub is safe enough to choose your next mistake.",
                RunWorldPhase.Completed => "The Hollow loosens its grip for one breath.",
                _ => "The branch rearranges itself around your build."
            };
        }

        private static int NormalizedWorld(int worldIndex)
        {
            return worldIndex <= 0 ? 1 : worldIndex;
        }
    }
}
