using System.Collections.Generic;
using System.Linq;
using Hollow.Rooms;
using UnityEngine;

namespace Hollow.Branches
{
    public static class ApprovedDesignerRoomImporter
    {
        public static ApprovedDesignerRoomImportReport ImportApprovedRooms(IEnumerable<TextAsset> templates)
        {
            var validRooms = new List<ImportedRoomRuntimeAsset>();
            var errors = new List<string>();
            var seenIds = new HashSet<string>();

            foreach (var template in templates ?? Enumerable.Empty<TextAsset>())
            {
                if (template == null)
                {
                    continue;
                }

                if (!TryImportApprovedRoom(template, out var asset, out var error))
                {
                    errors.Add(error);
                    continue;
                }

                if (!seenIds.Add(asset.Id))
                {
                    errors.Add($"Approved room '{template.name}' duplicates canonicalRoomId '{asset.Id}'.");
                    continue;
                }

                validRooms.Add(asset);
            }

            return new ApprovedDesignerRoomImportReport(validRooms, errors);
        }

        public static bool TryImportApprovedRoom(TextAsset template, out ImportedRoomRuntimeAsset asset, out string error)
        {
            asset = null;
            error = string.Empty;
            if (template == null)
            {
                error = "Approved room template is null.";
                return false;
            }

            if (!HollowRuntimeV2Importer.TryImport(template.text, out asset, out var importError))
            {
                error = $"Approved room '{template.name}' failed HollowRuntime V2 import: {importError}";
                return false;
            }

            if (string.IsNullOrWhiteSpace(asset.Id))
            {
                error = $"Approved room '{template.name}' is missing canonicalRoomId.";
                return false;
            }

            if (!RoomFootprintShapeUtility.IsSupported(asset.Footprint))
            {
                error = $"Approved room '{asset.Id}' has unsupported footprint.";
                return false;
            }

            if (asset.Layout?.WalkableTiles == null || asset.Layout.WalkableTiles.Count == 0)
            {
                error = $"Approved room '{asset.Id}' has no walkable floor tiles.";
                return false;
            }

            if (asset.SafeStart?.position == null)
            {
                error = $"Approved room '{asset.Id}' is missing player safe start.";
                return false;
            }

            if (asset.EnemySpawns == null || asset.EnemySpawns.Count == 0)
            {
                error = $"Approved room '{asset.Id}' must contain at least one enemy spawn.";
                return false;
            }

            return true;
        }
    }

    public sealed class ApprovedDesignerRoomImportReport
    {
        public ApprovedDesignerRoomImportReport(IReadOnlyList<ImportedRoomRuntimeAsset> validRooms, IReadOnlyList<string> errors)
        {
            ValidRooms = validRooms ?? System.Array.Empty<ImportedRoomRuntimeAsset>();
            Errors = errors ?? System.Array.Empty<string>();
        }

        public IReadOnlyList<ImportedRoomRuntimeAsset> ValidRooms { get; }

        public IReadOnlyList<string> Errors { get; }

        public bool HasErrors => Errors.Count > 0;
    }
}
