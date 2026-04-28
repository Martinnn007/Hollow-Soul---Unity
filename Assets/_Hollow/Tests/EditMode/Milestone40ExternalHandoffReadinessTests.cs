using System.IO;
using System.Linq;
using Hollow.Data.Definitions;
using Hollow.Editor.Build;
using Hollow.Editor.Generation;
using Hollow.Editor.Validation;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Hollow.Tests.EditMode
{
    public sealed class Milestone40ExternalHandoffReadinessTests
    {
        [Test]
        public void HandoffDefinitionPinsLatestReportsAndManualChecklist()
        {
            var definition = AssetDatabase.LoadAssetAtPath<ExternalHandoffDefinition>(Milestone40AssetGenerator.HandoffDefinitionPath);

            Assert.IsNotNull(definition);
            Assert.AreEqual("m40_vertical_slice_external_handoff_v1", definition.HandoffId);
            Assert.Contains("windows-development-build", definition.AcceptedEnvironmentBlocks.ToArray());
            Assert.Contains("output/reports/latest_platform_build_qa.json", definition.RequiredReports.ToArray());
            Assert.Contains("output/reports/latest_vertical_slice_lock.json", definition.RequiredReports.ToArray());
            Assert.GreaterOrEqual(definition.ManualHandoffChecklist.Count, 4);
        }

        [Test]
        public void HandoffReportAllowsDocumentedEnvironmentBlocksButNotFailures()
        {
            var report = new ExternalHandoffReport();
            report.checks.Add(ExternalHandoffCheckResult.Passed("platform-qa", "OK"));
            report.checks.Add(ExternalHandoffCheckResult.BlockedByEnvironment("environment-blocks", "Windows module missing."));
            report.Recalculate();
            Assert.AreEqual(PlatformBuildQaResult.PassedWithEnvironmentBlocks, report.result);

            report.checks.Add(ExternalHandoffCheckResult.Failed("vertical-slice-lock", "Broken lock."));
            report.Recalculate();
            Assert.AreEqual(PlatformBuildQaResult.Failed, report.result);
        }

        [Test]
        public void LatestHandoffReportReferencesQaLockAndWorldFraming()
        {
            var definition = AssetDatabase.LoadAssetAtPath<ExternalHandoffDefinition>(Milestone40AssetGenerator.HandoffDefinitionPath);
            Assert.IsNotNull(definition);
            Assert.IsTrue(File.Exists(definition.LatestJsonPath));

            var report = JsonUtility.FromJson<ExternalHandoffReport>(File.ReadAllText(definition.LatestJsonPath));
            Assert.IsNotNull(report);
            Assert.IsFalse(string.IsNullOrWhiteSpace(report.reportId));
            Assert.IsTrue(report.checks.Any(check => check.id == "platform-qa"));
            Assert.IsTrue(report.checks.Any(check => check.id == "vertical-slice-lock"));
            Assert.IsTrue(report.checks.Any(check => check.id == "m39-world-framing"));
        }

        [Test]
        public void Milestone40ValidatorReportsGeneratedStateValid()
        {
            Assert.DoesNotThrow(() => Milestone40Validator.Validate());
        }
    }
}
