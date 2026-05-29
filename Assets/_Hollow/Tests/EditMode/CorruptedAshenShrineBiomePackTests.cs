using Hollow.Data.Definitions;
using Hollow.Editor.DesignerRooms;
using Hollow.Editor.Generation;
using Hollow.Editor.Validation;
using Hollow.Presentation;
using Hollow.RoomDesigner;
using Hollow.Rooms;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Hollow.Tests.EditMode
{
    public sealed class CorruptedAshenShrineBiomePackTests
    {
        [Test]
        public void LiveValidatorPasses()
        {
            CollectionAssert.IsEmpty(CorruptedAshenShrineBiomePackValidator.ValidateAll());
        }

        [Test]
        public void CatalogResolvesAshenShrineBiomeWithRequiredOverrides()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<RoomBiomeCatalogDefinition>(CorruptedAshenShrineBiomeAssetGenerator.BiomeCatalogPath);
            Assert.That(catalog, Is.Not.Null);
            Assert.That(catalog.TryGetBiome(RoomBiomeIds.CorruptedAshenShrine, out var biome), Is.True);
            Assert.That(biome.DisplayName, Is.EqualTo("Ashen Shrine"));

            foreach (var role in CorruptedAshenShrineBiomeAssetGenerator.RequiredMaterialRoles)
            {
                Assert.That(biome.TryResolve(role, out var material), Is.True, $"Missing material override for {role}.");
                Assert.That(material, Is.Not.Null, $"Null material override for {role}.");
            }

            foreach (var role in new[]
                     {
                         PresentationPrefabRole.DecorGrassTuft,
                         PresentationPrefabRole.DecorCrystalCluster,
                         PresentationPrefabRole.DecorSmallTree,
                         PresentationPrefabRole.DecorStoneRuin
                     })
            {
                Assert.That(biome.TryResolve(role, out var prefab), Is.True, $"Missing prefab override for {role}.");
                Assert.That(prefab, Is.Not.Null, $"Null prefab override for {role}.");
            }
        }

        [Test]
        public void CorruptedChestRoomImportsWithAshenShrineBiome()
        {
            var text = AssetDatabase.LoadAssetAtPath<TextAsset>(CorruptedAshenShrineBiomeAssetGenerator.CorruptedRoomPath);
            Assert.That(text, Is.Not.Null);

            var asset = HollowRuntimeV2Importer.Import(text.text);
            Assert.That(asset.BiomeId, Is.EqualTo(RoomBiomeIds.CorruptedAshenShrine));
        }

        [Test]
        public void NormalBiomeRoomRemainsUnchanged()
        {
            var text = AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/_Hollow/Data/Rooms/Biomes/VerdantRuins/verdant_macro_single_1x1.hollowruntime.json");
            Assert.That(text, Is.Not.Null);

            var asset = HollowRuntimeV2Importer.Import(text.text);
            Assert.That(asset.BiomeId, Is.EqualTo(RoomBiomeIds.VerdantRuins));
        }

        [Test]
        public void RoomDesignerPreviewResolvesAshenShrineFloorAndCorruptedChestFallback()
        {
            var floorHost = new GameObject("AshenShrineFloorPreviewHost");
            var chestHost = new GameObject("AshenShrineChestPreviewHost");
            try
            {
                var floorBuilt = RoomDesignerScenePreviewBuilder.BuildVisualForCell(
                    floorHost,
                    new RoomDesignerCell(0, 0, 0, RoomDesignerCellKinds.Ground),
                    RoomBiomeIds.CorruptedAshenShrine);
                var chestBuilt = RoomDesignerScenePreviewBuilder.BuildVisualForMarker(
                    chestHost,
                    new RoomDesignerMarker("spawn_point_corruptedChest", RoomDesignerMarkerKinds.CorruptedChestSpawn, 0f, 0f, 0f),
                    RoomBiomeIds.CorruptedAshenShrine);

                Assert.That(floorBuilt, Is.True);
                Assert.That(chestBuilt, Is.True);
                Assert.That(floorHost.GetComponentInChildren<Renderer>(), Is.Not.Null);
                Assert.That(chestHost.GetComponentInChildren<Renderer>(), Is.Not.Null);
            }
            finally
            {
                Object.DestroyImmediate(floorHost);
                Object.DestroyImmediate(chestHost);
            }
        }

        [Test]
        public void DesignerRoomScenePreviewUsesAshenShrineBiomeMaterialsFromSource()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var rootObject = new GameObject("CorruptedChestRoomRoot");
            var root = rootObject.AddComponent<DesignerRoomSceneMarker>();
            root.ConfigureAuthoring(
                "corrupted_chest_single_1x1",
                DesignerRoomSceneMarkerKind.RoomRoot,
                "combat",
                "corrupted_chest_single_1x1",
                CorruptedAshenShrineBiomeAssetGenerator.CorruptedRoomPath,
                "Corrupted room scene preview material test.",
                false,
                "Corrupted Chest Room",
                true,
                true,
                0.5f);

            DesignerRoomSceneAuthoringUtility.RefreshSceneFromSource(root);
            var preview = DesignerRoomSceneVisualPreviewBuilder.BuildPreview(
                rootObject.scene,
                includeLighting: false,
                includeCamera: false);

            AssertPreviewUsesMaterial(
                preview,
                RoomBiomePresentationResolver.ResolveMaterial(RoomBiomeIds.CorruptedAshenShrine, MaterialRole.RoomFloor));
            AssertPreviewUsesMaterial(
                preview,
                RoomBiomePresentationResolver.ResolveMaterial(RoomBiomeIds.CorruptedAshenShrine, MaterialRole.RoomWall));
            AssertPreviewHasFloorSurfaceAtYZero(
                preview,
                RoomBiomePresentationResolver.ResolveMaterial(RoomBiomeIds.CorruptedAshenShrine, MaterialRole.RoomFloor));

            var chestAnchor = preview.transform.Find("Spawns/Items/Corrupted Chest.spawn_corrupted_chest_0");
            Assert.That(chestAnchor, Is.Not.Null);
            Assert.That(chestAnchor.localPosition.y, Is.EqualTo(0f).Within(0.001f));
            Assert.That(chestAnchor.localScale.x, Is.EqualTo(1f).Within(0.001f));
            Assert.That(chestAnchor.localScale.y, Is.EqualTo(1f).Within(0.001f));
            Assert.That(chestAnchor.localScale.z, Is.EqualTo(1f).Within(0.001f));
        }

        private static void AssertPreviewUsesMaterial(GameObject preview, Material expected)
        {
            Assert.That(preview, Is.Not.Null);
            Assert.That(expected, Is.Not.Null);

            foreach (var renderer in preview.GetComponentsInChildren<Renderer>(includeInactive: true))
            {
                if (renderer != null && renderer.sharedMaterial == expected)
                {
                    return;
                }
            }

            Assert.Fail($"Preview did not use expected material '{expected.name}'.");
        }

        private static void AssertPreviewHasFloorSurfaceAtYZero(GameObject preview, Material expected)
        {
            Assert.That(preview, Is.Not.Null);
            Assert.That(expected, Is.Not.Null);

            foreach (var renderer in preview.GetComponentsInChildren<Renderer>(includeInactive: true))
            {
                if (renderer != null &&
                    renderer.sharedMaterial == expected &&
                    Mathf.Abs(renderer.bounds.max.y) <= 0.002f)
                {
                    return;
                }
            }

            Assert.Fail("Preview did not include a floor surface at y=0.");
        }
    }
}
