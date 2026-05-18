using System.Linq;
using Hollow.Data.Definitions;
using Hollow.Editor.Generation;
using Hollow.Presentation;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Hollow.Tests.EditMode
{
    public sealed class MeshyEnvironmentPropArtPassTests
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
        public void MeshyEnvironmentPropsUseCanonicalMaterialsAndRemainVisualOnly()
        {
            foreach (var spec in MeshyEnvironmentPropAssetGenerator.PropRows())
            {
                var material = AssetDatabase.LoadAssetAtPath<Material>(spec.MaterialPath);
                Assert.IsNotNull(material, spec.MaterialPath);
                AssertTexture(material, "_BaseMap", spec.AlbedoPath);
                AssertTexture(material, "_BumpMap", spec.NormalPath);
                AssertTexture(material, "_MetallicGlossMap", spec.MetallicPath);
                AssertTexture(material, "_EmissionMap", spec.EmissionPath);

                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(spec.PrefabPath);
                Assert.IsNotNull(prefab, spec.PrefabPath);
                Assert.AreEqual(spec.PrefabRootName, prefab.name, spec.DisplayName);
                Assert.That(Quaternion.Angle(Quaternion.identity, prefab.transform.localRotation), Is.LessThan(0.1f), spec.DisplayName);
                Assert.AreEqual(Vector3.zero, prefab.transform.localPosition, spec.DisplayName);
                Assert.AreEqual(Vector3.one, prefab.transform.localScale, spec.DisplayName);
                Assert.AreEqual(1, prefab.GetComponentsInChildren<PresentationVisualMarker>(includeInactive: true)
                    .Count(marker => marker.Role == spec.PrefabRole), spec.DisplayName);
                Assert.AreEqual(0, prefab.GetComponentsInChildren<Collider>(includeInactive: true).Length, spec.DisplayName);
                Assert.AreEqual(0, prefab.GetComponentsInChildren<Rigidbody>(includeInactive: true).Length, spec.DisplayName);
                Assert.AreEqual(0, prefab.GetComponentsInChildren<Animator>(includeInactive: true).Length, spec.DisplayName);
                Assert.AreEqual(0, prefab.GetComponentsInChildren<Camera>(includeInactive: true).Length, spec.DisplayName);
                Assert.AreEqual(0, prefab.GetComponentsInChildren<Light>(includeInactive: true).Length, spec.DisplayName);

                var model = prefab.transform.Find(spec.ModelName);
                Assert.IsNotNull(model, $"{spec.DisplayName} should contain the Meshy FBX model root.");
                var expectedRotation = Quaternion.Euler(spec.ModelLocalEuler);
                if (Vector3.Angle(Vector3.down, expectedRotation * Vector3.back) < 0.1f)
                {
                    Assert.That(
                        Vector3.Angle(Vector3.down, model.localRotation * Vector3.back),
                        Is.LessThan(0.1f),
                        $"{spec.DisplayName} should keep the Meshy -Z base axis pointing down.");
                }

                if (!spec.StraightenFootprintYaw && Mathf.Abs(spec.ModelYawCorrectionDegrees) < 0.001f)
                {
                    Assert.That(Quaternion.Angle(expectedRotation, model.localRotation), Is.LessThan(0.1f), spec.DisplayName);
                }

                var renderers = prefab.GetComponentsInChildren<Renderer>(includeInactive: true);
                Assert.Greater(renderers.Length, 0, spec.DisplayName);
                Assert.IsTrue(renderers
                    .SelectMany(renderer => renderer.sharedMaterials)
                    .All(slot => slot != null && AssetDatabase.GetAssetPath(slot) == spec.MaterialPath), spec.DisplayName);

                Assert.IsTrue(TryGetRendererBounds(prefab.transform, out var bounds), spec.DisplayName);
                Assert.That(bounds.min.y, Is.EqualTo(spec.TargetBottomLocalY).Within(0.015f), spec.DisplayName);
                Assert.LessOrEqual(bounds.size.x, spec.TargetBounds.x + 0.015f, spec.DisplayName);
                Assert.LessOrEqual(bounds.size.y, spec.TargetBounds.y + 0.015f, spec.DisplayName);
                Assert.LessOrEqual(bounds.size.z, spec.TargetBounds.z + 0.015f, spec.DisplayName);
            }
        }

        [Test]
        public void MeshyEnvironmentPropRolesResolveThroughPresentationCatalog()
        {
            foreach (var spec in MeshyEnvironmentPropAssetGenerator.PropRows())
            {
                var prefab = PresentationPrefabResolver.Resolve(spec.PrefabRole);
                Assert.IsNotNull(prefab, spec.DisplayName);
                Assert.AreEqual(spec.PrefabPath, AssetDatabase.GetAssetPath(prefab), spec.DisplayName);
                Assert.AreEqual(spec.PrefabRole, prefab.GetComponent<PresentationVisualMarker>().Role, spec.DisplayName);

                var material = MaterialResolver.Resolve(spec.MaterialRole);
                Assert.IsNotNull(material, spec.DisplayName);
                Assert.AreEqual(spec.MaterialPath, AssetDatabase.GetAssetPath(material), spec.DisplayName);
            }
        }

        [Test]
        public void MeshyEnvironmentPropVisualInstantiationKeepsGameplayHostColliders()
        {
            foreach (var spec in MeshyEnvironmentPropAssetGenerator.PropRows())
            {
                var host = GameObject.CreatePrimitive(PrimitiveType.Cube);
                try
                {
                    var hostCollider = host.GetComponent<Collider>();
                    var visual = PresentationPrefabResolver.InstantiateVisual(spec.PrefabRole, host.transform, Vector3.zero, Vector3.one);

                    Assert.IsNotNull(hostCollider, spec.DisplayName);
                    Assert.IsNotNull(visual, spec.DisplayName);
                    Assert.IsNotNull(visual.transform.Find(spec.ModelName), spec.DisplayName);
                    Assert.AreEqual(0, visual.GetComponentsInChildren<Collider>(includeInactive: true).Length, spec.DisplayName);
                    Assert.AreEqual(hostCollider, host.GetComponent<Collider>(), spec.DisplayName);
                }
                finally
                {
                    Object.DestroyImmediate(host);
                }
            }
        }

        private static void AssertTexture(Material material, string propertyName, string expectedPath)
        {
            Assert.IsTrue(material.HasProperty(propertyName), $"{material.name} missing {propertyName}");
            var texture = material.GetTexture(propertyName);
            Assert.IsNotNull(texture, $"{material.name} missing texture {propertyName}");
            Assert.AreEqual(expectedPath, AssetDatabase.GetAssetPath(texture), $"{material.name} {propertyName}");
        }

        private static bool TryGetRendererBounds(Transform root, out Bounds bounds)
        {
            bounds = default;
            var renderers = root.GetComponentsInChildren<Renderer>(includeInactive: true);
            if (renderers.Length == 0)
            {
                return false;
            }

            bounds = renderers[0].bounds;
            for (var index = 1; index < renderers.Length; index++)
            {
                bounds.Encapsulate(renderers[index].bounds);
            }

            return bounds.size.sqrMagnitude > 0.0001f;
        }
    }
}
