using System.IO;
using System.Linq;
using Hollow.Editor.Generation;
using Hollow.Editor.Validation;
using NUnit.Framework;

namespace Hollow.Tests.EditMode
{
    public sealed class Milestone129ShipSoulLoopGreyboxTests
    {
        [Test]
        public void LiveReportPassesAllShipSoulLoopChecks()
        {
            var report = Milestone129ShipSoulLoopGreyboxAssetGenerator.BuildReport();

            Assert.IsTrue(report.passed, string.Join("\n", report.failures ?? new string[0]));
            Assert.Greater(report.totalChecks, 12);
            Assert.AreEqual(report.totalChecks, report.passedChecks);
            CollectionAssert.Contains(report.evidencePaths, Milestone129ShipSoulLoopGreyboxAssetGenerator.DocsPath);
            CollectionAssert.Contains(report.evidencePaths, Milestone129ShipSoulLoopGreyboxAssetGenerator.M128ReportPath);
        }

        [Test]
        public void DesignDocLocksPortalEngineModulesShipLogAndBankingRule()
        {
            var markdown = File.ReadAllText(Milestone129ShipSoulLoopGreyboxAssetGenerator.DocsPath);

            StringAssert.Contains("clean roguelite hub", markdown);
            StringAssert.Contains("Portal Engine", markdown);
            StringAssert.Contains("ship tech modules", markdown);
            StringAssert.Contains("Banked Souls", markdown);
            StringAssert.Contains("Ship Log", markdown);
            StringAssert.Contains("Return to Ship", markdown);
            StringAssert.Contains("Normal world-loop souls stay at risk", markdown);
            StringAssert.Contains("No runtime run-HUD rename to `Unbanked Souls`", markdown);
        }

        [Test]
        public void GeneratedReportsArePresentPassingAndUseM129LockId()
        {
            Assert.IsTrue(File.Exists(Milestone129ShipSoulLoopGreyboxAssetGenerator.ReportMarkdownPath));
            Assert.IsTrue(File.Exists(Milestone129ShipSoulLoopGreyboxAssetGenerator.ReportJsonPath));

            var markdown = File.ReadAllText(Milestone129ShipSoulLoopGreyboxAssetGenerator.ReportMarkdownPath);
            StringAssert.Contains("# M129 Ship-Soul Loop Greybox Report", markdown);
            StringAssert.Contains("- Result: PASSED", markdown);
            StringAssert.Contains(Milestone129ShipSoulLoopGreyboxAssetGenerator.LockId, markdown);
            StringAssert.Contains("Portal Engine launches normal runs", markdown);
            StringAssert.Contains("world-loop souls bank only on final `Return to Ship`", markdown);

            var json = UnityEngine.JsonUtility.FromJson<Milestone129ShipSoulLoopGreyboxReport>(
                File.ReadAllText(Milestone129ShipSoulLoopGreyboxAssetGenerator.ReportJsonPath));
            Assert.IsNotNull(json);
            Assert.AreEqual(Milestone129ShipSoulLoopGreyboxAssetGenerator.LockId, json.lockId);
            Assert.IsTrue(json.passed);
            Assert.IsTrue((json.failures ?? new string[0]).Length == 0);
            Assert.IsTrue((json.evidencePaths ?? new string[0]).Contains(Milestone129ShipSoulLoopGreyboxAssetGenerator.DocsPath));
        }

        [Test]
        public void ValidatorReportsGeneratedStateValid()
        {
            Assert.IsTrue(Milestone129ShipSoulLoopGreyboxValidator.Validate(exitOnFailure: false));
        }
    }
}
