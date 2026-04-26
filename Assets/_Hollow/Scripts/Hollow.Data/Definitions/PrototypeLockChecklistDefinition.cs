using System;
using UnityEngine;

namespace Hollow.Data.Definitions
{
    [CreateAssetMenu(menuName = "Hollow/Prototype Lock Checklist", fileName = "PrototypeLockChecklist")]
    public sealed class PrototypeLockChecklistDefinition : HollowDefinition
    {
        [SerializeField] private PrototypeLockChecklistItem[] items = Array.Empty<PrototypeLockChecklistItem>();

        public PrototypeLockChecklistItem[] Items => items;

        public bool RequiredItemsSatisfied
        {
            get
            {
                foreach (var item in items)
                {
                    if (item.Required && item.Status != PrototypeLockStatus.Passed)
                    {
                        return false;
                    }
                }

                return items.Length > 0;
            }
        }

        public void Configure(PrototypeLockChecklistItem[] nextItems)
        {
            items = nextItems ?? Array.Empty<PrototypeLockChecklistItem>();
        }
    }

    [Serializable]
    public struct PrototypeLockChecklistItem
    {
        [SerializeField] private string id;
        [SerializeField] private string title;
        [SerializeField] private string group;
        [SerializeField] private bool required;
        [SerializeField] private PrototypeLockStatus status;
        [SerializeField, TextArea] private string notes;

        public PrototypeLockChecklistItem(string id, string title, string group, bool required, PrototypeLockStatus status, string notes)
        {
            this.id = id ?? string.Empty;
            this.title = title ?? string.Empty;
            this.group = group ?? string.Empty;
            this.required = required;
            this.status = status;
            this.notes = notes ?? string.Empty;
        }

        public string Id => id;

        public string Title => title;

        public string Group => group;

        public bool Required => required;

        public PrototypeLockStatus Status => status;

        public string Notes => notes;
    }
}
