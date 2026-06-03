using System;
using System.Linq;
using Hollow.Data.Definitions;
using Hollow.Editor.Generation;
using Hollow.Editor.Validation;
using Hollow.Presentation;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Hollow.Tests.EditMode
{
    public sealed class Milestone38ArtPassRafalPipelineTests
    {
        [Test]
        public void TargetCatalogCoversEveryPresentationRole()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<ArtPassTargetCatalogDefinition>(Milestone38AssetGenerator.TargetCatalogPath);
            Assert.IsNotNull(catalog);

            foreach (PresentationPrefabRole role in Enum.GetValues(typeof(PresentationPrefabRole)))
            {
                Assert.IsTrue(catalog.Targets.Any(target => target != null && target.PrefabRole == role), $"Missing M38 target for {role}.");
            }

            Assert.IsTrue(catalog.Targets.Any(target => target.TargetId == "hub_shop_card" && target.Priority == ArtPassAssetTargetPriority.Critical));
            Assert.IsTrue(catalog.Targets.Any(target => target.TargetId == "boss_stone_warden" && target.RequiredForVerticalSlice));
        }

        [Test]
        public void NewM38PrefabRolesResolveToArtPassPrefabs()
        {
            var presentationCatalog = AssetDatabase.LoadAssetAtPath<PresentationContentCatalog>(Milestone9AssetGenerator.CatalogPath);
            Assert.IsNotNull(presentationCatalog);
            PresentationContentProvider.Configure(presentationCatalog);

            foreach (var role in new[]
            {
                PresentationPrefabRole.HubShopCard,
                PresentationPrefabRole.WeaponMelee,
                PresentationPrefabRole.WeaponRanged,
                PresentationPrefabRole.Armor,
                PresentationPrefabRole.ActiveItemPickup,
                PresentationPrefabRole.ConsumableCardPickup
            })
            {
                Assert.IsTrue(presentationCatalog.TryGetPrefab(role, out var prefab), $"Missing catalog prefab for {role}.");
                Assert.IsNotNull(prefab);
                Assert.IsTrue(AssetDatabase.GetAssetPath(prefab).StartsWith(Milestone23AssetGenerator.ArtPassRoot));
                Assert.IsNotNull(prefab.GetComponent<PresentationVisualMarker>());
            }
        }

        [Test]
        public void WeaponMeleeArtPassPrefabUsesMeshySilentBladeVisualOnlyAsset()
        {
            var presentationCatalog = AssetDatabase.LoadAssetAtPath<PresentationContentCatalog>(Milestone9AssetGenerator.CatalogPath);
            Assert.IsNotNull(presentationCatalog);
            Assert.IsTrue(presentationCatalog.TryGetPrefab(PresentationPrefabRole.WeaponMelee, out var boundPrefab));
            Assert.AreEqual(WeaponMeleeMeshyAssetGenerator.ArtPassWeaponMeleePrefabPath, AssetDatabase.GetAssetPath(boundPrefab));

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(WeaponMeleeMeshyAssetGenerator.ArtPassWeaponMeleePrefabPath);
            Assert.IsNotNull(prefab, "Run Meshy melee weapon generation before validating the ArtPass visual.");
            var errors = ArtPassProductionValidator.ValidatePrefabSafetyForTests(prefab, PresentationPrefabRole.WeaponMelee);
            Assert.IsEmpty(errors, string.Join("; ", errors));

            var renderers = prefab.GetComponentsInChildren<Renderer>(includeInactive: true);
            Assert.Greater(renderers.Length, 0);
            Assert.IsTrue(renderers
                    .SelectMany(renderer => renderer.sharedMaterials)
                    .Where(material => material != null)
                    .Any(material => AssetDatabase.GetAssetPath(material) == WeaponMeleeMeshyAssetGenerator.MeshyMaterialPath),
                "Melee weapon ArtPass renderers should use the imported Meshy Silent Blade material.");
            var instance = UnityEngine.Object.Instantiate(prefab);
            try
            {
                var activeRenderers = instance.GetComponentsInChildren<Renderer>(includeInactive: false)
                    .Where(renderer => renderer.enabled && renderer.gameObject.activeInHierarchy)
                    .ToArray();
                Assert.Greater(activeRenderers.Length, 0);
                var bounds = Encapsulate(activeRenderers);
                Assert.Greater(bounds.size.y, 0.7f, "Melee ArtPass sword should preserve the old local-Y weapon length footprint.");
                Assert.Greater(Mathf.Max(bounds.size.x, bounds.size.z), 0.08f, "Melee ArtPass sword should not be fitted as an edge-on sliver.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(instance);
            }

            Assert.AreEqual(0, prefab.GetComponentsInChildren<Collider>(includeInactive: true).Length);
            var markerScripts = prefab.GetComponentsInChildren<MonoBehaviour>(includeInactive: true);
            Assert.IsTrue(markerScripts.All(component => component is PresentationVisualMarker));
        }

        [Test]
        public void HubShopCardAttachesVisualWithoutGameplayColliders()
        {
            var root = GameObject.CreatePrimitive(PrimitiveType.Cube);
            try
            {
                var card = root.AddComponent<Hollow.Branches.HubShopCard>();
                card.Configure(Hollow.Branches.HubShopOffer.CreateSeededOffers(38001, 0, null, null)[0]);
                card.Refresh(runSouls: 99, runCoins: 99);

                var visual = root.GetComponentsInChildren<PresentationVisualMarker>(includeInactive: true)
                    .FirstOrDefault(marker => marker.Role == PresentationPrefabRole.HubShopCard);
                Assert.IsNotNull(visual);
                Assert.AreEqual(0, visual.GetComponentsInChildren<Collider>(includeInactive: true).Length);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void Milestone38ValidatorReportsGeneratedStateValid()
        {
            Assert.DoesNotThrow(() => Milestone38Validator.Validate());
        }

        private static Bounds Encapsulate(Renderer[] renderers)
        {
            Assert.Greater(renderers.Length, 0);
            var bounds = renderers[0].bounds;
            for (var index = 1; index < renderers.Length; index++)
            {
                bounds.Encapsulate(renderers[index].bounds);
            }

            return bounds;
        }
    }
}
