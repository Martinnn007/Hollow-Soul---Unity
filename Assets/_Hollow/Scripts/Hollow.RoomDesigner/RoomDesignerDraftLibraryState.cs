using System;
using System.Collections.Generic;
using System.Linq;
using Hollow.Data.Definitions;
using Hollow.Persistence;
using UnityEngine;

namespace Hollow.RoomDesigner
{
    public sealed class RoomDesignerDraftLibraryState
    {
        private readonly RoomDesignerStore store;
        private readonly ProfileSlotId slotId;
        private readonly bool autoCreateDefaultDraft;
        private readonly RoomDesignerCuratedDraftCatalogDefinition curatedCatalog;
        private readonly List<RoomDesignerProject> drafts = new();
        private readonly List<RoomDesignerProject> curatedDrafts = new();

        public RoomDesignerDraftLibraryState(RoomDesignerStore store, ProfileSlotId slotId)
            : this(store, slotId, autoCreateDefaultDraft: true, null)
        {
        }

        public RoomDesignerDraftLibraryState(RoomDesignerStore store, ProfileSlotId slotId, bool autoCreateDefaultDraft)
            : this(store, slotId, autoCreateDefaultDraft, null)
        {
        }

        public RoomDesignerDraftLibraryState(
            RoomDesignerStore store,
            ProfileSlotId slotId,
            bool autoCreateDefaultDraft,
            RoomDesignerCuratedDraftCatalogDefinition curatedCatalog)
        {
            this.store = store ?? throw new ArgumentNullException(nameof(store));
            this.slotId = slotId;
            this.autoCreateDefaultDraft = autoCreateDefaultDraft;
            this.curatedCatalog = curatedCatalog;
            Reload();
        }

        public IReadOnlyList<RoomDesignerProject> Drafts => drafts;

        public IReadOnlyList<RoomDesignerProject> CuratedDrafts => curatedDrafts;

        public RoomDesignerProject SelectedDraft { get; private set; }

        public string LatestMessage { get; private set; } = "Draft library ready";

        public RoomDesignerProject CreateDraft(RoomDesignerFootprintPreset preset, string displayName = null)
        {
            var project = RoomDesignerProject.CreateDefault(preset, displayName);
            store.SaveDraft(slotId, project);
            Reload(project.projectId);
            LatestMessage = $"Created {RoomDesignerFootprintUtility.DisplayName(preset)}";
            return SelectedDraft;
        }

        public RoomDesignerProject OpenDraft(string projectId)
        {
            SelectedDraft = drafts.FirstOrDefault(draft => draft.projectId == projectId) ?? drafts.FirstOrDefault();
            LatestMessage = SelectedDraft != null ? $"Opened {SelectedDraft.displayName}" : "No drafts available";
            return SelectedDraft;
        }

        public RoomDesignerProject OpenCuratedAsEditableCopy(string projectId)
        {
            var curated = curatedDrafts.FirstOrDefault(draft => draft.projectId == projectId)
                ?? throw new InvalidOperationException($"Curated Room Designer draft '{projectId}' was not found.");
            var copy = CloneForEditing(curated);
            store.SaveDraft(slotId, copy);
            Reload(copy.projectId);
            LatestMessage = $"Created edit copy of {curated.displayName}";
            return SelectedDraft;
        }

        public RoomDesignerProject DuplicateDraft(string projectId)
        {
            var duplicate = store.DuplicateDraft(slotId, projectId);
            Reload(duplicate.projectId);
            LatestMessage = $"Duplicated {duplicate.displayName}";
            return SelectedDraft;
        }

        public RoomDesignerProject DeleteDraft(string projectId)
        {
            store.DeleteDraft(slotId, projectId);
            Reload();
            LatestMessage = "Deleted draft";
            return SelectedDraft;
        }

        public void Reload(string preferredProjectId = null)
        {
            drafts.Clear();
            curatedDrafts.Clear();
            curatedDrafts.AddRange(LoadCuratedDrafts(curatedCatalog));
            drafts.AddRange(autoCreateDefaultDraft ? store.LoadDrafts(slotId) : store.LoadExistingDrafts(slotId));
            SelectedDraft = !string.IsNullOrWhiteSpace(preferredProjectId)
                ? drafts.FirstOrDefault(draft => draft.projectId == preferredProjectId) ?? drafts.FirstOrDefault()
                : drafts.FirstOrDefault();
        }

        private static IEnumerable<RoomDesignerProject> LoadCuratedDrafts(RoomDesignerCuratedDraftCatalogDefinition catalog)
        {
            if (catalog == null)
            {
                return Enumerable.Empty<RoomDesignerProject>();
            }

            var projects = new List<RoomDesignerProject>();
            foreach (var textAsset in catalog.CuratedDrafts)
            {
                if (textAsset == null)
                {
                    continue;
                }

                RoomDesignerProject project = null;
                try
                {
                    project = JsonUtility.FromJson<RoomDesignerProject>(textAsset.text);
                }
                catch (Exception exception)
                {
                    Debug.LogWarning($"Could not read curated Room Designer draft '{textAsset.name}': {exception.Message}");
                }

                if (project != null)
                {
                    projects.Add(project);
                }
            }

            return projects
                .OrderBy(RoomDesignerCatalogGroups.SortOrder)
                .ThenBy(project => project.displayName, StringComparer.Ordinal);
        }

        private static RoomDesignerProject CloneForEditing(RoomDesignerProject source)
        {
            var copy = JsonUtility.FromJson<RoomDesignerProject>(JsonUtility.ToJson(source));
            copy.projectId = Guid.NewGuid().ToString("N");
            copy.displayName = $"{source.displayName} - Edit Copy";
            copy.createdAtUtcTicks = DateTime.UtcNow.Ticks;
            copy.updatedAtUtcTicks = copy.createdAtUtcTicks;
            return copy;
        }
    }

    public static class RoomDesignerCatalogGroups
    {
        public static string ForProject(RoomDesignerProject project)
        {
            var haystack = $"{project?.projectId} {project?.displayName}".ToLowerInvariant();
            if (haystack.Contains("boss"))
            {
                return "Boss Rooms";
            }

            if (haystack.Contains("secret"))
            {
                return "Secret Rooms";
            }

            if (haystack.Contains("shop") ||
                haystack.Contains("hub"))
            {
                return "Hub / Shop Rooms";
            }

            if (haystack.Contains("treasure") ||
                haystack.Contains("reward"))
            {
                return "Treasure Rooms";
            }

            if (haystack.Contains("combat") ||
                haystack.Contains("approved") ||
                haystack.Contains("enemy"))
            {
                return "Combat Rooms";
            }

            return "Other Rooms";
        }

        public static int SortOrder(RoomDesignerProject project)
        {
            return ForProject(project) switch
            {
                "Combat Rooms" => 0,
                "Treasure Rooms" => 1,
                "Boss Rooms" => 2,
                "Secret Rooms" => 3,
                "Hub / Shop Rooms" => 4,
                _ => 5
            };
        }
    }
}
