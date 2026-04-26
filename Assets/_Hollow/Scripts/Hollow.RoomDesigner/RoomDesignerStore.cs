using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Hollow.Persistence;
using UnityEngine;

namespace Hollow.RoomDesigner
{
    public sealed class RoomDesignerStore
    {
        private const string RootFolder = "room_designer_drafts";
        private readonly string rootDirectory;

        public RoomDesignerStore()
            : this(Application.persistentDataPath)
        {
        }

        public RoomDesignerStore(string rootDirectory)
        {
            this.rootDirectory = string.IsNullOrWhiteSpace(rootDirectory)
                ? throw new ArgumentException("Designer root directory cannot be empty.", nameof(rootDirectory))
                : rootDirectory;
        }

        public IReadOnlyList<RoomDesignerProject> LoadDrafts(ProfileSlotId slotId)
        {
            var drafts = LoadExistingDrafts(slotId);
            if (drafts.Count > 0)
            {
                return drafts;
            }

            var defaultDraft = RoomDesignerProject.CreateDefault();
            SaveDraft(slotId, defaultDraft);
            return new[] { defaultDraft };
        }

        public IReadOnlyList<RoomDesignerProject> LoadExistingDrafts(ProfileSlotId slotId)
        {
            var slotDirectory = SlotDirectory(slotId);
            Directory.CreateDirectory(slotDirectory);
            return Directory.GetFiles(slotDirectory, "*.roomdesigner.json")
                .Select(ReadProject)
                .Where(project => project != null)
                .OrderByDescending(project => project.updatedAtUtcTicks)
                .ToList();
        }

        public RoomDesignerProject SaveDraft(ProfileSlotId slotId, RoomDesignerProject project)
        {
            if (project == null)
            {
                throw new ArgumentNullException(nameof(project));
            }

            if (string.IsNullOrWhiteSpace(project.projectId))
            {
                project.projectId = Guid.NewGuid().ToString("N");
            }

            project.updatedAtUtcTicks = DateTime.UtcNow.Ticks;
            Directory.CreateDirectory(SlotDirectory(slotId));
            File.WriteAllText(ProjectPath(slotId, project.projectId), JsonUtility.ToJson(project, prettyPrint: true));
            return project;
        }

        public RoomDesignerProject DuplicateDraft(ProfileSlotId slotId, string projectId)
        {
            var source = LoadDrafts(slotId).FirstOrDefault(project => project.projectId == projectId)
                ?? throw new FileNotFoundException($"Room designer draft '{projectId}' was not found.");
            var duplicate = source.CloneAsDuplicate();
            SaveDraft(slotId, duplicate);
            return duplicate;
        }

        public void DeleteDraft(ProfileSlotId slotId, string projectId)
        {
            var path = ProjectPath(slotId, projectId);
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }

        private string SlotDirectory(ProfileSlotId slotId)
        {
            return Path.Combine(rootDirectory, RootFolder, $"slot_{slotId.Value}");
        }

        private string ProjectPath(ProfileSlotId slotId, string projectId)
        {
            return Path.Combine(SlotDirectory(slotId), $"{Sanitize(projectId)}.roomdesigner.json");
        }

        private static RoomDesignerProject ReadProject(string path)
        {
            try
            {
                return JsonUtility.FromJson<RoomDesignerProject>(File.ReadAllText(path));
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"Could not read room designer draft '{path}': {exception.Message}");
                return null;
            }
        }

        private static string Sanitize(string value)
        {
            var sanitized = new string((value ?? string.Empty).Where(char.IsLetterOrDigit).ToArray());
            return string.IsNullOrWhiteSpace(sanitized) ? Guid.NewGuid().ToString("N") : sanitized;
        }
    }
}
