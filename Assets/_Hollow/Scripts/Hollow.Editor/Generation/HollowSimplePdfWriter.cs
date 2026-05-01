using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Hollow.Editor.Generation
{
    public static class HollowSimplePdfWriter
    {
        public static void Write(string path, IReadOnlyList<string> lines)
        {
            var directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var pages = SplitPages(lines ?? Array.Empty<string>(), 44);
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
