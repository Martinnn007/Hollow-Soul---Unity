using System;
using System.Globalization;
using System.IO;
using System.Text;

namespace Hollow.RoomDesigner
{
    public static class RoomDesignerUsdaExporter
    {
        public static string ExportScene(RoomDesignerProject project, string exportRoot = null)
        {
            if (project == null)
            {
                throw new ArgumentNullException(nameof(project));
            }

            var directory = RoomDesignerJsonExporter.ExportDirectory(project, exportRoot);
            Directory.CreateDirectory(directory);
            var path = Path.Combine(directory, "scene.usda");
            File.WriteAllText(path, BuildUsda(project));
            return path;
        }

        public static string BuildUsda(RoomDesignerProject project)
        {
            var builder = new StringBuilder();
            builder.AppendLine("#usda 1.0");
            builder.AppendLine("(");
            builder.AppendLine("    defaultPrim = \"RoomTemplateRoot\"");
            builder.AppendLine("    metersPerUnit = 1");
            builder.AppendLine(")");
            builder.AppendLine();
            builder.AppendLine("def Xform \"RoomTemplateRoot\"");
            builder.AppendLine("{");
            AppendGroup(builder, "FloorRegions");
            AppendGroup(builder, "DoorAnchors");
            AppendGroup(builder, "SpawnAnchors");
            AppendGroup(builder, "Obstacles");
            AppendGroup(builder, "OriginMarkers");
            AppendGroup(builder, "Architecture");
            AppendGroup(builder, "Decor");

            foreach (var cell in project.cells)
            {
                if (cell.kind == RoomDesignerCellKinds.Ground)
                {
                    AppendCube(builder, "FloorRegions", $"tileGround_{cell.x}_{cell.z}", cell.x, -0.5f, cell.z, 1f, 1f, 1f);
                }
                else if (cell.kind == RoomDesignerCellKinds.Hole)
                {
                    AppendCube(builder, "FloorRegions", $"tileHole_{cell.x}_{cell.z}", cell.x, -0.02f, cell.z, 0.86f, 0.04f, 0.86f);
                }
                else if (cell.kind == RoomDesignerCellKinds.Rock)
                {
                    AppendCube(builder, "Obstacles", $"rockTile_{cell.x}_{cell.z}_{cell.layer}", cell.x, cell.layer + 0.5f, cell.z, 1f, 1f, 1f);
                }
            }

            foreach (var door in project.doorPorts)
            {
                AppendCube(builder, "DoorAnchors", $"doorAnchor_{door.direction}_{door.laneIndex}_{door.state}", door.x, 0.65f, door.z, door.direction is "east" or "west" ? 0.18f : 1f, 1.3f, door.direction is "east" or "west" ? 1f : 0.18f);
            }

            foreach (var marker in project.markers)
            {
                AppendSphere(builder, "SpawnAnchors", $"{marker.kind}_{marker.id}", marker.x, marker.y + 0.16f, marker.z, 0.32f);
            }

            AppendCube(builder, "OriginMarkers", "origin_0_0", 0f, 0.02f, 0f, 0.28f, 0.04f, 0.28f);
            builder.AppendLine("}");
            return builder.ToString();
        }

        private static void AppendGroup(StringBuilder builder, string name)
        {
            builder.AppendLine($"    def Xform \"{name}\"");
            builder.AppendLine("    {");
            builder.AppendLine("    }");
        }

        private static void AppendCube(StringBuilder builder, string parent, string name, float x, float y, float z, float sx, float sy, float sz)
        {
            builder.AppendLine($"    def Cube \"{parent}_{Sanitize(name)}\"");
            builder.AppendLine("    {");
            builder.AppendLine($"        matrix4d xformOp:transform = ( ({F(sx)}, 0, 0, 0), (0, {F(sy)}, 0, 0), (0, 0, {F(sz)}, 0), ({F(x)}, {F(y)}, {F(z)}, 1) )");
            builder.AppendLine("        uniform token[] xformOpOrder = [\"xformOp:transform\"]");
            builder.AppendLine("    }");
        }

        private static void AppendSphere(StringBuilder builder, string parent, string name, float x, float y, float z, float scale)
        {
            builder.AppendLine($"    def Sphere \"{parent}_{Sanitize(name)}\"");
            builder.AppendLine("    {");
            builder.AppendLine($"        double radius = {F(scale * 0.5f)}");
            builder.AppendLine($"        double3 xformOp:translate = ({F(x)}, {F(y)}, {F(z)})");
            builder.AppendLine("        uniform token[] xformOpOrder = [\"xformOp:translate\"]");
            builder.AppendLine("    }");
        }

        private static string Sanitize(string value)
        {
            var builder = new StringBuilder();
            foreach (var character in value ?? string.Empty)
            {
                builder.Append(char.IsLetterOrDigit(character) || character == '_' ? character : '_');
            }

            return builder.Length == 0 ? "Entity" : builder.ToString();
        }

        private static string F(float value)
        {
            return value.ToString("0.###", CultureInfo.InvariantCulture);
        }
    }
}
