using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace Hollow.Editor.Build
{
    public static class VerticalSlicePdfExporter
    {
        private const float PageWidth = 612f;
        private const float PageHeight = 792f;
        private const float Left = 50f;
        private const float Top = 742f;
        private const float LineHeight = 14f;
        private const int MaxLineLength = 92;

        public static void WritePdf(string path, VerticalSliceLockReport report)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentException("PDF output path is required.", nameof(path));
            }

            Directory.CreateDirectory(Path.GetDirectoryName(path) ?? ".");
            var lines = BuildLines(report).ToList();
            var pages = Paginate(lines, 48).ToList();
            File.WriteAllBytes(path, BuildPdfBytes(pages));
        }

        private static IEnumerable<string> BuildLines(VerticalSliceLockReport report)
        {
            yield return "Hollow Soul - M25 Vertical Slice Content Lock";
            yield return string.Empty;
            yield return $"Result: {report?.result ?? "Unknown"}";
            yield return $"Generated: {report?.generatedAtUtc ?? string.Empty}";
            yield return $"Unity: {report?.unityVersion ?? string.Empty}";
            yield return $"Git: {report?.gitBranch ?? string.Empty} @ {report?.gitCommit ?? string.Empty}";
            yield return $"Branch: {report?.branchIdentity ?? string.Empty}";
            yield return $"Seed: {report?.lockedSeed ?? 0}";
            yield return $"Rooms: {report?.roomCount ?? 0}; Connections: {report?.connectionCount ?? 0}";
            yield return $"Fixtures: {report?.fixtureRoomCount ?? 0}; Approved rooms: {report?.approvedRoomCount ?? 0}";
            yield return $"Shop offers: {report?.shopOfferCount ?? 0}; Next portals: {report?.nextBranchPortalCount ?? 0}";
            yield return string.Empty;
            yield return "Lock Checks";
            if (report?.checks != null)
            {
                foreach (var check in report.checks)
                {
                    yield return $"- {check.id}: {check.result}";
                    foreach (var line in check.messages)
                    {
                        yield return $"  {line}";
                    }

                    foreach (var line in check.remediation)
                    {
                        yield return $"  Remediation: {line}";
                    }
                }
            }

            yield return string.Empty;
            yield return "Manual QA Checklist";
            if (report?.manualChecklist != null)
            {
                foreach (var item in report.manualChecklist)
                {
                    yield return $"- {item}";
                }
            }
        }

        private static IEnumerable<List<string>> Paginate(IReadOnlyList<string> sourceLines, int maxLinesPerPage)
        {
            var page = new List<string>();
            foreach (var sourceLine in sourceLines)
            {
                var wrapped = Wrap(sourceLine).ToList();
                if (wrapped.Count == 0)
                {
                    wrapped.Add(string.Empty);
                }

                foreach (var line in wrapped)
                {
                    if (page.Count >= maxLinesPerPage)
                    {
                        yield return page;
                        page = new List<string>();
                    }

                    page.Add(line);
                }
            }

            if (page.Count > 0)
            {
                yield return page;
            }
        }

        private static IEnumerable<string> Wrap(string line)
        {
            line = Sanitize(line);
            if (line.Length <= MaxLineLength)
            {
                yield return line;
                yield break;
            }

            var index = 0;
            while (index < line.Length)
            {
                var length = Math.Min(MaxLineLength, line.Length - index);
                if (index + length < line.Length)
                {
                    var breakAt = line.LastIndexOf(' ', index + length, length);
                    if (breakAt > index + 24)
                    {
                        length = breakAt - index;
                    }
                }

                yield return line.Substring(index, length).TrimEnd();
                index += length;
                while (index < line.Length && line[index] == ' ')
                {
                    index++;
                }
            }
        }

        private static byte[] BuildPdfBytes(IReadOnlyList<List<string>> pages)
        {
            var objects = new List<string>();
            var pageObjectIds = new List<int>();
            objects.Add("<< /Type /Catalog /Pages 2 0 R >>");
            objects.Add(string.Empty);
            objects.Add("<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>");

            foreach (var pageLines in pages)
            {
                var contentObjectId = objects.Count + 1;
                var pageObjectId = objects.Count + 2;
                pageObjectIds.Add(pageObjectId);
                var stream = BuildPageStream(pageLines);
                objects.Add($"<< /Length {Encoding.ASCII.GetByteCount(stream)} >>\nstream\n{stream}\nendstream");
                objects.Add($"<< /Type /Page /Parent 2 0 R /MediaBox [0 0 {PageWidth.ToString(CultureInfo.InvariantCulture)} {PageHeight.ToString(CultureInfo.InvariantCulture)}] /Resources << /Font << /F1 3 0 R >> >> /Contents {contentObjectId} 0 R >>");
            }

            objects[1] = $"<< /Type /Pages /Kids [{string.Join(" ", pageObjectIds.Select(id => $"{id} 0 R"))}] /Count {pageObjectIds.Count} >>";

            var builder = new StringBuilder();
            var offsets = new List<int> { 0 };
            builder.Append("%PDF-1.4\n");
            for (var index = 0; index < objects.Count; index++)
            {
                offsets.Add(Encoding.ASCII.GetByteCount(builder.ToString()));
                builder.Append(index + 1).Append(" 0 obj\n");
                builder.Append(objects[index]).Append('\n');
                builder.Append("endobj\n");
            }

            var xrefOffset = Encoding.ASCII.GetByteCount(builder.ToString());
            builder.Append("xref\n");
            builder.Append("0 ").Append(objects.Count + 1).Append('\n');
            builder.Append("0000000000 65535 f \n");
            for (var index = 1; index < offsets.Count; index++)
            {
                builder.Append(offsets[index].ToString("D10", CultureInfo.InvariantCulture)).Append(" 00000 n \n");
            }

            builder.Append("trailer\n");
            builder.Append("<< /Size ").Append(objects.Count + 1).Append(" /Root 1 0 R >>\n");
            builder.Append("startxref\n");
            builder.Append(xrefOffset).Append('\n');
            builder.Append("%%EOF\n");
            return Encoding.ASCII.GetBytes(builder.ToString());
        }

        private static string BuildPageStream(IReadOnlyList<string> lines)
        {
            var builder = new StringBuilder();
            builder.Append("BT\n");
            builder.Append("/F1 11 Tf\n");
            builder.Append(Left.ToString(CultureInfo.InvariantCulture)).Append(' ')
                .Append(Top.ToString(CultureInfo.InvariantCulture)).Append(" Td\n");
            for (var index = 0; index < lines.Count; index++)
            {
                if (index > 0)
                {
                    builder.Append("0 -").Append(LineHeight.ToString(CultureInfo.InvariantCulture)).Append(" Td\n");
                }

                builder.Append('(').Append(EscapePdf(lines[index])).Append(") Tj\n");
            }

            builder.Append("ET");
            return builder.ToString();
        }

        private static string Sanitize(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            var builder = new StringBuilder(value.Length);
            foreach (var character in value)
            {
                builder.Append(character is >= ' ' and <= '~' ? character : '-');
            }

            return builder.ToString();
        }

        private static string EscapePdf(string value)
        {
            return Sanitize(value).Replace("\\", "\\\\").Replace("(", "\\(").Replace(")", "\\)");
        }
    }
}
