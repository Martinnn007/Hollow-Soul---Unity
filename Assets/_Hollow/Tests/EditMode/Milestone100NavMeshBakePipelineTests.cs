using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Hollow.Editor.Generation;
using Hollow.Editor.Navigation;
using Hollow.Rooms;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Hollow.Tests.EditMode
{
    public sealed class Milestone100NavMeshBakePipelineTests
    {
        private const string TemplateRoomPath = "Assets/_Hollow/Data/Rooms/Templates/combat_single_sample.hollowruntime.json";

        [Test]
        public void RuntimeAndEditorBakersUseSharedNavMeshSettings()
        {
            var runtimeBuilder = File.ReadAllText("Assets/_Hollow/Scripts/Hollow.Rooms/RoomRuntimeNavMeshBuilder.cs");
            var editorBaker = File.ReadAllText("Assets/_Hollow/Scripts/Hollow.Editor/Navigation/RoomNavMeshBakeUtility.cs");

            StringAssert.Contains("RoomNavMeshBuildUtility.BuildRoom", runtimeBuilder);
            StringAssert.Contains("RoomNavMeshBuildUtility.BuildRoom", editorBaker);
            Assert.AreEqual(0.24f, RoomNavMeshBuildUtility.AgentRadiusMeters);
            Assert.AreEqual(1.05f, RoomNavMeshBuildUtility.AgentHeightMeters);
        }

        [Test]
        public void RuntimeBakeCatalogIncludesApprovedMacroAndDeveloperRoots()
        {
            var roots = RoomNavMeshBakeUtility.RuntimeRoomRoots;
            CollectionAssert.Contains(roots, RoomNavMeshBakeUtility.ApprovedRoomsRoot);
            CollectionAssert.Contains(roots, RoomNavMeshBakeUtility.MacroFixturesRoot);
            CollectionAssert.Contains(roots, RoomNavMeshBakeUtility.DeveloperLabRoomsRoot);

            var paths = RoomNavMeshBakeUtility.CollectRuntimeRoomPaths();
            Assert.IsTrue(paths.Any(path => path.Contains("/DesignerApproved/", System.StringComparison.Ordinal)));
            Assert.IsTrue(paths.Any(path => path.Contains("/MacroFixtures/", System.StringComparison.Ordinal)));
        }

        [Test]
        public void RequireCatalogBakeFailsWithActionableMissingBakeMessage()
        {
            var root = new GameObject("M100StrictNavMeshHarness");
            try
            {
                var room = root.AddComponent<RoomRuntimeRoot>();
                var asset = HollowRuntimeV2Importer.Import(File.ReadAllText(TemplateRoomPath));

                LogAssert.Expect(LogType.Error, new Regex("missing_navmesh_bake:combat_single_sample.*Bake Runtime Room NavMeshes"));
                room.BuildFrom(asset, RoomNavMeshRuntimeFallbackMode.RequireCatalogBake);

                Assert.IsFalse(room.HasNavMeshBake);
                StringAssert.Contains("missing_navmesh_bake:combat_single_sample", room.NavMeshBakeError);
                StringAssert.Contains(RoomNavMeshCatalogDefinition.PreferredBakeMenuPath, room.NavMeshBakeError);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void DevFallbackBuildsRuntimeNavMeshButMarksItAsFallback()
        {
            var root = new GameObject("M100DevFallbackNavMeshHarness");
            try
            {
                var room = root.AddComponent<RoomRuntimeRoot>();
                var asset = HollowRuntimeV2Importer.Import(File.ReadAllText(TemplateRoomPath));

                LogAssert.Expect(LogType.Warning, new Regex("dev-only runtime Unity NavMesh fallback"));
                room.BuildFrom(asset, RoomNavMeshRuntimeFallbackMode.EditorOrDevelopmentRuntimeBake);

                Assert.IsTrue(room.HasNavMeshBake, room.NavMeshBakeError);
                Assert.IsTrue(room.HasRuntimeBuiltNavMesh);
                Assert.AreEqual("dev-runtime-fallback", room.NavMeshBakeSource);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void DocsAndReportExist()
        {
            Assert.IsTrue(File.Exists(Milestone100NavMeshBakePipelineAssetGenerator.DocsPath), Milestone100NavMeshBakePipelineAssetGenerator.DocsPath);
            Assert.IsTrue(File.Exists(Milestone100NavMeshBakePipelineAssetGenerator.ReportPath), Milestone100NavMeshBakePipelineAssetGenerator.ReportPath);
            var markdown = File.ReadAllText(Milestone100NavMeshBakePipelineAssetGenerator.DocsPath);
            StringAssert.Contains("NavMesh Bake Pipeline", markdown);
            StringAssert.Contains("Bake Runtime Room NavMeshes", markdown);
            StringAssert.Contains("runtime fallback", markdown.ToLowerInvariant());
        }
    }
}
