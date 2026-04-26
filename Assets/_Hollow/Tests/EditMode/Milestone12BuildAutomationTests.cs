using System.IO;
using System.Linq;
using Hollow.Data.Definitions;
using Hollow.Editor.Build;
using Hollow.Editor.Generation;
using Hollow.Editor.Validation;
using NUnit.Framework;
using UnityEditor;

namespace Hollow.Tests.EditMode
{
    public sealed class Milestone12BuildAutomationTests
    {
        [Test]
        public void BuildAutomationProfileListsScenesAndMilestoneValidators()
        {
            var profile = LoadProfile();

            Assert.AreEqual(Milestone11AssetGenerator.RequiredBuildScenes.Length, profile.RequiredScenes.Length);
            foreach (var scenePath in Milestone11AssetGenerator.RequiredBuildScenes)
            {
                Assert.Contains(scenePath, profile.RequiredScenes);
            }

            Assert.AreEqual(Milestone12AssetGenerator.ValidationTypes.Length, profile.ValidationTypes.Length);
            foreach (var validatorType in Milestone12AssetGenerator.ValidationTypes)
            {
                Assert.Contains(validatorType, profile.ValidationTypes);
            }
        }

        [Test]
        public void BuildAutomationValidationHarnessPassesGeneratedProfile()
        {
            var report = BuildAutomationValidationHarness.ValidateAll(LoadProfile(), runAudit: false);

            Assert.IsTrue(report.IsValid, string.Join("\n", report.Failures));
        }

        [Test]
        public void PrototypeAuditRunnerExecutesM0ToM11ValidatorsWithoutExiting()
        {
            var audit = PrototypeAuditRunner.RunFullAudit(LoadProfile(), writeReports: false);

            Assert.IsTrue(audit.Passed, string.Join("\n", audit.entries.Where(entry => !entry.passed).SelectMany(entry => entry.messages)));
            Assert.AreEqual(Milestone12AssetGenerator.ValidationTypes.Length, audit.totalChecks);
            Assert.AreEqual(audit.totalChecks, audit.passedChecks);
        }

        [Test]
        public void BuildManifestRecordsScenesGitAuditAndTarget()
        {
            var profile = LoadProfile();
            var audit = new PrototypeAuditReport
            {
                auditId = "test-audit",
                generatedAtUtc = "now",
                unityVersion = "test",
                result = "Passed",
                totalChecks = 1,
                passedChecks = 1,
                failedChecks = 0
            };

            var manifest = BuildManifestWriter.WriteManifest(profile, audit, "StandaloneWindows64", HollowBuildAutomation.PlannedWindowsBuildPath(profile), "ManifestOnly");

            Assert.AreEqual(profile.PrototypeVersion, manifest.prototypeVersion);
            Assert.AreEqual("StandaloneWindows64", manifest.buildTarget);
            Assert.AreEqual("Passed", manifest.auditResult);
            Assert.IsTrue(manifest.scenes.Contains("Assets/_Hollow/Scenes/Boot.unity"));
            Assert.IsTrue(File.Exists(Path.Combine(profile.ReportRoot, profile.LatestBuildManifestFileName)));
        }

        [Test]
        public void VisionOSPlaceholderCheckRequiresPolishProfilesAndScenes()
        {
            Assert.IsTrue(HollowBuildAutomation.ValidateVisionOSProfilePlaceholders(LoadProfile(), out var error), error);
        }

        private static BuildAutomationProfileDefinition LoadProfile()
        {
            var profile = AssetDatabase.LoadAssetAtPath<BuildAutomationProfileDefinition>(Milestone12AssetGenerator.BuildAutomationProfilePath);
            Assert.IsNotNull(profile, "Run Hollow/Generation/Generate Milestone 12 Assets first.");
            return profile;
        }
    }
}
