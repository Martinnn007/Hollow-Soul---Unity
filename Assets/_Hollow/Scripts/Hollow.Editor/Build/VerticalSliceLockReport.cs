using System;
using System.Collections.Generic;

namespace Hollow.Editor.Build
{
    [Serializable]
    public sealed class VerticalSliceLockReport
    {
        public string reportId = string.Empty;
        public string generatedAtUtc = string.Empty;
        public string lockName = string.Empty;
        public string branchIdentity = string.Empty;
        public int lockedSeed;
        public string result = PlatformBuildQaResult.NotRun;
        public string unityVersion = string.Empty;
        public string gitBranch = string.Empty;
        public string gitCommit = string.Empty;
        public string reportRoot = string.Empty;
        public string pdfOutputPath = string.Empty;
        public int roomCount;
        public int connectionCount;
        public int fixtureRoomCount;
        public int approvedRoomCount;
        public int shopOfferCount;
        public int nextBranchPortalCount;
        public List<VerticalSliceCheckResult> checks = new();
        public List<string> manualChecklist = new();

        public bool HasFailure
        {
            get
            {
                foreach (var check in checks)
                {
                    if (check.result == PlatformBuildQaResult.Failed)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        public bool HasEnvironmentBlock
        {
            get
            {
                foreach (var check in checks)
                {
                    if (check.result == PlatformBuildQaResult.BlockedByEnvironment)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        public void Recalculate()
        {
            if (HasFailure)
            {
                result = PlatformBuildQaResult.Failed;
            }
            else if (HasEnvironmentBlock)
            {
                result = PlatformBuildQaResult.PassedWithEnvironmentBlocks;
            }
            else
            {
                result = PlatformBuildQaResult.Passed;
            }
        }
    }

    [Serializable]
    public sealed class VerticalSliceCheckResult
    {
        public string id = string.Empty;
        public string result = PlatformBuildQaResult.NotRun;
        public List<string> messages = new();
        public List<string> remediation = new();

        public static VerticalSliceCheckResult Passed(string id, params string[] messages)
        {
            return Create(id, PlatformBuildQaResult.Passed, messages, Array.Empty<string>());
        }

        public static VerticalSliceCheckResult Failed(string id, string message, params string[] remediation)
        {
            return Create(id, PlatformBuildQaResult.Failed, new[] { message }, remediation);
        }

        public static VerticalSliceCheckResult BlockedByEnvironment(string id, string message, params string[] remediation)
        {
            return Create(id, PlatformBuildQaResult.BlockedByEnvironment, new[] { message }, remediation);
        }

        private static VerticalSliceCheckResult Create(string id, string result, string[] messages, string[] remediation)
        {
            var check = new VerticalSliceCheckResult
            {
                id = id ?? string.Empty,
                result = result ?? PlatformBuildQaResult.NotRun
            };
            if (messages != null)
            {
                check.messages.AddRange(messages);
            }

            if (remediation != null)
            {
                check.remediation.AddRange(remediation);
            }

            return check;
        }
    }
}
