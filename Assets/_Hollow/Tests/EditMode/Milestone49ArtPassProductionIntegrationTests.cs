using System;
using System.IO;
using System.Linq;
using Hollow.Data.Definitions;
using Hollow.Editor.Generation;
using Hollow.Editor.Validation;
using Hollow.Presentation;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Hollow.Tests.EditMode
{
    public sealed class Milestone49ArtPassProductionIntegrationTests
    {
        [Test]
        public void ProductionStatusReportCoversEveryPresentationRole()
        {
            var report = ArtPassProductionValidator.BuildReport();
            Assert.AreEqual(Enum.GetValues(typeof(PresentationPrefabRole)).Length, report.totalTargets);

            foreach (PresentationPrefabRole role in Enum.GetValues(typeof(PresentationPrefabRole)))
            {
                Assert.IsTrue(report.targets.Any(target => target.role == role.ToString()), $"Missing M49 status row for {role}.");
            }
        }

        [Test]
        public void CoreSliceRolesAreBoundAndSafeEvenWhenStillPrototype()
        {
            var report = ArtPassProductionValidator.BuildReport();
            var coreRows = report.targets.Where(target => target.corePriority).ToArray();

            Assert.IsNotEmpty(coreRows);
            Assert.IsTrue(coreRows.All(target => target.status != ArtPassProductionStatus.MissingBinding), "Core roles must have active ArtPass bindings.");
            Assert.IsTrue(coreRows.All(target => target.status != ArtPassProductionStatus.UnsafePrefab), "Core roles must be visual-only and safe.");
        }

        [Test]
        public void PlaceholderArtWarnsWithoutBlockingM49()
        {
            var report = ArtPassProductionValidator.BuildReport();

            Assert.Greater(report.prototypeFallbackCount, 0, "Generated placeholder art should be classified as PrototypeFallback until Rafal replacements arrive.");
            Assert.AreEqual(0, report.missingBindingCount);
            Assert.AreEqual(0, report.unsafePrefabCount);
        }

        [Test]
        public void UnsafeVisualPrefabWithColliderIsRejected()
        {
            var root = new GameObject("UnsafeArtPassPrefab");
            try
            {
                root.AddComponent<PresentationVisualMarker>().Configure(PresentationPrefabRole.Player, isFallback: false);
                var child = GameObject.CreatePrimitive(PrimitiveType.Cube);
                child.transform.SetParent(root.transform, false);

                var errors = ArtPassProductionValidator.ValidatePrefabSafetyForTests(root, PresentationPrefabRole.Player);

                Assert.IsTrue(errors.Any(error => error.Contains("collider", StringComparison.OrdinalIgnoreCase)));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void Milestone49ReportsAndValidatorArePresent()
        {
            Assert.IsTrue(File.Exists(Milestone49AssetGenerator.ReportJsonPath), Milestone49AssetGenerator.ReportJsonPath);
            Assert.IsTrue(File.Exists(Milestone49AssetGenerator.ReportMarkdownPath), Milestone49AssetGenerator.ReportMarkdownPath);
            Assert.IsTrue(File.Exists(Milestone49AssetGenerator.PdfPath), Milestone49AssetGenerator.PdfPath);
            Assert.IsTrue(Milestone49Validator.Validate());
        }
    }
}
