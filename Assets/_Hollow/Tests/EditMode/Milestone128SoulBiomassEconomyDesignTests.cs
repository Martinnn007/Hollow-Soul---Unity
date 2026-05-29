using System.IO;
using System.Linq;
using Hollow.Editor.Generation;
using Hollow.Editor.Validation;
using NUnit.Framework;

namespace Hollow.Tests.EditMode
{
    public sealed class Milestone128SoulBiomassEconomyDesignTests
    {
        [Test]
        public void LiveReportPassesAllSoulBiomassDesignChecks()
        {
            var report = Milestone128SoulBiomassEconomyDesignAssetGenerator.BuildReport();

            Assert.IsTrue(report.passed, string.Join("\n", report.failures ?? new string[0]));
            Assert.Greater(report.totalChecks, 10);
            Assert.AreEqual(report.totalChecks, report.passedChecks);
            CollectionAssert.Contains(report.evidencePaths, Milestone128SoulBiomassEconomyDesignAssetGenerator.DocsPath);
            CollectionAssert.Contains(report.evidencePaths, Milestone128SoulBiomassEconomyDesignAssetGenerator.M127ReportPath);
        }

        [Test]
        public void DesignDocLocksSoulCoinBiomassAndDeferralDecisions()
        {
            var markdown = File.ReadAllText(Milestone128SoulBiomassEconomyDesignAssetGenerator.DocsPath);

            StringAssert.Contains("Unbanked Souls", markdown);
            StringAssert.Contains("lost on death", markdown);
            StringAssert.Contains("secured only through extraction or banking", markdown);
            StringAssert.Contains("repair or reopen Derelict Sanctuary ship systems", markdown);
            StringAssert.Contains("placeholder scaffolding", markdown);
            StringAssert.Contains("Coins are safe", markdown);
            StringAssert.Contains("run-local shop money", markdown);
            StringAssert.Contains("future organic crafting material", markdown);
            StringAssert.Contains("world salvage", markdown);
            StringAssert.Contains("No biomass HUD", markdown);
            StringAssert.Contains("No biomass save data", markdown);
            StringAssert.Contains("No runtime HUD rename", markdown);
        }

        [Test]
        public void GeneratedReportsArePresentPassingAndUseM128LockId()
        {
            Assert.IsTrue(File.Exists(Milestone128SoulBiomassEconomyDesignAssetGenerator.ReportMarkdownPath));
            Assert.IsTrue(File.Exists(Milestone128SoulBiomassEconomyDesignAssetGenerator.ReportJsonPath));

            var markdown = File.ReadAllText(Milestone128SoulBiomassEconomyDesignAssetGenerator.ReportMarkdownPath);
            StringAssert.Contains("# M128 Soul + Biomass Economy Design Pass Report", markdown);
            StringAssert.Contains("- Result: PASSED", markdown);
            StringAssert.Contains(Milestone128SoulBiomassEconomyDesignAssetGenerator.LockId, markdown);
            StringAssert.Contains("design artifact only", markdown);
            StringAssert.Contains("Canonical future run label: `Unbanked Souls`", markdown);

            var json = UnityEngine.JsonUtility.FromJson<Milestone128SoulBiomassEconomyDesignReport>(
                File.ReadAllText(Milestone128SoulBiomassEconomyDesignAssetGenerator.ReportJsonPath));
            Assert.IsNotNull(json);
            Assert.AreEqual(Milestone128SoulBiomassEconomyDesignAssetGenerator.LockId, json.lockId);
            Assert.IsTrue(json.passed);
            Assert.IsTrue((json.failures ?? new string[0]).Length == 0);
            Assert.IsTrue((json.evidencePaths ?? new string[0]).Contains(Milestone128SoulBiomassEconomyDesignAssetGenerator.DocsPath));
        }

        [Test]
        public void ValidatorReportsGeneratedStateValid()
        {
            Assert.IsTrue(Milestone128SoulBiomassEconomyDesignValidator.Validate(exitOnFailure: false));
        }
    }
}
