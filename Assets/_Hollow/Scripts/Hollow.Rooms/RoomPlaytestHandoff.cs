using Hollow.Core;
using Hollow.Core.App;

namespace Hollow.Rooms
{
    public static class RoomPlaytestHandoff
    {
        private static string runtimeJson;
        private static RuntimeSessionMode sessionMode;
        private static AppShellRoute returnRoute;

        public static bool HasPending => !string.IsNullOrWhiteSpace(runtimeJson);

        public static void Set(string nextRuntimeJson, RuntimeSessionMode nextSessionMode, AppShellRoute nextReturnRoute)
        {
            runtimeJson = nextRuntimeJson;
            sessionMode = nextSessionMode;
            returnRoute = nextReturnRoute;
        }

        public static bool TryConsume(out string nextRuntimeJson, out RuntimeSessionMode nextSessionMode, out AppShellRoute nextReturnRoute)
        {
            nextRuntimeJson = runtimeJson;
            nextSessionMode = sessionMode;
            nextReturnRoute = returnRoute;
            runtimeJson = string.Empty;
            return !string.IsNullOrWhiteSpace(nextRuntimeJson);
        }
    }
}
