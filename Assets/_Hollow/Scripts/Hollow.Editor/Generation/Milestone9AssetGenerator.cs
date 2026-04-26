using System;
using System.Collections.Generic;
using System.IO;
using Hollow.Data.Definitions;
using Hollow.Presentation;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEngine;

namespace Hollow.Editor.Generation
{
    public static class Milestone9AssetGenerator
    {
        public const string Root = "Assets/_Hollow";
        public const string MaterialDirectory = Root + "/Art/Materials/Prototype";
        public const string PresentationDataDirectory = Root + "/Data/Presentation";
        public const string VfxCueDirectory = PresentationDataDirectory + "/VFX";
        public const string AudioCueDirectory = PresentationDataDirectory + "/Audio";
        public const string ResourcesPresentationDirectory = Root + "/Resources/Hollow/Presentation";
        public const string PalettePath = PresentationDataDirectory + "/MaterialPalette_Prototype.asset";
        public const string CatalogPath = ResourcesPresentationDirectory + "/PresentationContentCatalog.asset";
        public const string AddressablesGroupName = "Hollow Local Content";

        public static readonly string[] RequiredAddressableLabels =
        {
            "hollow.core",
            "hollow.rooms",
            "hollow.enemies",
            "hollow.player",
            "hollow.ui",
            "hollow.audio",
            "hollow.vfx",
            "hollow.designer",
            "hollow.data"
        };

        [MenuItem("Hollow/Generation/Generate Milestone 9 Assets")]
        public static void Generate()
        {
            Milestone8AssetGenerator.Generate();
            EnsureDirectories();

            var palette = GenerateMaterialPalette();
            var vfxCues = GenerateVfxCues();
            var audioCues = GenerateAudioCues();
            var catalog = GeneratePresentationCatalog(palette, vfxCues, audioCues);
            PresentationContentProvider.Configure(catalog);

            ApplyPrototypeMaterialsToPrefabs();
            ConfigureAddressables(palette, vfxCues, audioCues, catalog);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Generated Hollow Milestone 9 content pipeline, prototype materials, cues, catalog, and Addressables labels.");
        }

        private static void EnsureDirectories()
        {
            foreach (var path in new[]
            {
                MaterialDirectory,
                PresentationDataDirectory,
                VfxCueDirectory,
                AudioCueDirectory,
                ResourcesPresentationDirectory
            })
            {
                Directory.CreateDirectory(path);
            }
        }

        private static MaterialPaletteDefinition GenerateMaterialPalette()
        {
            var bindings = new List<MaterialRoleBinding>();
            foreach (MaterialRole role in Enum.GetValues(typeof(MaterialRole)))
            {
                var color = MaterialResolver.FallbackColorFor(role);
                var material = CreateOrUpdateMaterial(role, color);
                bindings.Add(new MaterialRoleBinding(role, material, color));
            }

            var palette = AssetDatabase.LoadAssetAtPath<MaterialPaletteDefinition>(PalettePath);
            if (palette == null)
            {
                palette = ScriptableObject.CreateInstance<MaterialPaletteDefinition>();
                AssetDatabase.CreateAsset(palette, PalettePath);
            }

            palette.Configure(bindings.ToArray());
            EditorUtility.SetDirty(palette);
            return palette;
        }

        private static Material CreateOrUpdateMaterial(MaterialRole role, Color color)
        {
            var path = $"{MaterialDirectory}/M_{role}.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = MaterialResolver.CreateRuntimeMaterial(color);
                AssetDatabase.CreateAsset(material, path);
            }

            material.name = $"M_{role}";
            material.color = color;
            EditorUtility.SetDirty(material);
            return material;
        }

        private static VfxCueDefinition[] GenerateVfxCues()
        {
            var cues = new List<VfxCueDefinition>();
            foreach (VfxCueId cueId in Enum.GetValues(typeof(VfxCueId)))
            {
                var path = $"{VfxCueDirectory}/VfxCue_{cueId}.asset";
                var cue = AssetDatabase.LoadAssetAtPath<VfxCueDefinition>(path);
                if (cue == null)
                {
                    cue = ScriptableObject.CreateInstance<VfxCueDefinition>();
                    AssetDatabase.CreateAsset(cue, path);
                }

                cue.Configure(cueId, null, DebugColorFor(cueId), 0.16f, nextCreateDebugPrimitive: true);
                EditorUtility.SetDirty(cue);
                cues.Add(cue);
            }

            return cues.ToArray();
        }

        private static AudioCueDefinition[] GenerateAudioCues()
        {
            var cues = new List<AudioCueDefinition>();
            foreach (AudioCueId cueId in Enum.GetValues(typeof(AudioCueId)))
            {
                var path = $"{AudioCueDirectory}/AudioCue_{cueId}.asset";
                var cue = AssetDatabase.LoadAssetAtPath<AudioCueDefinition>(path);
                if (cue == null)
                {
                    cue = ScriptableObject.CreateInstance<AudioCueDefinition>();
                    AssetDatabase.CreateAsset(cue, path);
                }

                cue.Configure(cueId, null, 0.8f, cueId is AudioCueId.DesignerPlace or AudioCueId.DesignerErase ? 0f : 0.65f);
                EditorUtility.SetDirty(cue);
                cues.Add(cue);
            }

            return cues.ToArray();
        }

        private static PresentationContentCatalog GeneratePresentationCatalog(MaterialPaletteDefinition palette, VfxCueDefinition[] vfxCues, AudioCueDefinition[] audioCues)
        {
            var catalog = AssetDatabase.LoadAssetAtPath<PresentationContentCatalog>(CatalogPath);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<PresentationContentCatalog>();
                AssetDatabase.CreateAsset(catalog, CatalogPath);
            }

            catalog.Configure(palette, vfxCues, audioCues);
            EditorUtility.SetDirty(catalog);
            return catalog;
        }

        private static void ApplyPrototypeMaterialsToPrefabs()
        {
            ApplyPrefabMaterial($"{Root}/Prefabs/Player/PlayerCharacter.prefab", MaterialRole.PlayerBody);
            ApplyPrefabMaterial($"{Root}/Prefabs/Combat/EnemyBase.prefab", MaterialRole.EnemyNormal);
            ApplyPrefabMaterial($"{Root}/Prefabs/Combat/ProjectileBase.prefab", MaterialRole.Projectile);
            ApplyPrefabMaterial($"{Root}/Prefabs/Rewards/RoomRewardPickup.prefab", MaterialRole.RewardPickup);
            ApplyPrefabMaterial($"{Root}/Prefabs/Rewards/HubReturnPortal.prefab", MaterialRole.HubReturnPortal);
        }

        private static void ApplyPrefabMaterial(string path, MaterialRole role)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null)
            {
                return;
            }

            foreach (var renderer in prefab.GetComponentsInChildren<Renderer>(includeInactive: true))
            {
                MaterialResolver.ApplyTo(renderer, role);
                EditorUtility.SetDirty(renderer);
            }

            EditorUtility.SetDirty(prefab);
        }

        private static void ConfigureAddressables(MaterialPaletteDefinition palette, VfxCueDefinition[] vfxCues, AudioCueDefinition[] audioCues, PresentationContentCatalog catalog)
        {
            var settings = AddressableAssetSettingsDefaultObject.GetSettings(create: true);
            foreach (var label in RequiredAddressableLabels)
            {
                settings.AddLabel(label, postEvent: false);
            }

            var group = settings.FindGroup(AddressablesGroupName) ?? settings.CreateGroup(
                AddressablesGroupName,
                setAsDefaultGroup: false,
                readOnly: false,
                postEvent: false,
                schemasToCopy: null,
                typeof(ContentUpdateGroupSchema),
                typeof(BundledAssetGroupSchema));

            MarkAddressable(settings, group, CatalogPath, "hollow.presentation.catalog", "hollow.data");
            MarkAddressable(settings, group, PalettePath, "hollow.presentation.palette.prototype", "hollow.data");
            MarkAddressable(settings, group, "Assets/_Hollow/Data/Rooms/Templates/combat_single_sample.hollowruntime.json", "hollow.rooms.combat_single_sample", "hollow.rooms");
            MarkAddressable(settings, group, "Assets/_Hollow/Prefabs/Rooms/RoomRuntimeRoot.prefab", "hollow.prefab.room_runtime_root", "hollow.rooms");
            MarkAddressable(settings, group, "Assets/_Hollow/Prefabs/Combat/EnemyBase.prefab", "hollow.prefab.enemy_base", "hollow.enemies");
            MarkAddressable(settings, group, "Assets/_Hollow/Prefabs/Combat/ProjectileBase.prefab", "hollow.prefab.projectile_base", "hollow.player");
            MarkAddressable(settings, group, "Assets/_Hollow/Prefabs/Player/PlayerCharacter.prefab", "hollow.prefab.player_character", "hollow.player");
            MarkAddressable(settings, group, "Assets/_Hollow/Prefabs/UI/MainMenuRoot.prefab", "hollow.prefab.main_menu_root", "hollow.ui");
            MarkAddressable(settings, group, "Assets/_Hollow/Prefabs/Designer/RoomDesignerRoot.prefab", "hollow.prefab.room_designer_root", "hollow.designer");

            foreach (MaterialRole role in Enum.GetValues(typeof(MaterialRole)))
            {
                MarkAddressable(settings, group, $"{MaterialDirectory}/M_{role}.mat", $"hollow.material.{role}", "hollow.core");
            }

            foreach (var cue in vfxCues)
            {
                MarkAddressable(settings, group, AssetDatabase.GetAssetPath(cue), $"hollow.vfx.{cue.CueId}", "hollow.vfx");
            }

            foreach (var cue in audioCues)
            {
                MarkAddressable(settings, group, AssetDatabase.GetAssetPath(cue), $"hollow.audio.{cue.CueId}", "hollow.audio");
            }

            EditorUtility.SetDirty(settings);
        }

        private static void MarkAddressable(AddressableAssetSettings settings, AddressableAssetGroup group, string path, string address, string label)
        {
            var guid = AssetDatabase.AssetPathToGUID(path);
            if (string.IsNullOrWhiteSpace(guid))
            {
                return;
            }

            var entry = settings.CreateOrMoveEntry(guid, group, readOnly: false, postEvent: false);
            entry.address = address;
            entry.SetLabel(label, true, force: true, postEvent: false);
        }

        private static Color DebugColorFor(VfxCueId cueId)
        {
            return cueId switch
            {
                VfxCueId.ProjectileFire => MaterialResolver.FallbackColorFor(MaterialRole.Projectile),
                VfxCueId.EnemyHit => MaterialResolver.FallbackColorFor(MaterialRole.CombatHitFlash),
                VfxCueId.EnemyDeath => MaterialResolver.FallbackColorFor(MaterialRole.EnemyNormal),
                VfxCueId.PlayerHit => MaterialResolver.FallbackColorFor(MaterialRole.PlayerBody),
                VfxCueId.RewardClaim => MaterialResolver.FallbackColorFor(MaterialRole.RewardPickup),
                VfxCueId.DoorUnlock => MaterialResolver.FallbackColorFor(MaterialRole.DoorCleared),
                VfxCueId.RoomClear => MaterialResolver.FallbackColorFor(MaterialRole.DoorCleared),
                VfxCueId.PortalComplete => MaterialResolver.FallbackColorFor(MaterialRole.HubReturnPortal),
                VfxCueId.DesignerPlace => MaterialResolver.FallbackColorFor(MaterialRole.DesignerCursor),
                VfxCueId.DesignerErase => MaterialResolver.FallbackColorFor(MaterialRole.DesignerHole),
                _ => Color.white
            };
        }
    }
}
