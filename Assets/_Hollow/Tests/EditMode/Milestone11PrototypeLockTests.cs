using System.Linq;
using Hollow.Data.Definitions;
using Hollow.Editor.Generation;
using Hollow.Editor.Validation;
using NUnit.Framework;
using UnityEditor;

namespace Hollow.Tests.EditMode
{
    public sealed class Milestone11PrototypeLockTests
    {
        [Test]
        public void GeneratedChecklistCoversPrototypeLockAreas()
        {
            var checklist = AssetDatabase.LoadAssetAtPath<PrototypeLockChecklistDefinition>(Milestone11AssetGenerator.ChecklistPath);

            Assert.IsNotNull(checklist);
            Assert.IsTrue(checklist.RequiredItemsSatisfied);
            Assert.GreaterOrEqual(checklist.Items.Length, 10);
            Assert.IsTrue(checklist.Items.Any(item => item.Group == "QA"));
            Assert.IsTrue(checklist.Items.Any(item => item.Group == "Save/Load"));
            Assert.IsTrue(checklist.Items.Any(item => item.Group == "Performance"));
            Assert.IsTrue(checklist.Items.Any(item => item.Group == "Build Handoff"));
        }

        [Test]
        public void PerformanceBudgetCoversEveryPlatformProfile()
        {
            var budget = AssetDatabase.LoadAssetAtPath<PerformanceBudgetDefinition>(Milestone11AssetGenerator.PerformanceBudgetPath);

            Assert.IsNotNull(budget);
            AssertBudget(budget, PlatformPresentationMode.WindowsStandard3D, Milestone10AssetGenerator.WindowsProfilePath);
            AssertBudget(budget, PlatformPresentationMode.VisionOSBoundedTabletop, Milestone10AssetGenerator.BoundedProfilePath);
            AssertBudget(budget, PlatformPresentationMode.VisionOSImmersive, Milestone10AssetGenerator.ImmersiveProfilePath);
        }

        [Test]
        public void BuildHandoffListsAllRuntimeScenesAndValidationCommands()
        {
            var handoff = AssetDatabase.LoadAssetAtPath<BuildHandoffDefinition>(Milestone11AssetGenerator.BuildHandoffPath);

            Assert.IsNotNull(handoff);
            Assert.AreEqual("M11", handoff.LastVerifiedMilestone);
            foreach (var scenePath in Milestone11AssetGenerator.RequiredBuildScenes)
            {
                Assert.IsTrue(handoff.RequiredScenes.Contains(scenePath), scenePath);
                Assert.IsTrue(EditorBuildSettings.scenes.Any(scene => scene.enabled && scene.path == scenePath), scenePath);
            }

            Assert.GreaterOrEqual(handoff.ValidationCommands.Length, 3);
            Assert.GreaterOrEqual(handoff.HandoffNotes.Length, 3);
        }

        [Test]
        public void PrototypeLockHarnessPassesGeneratedProject()
        {
            var checklist = AssetDatabase.LoadAssetAtPath<PrototypeLockChecklistDefinition>(Milestone11AssetGenerator.ChecklistPath);
            var budget = AssetDatabase.LoadAssetAtPath<PerformanceBudgetDefinition>(Milestone11AssetGenerator.PerformanceBudgetPath);
            var handoff = AssetDatabase.LoadAssetAtPath<BuildHandoffDefinition>(Milestone11AssetGenerator.BuildHandoffPath);

            var report = PrototypeLockValidationHarness.ValidateAll(checklist, budget, handoff);

            Assert.IsTrue(report.IsValid, string.Join("\n", report.Failures));
        }

        private static void AssertBudget(PerformanceBudgetDefinition budget, PlatformPresentationMode mode, string profilePath)
        {
            var profile = AssetDatabase.LoadAssetAtPath<PlatformPolishProfileDefinition>(profilePath);
            Assert.IsNotNull(profile);
            Assert.IsTrue(budget.TryGetBudget(mode, out var platformBudget), $"Missing budget for {mode}");
            Assert.GreaterOrEqual(profile.TargetFrameRate, platformBudget.MinimumTargetFrameRate);
            Assert.LessOrEqual(profile.RenderScale, platformBudget.MaximumRenderScale + 0.0001f);
            Assert.Greater(platformBudget.MaximumVisibleEnemies, 0);
            Assert.Greater(platformBudget.MaximumProjectiles, 0);
            Assert.Greater(platformBudget.MaximumDrawCalls, 0);
        }
    }
}
