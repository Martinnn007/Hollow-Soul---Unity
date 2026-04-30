using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Hollow.Combat
{
    public sealed class BossCatalogDefinition : ScriptableObject
    {
        public const string DefaultCatalogId = "m53_boss_catalog_v1";

        [SerializeField] private string catalogId = DefaultCatalogId;
        [SerializeField] private List<BossDefinition> bosses = new();
        [SerializeField] private BossDefinition fallbackBoss;

        public string CatalogId => string.IsNullOrWhiteSpace(catalogId) ? DefaultCatalogId : catalogId;

        public IReadOnlyList<BossDefinition> Bosses => bosses;

        public BossDefinition FallbackBoss => fallbackBoss != null ? fallbackBoss : bosses.FirstOrDefault();

        public void Configure(string nextCatalogId, IEnumerable<BossDefinition> nextBosses, BossDefinition nextFallbackBoss)
        {
            catalogId = string.IsNullOrWhiteSpace(nextCatalogId) ? DefaultCatalogId : nextCatalogId;
            bosses = nextBosses?.Where(boss => boss != null).ToList() ?? new List<BossDefinition>();
            fallbackBoss = nextFallbackBoss != null ? nextFallbackBoss : bosses.FirstOrDefault();
        }

        public bool TryGetBoss(string bossId, out BossDefinition boss)
        {
            boss = bosses.FirstOrDefault(candidate => candidate != null && candidate.BossId == bossId);
            return boss != null;
        }

        public IReadOnlyList<BossDefinition> BossesForBand(BossWorldBand band)
        {
            return bosses.Where(boss => boss != null && boss.WorldBand == band).OrderBy(boss => boss.BossId).ToArray();
        }

        public static BossCatalogDefinition CreateRuntimeDefault()
        {
            var roster = CreateRuntimeRoster();
            var catalog = CreateInstance<BossCatalogDefinition>();
            catalog.Configure(DefaultCatalogId, roster, roster.FirstOrDefault(boss => boss.BossId == "stone_warden"));
            return catalog;
        }

        public static BossDefinition[] CreateRuntimeRoster()
        {
            return new[]
            {
                BossDefinition.CreateRuntime("stone_warden", "Stone Warden", BossWorldBand.World1, BossBehaviorId.StoneWarden, 24, "boss_arena_broken_gateyard", "Broken Gateyard", 0.82f, 0.68f, 2.0f, new Color(0.42f, 0.34f, 0.28f, 1f)),
                BossDefinition.CreateRuntime("splinter_saint", "Splinter Saint", BossWorldBand.World1, BossBehaviorId.SplinterSaint, 22, "boss_arena_narrow_shrine", "Narrow Shrine", 1.2f, 0.58f, 1.8f, new Color(0.64f, 0.42f, 0.22f, 1f)),
                BossDefinition.CreateRuntime("gravel_maw", "Gravel Maw", BossWorldBand.World1, BossBehaviorId.GravelMaw, 28, "boss_arena_sandy_pit_ring", "Sandy Pit-Ring", 1.75f, 0.52f, 2.25f, new Color(0.78f, 0.64f, 0.36f, 1f)),
                BossDefinition.CreateRuntime("cartouche_widow", "Cartouche Widow", BossWorldBand.World2, BossBehaviorId.CartoucheWidow, 32, "boss_arena_open_tomb", "Open Tomb", 1.0f, 0.66f, 2.25f, new Color(0.86f, 0.66f, 0.18f, 1f)),
                BossDefinition.CreateRuntime("iron_reliquary", "Iron Reliquary", BossWorldBand.World2, BossBehaviorId.IronReliquary, 36, "boss_arena_cover_maze", "Cover Maze", 0.95f, 0.72f, 2.4f, new Color(0.44f, 0.52f, 0.58f, 1f)),
                BossDefinition.CreateRuntime("mirror_husk", "Mirror Husk", BossWorldBand.World2, BossBehaviorId.MirrorHusk, 34, "boss_arena_symmetric_mirror", "Symmetric Mirror", 1.15f, 0.62f, 2.1f, new Color(0.62f, 0.74f, 0.9f, 1f)),
                BossDefinition.CreateRuntime("ash_comet", "Ash Comet", BossWorldBand.World2, BossBehaviorId.AshComet, 38, "boss_arena_charred_crossing", "Charred Crossing", 1.35f, 0.7f, 2.45f, new Color(0.95f, 0.32f, 0.12f, 1f)),
                BossDefinition.CreateRuntime("choir_of_teeth", "Choir of Teeth", BossWorldBand.World3, BossBehaviorId.ChoirOfTeeth, 42, "boss_arena_hell_heaven_dais", "Hell/Heaven Split Dais", 0.8f, 0.82f, 2.7f, new Color(0.92f, 0.86f, 0.72f, 1f)),
                BossDefinition.CreateRuntime("rust_bishop", "Rust Bishop", BossWorldBand.World3, BossBehaviorId.RustBishop, 46, "boss_arena_industrial_cover_grid", "Industrial Cover Grid", 0.95f, 0.8f, 2.85f, new Color(0.72f, 0.28f, 0.14f, 1f)),
                BossDefinition.CreateRuntime("hollow_star_larva", "Hollow Star Larva", BossWorldBand.World3, BossBehaviorId.HollowStarLarva, 50, "boss_arena_blind_deep", "Blind Deep", 1.15f, 0.86f, 3.0f, new Color(0.18f, 0.12f, 0.34f, 1f))
            };
        }
    }
}
