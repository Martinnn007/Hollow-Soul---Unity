using System;
using System.IO;
using UnityEngine;

namespace Hollow.RoomDesigner
{
    public static class RoomDesignerJsonExporter
    {
        public static string DefaultExportRoot => Path.Combine(Application.persistentDataPath, "room_designer_exports");

        public static string ExportProject(RoomDesignerProject project, string exportRoot = null)
        {
            if (project == null)
            {
                throw new ArgumentNullException(nameof(project));
            }

            var directory = ExportDirectory(project, exportRoot);
            Directory.CreateDirectory(directory);
            var path = Path.Combine(directory, "designerProject.json");
            File.WriteAllText(path, JsonUtility.ToJson(project, prettyPrint: true));
            return path;
        }

        public static string ExportRuntime(RoomDesignerProject project, string exportRoot = null)
        {
            if (project == null)
            {
                throw new ArgumentNullException(nameof(project));
            }

            var directory = ExportDirectory(project, exportRoot);
            Directory.CreateDirectory(directory);
            var path = Path.Combine(directory, "runtime.hollowruntime.json");
            File.WriteAllText(path, RoomDesignerCompiler.ExportRuntimeJson(project, prettyPrint: true));
            return path;
        }

        public static string ExportDirectory(RoomDesignerProject project, string exportRoot = null)
        {
            var root = string.IsNullOrWhiteSpace(exportRoot) ? DefaultExportRoot : exportRoot;
            var safeProjectId = string.IsNullOrWhiteSpace(project?.projectId) ? "draft" : project.projectId;
            return Path.Combine(root, safeProjectId);
        }
    }
}
