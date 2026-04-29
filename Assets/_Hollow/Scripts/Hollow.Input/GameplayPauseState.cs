namespace Hollow.Input
{
    public static class GameplayPauseState
    {
        public static bool IsPaused { get; private set; }

        public static void SetPaused(bool isPaused)
        {
            IsPaused = isPaused;
        }
    }
}
