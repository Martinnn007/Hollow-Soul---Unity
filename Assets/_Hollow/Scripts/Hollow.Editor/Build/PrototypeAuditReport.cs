using System;
using System.Collections.Generic;

namespace Hollow.Editor.Build
{
    [Serializable]
    public sealed class PrototypeAuditReport
    {
        public string auditId = string.Empty;
        public string generatedAtUtc = string.Empty;
        public string unityVersion = string.Empty;
        public string gitBranch = string.Empty;
        public string gitCommit = string.Empty;
        public string result = "Unknown";
        public int totalChecks;
        public int passedChecks;
        public int failedChecks;
        public List<PrototypeAuditEntry> entries = new();

        public bool Passed => failedChecks == 0 && totalChecks > 0;

        public void Recalculate()
        {
            totalChecks = entries.Count;
            passedChecks = 0;
            failedChecks = 0;
            foreach (var entry in entries)
            {
                if (entry.passed)
                {
                    passedChecks++;
                }
                else
                {
                    failedChecks++;
                }
            }

            result = Passed ? "Passed" : "Failed";
        }
    }

    [Serializable]
    public sealed class PrototypeAuditEntry
    {
        public string id = string.Empty;
        public string validatorType = string.Empty;
        public bool passed;
        public double durationMs;
        public List<string> messages = new();
    }
}
