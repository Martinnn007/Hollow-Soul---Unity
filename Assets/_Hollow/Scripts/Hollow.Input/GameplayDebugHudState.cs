namespace Hollow.Input
{
    public static class GameplayDebugHudState
    {
        public static bool IsVisible { get; private set; }

        public static void SetVisible(bool isVisible)
        {
            IsVisible = isVisible;
        }

        public static void Toggle()
        {
            IsVisible = !IsVisible;
        }
    }
}
