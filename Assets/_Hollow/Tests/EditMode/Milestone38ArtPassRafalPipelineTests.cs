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
    }
}
