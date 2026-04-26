using System;
using Hollow.Persistence;

namespace Hollow.Branches
{
    public sealed class NextBranchChoice
    {
        public NextBranchChoice(string choiceId, string displayName, int seed, int index)
        {
            ChoiceId = choiceId ?? string.Empty;
            DisplayName = displayName ?? string.Empty;
            Seed = seed == 0 ? 1 : seed;
            Index = Math.Max(0, index);
        }

        public string ChoiceId { get; }

        public string DisplayName { get; }

        public int Seed { get; }

        public int Index { get; }

        public NextBranchChoiceSaveState ToSaveState()
        {
            return new NextBranchChoiceSaveState
            {
                choiceId = ChoiceId,
                displayName = DisplayName,
                seed = Seed,
                index = Index
            };
        }

        public static NextBranchChoice FromSaveState(NextBranchChoiceSaveState save)
        {
            return save == null
                ? null
                : new NextBranchChoice(save.choiceId, save.displayName, save.seed, save.index);
        }

        public static NextBranchChoice Create(int currentSeed, int branchDepth, int index)
        {
            var seed = StableHash($"{currentSeed}|{branchDepth}|next|{index}");
            return new NextBranchChoice($"next_{index}", $"Branch {branchDepth + 2}.{index + 1}", seed, index);
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
