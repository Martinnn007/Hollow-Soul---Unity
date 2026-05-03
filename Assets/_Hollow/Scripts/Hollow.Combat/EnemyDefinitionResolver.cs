namespace Hollow.Combat
{
    public static class EnemyDefinitionResolver
    {
        public static EnemyDefinition Resolve(EnemyCatalog catalog, string spawnKind, out bool usedFallback)
        {
            var resolvedCatalog = catalog != null ? catalog : EnemyCatalog.CreateRuntimeDefault();
            var fallback = resolvedCatalog.FallbackDefinition != null
                ? resolvedCatalog.FallbackDefinition
                : EnemyDefinition.CreateRuntimeNormal();
            var resolved = resolvedCatalog.Resolve(spawnKind);
            usedFallback = resolved == null || resolved.SpawnKind != spawnKind;
            return resolved != null ? resolved : fallback;
        }
    }
}
