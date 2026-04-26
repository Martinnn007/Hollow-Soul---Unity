using System.Collections.Generic;

namespace Hollow.Combat
{
    public sealed class EnemySpawnResult
    {
        public EnemySpawnResult(IReadOnlyList<EnemyRuntimeController> enemies, IReadOnlyList<string> warnings)
        {
            Enemies = enemies;
            Warnings = warnings;
        }

        public IReadOnlyList<EnemyRuntimeController> Enemies { get; }

        public IReadOnlyList<string> Warnings { get; }
    }
}
