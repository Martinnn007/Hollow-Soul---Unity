namespace Hollow.Combat
{
    public static class EnemyDefinitionResolver
    {
        public static EnemyDefinition Resolve(EnemyCatalog catalog, string spawnKind, out bool usedFallback)
        {
            var fallback = catalog != null ? catalog.FallbackDefinition : EnemyDefinition.CreateRuntimeNormal();
            var resolved = catalog != null ? catalog.Resolve(spawnKind) : fallback;
            usedFallback = resolved == null || resolved.SpawnKind != spawnKind;
            return resolved != null ? resolved : fallback;
        }
    }
}
