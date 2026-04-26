using System;
using System.IO;
using Hollow.RoomDesigner;
using Hollow.Rooms;
using UnityEditor;
using UnityEngine;

namespace Hollow.Editor.Generation
{
    public static class Milestone13AssetGenerator
    {
        public const string MacroFixtureDirectory = "Assets/_Hollow/Data/Rooms/MacroFixtures";

        public static readonly MacroFixtureSpec[] MacroFixtures =
        {
            new(RoomDesignerFootprintPreset.Single1x1, "combat_macro_single_1x1", "Combat Macro Single 1x1", 13, 7, 1, 4),
            new(RoomDesignerFootprintPreset.Wide2x1, "combat_macro_wide_2x1", "Combat Macro Wide 2x1", 26, 7, 2, 6),
            new(RoomDesignerFootprintPreset.Tall1x2, "combat_macro_tall_1x2", "Combat Macro Tall 1x2", 13, 14, 2, 6),
            new(RoomDesignerFootprintPreset.Block2x2, "combat_macro_block_2x2", "Combat Macro Block 2x2", 26, 14, 4, 8),
            new(RoomDesignerFootprintPreset.L3Cell, "combat_macro_l_3cell", "Combat Macro L 3-Cell", 26, 14, 3, 8)
        };

        [MenuItem("Hollow/Generation/Generate Milestone 13 Assets")]
        public static void Generate()
        {
            Milestone12AssetGenerator.Generate();
            Directory.CreateDirectory(MacroFixtureDirectory);
            foreach (var fixture in MacroFixtures)
            {
                var project = RoomDesignerProject.CreateDefault(fixture.Preset, fixture.DisplayName);
                project.projectId = fixture.RoomId;
                project.displayName = fixture.DisplayName;
                var manifest = RoomDesignerCompiler.BuildManifest(project);
                manifest.hollowRuntime.canonicalRoomId = fixture.RoomId;
                manifest.hollowRuntime.roomType = "combat";
                manifest.hollowRuntime.rewardType = "macro-fixture";
                manifest.hollowRuntime.prototypeStatus = "test-fixture";
                var runtimeJson = JsonUtility.ToJson(manifest, prettyPrint: true);
                File.WriteAllText(fixture.AssetPath, runtimeJson);
                AssetDatabase.ImportAsset(fixture.AssetPath, ImportAssetOptions.ForceUpdate);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Generated Hollow Milestone 13 macro-room fixtures.");
        }

        [Serializable]
        public readonly struct MacroFixtureSpec
        {
            public MacroFixtureSpec(RoomDesignerFootprintPreset preset, string roomId, string displayName, int widthTiles, int heightTiles, int occupiedCellCount, int doorPortCount)
            {
                Preset = preset;
                RoomId = roomId;
                DisplayName = displayName;
                WidthTiles = widthTiles;
                HeightTiles = heightTiles;
                OccupiedCellCount = occupiedCellCount;
                DoorPortCount = doorPortCount;
            }

            public RoomDesignerFootprintPreset Preset { get; }

            public string RoomId { get; }

            public string DisplayName { get; }

            public int WidthTiles { get; }

            public int HeightTiles { get; }

            public int OccupiedCellCount { get; }

            public int DoorPortCount { get; }

            public string AssetPath => $"{MacroFixtureDirectory}/{RoomId}.hollowruntime.json";
        }
    }
}
