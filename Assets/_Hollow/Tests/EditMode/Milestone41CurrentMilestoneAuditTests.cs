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
    public sealed class Milestone41CurrentMilestoneAuditTests
    {
        [Test]
        public void AuditDefinitionCoversM31ThroughM40Validators()
        {
            var definition = AssetDatabase.LoadAssetAtPath<CurrentMilestoneAuditDefinition>(Milestone41AssetGenerator.AuditDefinitionPath);

            Assert.IsNotNull(definition);
            Assert.AreEqual("m41_current_milestone_audit_v1", definition.AuditId);
            foreach (var validator in Milestone41AssetGenerator.CurrentValidationTypes)
            {
                Assert.Contains(validator, definition.ValidationTypes.ToArray());
            }
        }

        [Test]
        public void LatestCurrentMilestoneAuditReportPassesEveryCurrentValidator()
        {
            var definition = AssetDatabase.LoadAssetAtPath<CurrentMilestoneAuditDefinition>(Milestone41AssetGenerator.AuditDefinitionPath);
            Assert.IsNotNull(definition);
            Assert.IsTrue(File.Exists(definition.LatestJsonPath));

            var report = JsonUtility.FromJson<PrototypeAuditReport>(File.ReadAllText(definition.LatestJsonPath));
            Assert.IsNotNull(report);
            Assert.IsTrue(report.Passed);
            Assert.AreEqual(Milestone41AssetGenerator.CurrentValidationTypes.Length, report.totalChecks);
            Assert.AreEqual(report.totalChecks, report.passedChecks);
            Assert.IsTrue(report.entries.Any(entry => entry.id == "Milestone40Validator" && entry.passed));
        }

        [Test]
        public void Milestone41ValidatorReportsGeneratedStateValid()
        {
            Assert.DoesNotThrow(() => Milestone41Validator.Validate());
        }
    }
}
