using System;
using System.Collections.Generic;
using System.Linq;
using Hollow.Persistence;

namespace Hollow.RoomDesigner
{
    public sealed class RoomDesignerDraftLibraryState
    {
        private readonly RoomDesignerStore store;
        private readonly ProfileSlotId slotId;
        private readonly List<RoomDesignerProject> drafts = new();

        public RoomDesignerDraftLibraryState(RoomDesignerStore store, ProfileSlotId slotId)
        {
            this.store = store ?? throw new ArgumentNullException(nameof(store));
            this.slotId = slotId;
            Reload();
        }

        public IReadOnlyList<RoomDesignerProject> Drafts => drafts;

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
            drafts.AddRange(store.LoadDrafts(slotId));
            SelectedDraft = !string.IsNullOrWhiteSpace(preferredProjectId)
                ? drafts.FirstOrDefault(draft => draft.projectId == preferredProjectId) ?? drafts.FirstOrDefault()
                : drafts.FirstOrDefault();
        }
    }
}
