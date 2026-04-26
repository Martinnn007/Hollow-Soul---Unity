using System.IO;
using System.Linq;
using Hollow.Data.Definitions;
using Hollow.Editor.Build;
using Hollow.Editor.Generation;
using Hollow.Editor.Validation;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEngine;

namespace Hollow.Tests.EditMode
{
    public sealed class Milestone24PlatformBuildQaTests
    {
        [Test]
        public void PlatformQaProfileIncludesScenesOutputsBudgetsAndValidationChain()
        {
            var profile = AssetDatabase.LoadAssetAtPath<PlatformBuildQaProfileDefinition>(Milestone24AssetGenerator.PlatformBuildQaProfilePath);

            Assert.IsNotNull(profile, "Run M24 generation before validating the platform QA profile.");
            CollectionAssert.IsSubsetOf(Milestone11AssetGenerator.RequiredBuildScenes, profile.RequiredScenes);
            CollectionAssert.IsSubsetOf(Milestone24AssetGenerator.ValidationTypes, profile.ValidationTypes);
            Assert.AreEqual(Milestone24AssetGenerator.WindowsM24BuildFolderName, profile.WindowsBuildFolderName);
            Assert.IsTrue(profile.RequireAddressablesBuild);
            Assert.IsTrue(profile.AllowEnvironmentBlocks);
            Assert.IsNotNull(profile.PerformanceBudget);
            Assert.IsNotNull(profile.BuildAutomationProfile);
            StringAssert.Contains(Milestone24AssetGenerator.WindowsM24BuildFolderName, PlatformBuildQaRunner.PlannedWindowsBuildPath(profile));
        }

        [Test]
        public void QaReportRecalculatesEnvironmentBlocksAsAllowedOutcome()
        {
            var report = new PlatformBuildQaReport();
            report.targets.Add(PlatformBuildTargetResult.Passed("audit", "Editor", "output/reports/audit.json", 10, "OK"));
            report.targets.Add(PlatformBuildTargetResult.BlockedByEnvironment("visionos", "visionOS", string.Empty, 2, "Missing simulator.", "Install Xcode."));

            report.Recalculate(allowEnvironmentBlocks: true);
            Assert.AreEqual(PlatformBuildQaResult.PassedWithEnvironmentBlocks, report.result);

            report.targets.Add(PlatformBuildTargetResult.Failed("addressables", "Addressables", string.Empty, 1, "Broken catalog."));
            report.Recalculate(allowEnvironmentBlocks: true);
            Assert.AreEqual(PlatformBuildQaResult.Failed, report.result);
        }

        [Test]
        public void BootstrapQaReportSerializesToJsonAndMarkdown()
        {
            var profile = AssetDatabase.LoadAssetAtPath<PlatformBuildQaProfileDefinition>(Milestone24AssetGenerator.PlatformBuildQaProfilePath);
            Assert.IsNotNull(profile);

            var latestJson = Path.Combine(profile.ReportRoot, profile.LatestQaJsonFileName);
            var latestMarkdown = Path.Combine(profile.ReportRoot, profile.LatestQaMarkdownFileName);
            Assert.IsTrue(File.Exists(latestJson), latestJson);
            Assert.IsTrue(File.Exists(latestMarkdown), latestMarkdown);

            var report = JsonUtility.FromJson<PlatformBuildQaReport>(File.ReadAllText(latestJson));
            Assert.IsFalse(string.IsNullOrWhiteSpace(report.reportId));
            Assert.IsTrue(report.targets.Count > 0);
            StringAssert.Contains("Hollow Platform Build QA", File.ReadAllText(latestMarkdown));
        }

        [Test]
        public void PlatformQaProfileIsAddressableWithExpectedLabels()
        {
            var settings = AddressableAssetSettingsDefaultObject.GetSettings(create: false);
            Assert.IsNotNull(settings);
            Assert.Contains(Milestone24AssetGenerator.PlatformQaAddressableLabel, settings.GetLabels().ToList());

            var guid = AssetDatabase.AssetPathToGUID(Milestone24AssetGenerator.PlatformBuildQaProfilePath);
            var entry = settings.FindAssetEntry(guid, includeImplicit: false);
            Assert.IsNotNull(entry);
            Assert.IsTrue(entry.labels.Contains(Milestone24AssetGenerator.PlatformQaAddressableLabel));
            Assert.IsTrue(entry.labels.Contains("hollow.data"));
        }

        [Test]
        public void VisionOSReadinessReturnsExplicitResult()
        {
            var result = PlatformBuildQaRunner.ValidateVisionOSReadiness();

            Assert.AreEqual("visionos-readiness", result.id);
            Assert.Contains(result.result, new[]
            {
                PlatformBuildQaResult.Passed,
                PlatformBuildQaResult.BlockedByEnvironment,
                PlatformBuildQaResult.Failed
            });
            Assert.IsFalse(string.IsNullOrWhiteSpace(result.platform));
        }

        [Test]
        public void Milestone24ValidatorReportsGeneratedStateValid()
        {
            Assert.DoesNotThrow(() => Milestone24Validator.Validate());
        }
    }
}
