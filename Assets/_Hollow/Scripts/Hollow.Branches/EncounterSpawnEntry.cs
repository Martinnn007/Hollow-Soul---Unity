using System;

namespace Hollow.Branches
{
    [Serializable]
    public sealed class EncounterSpawnEntry
    {
        public string spawnKind = "spawnEnemyNormal";
        public int count = 1;

        public EncounterSpawnEntry()
        {
        }

        public EncounterSpawnEntry(string nextSpawnKind, int nextCount)
        {
            spawnKind = string.IsNullOrWhiteSpace(nextSpawnKind) ? "spawnEnemyNormal" : nextSpawnKind;
            count = Math.Max(1, nextCount);
        }
    }
}
