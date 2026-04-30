using UnityEngine;
using Hollow.Entities;
using Hollow.Rooms;

namespace Hollow.Combat
{
    public sealed class BossLabController : MonoBehaviour
    {
        [SerializeField] private BossCatalogDefinition bossCatalog;
        [SerializeField] private string selectedBossId = "stone_warden";
        [SerializeField] private RoomCombatController roomCombatController;

        public string SelectedBossId => string.IsNullOrWhiteSpace(selectedBossId) ? "stone_warden" : selectedBossId;

        public void Configure(BossCatalogDefinition nextCatalog, string bossId)
        {
            bossCatalog = nextCatalog;
            selectedBossId = string.IsNullOrWhiteSpace(bossId) ? "stone_warden" : bossId;
        }

        public bool TryLaunchSelectedBoss()
        {
            roomCombatController = roomCombatController != null ? roomCombatController : FindFirstObjectByType<RoomCombatController>();
            if (roomCombatController == null)
            {
                return false;
            }

            var catalog = bossCatalog != null ? bossCatalog : BossCatalogDefinition.CreateRuntimeDefault();
            roomCombatController.ConfigureBossCatalog(catalog);
            return catalog.TryGetBoss(SelectedBossId, out _);
        }

        public bool TryLaunchSelectedBoss(RoomRuntimeRoot room, PlaceholderPlayerController player)
        {
            roomCombatController = roomCombatController != null ? roomCombatController : FindFirstObjectByType<RoomCombatController>();
            if (roomCombatController == null || room == null || player == null)
            {
                return false;
            }

            var catalog = bossCatalog != null ? bossCatalog : BossCatalogDefinition.CreateRuntimeDefault();
            if (!catalog.TryGetBoss(SelectedBossId, out var boss))
            {
                return false;
            }

            roomCombatController.ConfigureBossCatalog(catalog);
            roomCombatController.BeginRoom(
                room,
                player,
                alreadyCleared: false,
                RoomCombatEncounterKind.Boss,
                new RoomCombatEncounterContext("boss_lab", new[] { "spawnEnemyBoss" }, (int)boss.WorldBand, 0, 0, boss.BossId, boss.Arena.arenaId, (int)boss.WorldBand, string.Empty));
            return true;
        }
    }
}
