using System.IO;
using Hollow.Branches;
using Hollow.Combat;
using Hollow.Editor.Generation;
using Hollow.Editor.Validation;
using NUnit.Framework;
using UnityEngine;

namespace Hollow.Tests.EditMode
{
    public sealed class Milestone108DesignerDebuggingPassTests
    {
        [TearDown]
        public void TearDown()
        {
            EnemyDesignerDebugOverlay.SetEnabled(false);
            EnemyNavigationDebugOverlay.SetPathTracingEnabled(false);
            EnemyAiDebugOverlay.SetBlackboardEnabled(false);
            EnemyTacticalDebugOverlay.SetEnabled(false);
        }

        [Test]
        public void DesignerDebugOverlayBuildsUnifiedEnemyReadout()
        {
            var enemyObject = new GameObject("M108OverlayEnemy");
            try
            {
                var enemy = enemyObject.AddComponent<EnemyRuntimeController>();
                EnemyDesignerDebugOverlay.ResetDiagnostics();
                EnemyDesignerDebugOverlay.SetEnabled(true);

                var text = EnemyDesignerDebugOverlay.BuildOverlayText(enemy);

                StringAssert.Contains("State", text);
                StringAssert.Contains("Action", text);
                StringAssert.Contains("Tactical", text);
                StringAssert.Contains("Nav", text);
                StringAssert.Contains("Blocked", text);
                StringAssert.Contains("BT", text);
                StringAssert.Contains("Designer Debug active enemies", EnemyDesignerDebugOverlay.DiagnosticsSummary);
            }
            finally
            {
                Object.DestroyImmediate(enemyObject);
            }
        }

        [Test]
        public void DebugSpawnMenuDesignerToggleEnablesUnifiedOverlayStack()
        {
            var menuObject = new GameObject("M108DebugSpawnMenu");
            try
            {
                var menu = menuObject.AddComponent<DebugSpawnMenuController>();

                Assert.IsFalse(menu.DebugEnemyDesignerDebugEnabled);
                Assert.IsFalse(EnemyDesignerDebugOverlay.Enabled);

                menu.SetDebugEnemyDesignerDebugEnabled(true);

                Assert.IsTrue(menu.DebugEnemyDesignerDebugEnabled);
                Assert.IsTrue(EnemyDesignerDebugOverlay.Enabled);
                Assert.IsTrue(menu.DebugEnemyPathTracingEnabled);
                Assert.IsTrue(menu.DebugEnemyAiBlackboardEnabled);
                Assert.IsTrue(menu.DebugEnemyTacticalOverlayEnabled);

                menu.SetDebugEnemyDesignerDebugEnabled(false);

                Assert.IsFalse(menu.DebugEnemyDesignerDebugEnabled);
                Assert.IsFalse(EnemyDesignerDebugOverlay.Enabled);
                Assert.IsFalse(menu.DebugEnemyPathTracingEnabled);
                Assert.IsFalse(menu.DebugEnemyAiBlackboardEnabled);
                Assert.IsFalse(menu.DebugEnemyTacticalOverlayEnabled);
            }
            finally
            {
                Object.DestroyImmediate(menuObject);
            }
        }

        [Test]
        public void M108ArtifactsAndValidatorPass()
        {
            Assert.IsTrue(File.Exists(Milestone108DesignerDebuggingPassAssetGenerator.DocsPath), Milestone108DesignerDebuggingPassAssetGenerator.DocsPath);
            Assert.IsTrue(File.Exists(Milestone108DesignerDebuggingPassAssetGenerator.ReportPath), Milestone108DesignerDebuggingPassAssetGenerator.ReportPath);
            var docs = File.ReadAllText(Milestone108DesignerDebuggingPassAssetGenerator.DocsPath);
            StringAssert.Contains("Designer Debugging Pass", docs);
            StringAssert.Contains("NavMesh path", docs);
            StringAssert.Contains("Behavior graph", docs);
            StringAssert.Contains("active attack window", docs);
            Assert.IsTrue(Milestone108DesignerDebuggingPassValidator.Validate());
        }
    }
}
