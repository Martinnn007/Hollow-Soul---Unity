using Hollow.Core;
using Hollow.Persistence;
using Hollow.Platform;
using Hollow.Presentation;
using UnityEngine;

namespace Hollow.World
{
    public sealed class GameSessionState
    {
        private GameSessionState(
            RuntimeSessionMode sessionMode,
            HollowPlatformKind platformKind,
            RunLaunchMode launchMode,
            int profileSlotIndex,
            string profileId,
            string profileDisplayName,
            float presentationScale,
            Vector3 playerSpawnPosition)
        {
            SessionMode = sessionMode;
            PlatformKind = platformKind;
            LaunchMode = launchMode;
            ProfileSlotIndex = profileSlotIndex;
            ProfileId = profileId;
            ProfileDisplayName = profileDisplayName;
            PresentationScale = presentationScale;
            PlayerSpawnPosition = playerSpawnPosition;
        }

        public RuntimeSessionMode SessionMode { get; }

        public HollowPlatformKind PlatformKind { get; }

        public RunLaunchMode LaunchMode { get; }

        public int ProfileSlotIndex { get; }

        public string ProfileId { get; }

        public string ProfileDisplayName { get; }

        public float PresentationScale { get; }

        public Vector3 PlayerSpawnPosition { get; }

        public bool HasProfile => !string.IsNullOrWhiteSpace(ProfileId);

        public static GameSessionState Create(
            RuntimeSessionMode sessionMode,
            HollowPlatformKind platformKind,
            ProfileSlotSummary selectedProfile,
            Vector3 playerSpawnPosition)
        {
            return Create(sessionMode, platformKind, RunLaunchMode.NewRun, selectedProfile, playerSpawnPosition);
        }

        public static GameSessionState Create(
            RuntimeSessionMode sessionMode,
            HollowPlatformKind platformKind,
            RunLaunchMode launchMode,
            ProfileSlotSummary selectedProfile,
            Vector3 playerSpawnPosition)
        {
            return new GameSessionState(
                sessionMode,
                platformKind,
                launchMode,
                selectedProfile?.SlotIndex ?? -1,
                selectedProfile?.ProfileId ?? string.Empty,
                selectedProfile?.DisplayName ?? "Direct Scene Preview",
                PresentationScalePolicy.WorldScaleFor(platformKind),
                playerSpawnPosition);
        }
    }
}
