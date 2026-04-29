using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Hollow.Data.Definitions;
using Hollow.Editor.Validation;
using UnityEditor;
using UnityEngine;

namespace Hollow.Editor.Generation
{
    public static class Milestone49AssetGenerator
    {
        public const string ReportJsonPath = "output/reports/m49_artpass_production_status.json";
        public const string ReportMarkdownPath = "output/reports/m49_artpass_production_status.md";
        public const string PdfPath = "output/pdf/Hollow_M49_ArtPass_Production_Integration_II.pdf";
        public const string DocsPath = "Docs/Milestone49ArtPassProductionIntegrationII.md";

        [MenuItem("Hollow/Generation/Generate Milestone 49 Assets")]
        public static void Generate()
        {
            Milestone48AssetGenerator.Generate();
            Directory.CreateDirectory(Path.GetDirectoryName(ReportJsonPath) ?? "output/reports");
            Directory.CreateDirectory(Path.GetDirectoryName(PdfPath) ?? "output/pdf");

            var report = ArtPassProductionValidator.BuildReport();
            WriteReports(report);
            AssetDatabase.Refresh();
            Debug.Log($"Generated Hollow Milestone 49 ArtPass production status report with {report.totalTargets} tracked roles.");
        }

        public static void WriteReports(ArtPassProductionStatusReport report)
        {
            report.Recalculate();
            File.WriteAllText(ReportJsonPath, JsonUtility.ToJson(report, prettyPrint: true));
            File.WriteAllText(ReportMarkdownPath, ToMarkdown(report));
            WritePdf(report);
        }

        private static string ToMarkdown(ArtPassProductionStatusReport report)
        {
            var coreRows = report.targets
                .Where(target => target.corePriority)
                .OrderBy(target => target.group)
                .ThenBy(target => target.role)
                .Select(target => $"| {target.group} | {target.displayName} | {target.status} | `{target.prefabPath}` | {Format(target.warnings)} | {Format(target.errors)} |");
            var allRows = report.targets
                .OrderBy(target => target.group)
                .ThenBy(target => target.role)
                .Select(target => $"| {target.group} | {target.displayName} | {target.status} | {Format(target.warnings)} | {Format(target.errors)} |");

            return
                "# M49 ArtPass Production Integration II\n\n" +
                $"Generated: {report.generatedAtUtc}\n\n" +
                "## Summary\n\n" +
                $"- Total tracked visible roles: {report.totalTargets}\n" +
                $"- Production ready: {report.productionReadyCount}\n" +
                $"- Prototype fallback warnings: {report.prototypeFallbackCount}\n" +
                $"- Missing bindings: {report.missingBindingCount}\n" +
                $"- Unsafe prefabs: {report.unsafePrefabCount}\n" +
                $"- Blocking failures: {(report.HasBlockingFailures ? "Yes" : "No")}\n\n" +
                "## Direct Replacement Workflow\n\n" +
                "- Replace the active `AP_*` or `VFX_*` prefab under `Assets/_Hollow/Prefabs/ArtPass/`.\n" +
                "- Keep `PresentationVisualMarker` on the prefab root with the matching `PresentationPrefabRole`.\n" +
                "- Do not add gameplay colliders or gameplay scripts to visual prefabs.\n" +
                "- Keep gameplay collision, damage, traversal, rewards, and room layout in runtime code/data only.\n" +
                "- Room Designer Scene Mode previews the same active ArtPass catalog as gameplay.\n\n" +
                "## Core Vertical-Slice Pack\n\n" +
                "| Group | Target | Status | Prefab | Warnings | Errors |\n" +
                "| --- | --- | --- | --- | --- | --- |\n" +
                string.Join("\n", coreRows) +
                "\n\n## All Visible Roles\n\n" +
                "| Group | Target | Status | Warnings | Errors |\n" +
                "| --- | --- | --- | --- | --- |\n" +
                string.Join("\n", allRows) +
                "\n";
        }

        private static void WritePdf(ArtPassProductionStatusReport report)
        {
            var lines = new List<string>
            {
                "Hollow M49: ArtPass Production Integration II",
                $"Generated: {report.generatedAtUtc}",
                $"Tracked visible roles: {report.totalTargets}",
                $"Production ready: {report.productionReadyCount}",
                $"Prototype fallback warnings: {report.prototypeFallbackCount}",
                $"Missing bindings: {report.missingBindingCount}",
                $"Unsafe prefabs: {report.unsafePrefabCount}",
                "",
                "Direct Replacement Rules:",
                "- Replace existing AP_* / VFX_* prefabs directly under Assets/_Hollow/Prefabs/ArtPass/.",
                "- Keep PresentationVisualMarker on the root with the matching role.",
                "- Visual prefabs must not include gameplay colliders or gameplay scripts.",
                "- Missing production art is allowed in M49; unsafe prefab wiring is not.",
                "",
                "Core Vertical-Slice Targets:"
            };

            lines.AddRange(report.targets
                .Where(target => target.corePriority)
                .OrderBy(target => target.group)
                .ThenBy(target => target.role)
                .Select(target => $"- {target.displayName}: {target.status}"));

            lines.Add("");
            lines.Add("All Visible Roles:");
            lines.AddRange(report.targets
                .OrderBy(target => target.group)
                .ThenBy(target => target.role)
                .Select(target => $"- {target.group} / {target.displayName}: {target.status}"));

            lines.Add("");
            lines.Add("Next Rafal/Martin Handoff:");
            lines.Add("- Start with the core slice pack before secondary equipment/hazard polish.");
            lines.Add("- When a production mesh/material arrives, update the matching active prefab and rerun M49 validation.");
            lines.Add("- Room Designer Scene Mode should immediately preview the replaced ArtPass prefab.");

            SimplePdfWriter.Write(PdfPath, lines);
        }

        private static string Format(IEnumerable<string> values)
        {
            var list = values?.Where(value => !string.IsNullOrWhiteSpace(value)).ToArray() ?? Array.Empty<string>();
            return list.Length == 0 ? "OK" : string.Join("<br>", list.Select(EscapeMarkdown));
        }

        private static string EscapeMarkdown(string value)
        {
            return (value ?? string.Empty).Replace("|", "\\|");
        }

        private static class SimplePdfWriter
        {
            public static void Write(string path, IReadOnlyList<string> lines)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path) ?? "output/pdf");
                var pages = SplitPages(lines, 44);
                var objects = new List<string>
                {
                    string.Empty,
                    string.Empty,
                    "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>"
                };

                var pageObjectIds = new List<int>();
                foreach (var pageLines in pages)
                {
                    var contentId = objects.Count + 1;
                    objects.Add(ContentObjectFor(pageLines));
                    var pageId = objects.Count + 1;
                    pageObjectIds.Add(pageId);
                    objects.Add($"<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] /Resources << /Font << /F1 3 0 R >> >> /Contents {contentId} 0 R >>");
                }

                objects[0] = "<< /Type /Catalog /Pages 2 0 R >>";
                objects[1] = $"<< /Type /Pages /Kids [{string.Join(" ", pageObjectIds.Select(id => $"{id} 0 R"))}] /Count {pageObjectIds.Count} >>";

                using var stream = new MemoryStream();
                using var writer = new StreamWriter(stream, Encoding.ASCII, 1024, leaveOpen: true);
                writer.Write("%PDF-1.4\n");
                writer.Flush();
                var offsets = new List<long>();
                for (var index = 0; index < objects.Count; index++)
                {
                    offsets.Add(stream.Position);
                    writer.Write($"{index + 1} 0 obj\n{objects[index]}\nendobj\n");
                    writer.Flush();
                }

                var xrefOffset = stream.Position;
                writer.Write($"xref\n0 {objects.Count + 1}\n0000000000 65535 f \n");
                foreach (var offset in offsets)
                {
                    writer.Write($"{offset:0000000000} 00000 n \n");
                }

                writer.Write($"trailer\n<< /Size {objects.Count + 1} /Root 1 0 R >>\nstartxref\n{xrefOffset}\n%%EOF\n");
                writer.Flush();
                File.WriteAllBytes(path, stream.ToArray());
            }

            private static List<IReadOnlyList<string>> SplitPages(IReadOnlyList<string> lines, int linesPerPage)
            {
                var pages = new List<IReadOnlyList<string>>();
                for (var index = 0; index < lines.Count; index += linesPerPage)
                {
                    pages.Add(lines.Skip(index).Take(linesPerPage).ToArray());
                }

                if (pages.Count == 0)
                {
                    pages.Add(Array.Empty<string>());
                }

                return pages;
            }

            private static string ContentObjectFor(IReadOnlyList<string> lines)
            {
                var builder = new StringBuilder();
                builder.Append("BT\n/F1 10 Tf\n50 760 Td\n14 TL\n");
                foreach (var line in lines)
                {
                    builder.Append('(').Append(EscapePdf(line)).Append(") Tj\nT*\n");
                }

                builder.Append("ET\n");
                var content = builder.ToString();
                return $"<< /Length {Encoding.ASCII.GetByteCount(content)} >>\nstream\n{content}endstream";
            }

            private static string EscapePdf(string value)
            {
                var clean = new string((value ?? string.Empty)
                    .Select(character => character >= ' ' && character <= '~' ? character : '?')
                    .ToArray());
                return clean.Replace("\\", "\\\\").Replace("(", "\\(").Replace(")", "\\)");
            }
        }
    }
}
