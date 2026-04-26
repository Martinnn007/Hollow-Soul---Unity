using System.IO;
using System.Linq;
using Hollow.Branches;
using Hollow.Data.Definitions;
using Hollow.Editor.Build;
using Hollow.Editor.Generation;
using Hollow.Editor.Validation;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.AddressableAssets;

namespace Hollow.Tests.EditMode
{
    public sealed class Milestone25VerticalSliceContentLockTests
    {
        [Test]
        public void VerticalSliceLockPinsBranchSeedContentAndPlatformPolicy()
        {
            var definition = LoadDefinition();

            Assert.AreEqual(BranchGenerator.BranchFeaturesId, definition.BranchIdentity);
            Assert.AreEqual(BranchGenerator.DefaultSeededMacroSeed, definition.LockedSeed);
            Assert.IsTrue(definition.AllowEmptyApprovedRoomPool);
            Assert.IsTrue(definition.RequireArtPassPrefabs);
            Assert.AreEqual(3, definition.RequiredShopOfferCount);
            Assert.AreEqual(3, definition.RequiredNextBranchPortalCount);
            CollectionAssert.Contains(definition.PlatformChecklistTargets, "Windows");
            CollectionAssert.Contains(definition.PlatformChecklistTargets, "VisionOSBounded");
            CollectionAssert.Contains(definition.PlatformChecklistTargets, "VisionOSImmersive");
            CollectionAssert.Contains(definition.RequiredPrefabRoles, PresentationPrefabRole.Player);
            CollectionAssert.Contains(definition.RequiredPrefabRoles, PresentationPrefabRole.EnemyBoss);
            CollectionAssert.Contains(definition.RequiredPrefabRoles, PresentationPrefabRole.HubShop);
            CollectionAssert.Contains(definition.RequiredPrefabRoles, PresentationPrefabRole.NextBranchPortal);
        }

        [Test]
        public void LockedBranchGeneratesCompleteM20VerticalSlice()
        {
            var report = VerticalSliceContentValidator.ValidateLock(LoadDefinition());

            Assert.AreNotEqual(PlatformBuildQaResult.Failed, report.result, string.Join("; ", report.checks.SelectMany(check => check.messages)));
            Assert.AreEqual(BranchGenerator.BranchFeaturesId, report.branchIdentity);
            Assert.AreEqual(BranchGenerator.DefaultSeededMacroSeed, report.lockedSeed);
            Assert.AreEqual(8, report.roomCount);
            Assert.GreaterOrEqual(report.fixtureRoomCount, 5);
            Assert.AreEqual(3, report.shopOfferCount);
            Assert.AreEqual(3, report.nextBranchPortalCount);
            Assert.IsTrue(report.checks.Any(check => check.id == "branch-content" && check.result == PlatformBuildQaResult.Passed));
            Assert.IsTrue(report.checks.Any(check => check.id == "artpass-lock" && check.result == PlatformBuildQaResult.Passed));
        }

        [Test]
        public void LockReportWritesJsonMarkdownAndPdf()
        {
            var definition = LoadDefinition();
            var report = VerticalSliceContentValidator.ValidateLock(definition);
            VerticalSliceLockRunner.WriteReports(definition, report);

            var jsonPath = Path.Combine(definition.ReportRoot, definition.LatestJsonFileName);
            var markdownPath = Path.Combine(definition.ReportRoot, definition.LatestMarkdownFileName);
            Assert.IsTrue(File.Exists(jsonPath), jsonPath);
            Assert.IsTrue(File.Exists(markdownPath), markdownPath);
            Assert.IsTrue(File.Exists(definition.PdfOutputPath), definition.PdfOutputPath);
            StringAssert.Contains("Hollow M25 Vertical Slice Content Lock", File.ReadAllText(markdownPath));
            var jsonReport = UnityEngine.JsonUtility.FromJson<VerticalSliceLockReport>(File.ReadAllText(jsonPath));
            Assert.AreEqual(report.reportId, jsonReport.reportId);
        }

        [Test]
        public void LockAssetIsAddressableWithVerticalSliceLabel()
        {
            var settings = AddressableAssetSettingsDefaultObject.GetSettings(create: false);
            Assert.IsNotNull(settings);
            Assert.Contains(Milestone25AssetGenerator.VerticalSliceAddressableLabel, settings.GetLabels().ToList());

            var guid = AssetDatabase.AssetPathToGUID(Milestone25AssetGenerator.VerticalSliceLockPath);
            var entry = settings.FindAssetEntry(guid, includeImplicit: false);
            Assert.IsNotNull(entry);
            Assert.IsTrue(entry.labels.Contains(Milestone25AssetGenerator.VerticalSliceAddressableLabel));
            Assert.IsTrue(entry.labels.Contains("hollow.data"));
        }

        [Test]
        public void Milestone25ValidatorReportsGeneratedStateValid()
        {
            Assert.DoesNotThrow(() => Milestone25Validator.Validate());
        }

        private static VerticalSliceLockDefinition LoadDefinition()
        {
            var definition = AssetDatabase.LoadAssetAtPath<VerticalSliceLockDefinition>(Milestone25AssetGenerator.VerticalSliceLockPath);
            Assert.IsNotNull(definition, "Run M25 generation before validating the vertical slice lock.");
            return definition;
        }
    }
}
