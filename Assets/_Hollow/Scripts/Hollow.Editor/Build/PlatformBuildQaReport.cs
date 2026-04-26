using System;
using System.Collections.Generic;

namespace Hollow.Editor.Build
{
    [Serializable]
    public sealed class PlatformBuildQaReport
    {
        public string reportId = string.Empty;
        public string generatedAtUtc = string.Empty;
        public string prototypeVersion = string.Empty;
        public string unityVersion = string.Empty;
        public string gitBranch = string.Empty;
        public string gitCommit = string.Empty;
        public string result = PlatformBuildQaResult.NotRun;
        public string reportRoot = string.Empty;
        public string buildRoot = string.Empty;
        public List<PlatformBuildTargetResult> targets = new();
        public List<string> manualChecklist = new();

        public bool HasFailure
        {
            get
            {
                foreach (var target in targets)
                {
                    if (target.result == PlatformBuildQaResult.Failed)
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
                foreach (var target in targets)
                {
                    if (target.result == PlatformBuildQaResult.BlockedByEnvironment)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        public void Recalculate(bool allowEnvironmentBlocks)
        {
            if (HasFailure)
            {
                result = PlatformBuildQaResult.Failed;
            }
            else if (HasEnvironmentBlock)
            {
                result = allowEnvironmentBlocks ? PlatformBuildQaResult.PassedWithEnvironmentBlocks : PlatformBuildQaResult.BlockedByEnvironment;
            }
            else
            {
                result = PlatformBuildQaResult.Passed;
            }
        }
    }

    [Serializable]
    public sealed class PlatformBuildTargetResult
    {
        public string id = string.Empty;
        public string platform = string.Empty;
        public string result = PlatformBuildQaResult.NotRun;
        public string outputPath = string.Empty;
        public double durationMs;
        public List<string> messages = new();
        public List<string> remediation = new();

        public static PlatformBuildTargetResult Passed(string id, string platform, string outputPath, double durationMs, params string[] messages)
        {
            return Create(id, platform, PlatformBuildQaResult.Passed, outputPath, durationMs, messages, Array.Empty<string>());
        }

        public static PlatformBuildTargetResult Failed(string id, string platform, string outputPath, double durationMs, string message, params string[] remediation)
        {
            return Create(id, platform, PlatformBuildQaResult.Failed, outputPath, durationMs, new[] { message }, remediation);
        }

        public static PlatformBuildTargetResult BlockedByEnvironment(string id, string platform, string outputPath, double durationMs, string message, params string[] remediation)
        {
            return Create(id, platform, PlatformBuildQaResult.BlockedByEnvironment, outputPath, durationMs, new[] { message }, remediation);
        }

        public static PlatformBuildTargetResult NotRun(string id, string platform, string message, params string[] remediation)
        {
            return Create(id, platform, PlatformBuildQaResult.NotRun, string.Empty, 0, new[] { message }, remediation);
        }

        private static PlatformBuildTargetResult Create(
            string id,
            string platform,
            string result,
            string outputPath,
            double durationMs,
            string[] messages,
            string[] remediation)
        {
            var entry = new PlatformBuildTargetResult
            {
                id = id ?? string.Empty,
                platform = platform ?? string.Empty,
                result = result ?? PlatformBuildQaResult.NotRun,
                outputPath = outputPath ?? string.Empty,
                durationMs = durationMs
            };
            if (messages != null)
            {
                entry.messages.AddRange(messages);
            }

            if (remediation != null)
            {
                entry.remediation.AddRange(remediation);
            }

            return entry;
        }
    }

    public static class PlatformBuildQaResult
    {
        public const string NotRun = "NotRun";
        public const string Passed = "Passed";
        public const string PassedWithEnvironmentBlocks = "PassedWithEnvironmentBlocks";
        public const string Failed = "Failed";
        public const string BlockedByEnvironment = "BlockedByEnvironment";
    }
}
