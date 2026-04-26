using System;
using System.Collections.Generic;

namespace Hollow.RoomDesigner
{
    public sealed class RoomDesignerLibraryViewModel
    {
        public RoomDesignerLibraryViewModel(
            RoomDesignerMode mode,
            int selectedIndex,
            IReadOnlyList<RoomDesignerLibraryRow> drafts,
            IReadOnlyList<RoomDesignerTemplateRow> templates,
            string latestMessage,
            string pendingDeleteProjectId,
            string pendingDeleteDisplayName)
        {
            Mode = mode;
            SelectedIndex = selectedIndex;
            Drafts = drafts ?? Array.Empty<RoomDesignerLibraryRow>();
            Templates = templates ?? Array.Empty<RoomDesignerTemplateRow>();
            LatestMessage = latestMessage ?? string.Empty;
            PendingDeleteProjectId = pendingDeleteProjectId ?? string.Empty;
            PendingDeleteDisplayName = pendingDeleteDisplayName ?? string.Empty;
        }

        public RoomDesignerMode Mode { get; }

        public int SelectedIndex { get; }

        public IReadOnlyList<RoomDesignerLibraryRow> Drafts { get; }

        public IReadOnlyList<RoomDesignerTemplateRow> Templates { get; }

        public string LatestMessage { get; }

        public string PendingDeleteProjectId { get; }

        public string PendingDeleteDisplayName { get; }
    }

    public readonly struct RoomDesignerLibraryRow
    {
        public RoomDesignerLibraryRow(string projectId, string displayName, RoomDesignerFootprintPreset preset, int widthTiles, int heightTiles)
        {
            ProjectId = projectId ?? string.Empty;
            DisplayName = displayName ?? string.Empty;
            Preset = preset;
            WidthTiles = widthTiles;
            HeightTiles = heightTiles;
        }

        public string ProjectId { get; }

        public string DisplayName { get; }

        public RoomDesignerFootprintPreset Preset { get; }

        public int WidthTiles { get; }

        public int HeightTiles { get; }
    }

    public readonly struct RoomDesignerTemplateRow
    {
        public RoomDesignerTemplateRow(RoomDesignerFootprintPreset preset)
        {
            Preset = preset;
            DisplayName = RoomDesignerFootprintUtility.DisplayName(preset);
            var dimensions = RoomDesignerFootprintUtility.Dimensions(preset);
            WidthTiles = dimensions.x;
            HeightTiles = dimensions.y;
        }

        public RoomDesignerFootprintPreset Preset { get; }

        public string DisplayName { get; }

        public int WidthTiles { get; }

        public int HeightTiles { get; }
    }
}
