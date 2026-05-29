using System;
using System.Collections.Generic;
using Hollow.Core.Diagnostics;
using Hollow.Data.Definitions;
using Hollow.Presentation;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Hollow.Tests.EditMode
{
    public sealed class Milestone9PresentationBiomeLookupCacheTests
    {
        private readonly List<Object> createdObjects = new();

        [SetUp]
        public void SetUp()
        {
            M136PerformanceOperationCounters.Reset();
            PresentationContentProvider.Reset();
        }

        [TearDown]
        public void TearDown()
        {
            PresentationContentProvider.Reset();
            for (var index = createdObjects.Count - 1; index >= 0; index--)
            {
                if (createdObjects[index] != null)
                {
                    Object.DestroyImmediate(createdObjects[index]);
                }
            }

            createdObjects.Clear();
        }

        [Test]
        public void PresentationContentCatalogUsesLookupCacheAndInvalidatesAfterConfigure()
        {
            var catalog = Track(ScriptableObject.CreateInstance<PresentationContentCatalog>());
            var firstPrefab = Track(new GameObject("M9FirstPrefab"));
            var secondPrefab = Track(new GameObject("M9SecondPrefab"));
            var firstVfx = Track(ScriptableObject.CreateInstance<VfxCueDefinition>());
            var secondVfx = Track(ScriptableObject.CreateInstance<VfxCueDefinition>());
            var firstAudio = Track(ScriptableObject.CreateInstance<AudioCueDefinition>());
            var secondAudio = Track(ScriptableObject.CreateInstance<AudioCueDefinition>());
            var firstClip = Track(AudioClip.Create("M9FirstClip", 1, 1, 44100, false));
            var secondClip = Track(AudioClip.Create("M9SecondClip", 1, 1, 44100, false));
            firstVfx.Configure(VfxCueId.RoomClear, firstPrefab, Color.white, 0.1f, false);
            secondVfx.Configure(VfxCueId.RoomClear, secondPrefab, Color.green, 0.2f, false);
            firstAudio.Configure(AudioCueId.RoomClear, firstClip, 0.5f, 0.6f);
            secondAudio.Configure(AudioCueId.RoomClear, secondClip, 0.7f, 0.2f);

            catalog.Configure(
                null,
                new[] { firstVfx },
                new[] { firstAudio },
                new[] { new PresentationPrefabBinding(PresentationPrefabRole.RoomFloor, firstPrefab) });
            Assert.IsTrue(catalog.TryGetPrefab(PresentationPrefabRole.RoomFloor, out var resolvedPrefab));
            Assert.AreSame(firstPrefab, resolvedPrefab);
            Assert.IsTrue(catalog.TryGetVfxCue(VfxCueId.RoomClear, out var resolvedVfx));
            Assert.AreSame(firstVfx, resolvedVfx);
            Assert.IsTrue(catalog.TryGetAudioCue(AudioCueId.RoomClear, out var resolvedAudio));
            Assert.AreSame(firstAudio, resolvedAudio);

            catalog.Configure(
                null,
                new[] { secondVfx },
                new[] { secondAudio },
                new[] { new PresentationPrefabBinding(PresentationPrefabRole.RoomFloor, secondPrefab) });

            Assert.IsTrue(catalog.TryGetPrefab(PresentationPrefabRole.RoomFloor, out resolvedPrefab));
            Assert.AreSame(secondPrefab, resolvedPrefab);
            Assert.IsTrue(catalog.TryGetVfxCue(VfxCueId.RoomClear, out resolvedVfx));
            Assert.AreSame(secondVfx, resolvedVfx);
            Assert.IsTrue(catalog.TryGetAudioCue(AudioCueId.RoomClear, out resolvedAudio));
            Assert.AreSame(secondAudio, resolvedAudio);
        }

        [Test]
        public void MaterialPaletteUsesLookupCacheAndInvalidatesAfterConfigure()
        {
            var palette = Track(ScriptableObject.CreateInstance<MaterialPaletteDefinition>());
            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            var first = Track(new Material(shader) { color = Color.red });
            var second = Track(new Material(shader) { color = Color.blue });

            palette.Configure(new[]
            {
                new MaterialRoleBinding(MaterialRole.RoomFloor, first, Color.red)
            });
            Assert.IsTrue(palette.TryResolve(MaterialRole.RoomFloor, out var resolved));
            Assert.AreSame(first, resolved);
            Assert.IsTrue(palette.TryGetFallbackColor(MaterialRole.RoomFloor, out var fallback));
            Assert.AreEqual(Color.red, fallback);

            palette.Configure(new[]
            {
                new MaterialRoleBinding(MaterialRole.RoomFloor, second, Color.blue)
            });
            Assert.IsTrue(palette.TryResolve(MaterialRole.RoomFloor, out resolved));
            Assert.AreSame(second, resolved);
            Assert.IsTrue(palette.TryGetFallbackColor(MaterialRole.RoomFloor, out fallback));
            Assert.AreEqual(Color.blue, fallback);
        }

        [Test]
        public void BiomePrewarmMakesRoomVisualLookupsCacheOnly()
        {
            RoomBiomePresentationResolver.Prewarm(RoomBiomeIds.HollowThreshold);
            M136PerformanceOperationCounters.Reset();

            var material = RoomBiomePresentationResolver.ResolveMaterial(RoomBiomeIds.HollowThreshold, MaterialRole.RoomFloor);
            var prefab = RoomBiomePresentationResolver.ResolvePrefab(RoomBiomeIds.HollowThreshold, PresentationPrefabRole.RoomFloor);
            var decorResolved = RoomBiomePresentationResolver.TryResolveDecorPrefabRole(
                RoomBiomeIds.HollowThreshold,
                RoomBiomeDecorKinds.GrassTuft,
                out var decorRole);

            Assert.IsNotNull(material);
            Assert.IsNotNull(prefab);
            Assert.IsTrue(decorResolved);
            Assert.AreEqual(PresentationPrefabRole.DecorGrassTuft, decorRole);
            var snapshot = M136PerformanceOperationCounters.Snapshot();
            Assert.Greater(snapshot.PresentationBiomeCacheHits, 0);
            Assert.AreEqual(0, snapshot.PresentationMaterialCacheMisses);
            Assert.AreEqual(0, snapshot.PresentationPrefabCacheMisses);
        }

        [Test]
        public void SafeResolvedPrefabInstantiatesWithoutColliderStripOrPrefabMisses()
        {
            var catalog = Track(ScriptableObject.CreateInstance<PresentationContentCatalog>());
            var prefab = Track(new GameObject("M9ColliderFreePrefab", typeof(MeshFilter), typeof(MeshRenderer)));
            var firstParent = Track(new GameObject("M9FirstParent"));
            var secondParent = Track(new GameObject("M9SecondParent"));
            catalog.Configure(
                null,
                Array.Empty<VfxCueDefinition>(),
                Array.Empty<AudioCueDefinition>(),
                new[] { new PresentationPrefabBinding(PresentationPrefabRole.RoomFloor, prefab) });
            PresentationContentProvider.Configure(catalog);
            PresentationPrefabResolver.Resolve(PresentationPrefabRole.RoomFloor);
            M136PerformanceOperationCounters.Reset();

            var first = PresentationPrefabResolver.InstantiateVisual(
                PresentationPrefabRole.RoomFloor,
                firstParent.transform,
                Vector3.zero,
                Vector3.one);
            var second = PresentationPrefabResolver.InstantiateVisual(
                PresentationPrefabRole.RoomFloor,
                secondParent.transform,
                Vector3.zero,
                Vector3.one);

            Assert.IsNotNull(first);
            Assert.IsNotNull(second);
            var snapshot = M136PerformanceOperationCounters.Snapshot();
            Assert.GreaterOrEqual(snapshot.PresentationPrefabCacheHits, 2);
            Assert.AreEqual(0, snapshot.PresentationPrefabCacheMisses);
            Assert.AreEqual(0, snapshot.PresentationColliderStripPasses);
        }

        private T Track<T>(T instance) where T : Object
        {
            createdObjects.Add(instance);
            return instance;
        }
    }
}
