using System;
using System.Collections.Generic;
using System.IO;
using Hollow.Branches;
using Hollow.RoomDesigner;
using UnityEditor;
using UnityEngine;

namespace Hollow.Editor.Generation
{
    public static class Milestone55AssetGenerator
    {
        public const string LabRoomDirectory = "Assets/_Hollow/Data/Rooms/DeveloperLab";
        public const string DocsPath = "Docs/Milestone55DeveloperInspectionBranch.md";
        public const string ReportPath = "output/reports/m55_developer_inspection_branch.md";

        public static IReadOnlyList<string> LabRoomIds => DeveloperLabDefinition.RoomAssetIds;

        [MenuItem("Hollow/Generation/Generate Milestone 55 Assets")]
        public static void Generate()
        {
            Milestone54AssetGenerator.Generate();
            Directory.CreateDirectory(LabRoomDirectory);
            Directory.CreateDirectory(Path.GetDirectoryName(ReportPath) ?? "output/reports");
            WriteLabRooms();
            AssetDatabase.Refresh();
            CuratedRoomDesignerDraftGenerator.Generate();
            WriteText(DocsPath, DocumentationText());
            WriteText(ReportPath, DocumentationText());
            AssetDatabase.Refresh();
            Debug.Log("Generated Hollow Milestone 55 Developer Lab docs/report.");
        }

        private static void WriteLabRooms()
        {
            for (var index = 0; index < LabRoomIds.Count; index++)
            {
                var project = RoomDesignerProject.CreateDefault(RoomDesignerFootprintPreset.Wide2x1, LabTitle(index + 1));
                project.projectId = LabRoomIds[index];
                project.displayName = LabTitle(index + 1);
                project.cells.RemoveAll(cell =>
                    cell.kind == RoomDesignerCellKinds.Rock ||
                    cell.kind == RoomDesignerCellKinds.Hole ||
                    cell.kind == RoomDesignerCellKinds.Spike);
                project.markers.Clear();
                AddCommonMarkers(project);
                AddRoomSpecificAuthoring(project, index + 1);
                foreach (var door in project.doorPorts)
                {
                    door.state = door.id == "west_0" || door.id == "east_0"
                        ? RoomDesignerDoorKinds.Door
                        : RoomDesignerDoorKinds.Inactive;
                }

                var validation = RoomDesignerDraftValidator.Validate(project);
                if (!validation.IsValid)
                {
                    throw new InvalidOperationException($"M55 lab room '{project.projectId}' failed Room Designer validation: {string.Join("; ", validation.Errors)}");
                }

                var manifest = RoomDesignerCompiler.BuildManifest(project);
                manifest.hollowRuntime.canonicalRoomId = project.projectId;
                manifest.hollowRuntime.displayName = project.displayName;
                manifest.hollowRuntime.roomType = "developer-lab";
                manifest.hollowRuntime.rewardType = "inspection-only";
                manifest.hollowRuntime.prototypeStatus = "m55-developer-lab";
                var outputPath = $"{LabRoomDirectory}/{project.projectId}.hollowruntime.json";
                File.WriteAllText(outputPath, JsonUtility.ToJson(manifest, prettyPrint: true));
                AssetDatabase.ImportAsset(outputPath, ImportAssetOptions.ForceUpdate);
            }
        }

        private static void AddCommonMarkers(RoomDesignerProject project)
        {
            project.markers.Add(new RoomDesignerMarker("spawn_safeStart", RoomDesignerMarkerKinds.SafeStart, -10f, 0f, 0f));
            project.markers.Add(new RoomDesignerMarker("spawn_enemy_inspection_anchor", RoomDesignerMarkerKinds.EnemyNormal, -8f, 0f, -1f));
            project.markers.Add(new RoomDesignerMarker("spawn_reward_0", RoomDesignerMarkerKinds.RoomReward, 9.5f, 0f, 0f));
        }

        private static void AddRoomSpecificAuthoring(RoomDesignerProject project, int roomNumber)
        {
            switch (roomNumber)
            {
                case 1:
                    project.cells.Add(new RoomDesignerCell(-8, 1, 0, RoomDesignerCellKinds.Rock));
                    project.cells.Add(new RoomDesignerCell(-5, 1, 0, RoomDesignerCellKinds.Hole));
                    project.cells.Add(new RoomDesignerCell(-2, 1, 0, RoomDesignerCellKinds.Spike));
                    project.markers.Add(new RoomDesignerMarker("barrel_standard_0", RoomDesignerMarkerKinds.StandardBarrel, 1f, 0f, 1f));
                    project.markers.Add(new RoomDesignerMarker("barrel_explosive_0", RoomDesignerMarkerKinds.ExplosiveBarrel, 4f, 0f, 1f));
                    break;
                case 4:
                    AddEnemyMarkers(project);
                    break;
                case 6:
                    project.cells.Add(new RoomDesignerCell(-7, 0, 0, RoomDesignerCellKinds.Spike));
                    project.cells.Add(new RoomDesignerCell(-3, 0, 0, RoomDesignerCellKinds.Hole));
                    project.markers.Add(new RoomDesignerMarker("barrel_standard_0", RoomDesignerMarkerKinds.StandardBarrel, 1f, 0f, 0f));
                    project.markers.Add(new RoomDesignerMarker("barrel_explosive_0", RoomDesignerMarkerKinds.ExplosiveBarrel, 4f, 0f, 0f));
                    project.markers.Add(new RoomDesignerMarker("barrel_explosive_1", RoomDesignerMarkerKinds.ExplosiveBarrel, 5.1f, 0f, 0f));
                    break;
                case 8:
                case 9:
                case 10:
                    project.markers.Add(new RoomDesignerMarker("spawn_boss_anchor_0", RoomDesignerMarkerKinds.EnemyHeavy, 0f, 0f, 0f));
                    break;
            }
        }

        private static void AddEnemyMarkers(RoomDesignerProject project)
        {
            var kinds = new[]
            {
                RoomDesignerMarkerKinds.EnemyNormal,
                RoomDesignerMarkerKinds.EnemyFlying,
                RoomDesignerMarkerKinds.EnemyFast,
                RoomDesignerMarkerKinds.EnemyHeavy,
                RoomDesignerMarkerKinds.EnemyCharger,
                RoomDesignerMarkerKinds.EnemyTurret,
                RoomDesignerMarkerKinds.EnemySplitter,
                RoomDesignerMarkerKinds.EnemySpittingPod,
                RoomDesignerMarkerKinds.EnemyRat,
                RoomDesignerMarkerKinds.EnemySpider
            };
            for (var index = 0; index < kinds.Length; index++)
            {
                project.markers.Add(new RoomDesignerMarker($"spawn_enemy_{index}", kinds[index], -9f + index * 3f, 0f, 0.8f));
            }
        }

        private static string LabTitle(int roomNumber)
        {
            return roomNumber switch
            {
                1 => "Developer Lab 01 - Environment Basics",
                2 => "Developer Lab 02 - Economy And Sustain",
                3 => "Developer Lab 03 - Build Pickups",
                4 => "Developer Lab 04 - Enemy Gallery",
                5 => "Developer Lab 05 - Projectile VFX Gallery",
                6 => "Developer Lab 06 - Hazard Physics Lane",
                7 => "Developer Lab 07 - Progression Props",
                8 => "Developer Lab 08 - World 1 Boss Gallery",
                9 => "Developer Lab 09 - World 2 Boss Gallery",
                10 => "Developer Lab 10 - World 3 Boss Gallery",
                _ => $"Developer Lab {roomNumber:00}"
            };
        }

        private static void WriteText(string path, string contents)
        {
            var directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllText(path, contents);
        }

        private static string DocumentationText()
        {
            return @"# M55 Developer Inspection Branch + Debug Spawn Menu

M55 adds a non-persistent Developer Lab launched from the selected-profile main menu. It creates a fixed ten-room left-to-right Wide2x1 branch for inspecting environment pieces, pickups, enemies, bosses, VFX, portals, doors, and hazards.

## Runtime Rules
- `RuntimeSessionMode.DeveloperLab` never writes active run saves, challenge attempts, completions, or banked rewards.
- Lab rooms are pre-cleared so traversal remains open while frozen runtime entities are visible.
- Lab source rooms are generated under `Assets/_Hollow/Data/Rooms/DeveloperLab/` and mirrored into the curated Room Designer catalog as editable inspection drafts.
- Lab enemies and bosses are real runtime entities in `FrozenRuntime` inspection mode: they keep visuals and health, but do not move, attack, contact-damage, summon, or block room clear.
- The bottom-right `Debug Spawn` button opens the debug spawn menu in editor/development gameplay routes. Menu buttons change group/entity, spawn in front of the player, and toggle live/frozen mode.
- Manual debug spawns are live by default but non-authoritative: they never count for room clear, persistence, challenge records, rewards, or branch progression.

## Room Layout
1. Environment basics
2. Economy and sustain
3. Weapons, armor, items, cards, set pieces
4. Normal enemy gallery
5. Projectile/VFX/audio cue gallery
6. Live hazard/physics lane
7. Hub/progression props
8. World 1 boss gallery
9. World 2 boss gallery
10. World 3 boss gallery
";
        }
    }
}
