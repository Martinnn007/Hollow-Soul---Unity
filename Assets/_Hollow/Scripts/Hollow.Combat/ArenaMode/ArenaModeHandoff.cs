using Hollow.Core.App;

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
#endif

        private static string presetId;
        private static bool autoStart;
        private static AppShellRoute returnRoute = AppShellRoute.MainMenu;

        public static bool HasPending
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(presetId))
                {
                    return true;
                }

#if UNITY_EDITOR
                return !string.IsNullOrWhiteSpace(SessionState.GetString(PresetIdSessionKey, string.Empty));
#else
                return false;
#endif
            }
        }

        public static void Set(string nextPresetId, bool nextAutoStart, AppShellRoute nextReturnRoute)
        {
            presetId = string.IsNullOrWhiteSpace(nextPresetId) ? string.Empty : nextPresetId;
            autoStart = nextAutoStart;
            returnRoute = nextReturnRoute;
#if UNITY_EDITOR
            SessionState.SetString(PresetIdSessionKey, presetId);
            SessionState.SetBool(AutoStartSessionKey, autoStart);
            SessionState.SetInt(ReturnRouteSessionKey, (int)returnRoute);
#endif
        }

        public static bool TryConsume(out string nextPresetId, out bool nextAutoStart, out AppShellRoute nextReturnRoute)
        {
#if UNITY_EDITOR
            if (string.IsNullOrWhiteSpace(presetId))
            {
                presetId = SessionState.GetString(PresetIdSessionKey, string.Empty);
                autoStart = SessionState.GetBool(AutoStartSessionKey, false);
                returnRoute = (AppShellRoute)SessionState.GetInt(ReturnRouteSessionKey, (int)AppShellRoute.MainMenu);
            }
#endif

            nextPresetId = presetId;
            nextAutoStart = autoStart;
            nextReturnRoute = returnRoute;
            presetId = string.Empty;
            autoStart = false;
            returnRoute = AppShellRoute.MainMenu;
#if UNITY_EDITOR
            SessionState.SetString(PresetIdSessionKey, string.Empty);
            SessionState.SetBool(AutoStartSessionKey, false);
            SessionState.SetInt(ReturnRouteSessionKey, (int)AppShellRoute.MainMenu);
#endif
            return !string.IsNullOrWhiteSpace(nextPresetId);
        }
    }
}
