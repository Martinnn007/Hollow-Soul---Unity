using System.Collections.Generic;
using System.Linq;
using Hollow.Persistence;

namespace Hollow.Rewards
{
    public sealed class RunInventoryState
    {
        private readonly List<PassiveItemStack> passiveItemStacks = new();
        private readonly List<string> passiveCardIds = new();

        public IReadOnlyList<string> PassiveItemIds => passiveItemStacks.Select(stack => stack.ItemId).ToArray();

        public IReadOnlyList<PassiveItemStack> PassiveItemStacks => passiveItemStacks;

        public IReadOnlyList<string> PassiveCardIds => passiveCardIds;

        public void AddPassiveItem(string itemId)
        {
            AddPassiveItem(itemId, 1);
        }

        public void AddPassiveItem(string itemId, int maxStacks)
        {
            if (string.IsNullOrWhiteSpace(itemId))
            {
                return;
            }

            var safeMax = System.Math.Max(1, maxStacks);
            for (var index = 0; index < passiveItemStacks.Count; index++)
            {
                var stack = passiveItemStacks[index];
                if (stack.ItemId != itemId)
                {
                    continue;
                }

                passiveItemStacks[index] = new PassiveItemStack(itemId, System.Math.Min(safeMax, stack.Count + 1));
                return;
            }

            passiveItemStacks.Add(new PassiveItemStack(itemId, 1));
        }

        public int PassiveItemCount(string itemId)
        {
            return passiveItemStacks.FirstOrDefault(stack => stack.ItemId == itemId).Count;
        }

        public void AddPassiveCard(string cardId)
        {
            AddUnique(passiveCardIds, cardId);
        }

        public RunInventoryStateSaveState ToSaveState()
        {
            return new RunInventoryStateSaveState
            {
                passiveItemIds = passiveItemStacks.Select(stack => stack.ItemId).ToList(),
                passiveItemStacks = passiveItemStacks
                    .Select(stack => new PassiveItemStackSaveState { itemId = stack.ItemId, count = stack.Count })
                    .ToList(),
                passiveCardIds = passiveCardIds.ToList()
            };
        }

        public static RunInventoryState FromSaveState(RunInventoryStateSaveState saveState)
        {
            var state = new RunInventoryState();
            if (saveState == null)
            {
                return state;
            }

            if (saveState.passiveItemStacks != null && saveState.passiveItemStacks.Count > 0)
            {
                foreach (var stack in saveState.passiveItemStacks)
                {
                    if (stack == null || string.IsNullOrWhiteSpace(stack.itemId))
                    {
                        continue;
                    }

                    state.SetPassiveItemCount(stack.itemId, System.Math.Max(1, stack.count));
                }
            }

            foreach (var itemId in saveState.passiveItemIds ?? new List<string>())
            {
                if (!string.IsNullOrWhiteSpace(itemId) && state.PassiveItemCount(itemId) <= 0)
                {
                    state.SetPassiveItemCount(itemId, 1);
                }
            }

            foreach (var cardId in saveState.passiveCardIds ?? new List<string>())
            {
                state.AddPassiveCard(cardId);
            }

            return state;
        }

        private static void AddUnique(List<string> values, string value)
        {
            if (!string.IsNullOrWhiteSpace(value) && !values.Contains(value))
            {
                values.Add(value);
            }
        }

        private void SetPassiveItemCount(string itemId, int count)
        {
            if (string.IsNullOrWhiteSpace(itemId))
            {
                return;
            }

            if (ProjectilePassiveResolver.IsM54ProjectilePassive(itemId))
            {
                count = System.Math.Min(count, ProjectilePassiveResolver.MaxStacksForReward(itemId));
            }

            for (var index = 0; index < passiveItemStacks.Count; index++)
            {
                if (passiveItemStacks[index].ItemId == itemId)
                {
                    passiveItemStacks[index] = new PassiveItemStack(itemId, System.Math.Max(1, count));
                    return;
                }
            }

            passiveItemStacks.Add(new PassiveItemStack(itemId, System.Math.Max(1, count)));
        }
    }

    public readonly struct PassiveItemStack
    {
        public PassiveItemStack(string itemId, int count)
        {
            ItemId = itemId ?? string.Empty;
            Count = System.Math.Max(1, count);
        }

        public string ItemId { get; }

        public int Count { get; }
    }
}
