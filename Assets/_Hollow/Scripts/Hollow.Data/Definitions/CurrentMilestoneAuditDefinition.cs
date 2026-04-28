using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Hollow.Data.Definitions
{
    [CreateAssetMenu(menuName = "Hollow/Handoff/Current Milestone Audit Definition")]
    public sealed class CurrentMilestoneAuditDefinition : ScriptableObject
    {
        [SerializeField] private string auditId = "m41_current_milestone_audit";
        [SerializeField] private string displayName = "M41 Current Milestone Audit";
        [SerializeField] private string reportRoot = "output/reports";
        [SerializeField] private string latestJsonFileName = "latest_m41_current_milestone_audit.json";
        [SerializeField] private string latestMarkdownFileName = "latest_m41_current_milestone_audit.md";
        [SerializeField] private string[] validationTypes = Array.Empty<string>();
        [SerializeField] private string[] requiredEvidenceReports = Array.Empty<string>();

        public string AuditId => string.IsNullOrWhiteSpace(auditId) ? "m41_current_milestone_audit" : auditId;

        public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? "M41 Current Milestone Audit" : displayName;

        public string ReportRoot => string.IsNullOrWhiteSpace(reportRoot) ? "output/reports" : reportRoot;

        public string LatestJsonFileName => string.IsNullOrWhiteSpace(latestJsonFileName) ? "latest_m41_current_milestone_audit.json" : latestJsonFileName;

        public string LatestMarkdownFileName => string.IsNullOrWhiteSpace(latestMarkdownFileName) ? "latest_m41_current_milestone_audit.md" : latestMarkdownFileName;

        public IReadOnlyList<string> ValidationTypes => validationTypes;

        public IReadOnlyList<string> RequiredEvidenceReports => requiredEvidenceReports;

        public string LatestJsonPath => System.IO.Path.Combine(ReportRoot, LatestJsonFileName);

        public string LatestMarkdownPath => System.IO.Path.Combine(ReportRoot, LatestMarkdownFileName);

        public void Configure(
            string nextAuditId,
            string nextDisplayName,
            string nextReportRoot,
            string nextLatestJsonFileName,
            string nextLatestMarkdownFileName,
            IEnumerable<string> nextValidationTypes,
            IEnumerable<string> nextRequiredEvidenceReports)
        {
            auditId = string.IsNullOrWhiteSpace(nextAuditId) ? "m41_current_milestone_audit" : nextAuditId;
            displayName = string.IsNullOrWhiteSpace(nextDisplayName) ? "M41 Current Milestone Audit" : nextDisplayName;
            reportRoot = string.IsNullOrWhiteSpace(nextReportRoot) ? "output/reports" : nextReportRoot;
            latestJsonFileName = string.IsNullOrWhiteSpace(nextLatestJsonFileName) ? "latest_m41_current_milestone_audit.json" : nextLatestJsonFileName;
            latestMarkdownFileName = string.IsNullOrWhiteSpace(nextLatestMarkdownFileName) ? "latest_m41_current_milestone_audit.md" : nextLatestMarkdownFileName;
            validationTypes = Clean(nextValidationTypes);
            requiredEvidenceReports = Clean(nextRequiredEvidenceReports);
        }

        private static string[] Clean(IEnumerable<string> values)
        {
            return (values ?? Enumerable.Empty<string>())
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Distinct()
                .ToArray();
        }
    }
}
