using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Hollow.Data.Definitions
{
    [CreateAssetMenu(menuName = "Hollow/Characters/Character Catalog")]
    public sealed class CharacterCatalogDefinition : ScriptableObject
    {
        [SerializeField] private string catalogId;
        [SerializeField] private List<CharacterDefinition> characters = new();

        public string CatalogId => catalogId;

        public IReadOnlyList<CharacterDefinition> Characters => characters;

        public void Configure(string nextCatalogId, IEnumerable<CharacterDefinition> nextCharacters)
        {
            catalogId = nextCatalogId ?? string.Empty;
            characters = (nextCharacters ?? Enumerable.Empty<CharacterDefinition>())
                .Where(character => character != null && !string.IsNullOrWhiteSpace(character.CharacterId))
                .GroupBy(character => character.CharacterId)
                .Select(group => group.First())
                .ToList();
        }

        public bool TryGetCharacter(string characterId, out CharacterDefinition character)
        {
            character = characters.FirstOrDefault(candidate => candidate != null && candidate.CharacterId == characterId);
            return character != null;
        }

        public CharacterDefinition Resolve(string characterId)
        {
            if (TryGetCharacter(characterId, out var requested))
            {
                return requested;
            }

            if (TryGetCharacter("balanced", out var balanced))
            {
                return balanced;
            }

            return characters.FirstOrDefault(character => character != null);
        }

        public static CharacterCatalogDefinition CreateRuntimeDefault()
        {
            var catalog = CreateInstance<CharacterCatalogDefinition>();
            catalog.Configure(
                "runtime_character_catalog_default",
                new[]
                {
                    CreateRuntimeCharacter(
                        "balanced",
                        "Balanced",
                        new PlayerBaseStats(
                            maxHealth: 3,
                            speedMetersPerSecond: 4f,
                            strength: 1,
                            maxStamina: 100f,
                            staminaRegenPerSecond: 18f,
                            defense: 0,
                            meleeDamageBonus: 0,
                            rangedDamageBonus: 0,
                            attackCooldownMultiplier: 1f)),
                    CreateRuntimeCharacter(
                        "heavy",
                        "Heavy",
                        new PlayerBaseStats(
                            maxHealth: 5,
                            speedMetersPerSecond: 3.15f,
                            strength: 2,
                            maxStamina: 130f,
                            staminaRegenPerSecond: 18f,
                            defense: 2,
                            meleeDamageBonus: 1,
                            rangedDamageBonus: 0,
                            attackCooldownMultiplier: 1f))
                });
            return catalog;
        }

        private static CharacterDefinition CreateRuntimeCharacter(string characterId, string displayName, PlayerBaseStats baseStats)
        {
            var character = CreateInstance<CharacterDefinition>();
            character.Configure(characterId, displayName, baseStats, "starter_blade", "starter_bow", null);
            return character;
        }
    }
}
