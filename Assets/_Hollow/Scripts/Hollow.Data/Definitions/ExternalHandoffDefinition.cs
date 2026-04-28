using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Hollow.Data.Definitions
{
    [CreateAssetMenu(menuName = "Hollow/Handoff/External Handoff Definition")]
    public sealed class ExternalHandoffDefinition : ScriptableObject
    {
        [SerializeField] private string handoffId = "m40_external_handoff";
        [SerializeField] private string displayName = "M40 Vertical Slice Re-Lock + External Handoff";
        [SerializeField] private string reportRoot = "output/reports";
        [SerializeField] private string latestJsonFileName = "latest_m40_external_handoff.json";
        [SerializeField] private string latestMarkdownFileName = "latest_m40_external_handoff.md";
        [SerializeField] private string verticalSliceReportPath = "output/reports/latest_vertical_slice_lock.json";
        [SerializeField] private string platformQaReportPath = "output/reports/latest_platform_build_qa.json";
        [SerializeField] private string editModeResultsPath = "output/reports/m24-editmode-results.xml";
        [SerializeField] private string[] requiredDocs = Array.Empty<string>();
        [SerializeField] private string[] requiredReports = Array.Empty<string>();
        [SerializeField] private string[] acceptedEnvironmentBlocks = Array.Empty<string>();
        [SerializeField] private string[] manualHandoffChecklist = Array.Empty<string>();

        public string HandoffId => string.IsNullOrWhiteSpace(handoffId) ? "m40_external_handoff" : handoffId;

        public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? "M40 External Handoff" : displayName;

        public string ReportRoot => string.IsNullOrWhiteSpace(reportRoot) ? "output/reports" : reportRoot;

        public string LatestJsonFileName => string.IsNullOrWhiteSpace(latestJsonFileName) ? "latest_m40_external_handoff.json" : latestJsonFileName;

        public string LatestMarkdownFileName => string.IsNullOrWhiteSpace(latestMarkdownFileName) ? "latest_m40_external_handoff.md" : latestMarkdownFileName;

        public string VerticalSliceReportPath => verticalSliceReportPath ?? string.Empty;

        public string PlatformQaReportPath => platformQaReportPath ?? string.Empty;

        public string EditModeResultsPath => editModeResultsPath ?? string.Empty;

        public IReadOnlyList<string> RequiredDocs => requiredDocs;

        public IReadOnlyList<string> RequiredReports => requiredReports;

        public IReadOnlyList<string> AcceptedEnvironmentBlocks => acceptedEnvironmentBlocks;

        public IReadOnlyList<string> ManualHandoffChecklist => manualHandoffChecklist;

        public string LatestJsonPath => System.IO.Path.Combine(ReportRoot, LatestJsonFileName);

        public string LatestMarkdownPath => System.IO.Path.Combine(ReportRoot, LatestMarkdownFileName);

        public void Configure(
            string nextHandoffId,
            string nextDisplayName,
            string nextReportRoot,
            string nextLatestJsonFileName,
            string nextLatestMarkdownFileName,
            string nextVerticalSliceReportPath,
            string nextPlatformQaReportPath,
            string nextEditModeResultsPath,
            IEnumerable<string> nextRequiredDocs,
            IEnumerable<string> nextRequiredReports,
            IEnumerable<string> nextAcceptedEnvironmentBlocks,
            IEnumerable<string> nextManualHandoffChecklist)
        {
            handoffId = string.IsNullOrWhiteSpace(nextHandoffId) ? "m40_external_handoff" : nextHandoffId;
            displayName = string.IsNullOrWhiteSpace(nextDisplayName) ? "M40 External Handoff" : nextDisplayName;
            reportRoot = string.IsNullOrWhiteSpace(nextReportRoot) ? "output/reports" : nextReportRoot;
            latestJsonFileName = string.IsNullOrWhiteSpace(nextLatestJsonFileName) ? "latest_m40_external_handoff.json" : nextLatestJsonFileName;
            latestMarkdownFileName = string.IsNullOrWhiteSpace(nextLatestMarkdownFileName) ? "latest_m40_external_handoff.md" : nextLatestMarkdownFileName;
            verticalSliceReportPath = nextVerticalSliceReportPath ?? string.Empty;
            platformQaReportPath = nextPlatformQaReportPath ?? string.Empty;
            editModeResultsPath = nextEditModeResultsPath ?? string.Empty;
            requiredDocs = Clean(nextRequiredDocs);
            requiredReports = Clean(nextRequiredReports);
            acceptedEnvironmentBlocks = Clean(nextAcceptedEnvironmentBlocks);
            manualHandoffChecklist = Clean(nextManualHandoffChecklist);
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
