using Hollow.Core.App;
using Hollow.Platform;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Hollow.World
{
    public enum SpaceshipArrivalReason
    {
        DirectProfile = 0,
        NormalSuccess = 1,
        NormalDeath = 2,
        ChallengeSuccess = 3,
        ChallengeDeath = 4
    }

    public readonly struct SpaceshipArrivalSnapshot
    {
        public SpaceshipArrivalSnapshot(
            SpaceshipArrivalReason reason,
            HollowPlatformKind platformKind,
            int soulsBanked,
            string challengeId)
        {
            Reason = reason;
            PlatformKind = platformKind;
            SoulsBanked = soulsBanked < 0 ? 0 : soulsBanked;
            ChallengeId = challengeId ?? string.Empty;
        }

        public SpaceshipArrivalReason Reason { get; }

        public HollowPlatformKind PlatformKind { get; }

        public int SoulsBanked { get; }

        public string ChallengeId { get; }

        public bool RequiresQuarantine => Reason != SpaceshipArrivalReason.DirectProfile;
    }

    public static class SpaceshipArrivalHandoff
    {
#if UNITY_EDITOR
        private const string HasPendingSessionKey = "Hollow.SpaceshipArrival.HasPending";
        private const string ReasonSessionKey = "Hollow.SpaceshipArrival.Reason";
        private const string PlatformSessionKey = "Hollow.SpaceshipArrival.Platform";
        private const string SoulsBankedSessionKey = "Hollow.SpaceshipArrival.SoulsBanked";
        private const string ChallengeIdSessionKey = "Hollow.SpaceshipArrival.ChallengeId";
#endif

        private static bool hasPending;
        private static SpaceshipArrivalSnapshot pending = new(
            SpaceshipArrivalReason.DirectProfile,
            HollowPlatformKind.WindowsStandard3D,
            0,
            string.Empty);

        public static void Set(
            SpaceshipArrivalReason reason,
            HollowPlatformKind platformKind,
            int soulsBanked = 0,
            string challengeId = "")
        {
            hasPending = true;
            pending = new SpaceshipArrivalSnapshot(reason, platformKind, soulsBanked, challengeId);
#if UNITY_EDITOR
            SessionState.SetBool(HasPendingSessionKey, true);
            SessionState.SetInt(ReasonSessionKey, (int)pending.Reason);
            SessionState.SetInt(PlatformSessionKey, (int)pending.PlatformKind);
            SessionState.SetInt(SoulsBankedSessionKey, pending.SoulsBanked);
            SessionState.SetString(ChallengeIdSessionKey, pending.ChallengeId);
#endif
        }

        public static void SetDirectProfile(HollowPlatformKind platformKind)
        {
            Set(SpaceshipArrivalReason.DirectProfile, platformKind);
        }

        public static bool TryConsume(out SpaceshipArrivalSnapshot snapshot)
        {
            RestoreFromEditorSessionIfNeeded();
            snapshot = pending;
            var consumed = hasPending;
            Clear();
            return consumed;
        }

        public static SpaceshipArrivalSnapshot PeekOrDefault(HollowPlatformKind platformKind)
        {
            RestoreFromEditorSessionIfNeeded();
            return hasPending
                ? pending
                : new SpaceshipArrivalSnapshot(SpaceshipArrivalReason.DirectProfile, platformKind, 0, string.Empty);
        }

        public static AppShellRoute ShipRouteFor(HollowPlatformKind platformKind)
        {
            return PlatformPresentationModeResolver.SpaceshipRouteForPlatform(platformKind);
        }

        public static void Clear()
        {
            hasPending = false;
            pending = new SpaceshipArrivalSnapshot(
                SpaceshipArrivalReason.DirectProfile,
                HollowPlatformKind.WindowsStandard3D,
                0,
                string.Empty);
#if UNITY_EDITOR
            SessionState.SetBool(HasPendingSessionKey, false);
            SessionState.SetInt(ReasonSessionKey, (int)SpaceshipArrivalReason.DirectProfile);
            SessionState.SetInt(PlatformSessionKey, (int)HollowPlatformKind.WindowsStandard3D);
            SessionState.SetInt(SoulsBankedSessionKey, 0);
            SessionState.SetString(ChallengeIdSessionKey, string.Empty);
#endif
        }

        private static void RestoreFromEditorSessionIfNeeded()
        {
#if UNITY_EDITOR
            if (hasPending)
            {
                return;
            }

            hasPending = SessionState.GetBool(HasPendingSessionKey, false);
            if (!hasPending)
            {
                return;
            }

            pending = new SpaceshipArrivalSnapshot(
                (SpaceshipArrivalReason)SessionState.GetInt(ReasonSessionKey, (int)SpaceshipArrivalReason.DirectProfile),
                (HollowPlatformKind)SessionState.GetInt(PlatformSessionKey, (int)HollowPlatformKind.WindowsStandard3D),
                SessionState.GetInt(SoulsBankedSessionKey, 0),
                SessionState.GetString(ChallengeIdSessionKey, string.Empty));
#endif
        }
    }
}
