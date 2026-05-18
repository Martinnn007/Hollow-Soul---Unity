using System;
using System.Collections.Generic;
using System.Linq;

namespace Hollow.Editor.Build
{
    public enum WholeGameAuditSeverity
    {
        Info,
        Warning,
        Blocker
    }

    [Serializable]
    public sealed class WholeGameAuditReport
    {
        public string auditId = string.Empty;
        public string generatedAtUtc = string.Empty;
        public string unityVersion = string.Empty;
        public string gitBranch = string.Empty;
        public string gitCommit = string.Empty;
        public string result = "NotRun";
        public bool strictReleaseGate;
        public int totalFindings;
        public int infoCount;
        public int warningCount;
        public int blockerCount;
        public List<WholeGameAuditMilestone> milestones = new();
        public List<WholeGameAuditFinding> findings = new();

        public bool Passed => blockerCount == 0;

        public IReadOnlyList<WholeGameAuditFinding> Blockers =>
            findings?.Where(finding => finding != null && finding.IsBlocker).ToArray()
            ?? Array.Empty<WholeGameAuditFinding>();

        public void Recalculate()
        {
            findings ??= new List<WholeGameAuditFinding>();
            milestones ??= new List<WholeGameAuditMilestone>();

            totalFindings = findings.Count;
            infoCount = findings.Count(finding => finding.Severity == WholeGameAuditSeverity.Info);
            warningCount = findings.Count(finding => finding.Severity == WholeGameAuditSeverity.Warning);
            blockerCount = findings.Count(finding => finding.Severity == WholeGameAuditSeverity.Blocker);
            result = blockerCount == 0 ? "Passed" : "Failed";

            foreach (var milestone in milestones)
            {
                if (milestone == null)
                {
                    continue;
                }

                milestone.infoCount = findings.Count(finding =>
                    finding.milestone == milestone.milestone &&
                    finding.Severity == WholeGameAuditSeverity.Info);
                milestone.warningCount = findings.Count(finding =>
                    finding.milestone == milestone.milestone &&
                    finding.Severity == WholeGameAuditSeverity.Warning);
                milestone.blockerCount = findings.Count(finding =>
                    finding.milestone == milestone.milestone &&
                    finding.Severity == WholeGameAuditSeverity.Blocker);
            }
        }
    }

    [Serializable]
    public sealed class WholeGameAuditMilestone
    {
        public int milestone;
        public string title = string.Empty;
        public string goal = string.Empty;
        public string primarySubsystem = string.Empty;
        public string defaultSolution = string.Empty;
        public int infoCount;
        public int warningCount;
        public int blockerCount;
    }

    [Serializable]
    public sealed class WholeGameAuditFinding
    {
        public int milestone;
        public string category = string.Empty;
        public string severity = WholeGameAuditSeverity.Warning.ToString();
        public string title = string.Empty;
        public string message = string.Empty;
        public string location = string.Empty;
        public string solution = string.Empty;

        public WholeGameAuditSeverity Severity =>
            Enum.TryParse<WholeGameAuditSeverity>(severity, ignoreCase: true, out var parsed)
                ? parsed
                : WholeGameAuditSeverity.Warning;

        public bool IsBlocker => Severity == WholeGameAuditSeverity.Blocker;

        public static WholeGameAuditFinding Info(
            int milestone,
            string category,
            string title,
            string message,
            string location,
            string solution)
        {
            return Create(milestone, category, WholeGameAuditSeverity.Info, title, message, location, solution);
        }

        public static WholeGameAuditFinding Warning(
            int milestone,
            string category,
            string title,
            string message,
            string location,
            string solution)
        {
            return Create(milestone, category, WholeGameAuditSeverity.Warning, title, message, location, solution);
        }

        public static WholeGameAuditFinding Blocker(
            int milestone,
            string category,
            string title,
            string message,
            string location,
            string solution)
        {
            return Create(milestone, category, WholeGameAuditSeverity.Blocker, title, message, location, solution);
        }

        private static WholeGameAuditFinding Create(
            int milestone,
            string category,
            WholeGameAuditSeverity severity,
            string title,
            string message,
            string location,
            string solution)
        {
            return new WholeGameAuditFinding
            {
                milestone = milestone,
                category = category ?? string.Empty,
                severity = severity.ToString(),
                title = title ?? string.Empty,
                message = message ?? string.Empty,
                location = location ?? string.Empty,
                solution = solution ?? string.Empty
            };
        }
    }
}
