namespace Hollow.RoomDesigner
{
    public readonly struct RoomDesignerInputSnapshot
    {
        public RoomDesignerInputSnapshot(
            int moveX,
            int moveZ,
            int toolDelta,
            int layerDelta,
            bool placePressed,
            bool erasePressed,
            bool eyedropperPressed,
            bool toggleLabelsPressed,
            bool playtestPressed,
            bool exportJsonPressed,
            bool exportUsdaPressed,
            bool backPressed,
            bool togglePreviewModePressed = false,
            bool toggleCameraModePressed = false,
            int zoomDelta = 0)
        {
            MoveX = moveX;
            MoveZ = moveZ;
            ToolDelta = toolDelta;
            LayerDelta = layerDelta;
            PlacePressed = placePressed;
            ErasePressed = erasePressed;
            EyedropperPressed = eyedropperPressed;
            ToggleLabelsPressed = toggleLabelsPressed;
            PlaytestPressed = playtestPressed;
            ExportJsonPressed = exportJsonPressed;
            ExportUsdaPressed = exportUsdaPressed;
            BackPressed = backPressed;
            TogglePreviewModePressed = togglePreviewModePressed;
            ToggleCameraModePressed = toggleCameraModePressed;
            ZoomDelta = zoomDelta;
        }

        public int MoveX { get; }

        public int MoveZ { get; }

        public int ToolDelta { get; }

        public int LayerDelta { get; }

        public bool PlacePressed { get; }

        public bool ErasePressed { get; }

        public bool EyedropperPressed { get; }

        public bool ToggleLabelsPressed { get; }

        public bool PlaytestPressed { get; }

        public bool ExportJsonPressed { get; }

        public bool ExportUsdaPressed { get; }

        public bool BackPressed { get; }

        public bool TogglePreviewModePressed { get; }

        public bool ToggleCameraModePressed { get; }

        public int ZoomDelta { get; }
    }
}
