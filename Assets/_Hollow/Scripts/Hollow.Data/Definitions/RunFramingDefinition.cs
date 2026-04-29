using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Hollow.Data.Definitions
{
    [CreateAssetMenu(menuName = "Hollow/World/Run Framing Definition")]
    public sealed class RunFramingDefinition : ScriptableObject
    {
        [SerializeField] private int worldIndex = 1;
        [SerializeField] private string identityId = "hollow_threshold";
        [SerializeField] private string displayName = "The Hollow Threshold";
        [SerializeField] private string subtitle = "A room-made wound in the dark.";
        [SerializeField] private WorldBiomeTag[] biomeTags = { WorldBiomeTag.MixedThreshold };
        [SerializeField] private string paletteHint = string.Empty;
        [SerializeField] private string lightingHint = string.Empty;
        [SerializeField] private string materialNotes = string.Empty;
        [SerializeField] private string prologueLine = "The first branch opens before the hub remembers you.";
        [SerializeField] private string branchLine = "A chosen branch bends around your current build.";
        [SerializeField] private string hubLine = "The hub catches its breath. Spend carefully, then choose a door deeper.";
        [SerializeField] private string bossLine = "A warden waits at the end of the branch.";
        [SerializeField] private string extractionLine = "The run can end here, if you still know what you came to keep.";
        [SerializeField] private string[] branchEchoNames = { "Ashen Door", "Cold Spur", "Mourning Lane" };

        public int WorldIndex => Mathf.Max(1, worldIndex);

        public string IdentityId => string.IsNullOrWhiteSpace(identityId) ? $"world_{WorldIndex:00}" : identityId;

        public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? $"World {WorldIndex}" : displayName;

        public string Subtitle => subtitle ?? string.Empty;

        public IReadOnlyList<WorldBiomeTag> BiomeTags => biomeTags ?? Array.Empty<WorldBiomeTag>();

        public string PaletteHint => paletteHint ?? string.Empty;

        public string LightingHint => lightingHint ?? string.Empty;

        public string MaterialNotes => materialNotes ?? string.Empty;

        public string PrologueLine => prologueLine ?? string.Empty;

        public string BranchLine => branchLine ?? string.Empty;

        public string HubLine => hubLine ?? string.Empty;

        public string BossLine => bossLine ?? string.Empty;

        public string ExtractionLine => extractionLine ?? string.Empty;

        public IReadOnlyList<string> BranchEchoNames => branchEchoNames ?? Array.Empty<string>();

        public void Configure(
            int nextWorldIndex,
            string nextDisplayName,
            string nextSubtitle,
            string nextPrologueLine,
            string nextBranchLine,
            string nextHubLine,
            string nextBossLine,
            string nextExtractionLine)
        {
            Configure(
                $"world_{Mathf.Max(1, nextWorldIndex):00}",
                nextWorldIndex,
                nextDisplayName,
                nextSubtitle,
                Array.Empty<WorldBiomeTag>(),
                string.Empty,
                string.Empty,
                string.Empty,
                nextPrologueLine,
                nextBranchLine,
                nextHubLine,
                nextBossLine,
                nextExtractionLine,
                Array.Empty<string>());
        }

        public void Configure(
            string nextIdentityId,
            int nextWorldIndex,
            string nextDisplayName,
            string nextSubtitle,
            IEnumerable<WorldBiomeTag> nextBiomeTags,
            string nextPaletteHint,
            string nextLightingHint,
            string nextMaterialNotes,
            string nextPrologueLine,
            string nextBranchLine,
            string nextHubLine,
            string nextBossLine,
            string nextExtractionLine,
            IEnumerable<string> nextBranchEchoNames)
        {
            worldIndex = Mathf.Max(1, nextWorldIndex);
            identityId = string.IsNullOrWhiteSpace(nextIdentityId) ? $"world_{worldIndex:00}" : nextIdentityId.Trim();
            displayName = nextDisplayName ?? string.Empty;
            subtitle = nextSubtitle ?? string.Empty;
            biomeTags = (nextBiomeTags ?? Array.Empty<WorldBiomeTag>())
                .Distinct()
                .ToArray();
            paletteHint = nextPaletteHint ?? string.Empty;
            lightingHint = nextLightingHint ?? string.Empty;
            materialNotes = nextMaterialNotes ?? string.Empty;
            prologueLine = nextPrologueLine ?? string.Empty;
            branchLine = nextBranchLine ?? string.Empty;
            hubLine = nextHubLine ?? string.Empty;
            bossLine = nextBossLine ?? string.Empty;
            extractionLine = nextExtractionLine ?? string.Empty;
            branchEchoNames = (nextBranchEchoNames ?? Array.Empty<string>())
                .Where(echo => !string.IsNullOrWhiteSpace(echo))
                .Select(echo => echo.Trim())
                .Distinct()
                .ToArray();
        }
    }
}
