using System.IO;
using Hollow.Core.Diagnostics;
using Hollow.Diagnostics;
using NUnit.Framework;

namespace Hollow.Tests.EditMode
{
    public sealed class CpuTruthCaptureTests
    {
        private const string BranchSessionControllerPath = "Assets/_Hollow/Scripts/Hollow.Branches/BranchSessionController.cs";
        private const string RoomRuntimeRootPath = "Assets/_Hollow/Scripts/Hollow.Rooms/RoomRuntimeRoot.cs";

        [SetUp]
        public void SetUp()
        {
            M136PerformanceOperationCounters.Reset();
        }

        [Test]
        public void TruthCaptureModeDisablesPerCadenceObjectCounts()
        {
            Assert.IsTrue(M136EditorLaptopPerformancePolicy.IsTruthCaptureMode(M136EditorLaptopPerformancePolicy.TruthCaptureMode));
            Assert.IsFalse(M136EditorLaptopPerformancePolicy.ShouldCollectObjectCountsDuringCapture(M136EditorLaptopPerformancePolicy.TruthCaptureMode));
            Assert.IsFalse(M136EditorLaptopPerformancePolicy.ShouldCollectObjectCountsDuringCapture("m140-built-player"));
            Assert.IsTrue(M136EditorLaptopPerformancePolicy.ShouldCollectObjectCountsDuringCapture(M136EditorLaptopPerformancePolicy.DiagnosticCaptureMode));
        }

        [Test]
        public void CpuStageSummaryReportsTargetedTraversalStages()
        {
            M136PerformanceOperationCounters.ReportCpuStage(M136CpuStageKind.LiveRoomActivation, 2.5f, 128);
            M136PerformanceOperationCounters.ReportCpuStage(M136CpuStageKind.LiveRoomActivation, 4.25f, 64);
            M136PerformanceOperationCounters.ReportCpuStage(M136CpuStageKind.DoorVisualState, 1.5f, 0);

            var summary = M136PerformanceOperationCounters.Snapshot().CpuStageSummary;
            StringAssert.Contains("live_room_activation count=2 maxMs=4.25 gcMax=128", summary);
            StringAssert.Contains("door_visual_state count=1 maxMs=1.5 gcMax=0", summary);
        }

        [Test]
        public void TransitionRoutineNoLongerWrapsCoroutineMoveNextInRoomTransitionProfilerMarker()
        {
            var source = File.ReadAllText(BranchSessionControllerPath);
            Assert.IsFalse(source.Contains("RunProfiledTransitionStage", System.StringComparison.Ordinal));
            Assert.IsFalse(source.Contains("RoomTransitionLoad.Auto()", System.StringComparison.Ordinal));
            StringAssert.Contains("RunTransitionStage(roomLoadRoutine)", source);
        }

        [Test]
        public void NormalTraversalDoesNotSchedulePredictivePreloadWhenLiveRoomCacheIsComplete()
        {
            var source = File.ReadAllText(BranchSessionControllerPath);
            var schedule = ExtractMethodBlock(source, "private void ScheduleBranchPreload");
            StringAssert.Contains("HasCompleteBranchLiveRoomCache()", schedule);
            StringAssert.Contains("return;", schedule);
        }

        [Test]
        public void DoorVisualsCacheStateBeforeReinstantiatingArtChildren()
        {
            var source = File.ReadAllText(RoomRuntimeRootPath);
            StringAssert.Contains("doorVisualStateByPortId", source);
            StringAssert.Contains("existingState == state", source);
            StringAssert.Contains("doorVisualStateByPortId[portId] = state", source);
        }

        private static string ExtractMethodBlock(string source, string signature)
        {
            var start = source.IndexOf(signature, System.StringComparison.Ordinal);
            Assert.GreaterOrEqual(start, 0, $"Could not find `{signature}`.");
            var brace = source.IndexOf('{', start);
            Assert.Greater(brace, start, $"Could not find method body for `{signature}`.");
            var depth = 0;
            for (var index = brace; index < source.Length; index++)
            {
                if (source[index] == '{')
                {
                    depth++;
                }
                else if (source[index] == '}')
                {
                    depth--;
                    if (depth == 0)
                    {
                        return source.Substring(start, index - start + 1);
                    }
                }
            }

            Assert.Fail($"Could not parse method block for `{signature}`.");
            return string.Empty;
        }
    }
}
