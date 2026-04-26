using System;
using System.IO;
using System.Linq;
using Hollow.Data.Definitions;
using Hollow.Editor.Generation;
using Hollow.Editor.Validation;
using Hollow.Presentation;
using Hollow.Rooms;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.AddressableAssets;
using Object = UnityEngine.Object;

namespace Hollow.Tests.EditMode
{
    public sealed class Milestone9ContentPipelineTests
    {
        [SetUp]
        public void SetUp()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<PresentationContentCatalog>(Milestone9AssetGenerator.CatalogPath);
            PresentationContentProvider.Configure(catalog);
        }

        [TearDown]
        public void TearDown()
        {
            PresentationContentProvider.Reset();
        }

        [Test]
        public void PrototypePaletteResolvesEveryMaterialRole()
        {
            foreach (MaterialRole role in Enum.GetValues(typeof(MaterialRole)))
            {
                Assert.IsNotNull(MaterialResolver.Resolve(role), $"Missing material for {role}");
            }
        }

        [Test]
        public void RoomRuntimeBuilderUsesResolvedPrototypeMaterials()
        {
            var json = File.ReadAllText("Assets/_Hollow/Data/Rooms/Templates/combat_single_sample.hollowruntime.json");
            Assert.IsTrue(HollowRuntimeV2Importer.TryImport(json, out var asset, out var error), error);

            var rootObject = new GameObject("M9RoomRuntimeRoot");
            try
            {
                var root = rootObject.AddComponent<RoomRuntimeRoot>();
                root.BuildFrom(asset);

                var floorRenderer = rootObject.GetComponentsInChildren<Renderer>()
                    .FirstOrDefault(renderer => renderer.gameObject.name.StartsWith("tileGround.", StringComparison.Ordinal));
                Assert.IsNotNull(floorRenderer);
                Assert.AreSame(MaterialResolver.Resolve(MaterialRole.RoomFloor), floorRenderer.sharedMaterial);
                Assert.IsTrue(rootObject.GetComponentsInChildren<Renderer>().Length > 10);
            }
            finally
            {
                Object.DestroyImmediate(rootObject);
            }
        }

        [Test]
        public void VfxAndAudioPresentersHandlePlaceholderCuesSafely()
        {
            var root = new GameObject("M9CueRoot");
            try
            {
                var vfx = VfxPresenter.Play(VfxCueId.ProjectileFire, Vector3.zero, root.transform);
                var audio = AudioPresenter.Play(AudioCueId.ProjectileFire, Vector3.zero);

                Assert.IsNotNull(vfx, "Prototype VFX cues should create debug placeholders.");
                if (audio != null)
                {
                    Object.DestroyImmediate(audio.gameObject);
                }
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void ContentImportValidatorReportsGeneratedPipelineValid()
        {
            var report = ContentImportValidator.ValidateAll();
            Assert.IsTrue(report.IsValid, string.Join("\n", report.Failures));
        }

        [Test]
        public void AddressableAssetLoaderExposesFutureLoadBoundary()
        {
            var handle = AddressableAssetLoader.LoadAssetAsync<Material>("hollow.material.RoomFloor");
            Assert.IsTrue(handle.IsValid());
            Addressables.Release(handle);
        }
    }
}
