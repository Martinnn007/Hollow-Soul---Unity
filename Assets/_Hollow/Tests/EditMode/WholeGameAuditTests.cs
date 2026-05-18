using System.IO;
using System.Linq;
using Hollow.Editor.Build;
using NUnit.Framework;
using UnityEngine;

namespace Hollow.Tests.EditMode
{
    public sealed class WholeGameAuditTests
    {
        [Test]
        public void WholeGameAuditDefinesTenRoadmapMilestones()
        {
            var report = WholeGameAuditRunner.RunAudit(writeReports: false, strictReleaseGate: false);
            var milestones = report.milestones.Select(milestone => milestone.milestone).ToArray();

            CollectionAssert.AreEqual(Enumerable.Range(116, 10), milestones);
            Assert.AreEqual(10, report.milestones.Select(milestone => milestone.title).Distinct().Count());
            Assert.IsTrue(report.milestones.All(milestone => !string.IsNullOrWhiteSpace(milestone.defaultSolution)));
        }

        [Test]
        public void WholeGameAuditIsCleanOfShippableBlockers()
        {
            var report = WholeGameAuditRunner.RunAudit(writeReports: false, strictReleaseGate: false);
            var blockers = string.Join("\n", report.Blockers.Select(finding => $"M{finding.milestone} {finding.title}: {finding.message} ({finding.location})"));

            Assert.AreEqual(0, report.blockerCount, blockers);
        }

        [Test]
        public void MissingScriptMarkerScannerDetectsSerializedNullMonoScript()
        {
            var tempPath = Path.Combine(Application.temporaryCachePath, "whole_game_audit_missing_script.prefab");
            try
            {
                File.WriteAllText(tempPath, "%YAML 1.1\n--- !u!114 &1\nMonoBehaviour:\n  m_Script: {fileID: 0}\n");

                var matches = WholeGameAuditRunner.FindSerializedMissingScriptMarkersForTests(new[] { tempPath });

                Assert.AreEqual(1, matches.Count);
                Assert.IsTrue(matches[0].EndsWith("whole_game_audit_missing_script.prefab"));
            }
            finally
            {
                if (File.Exists(tempPath))
                {
                    File.Delete(tempPath);
                }
            }
        }

        [Test]
        public void WholeGameAuditMarkdownIncludesEveryMilestone()
        {
            var report = WholeGameAuditRunner.RunAudit(writeReports: false, strictReleaseGate: false);
            var markdown = WholeGameAuditRunner.ToMarkdown(report);

            foreach (var milestone in Enumerable.Range(116, 10))
            {
                StringAssert.Contains($"M{milestone}", markdown);
            }

            StringAssert.Contains("Whole Game Audit M116-M125", markdown);
        }
    }
}
