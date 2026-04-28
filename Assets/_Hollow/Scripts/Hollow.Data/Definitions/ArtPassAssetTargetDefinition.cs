using System;
using System.Collections.Generic;
using UnityEngine;

namespace Hollow.Data.Definitions
{
    [CreateAssetMenu(menuName = "Hollow/ArtPass/Asset Target", fileName = "ArtPassAssetTarget")]
    public sealed class ArtPassAssetTargetDefinition : ScriptableObject
    {
        [SerializeField] private string targetId = string.Empty;
        [SerializeField] private string displayName = string.Empty;
        [SerializeField] private string group = string.Empty;
        [SerializeField] private PresentationPrefabRole prefabRole;
        [SerializeField] private ArtPassAssetTargetPriority priority = ArtPassAssetTargetPriority.Medium;
        [SerializeField] private bool requiredForVerticalSlice;
        [SerializeField] private string owner = "Rafal";
        [SerializeField] private string sourceFolder = string.Empty;
        [SerializeField] private string prefabPath = string.Empty;
        [SerializeField] private string goal = string.Empty;
        [SerializeField] private string[] requiredAssets = Array.Empty<string>();
        [SerializeField] private string[] acceptanceChecks = Array.Empty<string>();
        [SerializeField] private string notes = string.Empty;

        public string TargetId => targetId;

        public string DisplayName => displayName;

        public string Group => group;

        public PresentationPrefabRole PrefabRole => prefabRole;

        public ArtPassAssetTargetPriority Priority => priority;

        public bool RequiredForVerticalSlice => requiredForVerticalSlice;

        public string Owner => owner;

        public string SourceFolder => sourceFolder;

        public string PrefabPath => prefabPath;

        public string Goal => goal;

        public IReadOnlyList<string> RequiredAssets => requiredAssets;

        public IReadOnlyList<string> AcceptanceChecks => acceptanceChecks;

        public string Notes => notes;

        public void Configure(
            string nextTargetId,
            string nextDisplayName,
            string nextGroup,
            PresentationPrefabRole nextPrefabRole,
            ArtPassAssetTargetPriority nextPriority,
            bool nextRequiredForVerticalSlice,
            string nextOwner,
            string nextSourceFolder,
            string nextPrefabPath,
            string nextGoal,
            string[] nextRequiredAssets,
            string[] nextAcceptanceChecks,
            string nextNotes)
        {
            targetId = nextTargetId ?? string.Empty;
            displayName = nextDisplayName ?? string.Empty;
            group = nextGroup ?? string.Empty;
            prefabRole = nextPrefabRole;
            priority = nextPriority;
            requiredForVerticalSlice = nextRequiredForVerticalSlice;
            owner = nextOwner ?? string.Empty;
            sourceFolder = nextSourceFolder ?? string.Empty;
            prefabPath = nextPrefabPath ?? string.Empty;
            goal = nextGoal ?? string.Empty;
            requiredAssets = nextRequiredAssets ?? Array.Empty<string>();
            acceptanceChecks = nextAcceptanceChecks ?? Array.Empty<string>();
            notes = nextNotes ?? string.Empty;
        }
    }
}
