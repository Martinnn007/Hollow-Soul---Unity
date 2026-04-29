using System;

namespace Hollow.Data.Definitions
{
    [Serializable]
    public sealed class ArtPassProductionTargetRecord
    {
        public string role = string.Empty;
        public string displayName = string.Empty;
        public string group = string.Empty;
        public string prefabPath = string.Empty;
        public ArtPassProductionStatus status = ArtPassProductionStatus.PrototypeFallback;
        public bool corePriority;
        public bool sceneModePreviewRole;
        public string[] warnings = Array.Empty<string>();
        public string[] errors = Array.Empty<string>();
    }
}
