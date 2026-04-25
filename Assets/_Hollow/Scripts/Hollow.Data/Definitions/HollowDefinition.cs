using Hollow.Core;
using UnityEngine;

namespace Hollow.Data.Definitions
{
    public abstract class HollowDefinition : ScriptableObject
    {
        [SerializeField] private string id;
        [SerializeField] private string displayName;
        [SerializeField, TextArea] private string notes;

        public HollowId Id => new(id);

        public string DisplayName => displayName;

        public string Notes => notes;
    }
}
