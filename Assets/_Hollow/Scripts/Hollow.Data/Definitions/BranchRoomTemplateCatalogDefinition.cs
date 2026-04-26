using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Hollow.Data.Definitions
{
    [CreateAssetMenu(menuName = "Hollow/Branches/Room Template Catalog", fileName = "BranchRoomTemplateCatalog")]
    public sealed class BranchRoomTemplateCatalogDefinition : HollowDefinition
    {
        [SerializeField] private TextAsset single1x1;
        [SerializeField] private TextAsset wide2x1;
        [SerializeField] private TextAsset tall1x2;
        [SerializeField] private TextAsset block2x2;
        [SerializeField] private TextAsset l3Cell;
        [SerializeField] private List<TextAsset> additionalTemplates = new();
        [SerializeField] private int defaultSeed = 14001;

        public TextAsset Single1x1 => single1x1;

        public TextAsset Wide2x1 => wide2x1;

        public TextAsset Tall1x2 => tall1x2;

        public TextAsset Block2x2 => block2x2;

        public TextAsset L3Cell => l3Cell;

        public int DefaultSeed => defaultSeed;

        public TextAsset[] FixtureTemplates => new[] { single1x1, wide2x1, tall1x2, block2x2, l3Cell };

        public TextAsset[] AdditionalTemplates => additionalTemplates?.Where(template => template != null).ToArray() ?? System.Array.Empty<TextAsset>();

        public TextAsset[] AllTemplates => FixtureTemplates.Concat(AdditionalTemplates).ToArray();

        public void Configure(
            TextAsset nextSingle1x1,
            TextAsset nextWide2x1,
            TextAsset nextTall1x2,
            TextAsset nextBlock2x2,
            TextAsset nextL3Cell,
            int nextDefaultSeed)
        {
            Configure(nextSingle1x1, nextWide2x1, nextTall1x2, nextBlock2x2, nextL3Cell, nextDefaultSeed, System.Array.Empty<TextAsset>());
        }

        public void Configure(
            TextAsset nextSingle1x1,
            TextAsset nextWide2x1,
            TextAsset nextTall1x2,
            TextAsset nextBlock2x2,
            TextAsset nextL3Cell,
            int nextDefaultSeed,
            IEnumerable<TextAsset> nextAdditionalTemplates)
        {
            single1x1 = nextSingle1x1;
            wide2x1 = nextWide2x1;
            tall1x2 = nextTall1x2;
            block2x2 = nextBlock2x2;
            l3Cell = nextL3Cell;
            defaultSeed = nextDefaultSeed;
            additionalTemplates = nextAdditionalTemplates?.Where(template => template != null).Distinct().ToList() ?? new List<TextAsset>();
        }
    }
}
