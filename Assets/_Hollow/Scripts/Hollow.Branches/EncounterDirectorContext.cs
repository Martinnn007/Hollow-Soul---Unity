using Hollow.Data.Definitions;

namespace Hollow.Branches
{
    public sealed class EncounterDirectorContext
    {
        public EncounterDirectorContext(BranchFloorGraph graph, int seed, int worldIndex, EncounterDirectorProfileDefinition profile)
        {
            Graph = graph;
            Seed = seed == 0 ? graph?.Seed ?? 0 : seed;
            WorldIndex = worldIndex <= 0 ? 1 : worldIndex;
            Profile = EncounterDirectorProfileDefinition.Resolve(profile);
            WorldConfig = Profile.WorldConfigFor(WorldIndex);
        }

        public BranchFloorGraph Graph { get; }
        public int Seed { get; }
        public int WorldIndex { get; }
        public EncounterDirectorProfileDefinition Profile { get; }
        public EncounterDirectorWorldConfig WorldConfig { get; }
    }
}
