using System.Collections.Generic;
using System.Linq;
using Hollow.Persistence;

namespace Hollow.Rewards
{
    public sealed class RunInventoryState
    {
        private readonly List<string> passiveItemIds = new();
        private readonly List<string> passiveCardIds = new();

        public IReadOnlyList<string> PassiveItemIds => passiveItemIds;

        public IReadOnlyList<string> PassiveCardIds => passiveCardIds;

        public void AddPassiveItem(string itemId)
        {
            AddUnique(passiveItemIds, itemId);
        }

        public void AddPassiveCard(string cardId)
        {
            AddUnique(passiveCardIds, cardId);
        }

        public RunInventoryStateSaveState ToSaveState()
        {
            return new RunInventoryStateSaveState
            {
                passiveItemIds = passiveItemIds.ToList(),
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

            foreach (var itemId in saveState.passiveItemIds ?? new List<string>())
            {
                state.AddPassiveItem(itemId);
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
    }
}
