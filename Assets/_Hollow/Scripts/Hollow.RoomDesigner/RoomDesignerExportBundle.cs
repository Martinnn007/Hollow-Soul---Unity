using System;
using System.IO;
using UnityEngine;

namespace Hollow.RoomDesigner
{
    public sealed class RoomDesignerExportBundle
    {
        public string directory;
        public string projectJsonPath;
        public string runtimeJsonPath;
        public string usdaPath;
        public string validationReportPath;
        public RoomDesignerValidationReport validationReport;

        public static RoomDesignerExportBundle Export(RoomDesignerProject project, string exportRoot = null)
        {
            if (project == null)
            {
                throw new ArgumentNullException(nameof(project));
            }

            var report = RoomDesignerDraftValidator.Validate(project);
            if (!report.IsValid)
            {
                throw new InvalidOperationException($"Room Designer export blocked: {report.Summary()}");
            }

            var directory = RoomDesignerJsonExporter.ExportDirectory(project, exportRoot);
            Directory.CreateDirectory(directory);
            var bundle = new RoomDesignerExportBundle
            {
                directory = directory,
                projectJsonPath = RoomDesignerJsonExporter.ExportProject(project, exportRoot),
                runtimeJsonPath = RoomDesignerJsonExporter.ExportRuntime(project, exportRoot),
                usdaPath = RoomDesignerUsdaExporter.ExportScene(project, exportRoot),
                validationReportPath = Path.Combine(directory, "validation-report.json"),
                validationReport = report
            };
            File.WriteAllText(bundle.validationReportPath, JsonUtility.ToJson(report, prettyPrint: true));
            return bundle;
        }
    }
}
