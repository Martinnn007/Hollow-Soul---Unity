using System;
using System.Collections.Generic;
using System.Linq;
using Hollow.Rooms;
using UnityEngine;

namespace Hollow.RoomDesigner
{
    [Serializable]
    public sealed class RoomDesignerValidationReport
    {
        public string projectId = string.Empty;
        public string displayName = string.Empty;
        public RoomDesignerFootprintPreset footprintPreset;
        public bool isValid;
        public List<string> errors = new();
        public List<string> warnings = new();

        public IReadOnlyList<string> Errors => errors;

        public IReadOnlyList<string> Warnings => warnings;

        public bool IsValid => isValid;

        public void AddError(string message)
        {
            if (!string.IsNullOrWhiteSpace(message))
            {
                errors.Add(message);
                isValid = false;
            }
        }

        public void AddWarning(string message)
        {
            if (!string.IsNullOrWhiteSpace(message))
            {
                warnings.Add(message);
            }
        }

        public string Summary()
        {
            return isValid
                ? warnings.Count == 0 ? "Branch-ready" : $"Branch-ready with {warnings.Count} warning(s)"
                : $"Blocked: {errors.Count} error(s)";
        }
    }

    public static class RoomDesignerDraftValidator
    {
        private const float HighHoleRatio = 0.32f;

        public static RoomDesignerValidationReport Validate(RoomDesignerProject project)
        {
            var report = new RoomDesignerValidationReport
            {
                projectId = project?.projectId ?? string.Empty,
                displayName = project?.displayName ?? string.Empty,
                footprintPreset = project?.footprintPreset ?? RoomDesignerFootprintPreset.Single1x1,
                isValid = true
            };

            if (project == null)
            {
                report.AddError("Project is missing.");
                return report;
            }

            if (!Enum.IsDefined(typeof(RoomDesignerFootprintPreset), project.footprintPreset))
            {
                report.AddError("Unsupported room footprint preset.");
            }

            var enabledPorts = (project.doorPorts ?? new List<RoomDesignerDoorPortState>())
                .Where(port => port != null && port.state != RoomDesignerDoorKinds.Inactive)
                .ToList();
            if (enabledPorts.Count == 0)
            {
                report.AddError("At least one non-inactive exposed door port is required.");
            }

            ValidateSafeStart(project, report);
            ValidateMarkers(project, report);
            ValidateWarnings(project, enabledPorts, report);
            ValidateRuntimeImport(project, report);
            report.isValid = report.errors.Count == 0;
            return report;
        }

        private static void ValidateSafeStart(RoomDesignerProject project, RoomDesignerValidationReport report)
        {
            var safeStarts = (project.markers ?? new List<RoomDesignerMarker>())
                .Where(marker => marker?.kind == RoomDesignerMarkerKinds.SafeStart)
                .ToList();
            if (safeStarts.Count != 1)
            {
                report.AddError("Exactly one safe-start marker is required.");
                return;
            }

            if (!IsWalkablePlacement(project, safeStarts[0], out var reason))
            {
                report.AddError($"Safe-start marker is invalid: {reason}.");
            }
        }

        private static void ValidateMarkers(RoomDesignerProject project, RoomDesignerValidationReport report)
        {
            var markers = project.markers ?? new List<RoomDesignerMarker>();
            if (!markers.Any(marker => marker != null && RoomDesignerMarkerKinds.IsEnemy(marker.kind)))
            {
                report.AddError("At least one enemy spawn marker is required.");
            }

            var seenIds = new HashSet<string>();
            foreach (var marker in markers.Where(marker => marker != null))
            {
                if (string.IsNullOrWhiteSpace(marker.id))
                {
                    report.AddError($"Marker '{marker.kind}' is missing an id.");
                }
                else if (!seenIds.Add(marker.id))
                {
                    report.AddError($"Marker id '{marker.id}' is duplicated.");
                }

                if (marker.kind == RoomDesignerMarkerKinds.RoomReward || RoomDesignerMarkerKinds.IsEnemy(marker.kind))
                {
                    if (!IsWalkablePlacement(project, marker, out var reason))
                    {
                        report.AddError($"Marker '{marker.id}' is invalid: {reason}.");
                    }
                }
            }
        }

        private static void ValidateWarnings(RoomDesignerProject project, IReadOnlyList<RoomDesignerDoorPortState> enabledPorts, RoomDesignerValidationReport report)
        {
            var markers = project.markers ?? new List<RoomDesignerMarker>();
            var occupiedChunks = RoomDesignerFootprintUtility.OccupiedCells(project.footprintPreset).Count;
            var enemyCount = markers.Count(marker => marker != null && RoomDesignerMarkerKinds.IsEnemy(marker.kind));
            if (occupiedChunks > 1 && enemyCount < occupiedChunks * 2)
            {
                report.AddWarning("Low enemy-anchor density for a macro room.");
            }

            if (!markers.Any(marker => marker?.kind == RoomDesignerMarkerKinds.RoomReward))
            {
                report.AddWarning("No room reward marker is authored.");
            }

            if (enabledPorts.Count > 1 && enabledPorts.Select(port => port.direction).Distinct().Count() == 1)
            {
                report.AddWarning("All enabled door ports are on one side.");
            }

            var groundCount = CountCells(project, RoomDesignerCellKinds.Ground);
            var holeCount = CountCells(project, RoomDesignerCellKinds.Hole);
            if (groundCount > 0 && holeCount / (float)groundCount > HighHoleRatio)
            {
                report.AddWarning("Many holes reduce walkable coverage.");
            }

            if (markers.Any(marker => marker?.kind == RoomDesignerMarkerKinds.Enemy))
            {
                report.AddWarning("Legacy generic enemy markers export as spawnEnemyNormal.");
            }
        }

        private static void ValidateRuntimeImport(RoomDesignerProject project, RoomDesignerValidationReport report)
        {
            try
            {
                var json = RoomDesignerCompiler.ExportRuntimeJson(project, prettyPrint: false);
                if (!HollowRuntimeV2Importer.TryImport(json, out _, out var error))
                {
                    report.AddError($"Runtime import failed: {error}");
                }
            }
            catch (Exception exception)
            {
                report.AddError($"Runtime export failed: {exception.Message}");
            }
        }

        private static bool IsWalkablePlacement(RoomDesignerProject project, RoomDesignerMarker marker, out string reason)
        {
            var x = Mathf.RoundToInt(marker.x);
            var z = Mathf.RoundToInt(marker.z);
            if (!RoomDesignerFootprintUtility.ContainsTile(project.footprintPreset, x, z))
            {
                reason = "outside the footprint";
                return false;
            }

            var cells = project.cells ?? new List<RoomDesignerCell>();
            if (cells.Any(cell => cell.x == x && cell.z == z && cell.layer == 0 && cell.kind == RoomDesignerCellKinds.Hole))
            {
                reason = "on a hole";
                return false;
            }

            if (!cells.Any(cell => cell.x == x && cell.z == z && cell.layer == 0 && cell.kind == RoomDesignerCellKinds.Ground))
            {
                reason = "not on ground";
                return false;
            }

            if (cells.Any(cell => cell.x == x && cell.z == z && cell.kind == RoomDesignerCellKinds.Rock))
            {
                reason = "on a blocking rock";
                return false;
            }

            reason = string.Empty;
            return true;
        }

        private static int CountCells(RoomDesignerProject project, string kind)
        {
            return project.cells?.Count(cell => cell.kind == kind) ?? 0;
        }
    }
}
