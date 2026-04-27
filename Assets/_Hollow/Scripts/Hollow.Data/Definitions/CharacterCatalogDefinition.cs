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
    }
}
