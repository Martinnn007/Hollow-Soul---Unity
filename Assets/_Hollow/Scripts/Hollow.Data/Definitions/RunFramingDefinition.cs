using UnityEngine;

namespace Hollow.Data.Definitions
{
    [CreateAssetMenu(menuName = "Hollow/World/Run Framing Definition")]
    public sealed class RunFramingDefinition : ScriptableObject
    {
        [SerializeField] private int worldIndex = 1;
        [SerializeField] private string displayName = "The Hollow Threshold";
        [SerializeField] private string subtitle = "A room-made wound in the dark.";
        [SerializeField] private string prologueLine = "The first branch opens before the hub remembers you.";
        [SerializeField] private string branchLine = "A chosen branch bends around your current build.";
        [SerializeField] private string hubLine = "The hub catches its breath. Spend carefully, then choose a door deeper.";
        [SerializeField] private string bossLine = "A warden waits at the end of the branch.";
        [SerializeField] private string extractionLine = "The run can end here, if you still know what you came to keep.";

        public int WorldIndex => Mathf.Max(1, worldIndex);

        public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? $"World {WorldIndex}" : displayName;

        public string Subtitle => subtitle ?? string.Empty;

        public string PrologueLine => prologueLine ?? string.Empty;

        public string BranchLine => branchLine ?? string.Empty;

        public string HubLine => hubLine ?? string.Empty;

        public string BossLine => bossLine ?? string.Empty;

        public string ExtractionLine => extractionLine ?? string.Empty;

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
            worldIndex = Mathf.Max(1, nextWorldIndex);
            displayName = nextDisplayName ?? string.Empty;
            subtitle = nextSubtitle ?? string.Empty;
            prologueLine = nextPrologueLine ?? string.Empty;
            branchLine = nextBranchLine ?? string.Empty;
            hubLine = nextHubLine ?? string.Empty;
            bossLine = nextBossLine ?? string.Empty;
            extractionLine = nextExtractionLine ?? string.Empty;
        }
    }
}
