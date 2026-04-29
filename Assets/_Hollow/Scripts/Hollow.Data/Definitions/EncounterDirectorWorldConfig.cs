using System;
using UnityEngine;

namespace Hollow.Data.Definitions
{
    [Serializable]
    public sealed class EncounterDirectorWorldConfig
    {
        [SerializeField] private int worldIndex = 1;
        [SerializeField] private int targetRoomCount = 8;
        [SerializeField] private int difficultyOffset;
        [SerializeField] private int hardEncounterWeightBonus;
        [SerializeField] private int veryHardEncounterWeightBonus;

        public int WorldIndex => Mathf.Max(1, worldIndex);
        public int TargetRoomCount => Mathf.Max(2, targetRoomCount);
        public int DifficultyOffset => Mathf.Max(0, difficultyOffset);
        public int HardEncounterWeightBonus => Mathf.Max(0, hardEncounterWeightBonus);
        public int VeryHardEncounterWeightBonus => Mathf.Max(0, veryHardEncounterWeightBonus);

        public void Configure(int nextWorldIndex, int nextTargetRoomCount, int nextDifficultyOffset, int nextHardWeightBonus, int nextVeryHardWeightBonus)
        {
            worldIndex = Mathf.Max(1, nextWorldIndex);
            targetRoomCount = Mathf.Max(2, nextTargetRoomCount);
            difficultyOffset = Mathf.Max(0, nextDifficultyOffset);
            hardEncounterWeightBonus = Mathf.Max(0, nextHardWeightBonus);
            veryHardEncounterWeightBonus = Mathf.Max(0, nextVeryHardWeightBonus);
        }
    }
}
