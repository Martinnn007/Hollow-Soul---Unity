using System;
using System.Collections.Generic;

namespace Hollow.Editor.Build
{
    [Serializable]
    public sealed class ExternalHandoffReport
    {
        public string reportId = string.Empty;
        public string generatedAtUtc = string.Empty;
        public string handoffId = string.Empty;
        public string displayName = string.Empty;
        public string result = PlatformBuildQaResult.NotRun;
        public string unityVersion = string.Empty;
        public string gitBranch = string.Empty;
        public string gitCommit = string.Empty;
        public string platformQaResult = PlatformBuildQaResult.NotRun;
        public string verticalSliceResult = PlatformBuildQaResult.NotRun;
        public string acceptedEnvironmentBlocks = string.Empty;
        public List<ExternalHandoffCheckResult> checks = new();
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
    public sealed class ExternalHandoffCheckResult
    {
        public string id = string.Empty;
        public string result = PlatformBuildQaResult.NotRun;
        public List<string> messages = new();
        public List<string> remediation = new();

        public static ExternalHandoffCheckResult Passed(string id, params string[] messages)
        {
            return Create(id, PlatformBuildQaResult.Passed, messages, Array.Empty<string>());
        }

        public static ExternalHandoffCheckResult Failed(string id, string message, params string[] remediation)
        {
            return Create(id, PlatformBuildQaResult.Failed, new[] { message }, remediation);
        }

        public static ExternalHandoffCheckResult BlockedByEnvironment(string id, string message, params string[] remediation)
        {
            return Create(id, PlatformBuildQaResult.BlockedByEnvironment, new[] { message }, remediation);
        }

        private static ExternalHandoffCheckResult Create(string id, string result, string[] messages, string[] remediation)
        {
            var check = new ExternalHandoffCheckResult
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
