using Hollow.Core.App;
using Hollow.Platform;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Hollow.Combat
{
    public static class ArenaModeHandoff
    {
#if UNITY_EDITOR
        private const string PresetIdSessionKey = "Hollow.ArenaModeHandoff.PresetId";
        private const string AutoStartSessionKey = "Hollow.ArenaModeHandoff.AutoStart";
        private const string ReturnRouteSessionKey = "Hollow.ArenaModeHandoff.ReturnRoute";
        private const string HasPendingSessionKey = "Hollow.ArenaModeHandoff.HasPending";
        private const string PlatformKindSessionKey = "Hollow.ArenaModeHandoff.PlatformKind";
        private const string CharacterIdSessionKey = "Hollow.ArenaModeHandoff.CharacterId";
#endif

        private static string presetId;
        private static bool autoStart;
        private static bool hasPending;
        private static AppShellRoute returnRoute = AppShellRoute.MainMenu;
        private static HollowPlatformKind platformKind = HollowPlatformKind.WindowsStandard3D;
        private static string selectedCharacterId = "balanced";

        public static bool HasPending
        {
            get
            {
                if (hasPending || !string.IsNullOrWhiteSpace(presetId))
                {
                    return true;
                }

#if UNITY_EDITOR
                return SessionState.GetBool(HasPendingSessionKey, false) ||
                       !string.IsNullOrWhiteSpace(SessionState.GetString(PresetIdSessionKey, string.Empty));
#else
                return false;
#endif
            }
        }

        public static void Set(
            string nextPresetId,
            bool nextAutoStart,
            AppShellRoute nextReturnRoute,
            HollowPlatformKind nextPlatformKind = HollowPlatformKind.WindowsStandard3D,
            string nextSelectedCharacterId = "balanced")
        {
            presetId = string.IsNullOrWhiteSpace(nextPresetId) ? string.Empty : nextPresetId;
            autoStart = nextAutoStart;
            hasPending = true;
            returnRoute = nextReturnRoute;
            platformKind = nextPlatformKind;
            selectedCharacterId = string.IsNullOrWhiteSpace(nextSelectedCharacterId) ? "balanced" : nextSelectedCharacterId;
#if UNITY_EDITOR
            SessionState.SetString(PresetIdSessionKey, presetId);
            SessionState.SetBool(AutoStartSessionKey, autoStart);
            SessionState.SetBool(HasPendingSessionKey, hasPending);
            SessionState.SetInt(ReturnRouteSessionKey, (int)returnRoute);
            SessionState.SetInt(PlatformKindSessionKey, (int)platformKind);
            SessionState.SetString(CharacterIdSessionKey, selectedCharacterId);
#endif
        }

        public static bool TryConsume(out string nextPresetId, out bool nextAutoStart, out AppShellRoute nextReturnRoute)
        {
            return TryConsume(out nextPresetId, out nextAutoStart, out nextReturnRoute, out _, out _);
        }

        public static bool TryConsume(
            out string nextPresetId,
            out bool nextAutoStart,
            out AppShellRoute nextReturnRoute,
            out HollowPlatformKind nextPlatformKind,
            out string nextSelectedCharacterId)
        {
#if UNITY_EDITOR
            if (!hasPending && string.IsNullOrWhiteSpace(presetId))
            {
                presetId = SessionState.GetString(PresetIdSessionKey, string.Empty);
                autoStart = SessionState.GetBool(AutoStartSessionKey, false);
                hasPending = SessionState.GetBool(HasPendingSessionKey, false);
                returnRoute = (AppShellRoute)SessionState.GetInt(ReturnRouteSessionKey, (int)AppShellRoute.MainMenu);
                platformKind = (HollowPlatformKind)SessionState.GetInt(PlatformKindSessionKey, (int)HollowPlatformKind.WindowsStandard3D);
                selectedCharacterId = SessionState.GetString(CharacterIdSessionKey, "balanced");
            }
#endif

            nextPresetId = presetId;
            nextAutoStart = autoStart;
            nextReturnRoute = returnRoute;
            nextPlatformKind = platformKind;
            nextSelectedCharacterId = string.IsNullOrWhiteSpace(selectedCharacterId) ? "balanced" : selectedCharacterId;
            var consumed = hasPending || !string.IsNullOrWhiteSpace(nextPresetId);
            presetId = string.Empty;
            autoStart = false;
            hasPending = false;
            returnRoute = AppShellRoute.MainMenu;
            platformKind = HollowPlatformKind.WindowsStandard3D;
            selectedCharacterId = "balanced";
#if UNITY_EDITOR
            SessionState.SetString(PresetIdSessionKey, string.Empty);
            SessionState.SetBool(AutoStartSessionKey, false);
            SessionState.SetBool(HasPendingSessionKey, false);
            SessionState.SetInt(ReturnRouteSessionKey, (int)AppShellRoute.MainMenu);
            SessionState.SetInt(PlatformKindSessionKey, (int)HollowPlatformKind.WindowsStandard3D);
            SessionState.SetString(CharacterIdSessionKey, "balanced");
#endif
            return consumed;
        }
    }
}
