using Hollow.Core;
using Hollow.Core.App;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Hollow.Rooms
{
    public static class RoomPlaytestHandoff
    {
#if UNITY_EDITOR
        private const string RuntimeJsonSessionKey = "Hollow.RoomPlaytestHandoff.RuntimeJson";
        private const string SessionModeSessionKey = "Hollow.RoomPlaytestHandoff.SessionMode";
        private const string ReturnRouteSessionKey = "Hollow.RoomPlaytestHandoff.ReturnRoute";
        private const string CharacterIdSessionKey = "Hollow.RoomPlaytestHandoff.CharacterId";
#endif

        private static string runtimeJson;
        private static RuntimeSessionMode sessionMode;
        private static AppShellRoute returnRoute;
        private static string selectedCharacterId = "balanced";

        public static bool HasPending
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(runtimeJson))
                {
                    return true;
                }

#if UNITY_EDITOR
                return !string.IsNullOrWhiteSpace(SessionState.GetString(RuntimeJsonSessionKey, string.Empty));
#else
                return false;
#endif
            }
        }

        public static void Set(string nextRuntimeJson, RuntimeSessionMode nextSessionMode, AppShellRoute nextReturnRoute)
        {
            Set(nextRuntimeJson, nextSessionMode, nextReturnRoute, "balanced");
        }

        public static void Set(string nextRuntimeJson, RuntimeSessionMode nextSessionMode, AppShellRoute nextReturnRoute, string nextSelectedCharacterId)
        {
            runtimeJson = nextRuntimeJson;
            sessionMode = nextSessionMode;
            returnRoute = nextReturnRoute;
            selectedCharacterId = string.IsNullOrWhiteSpace(nextSelectedCharacterId) ? "balanced" : nextSelectedCharacterId;
#if UNITY_EDITOR
            SessionState.SetString(RuntimeJsonSessionKey, runtimeJson ?? string.Empty);
            SessionState.SetInt(SessionModeSessionKey, (int)sessionMode);
            SessionState.SetInt(ReturnRouteSessionKey, (int)returnRoute);
            SessionState.SetString(CharacterIdSessionKey, selectedCharacterId);
#endif
        }

        public static bool TryConsume(out string nextRuntimeJson, out RuntimeSessionMode nextSessionMode, out AppShellRoute nextReturnRoute)
        {
            return TryConsume(out nextRuntimeJson, out nextSessionMode, out nextReturnRoute, out _);
        }

        public static bool TryConsume(out string nextRuntimeJson, out RuntimeSessionMode nextSessionMode, out AppShellRoute nextReturnRoute, out string nextSelectedCharacterId)
        {
#if UNITY_EDITOR
            if (string.IsNullOrWhiteSpace(runtimeJson))
            {
                runtimeJson = SessionState.GetString(RuntimeJsonSessionKey, string.Empty);
                sessionMode = (RuntimeSessionMode)SessionState.GetInt(SessionModeSessionKey, 0);
                returnRoute = (AppShellRoute)SessionState.GetInt(ReturnRouteSessionKey, 0);
                selectedCharacterId = SessionState.GetString(CharacterIdSessionKey, "balanced");
            }
#endif

            nextRuntimeJson = runtimeJson;
            nextSessionMode = sessionMode;
            nextReturnRoute = returnRoute;
            nextSelectedCharacterId = string.IsNullOrWhiteSpace(selectedCharacterId) ? "balanced" : selectedCharacterId;
            runtimeJson = string.Empty;
#if UNITY_EDITOR
            SessionState.SetString(RuntimeJsonSessionKey, string.Empty);
            SessionState.SetInt(SessionModeSessionKey, 0);
            SessionState.SetInt(ReturnRouteSessionKey, 0);
            SessionState.SetString(CharacterIdSessionKey, string.Empty);
#endif
            return !string.IsNullOrWhiteSpace(nextRuntimeJson);
        }
    }
}
