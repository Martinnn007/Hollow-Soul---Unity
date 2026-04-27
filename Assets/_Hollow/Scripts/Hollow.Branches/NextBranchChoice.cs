using System;
using Hollow.Persistence;

namespace Hollow.Branches
{
    public sealed class NextBranchChoice
    {
        public NextBranchChoice(string choiceId, string displayName, int seed, int index)
            : this(choiceId, displayName, seed, index, 1, index, HubPortalKind.Branch, HubBranchPortalState.Open)
        {
        }

        public NextBranchChoice(
            string choiceId,
            string displayName,
            int seed,
            int index,
            int worldIndex,
            int slotIndex,
            HubPortalKind kind,
            HubBranchPortalState state)
        {
            ChoiceId = choiceId ?? string.Empty;
            DisplayName = displayName ?? string.Empty;
            Seed = seed == 0 ? 1 : seed;
            Index = Math.Max(0, index);
            WorldIndex = Math.Max(1, worldIndex);
            SlotIndex = Math.Max(0, slotIndex);
            Kind = kind;
            State = state;
        }

        public string ChoiceId { get; }

        public string DisplayName { get; }

        public int Seed { get; }

        public int Index { get; }

        public int WorldIndex { get; }

        public int SlotIndex { get; }

        public HubPortalKind Kind { get; }

        public HubBranchPortalState State { get; }

        public bool IsInteractable => Kind != HubPortalKind.Branch || State == HubBranchPortalState.Open;

        public NextBranchChoiceSaveState ToSaveState()
        {
            return new NextBranchChoiceSaveState
            {
                choiceId = ChoiceId,
                displayName = DisplayName,
                seed = Seed,
                index = Index,
                worldIndex = WorldIndex,
                slotIndex = SlotIndex,
                kind = Kind.ToString(),
                state = State.ToString()
            };
        }

        public static NextBranchChoice FromSaveState(NextBranchChoiceSaveState save)
        {
            if (save == null)
            {
                return null;
            }

            var kind = Enum.TryParse(save.kind, out HubPortalKind parsedKind) ? parsedKind : HubPortalKind.Branch;
            var state = Enum.TryParse(save.state, out HubBranchPortalState parsedState) ? parsedState : HubBranchPortalState.Open;
            var worldIndex = save.worldIndex <= 0 ? 1 : save.worldIndex;
            var slotIndex = save.slotIndex <= 0 ? save.index : save.slotIndex;
            return new NextBranchChoice(save.choiceId, save.displayName, save.seed, save.index, worldIndex, slotIndex, kind, state);
        }

        public static NextBranchChoice Create(int currentSeed, int branchDepth, int index)
        {
            var seed = StableHash($"{currentSeed}|{branchDepth}|next|{index}");
            return new NextBranchChoice($"next_{index}", $"Branch {branchDepth + 2}.{index + 1}", seed, index);
        }

        public static NextBranchChoice CreateWorldBranch(int runSeed, int worldIndex, int slotIndex, HubBranchPortalState state)
        {
            var seed = RunSeedDeriver.BranchSlotSeed(runSeed, worldIndex, slotIndex);
            return new NextBranchChoice(
                $"world_{Math.Max(1, worldIndex)}_branch_{Math.Max(0, slotIndex)}",
                $"World {Math.Max(1, worldIndex)} Branch {Math.Max(0, slotIndex) + 1}",
                seed,
                slotIndex,
                worldIndex,
                slotIndex,
                HubPortalKind.Branch,
                state);
        }

        public static NextBranchChoice CreateNextWorld(int runSeed, int worldIndex)
        {
            var nextWorld = Math.Max(1, worldIndex) + 1;
            return new NextBranchChoice(
                $"world_{Math.Max(1, worldIndex)}_next_world",
                $"Descend to World {nextWorld}",
                RunSeedDeriver.PrologueBranchSeed(runSeed, nextWorld),
                3,
                nextWorld,
                3,
                HubPortalKind.NextWorld,
                HubBranchPortalState.Open);
        }

        public static NextBranchChoice CreateFinalExtraction(int runSeed, int worldIndex)
        {
            return new NextBranchChoice(
                $"world_{Math.Max(1, worldIndex)}_final_extraction",
                "Final Extraction",
                RunSeedDeriver.ShopSeed(runSeed, worldIndex, 99),
                3,
                Math.Max(1, worldIndex),
                3,
                HubPortalKind.FinalExtraction,
                HubBranchPortalState.Open);
        }

        public NextBranchChoice WithState(HubBranchPortalState state)
        {
            return new NextBranchChoice(ChoiceId, DisplayName, Seed, Index, WorldIndex, SlotIndex, Kind, state);
        }

        private static int StableHash(string value)
        {
            unchecked
            {
                var hash = 2166136261u;
                foreach (var character in value ?? string.Empty)
                {
                    hash ^= character;
                    hash *= 16777619u;
                }

                return (int)(hash & 0x7fffffff);
            }
        }
    }
}
