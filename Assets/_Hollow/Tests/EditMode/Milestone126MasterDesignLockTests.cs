using System.IO;
using System.Linq;
using Hollow.Editor.Generation;
using Hollow.Editor.Validation;
using NUnit.Framework;

namespace Hollow.Tests.EditMode
{
    public sealed class Milestone126MasterDesignLockTests
    {
        [Test]
        public void MasterDesignPackagePassesAllLiveLockChecks()
        {
            var report = Milestone126MasterDesignLockAssetGenerator.BuildReport();

            Assert.IsTrue(report.passed, string.Join("\n", report.failures ?? new string[0]));
            Assert.Greater(report.totalChecks, 10);
            Assert.AreEqual(report.totalChecks, report.passedChecks);
            CollectionAssert.Contains(report.evidencePaths, Milestone126MasterDesignLockAssetGenerator.MasterDesignPath);
            CollectionAssert.Contains(report.evidencePaths, Milestone126MasterDesignLockAssetGenerator.EnglishPdfPath);
            CollectionAssert.Contains(report.evidencePaths, Milestone126MasterDesignLockAssetGenerator.PolishPdfPath);
        }

        [Test]
        public void MasterDocumentContainsM126GovernanceCrossReferences()
        {
            var markdown = File.ReadAllText(Milestone126MasterDesignLockAssetGenerator.MasterDesignPath);

            StringAssert.Contains("M69-M78", markdown);
            StringAssert.Contains("Art, Pipeline, And Production Roles", markdown);
            StringAssert.Contains("Martin owns Unity implementation and milestone gate review", markdown);
            StringAssert.Contains("Rafal owns authored art assets", markdown);
            StringAssert.Contains("It is not an implementation changelist", markdown);
            StringAssert.Contains("before runtime changes", markdown);
        }

        [Test]
        public void GeneratedReportsArePresentPassingAndIncludePolishPdfEvidence()
        {
            Assert.IsTrue(File.Exists(Milestone126MasterDesignLockAssetGenerator.ReportMarkdownPath));
            Assert.IsTrue(File.Exists(Milestone126MasterDesignLockAssetGenerator.ReportJsonPath));

            var markdown = File.ReadAllText(Milestone126MasterDesignLockAssetGenerator.ReportMarkdownPath);
            StringAssert.Contains("# M126 Master Design Lock Report", markdown);
            StringAssert.Contains("- Result: PASSED", markdown);
            StringAssert.Contains(Milestone126MasterDesignLockAssetGenerator.PolishPdfPath, markdown);

            var json = UnityEngine.JsonUtility.FromJson<Milestone126MasterDesignLockReport>(
                File.ReadAllText(Milestone126MasterDesignLockAssetGenerator.ReportJsonPath));
            Assert.IsNotNull(json);
            Assert.AreEqual(Milestone126MasterDesignLockAssetGenerator.LockId, json.lockId);
            Assert.IsTrue(json.passed);
            Assert.IsTrue((json.evidencePaths ?? new string[0]).Contains(Milestone126MasterDesignLockAssetGenerator.PolishPdfPath));
        }

        [Test]
        public void Milestone126ValidatorReportsGeneratedStateValid()
        {
            Assert.IsTrue(Milestone126MasterDesignLockValidator.Validate(exitOnFailure: false));
        }
    }
}
