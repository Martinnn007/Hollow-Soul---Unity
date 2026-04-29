using System;
using System.Linq;

namespace Hollow.Data.Definitions
{
    [Serializable]
    public sealed class ArtPassProductionStatusReport
    {
        public string generatedAtUtc = string.Empty;
        public string catalogPath = string.Empty;
        public string artPassRoot = string.Empty;
        public int totalTargets;
        public int productionReadyCount;
        public int prototypeFallbackCount;
        public int missingBindingCount;
        public int unsafePrefabCount;
        public int warningCount;
        public int errorCount;
        public ArtPassProductionTargetRecord[] targets = Array.Empty<ArtPassProductionTargetRecord>();

        public bool HasBlockingFailures => missingBindingCount > 0 || unsafePrefabCount > 0 || errorCount > 0;

        public void Recalculate()
        {
            targets ??= Array.Empty<ArtPassProductionTargetRecord>();
            totalTargets = targets.Length;
            productionReadyCount = targets.Count(target => target.status == ArtPassProductionStatus.ProductionReady);
            prototypeFallbackCount = targets.Count(target => target.status == ArtPassProductionStatus.PrototypeFallback);
            missingBindingCount = targets.Count(target => target.status == ArtPassProductionStatus.MissingBinding);
            unsafePrefabCount = targets.Count(target => target.status == ArtPassProductionStatus.UnsafePrefab);
            warningCount = targets.Sum(target => target.warnings?.Length ?? 0);
            errorCount = targets.Sum(target => target.errors?.Length ?? 0);
        }
    }
}
