using System;
using UnityEngine;

namespace Hollow.Data.Definitions
{
    [CreateAssetMenu(menuName = "Hollow/Vertical Slice/Vertical Slice Lock", fileName = "VerticalSliceLock")]
    public sealed class VerticalSliceLockDefinition : HollowDefinition
    {
        [SerializeField] private string lockName = "M25 Vertical Slice Content Lock";
        [SerializeField] private string branchIdentity = "m20_branch_features_v1";
        [SerializeField] private int lockedSeed = 15001;
        [SerializeField] private string roomPoolPolicy = "M13 fixtures required; DesignerApproved rooms optional and additive";
        [SerializeField] private string reportRoot = "output/reports";
        [SerializeField] private string pdfOutputPath = "output/pdf/Hollow_M25_Vertical_Slice_Content_Lock.pdf";
        [SerializeField] private string latestJsonFileName = "latest_vertical_slice_lock.json";
        [SerializeField] private string latestMarkdownFileName = "latest_vertical_slice_lock.md";
        [SerializeField] private bool requireArtPassPrefabs = true;
        [SerializeField] private bool allowEmptyApprovedRoomPool = true;
        [SerializeField] private bool requireEqualPlatformChecklist = true;
        [SerializeField] private int requiredShopOfferCount = 3;
        [SerializeField] private int requiredNextBranchPortalCount = 3;
        [SerializeField] private string[] requiredRoomRoles = Array.Empty<string>();
        [SerializeField] private PresentationPrefabRole[] requiredPrefabRoles = Array.Empty<PresentationPrefabRole>();
        [SerializeField] private VfxCueId[] requiredVfxCues = Array.Empty<VfxCueId>();
        [SerializeField] private AudioCueId[] requiredAudioCues = Array.Empty<AudioCueId>();
        [SerializeField] private string[] platformChecklistTargets = Array.Empty<string>();
        [SerializeField] private BranchGenerationSettingsDefinition branchGenerationSettings;
        [SerializeField] private BranchRoomTemplateCatalogDefinition roomTemplateCatalog;
        [SerializeField] private PresentationContentCatalog presentationCatalog;
        [SerializeField] private PlatformBuildQaProfileDefinition platformQaProfile;

        public string LockName => lockName;
        public string BranchIdentity => branchIdentity;
        public int LockedSeed => lockedSeed;
        public string RoomPoolPolicy => roomPoolPolicy;
        public string ReportRoot => reportRoot;
        public string PdfOutputPath => pdfOutputPath;
        public string LatestJsonFileName => latestJsonFileName;
        public string LatestMarkdownFileName => latestMarkdownFileName;
        public bool RequireArtPassPrefabs => requireArtPassPrefabs;
        public bool AllowEmptyApprovedRoomPool => allowEmptyApprovedRoomPool;
        public bool RequireEqualPlatformChecklist => requireEqualPlatformChecklist;
        public int RequiredShopOfferCount => Mathf.Max(0, requiredShopOfferCount);
        public int RequiredNextBranchPortalCount => Mathf.Max(0, requiredNextBranchPortalCount);
        public string[] RequiredRoomRoles => requiredRoomRoles;
        public PresentationPrefabRole[] RequiredPrefabRoles => requiredPrefabRoles;
        public VfxCueId[] RequiredVfxCues => requiredVfxCues;
        public AudioCueId[] RequiredAudioCues => requiredAudioCues;
        public string[] PlatformChecklistTargets => platformChecklistTargets;
        public BranchGenerationSettingsDefinition BranchGenerationSettings => branchGenerationSettings;
        public BranchRoomTemplateCatalogDefinition RoomTemplateCatalog => roomTemplateCatalog;
        public PresentationContentCatalog PresentationCatalog => presentationCatalog;
        public PlatformBuildQaProfileDefinition PlatformQaProfile => platformQaProfile;

        public void Configure(
            string nextLockName,
            string nextBranchIdentity,
            int nextLockedSeed,
            string nextRoomPoolPolicy,
            string nextReportRoot,
            string nextPdfOutputPath,
            string nextLatestJsonFileName,
            string nextLatestMarkdownFileName,
            bool nextRequireArtPassPrefabs,
            bool nextAllowEmptyApprovedRoomPool,
            bool nextRequireEqualPlatformChecklist,
            int nextRequiredShopOfferCount,
            int nextRequiredNextBranchPortalCount,
            string[] nextRequiredRoomRoles,
            PresentationPrefabRole[] nextRequiredPrefabRoles,
            VfxCueId[] nextRequiredVfxCues,
            AudioCueId[] nextRequiredAudioCues,
            string[] nextPlatformChecklistTargets,
            BranchGenerationSettingsDefinition nextBranchGenerationSettings,
            BranchRoomTemplateCatalogDefinition nextRoomTemplateCatalog,
            PresentationContentCatalog nextPresentationCatalog,
            PlatformBuildQaProfileDefinition nextPlatformQaProfile)
        {
            lockName = string.IsNullOrWhiteSpace(nextLockName) ? "M25 Vertical Slice Content Lock" : nextLockName;
            branchIdentity = string.IsNullOrWhiteSpace(nextBranchIdentity) ? "m20_branch_features_v1" : nextBranchIdentity;
            lockedSeed = nextLockedSeed == 0 ? 15001 : nextLockedSeed;
            roomPoolPolicy = string.IsNullOrWhiteSpace(nextRoomPoolPolicy)
                ? "M13 fixtures required; DesignerApproved rooms optional and additive"
                : nextRoomPoolPolicy;
            reportRoot = string.IsNullOrWhiteSpace(nextReportRoot) ? "output/reports" : nextReportRoot;
            pdfOutputPath = string.IsNullOrWhiteSpace(nextPdfOutputPath) ? "output/pdf/Hollow_M25_Vertical_Slice_Content_Lock.pdf" : nextPdfOutputPath;
            latestJsonFileName = string.IsNullOrWhiteSpace(nextLatestJsonFileName) ? "latest_vertical_slice_lock.json" : nextLatestJsonFileName;
            latestMarkdownFileName = string.IsNullOrWhiteSpace(nextLatestMarkdownFileName) ? "latest_vertical_slice_lock.md" : nextLatestMarkdownFileName;
            requireArtPassPrefabs = nextRequireArtPassPrefabs;
            allowEmptyApprovedRoomPool = nextAllowEmptyApprovedRoomPool;
            requireEqualPlatformChecklist = nextRequireEqualPlatformChecklist;
            requiredShopOfferCount = Mathf.Max(0, nextRequiredShopOfferCount);
            requiredNextBranchPortalCount = Mathf.Max(0, nextRequiredNextBranchPortalCount);
            requiredRoomRoles = nextRequiredRoomRoles ?? Array.Empty<string>();
            requiredPrefabRoles = nextRequiredPrefabRoles ?? Array.Empty<PresentationPrefabRole>();
            requiredVfxCues = nextRequiredVfxCues ?? Array.Empty<VfxCueId>();
            requiredAudioCues = nextRequiredAudioCues ?? Array.Empty<AudioCueId>();
            platformChecklistTargets = nextPlatformChecklistTargets ?? Array.Empty<string>();
            branchGenerationSettings = nextBranchGenerationSettings;
            roomTemplateCatalog = nextRoomTemplateCatalog;
            presentationCatalog = nextPresentationCatalog;
            platformQaProfile = nextPlatformQaProfile;
        }
    }
}
