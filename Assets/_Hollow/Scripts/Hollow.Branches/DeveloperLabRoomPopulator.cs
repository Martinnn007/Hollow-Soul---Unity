using System;
using System.Collections.Generic;
using System.Linq;
using Hollow.Combat;
using Hollow.Data.Definitions;
using Hollow.Entities;
using Hollow.Presentation;
using Hollow.Rewards;
using Hollow.Rooms;
using UnityEngine;

namespace Hollow.Branches
{
    public static class DeveloperLabRoomPopulator
    {
        private static readonly string[] EnemySpawnKinds =
        {
            "spawnEnemyNormal",
            "spawnEnemyFlying",
            "spawnEnemyFast",
            "spawnEnemyHeavy",
            "spawnEnemyCharger",
            "spawnEnemyTurret",
            "spawnEnemySplitter"
        };

        private static readonly string[] BuildPickupLabels =
        {
            "starter_blade",
            "starter_bolt",
            "skeletal_sword",
            "bone_bow",
            "dragon_fang",
            "dragon_bow",
            "skeletal_armor",
            "dragon_scale_armor",
            "mending_charm",
            "echo_burst",
            "ember_card",
            "swift_card",
            "mend_card",
            "double_barrel",
            "triple_shot",
            "quad_shot",
            "power_up",
            "fire_rate_up"
        };

        public static void Populate(
            BranchRoomId roomId,
            RoomRuntimeRoot room,
            PlaceholderPlayerController player,
            RoomCombatController combat,
            EnemyCatalog enemyCatalog,
            BossCatalogDefinition bossCatalog,
            DifficultyTierDefinition difficultyTier)
        {
            if (room == null)
            {
                return;
            }

            var root = CreateRoot(room);
            var index = RoomIndex(roomId);
            AddTitle(root, TitleFor(index), room.LocalBounds);
            switch (index)
            {
                case 1:
                    PopulateEnvironment(root);
                    break;
                case 2:
                    PopulateEconomy(root);
                    break;
                case 3:
                    PopulateBuildPickups(root);
                    break;
                case 4:
                    PopulateEnemies(root, room, player, combat, enemyCatalog, difficultyTier);
                    break;
                case 5:
                    PopulateProjectilesAndVfx(root);
                    break;
                case 6:
                    PopulateHazardLaneNotes(root);
                    break;
                case 7:
                    PopulateProgressionProps(root);
                    break;
                case 8:
                    PopulateBosses(root, room, player, combat, bossCatalog, difficultyTier, BossWorldBand.World1);
                    break;
                case 9:
                    PopulateBosses(root, room, player, combat, bossCatalog, difficultyTier, BossWorldBand.World2);
                    break;
                case 10:
                    PopulateBosses(root, room, player, combat, bossCatalog, difficultyTier, BossWorldBand.World3);
                    break;
            }
        }

        private static void PopulateEnvironment(Transform root)
        {
            AddDisplay(root, "Rock obstacle", new Vector3(-8f, 0.5f, 1.6f), new Vector3(1f, 1f, 1f), MaterialRole.RoomObstacleRock, PrimitiveType.Cube);
            AddDisplay(root, "Pit / hole marker", new Vector3(-5.5f, 0.03f, 1.6f), new Vector3(1.2f, 0.06f, 1.2f), MaterialRole.DesignerHole, PrimitiveType.Cube);
            AddDisplay(root, "Spike hazard", new Vector3(-3f, 0.08f, 1.6f), new Vector3(0.8f, 0.12f, 0.8f), MaterialRole.RoomHazardSpike, PrimitiveType.Cylinder);
            AddDisplay(root, "Standard barrel", new Vector3(-0.5f, 0.45f, 1.6f), new Vector3(0.7f, 0.9f, 0.7f), MaterialRole.RoomBarrel, PrimitiveType.Cylinder);
            AddDisplay(root, "Explosive barrel", new Vector3(2f, 0.45f, 1.6f), new Vector3(0.7f, 0.9f, 0.7f), MaterialRole.RoomExplosiveBarrel, PrimitiveType.Cylinder);
            AddDisplay(root, "Door active", new Vector3(4.5f, 0.65f, 1.6f), new Vector3(1f, 1.3f, 0.18f), MaterialRole.DoorActive, PrimitiveType.Cube);
            AddDisplay(root, "Door locked", new Vector3(7f, 0.65f, 1.6f), new Vector3(1f, 1.3f, 0.18f), MaterialRole.DoorLocked, PrimitiveType.Cube);
        }

        private static void PopulateEconomy(Transform root)
        {
            AddCoin(root, CoinDenomination.Copper, new Vector3(-8f, 0.24f, 1.4f));
            AddCoin(root, CoinDenomination.Silver, new Vector3(-6.8f, 0.24f, 1.4f));
            AddCoin(root, CoinDenomination.Gold, new Vector3(-5.6f, 0.24f, 1.4f));
            AddDisplay(root, "HP refill", new Vector3(-3.4f, 0.32f, 1.4f), Vector3.one * 0.58f, MaterialRole.RewardPickup, PrimitiveType.Sphere);
            AddChest(root, ChestKind.Normal, new Vector3(-0.8f, 0.34f, 1.4f));
            AddChest(root, ChestKind.Golden, new Vector3(1.8f, 0.34f, 1.4f));
            AddDisplay(root, "Room reward pickup", new Vector3(4.6f, 0.32f, 1.4f), Vector3.one * 0.62f, MaterialRole.RewardPickup, PrimitiveType.Sphere);
        }

        private static void PopulateBuildPickups(Transform root)
        {
            for (var index = 0; index < BuildPickupLabels.Length; index++)
            {
                var x = -10f + (index % 6) * 4f;
                var z = index < 6 ? 1.9f : index < 12 ? 0f : -1.9f;
                AddDisplay(root, BuildPickupLabels[index], new Vector3(x, 0.32f, z), Vector3.one * 0.5f, MaterialRole.RewardPickup, PrimitiveType.Cube);
            }
        }

        private static void PopulateEnemies(Transform root, RoomRuntimeRoot room, PlaceholderPlayerController player, RoomCombatController combat, EnemyCatalog enemyCatalog, DifficultyTierDefinition difficultyTier)
        {
            for (var index = 0; index < EnemySpawnKinds.Length; index++)
            {
                var x = -9f + index * 3f;
                SpawnEnemy(root, room, player, combat, enemyCatalog, difficultyTier, EnemySpawnKinds[index], new Vector3(x, 0.35f, 0.8f), InspectionEntityMode.FrozenRuntime);
            }
        }

        private static void PopulateProjectilesAndVfx(Transform root)
        {
            AddDisplay(root, "Player projectile", new Vector3(-8f, 0.28f, 1.4f), Vector3.one * 0.28f, MaterialRole.Projectile, PrimitiveType.Sphere);
            AddDisplay(root, "Power red shot", new Vector3(-6.4f, 0.28f, 1.4f), Vector3.one * 0.32f, MaterialRole.ProjectilePower, PrimitiveType.Sphere);
            AddDisplay(root, "Enemy projectile", new Vector3(-4.8f, 0.28f, 1.4f), Vector3.one * 0.32f, MaterialRole.EnemyProjectile, PrimitiveType.Sphere);
            AddDisplay(root, "Shield guard", new Vector3(-2.6f, 0.5f, 1.4f), new Vector3(0.08f, 1f, 1.4f), MaterialRole.ShieldGuard, PrimitiveType.Cube);
            AddDisplay(root, "Shield parry", new Vector3(-0.8f, 0.5f, 1.4f), new Vector3(0.08f, 1f, 1.4f), MaterialRole.ShieldParry, PrimitiveType.Cube);
            AddDisplay(root, "Explosion cue", new Vector3(1.4f, 0.24f, 1.4f), Vector3.one * 0.8f, MaterialRole.RoomExplosiveBarrel, PrimitiveType.Sphere);
            AddDisplay(root, "Pickup VFX", new Vector3(3.8f, 0.24f, 1.4f), Vector3.one * 0.5f, MaterialRole.VfxDebug, PrimitiveType.Sphere);
        }

        private static void PopulateHazardLaneNotes(Transform root)
        {
            AddLabel(root, "This room uses the active runtime room asset for live hazards/barrels.\nWalk into spikes or attack barrels to test real M45 behavior.", new Vector3(0f, 0.25f, -2.4f), Color.white, 0.11f);
        }

        private static void PopulateProgressionProps(Transform root)
        {
            AddDisplay(root, "Boss key", new Vector3(-8f, 0.38f, 1.4f), Vector3.one * 0.48f, MaterialRole.BossKeyPickup, PrimitiveType.Sphere);
            AddDisplay(root, "Shop stand/card", new Vector3(-5.2f, 0.48f, 1.4f), new Vector3(1f, 0.9f, 0.45f), MaterialRole.HubShop, PrimitiveType.Cube);
            AddDisplay(root, "Hub portal", new Vector3(-2f, 0.6f, 1.4f), Vector3.one * 0.9f, MaterialRole.HubReturnPortal, PrimitiveType.Cylinder);
            AddDisplay(root, "Branch portal", new Vector3(1f, 0.6f, 1.4f), Vector3.one * 0.9f, MaterialRole.NextBranchPortal, PrimitiveType.Cylinder);
            AddDisplay(root, "Defeated portal", new Vector3(4f, 0.6f, 1.4f), Vector3.one * 0.9f, MaterialRole.DoorUnavailable, PrimitiveType.Cylinder);
            AddDisplay(root, "Final portal", new Vector3(7f, 0.6f, 1.4f), Vector3.one * 0.9f, MaterialRole.SecretDoorDebug, PrimitiveType.Cylinder);
        }

        private static void PopulateBosses(
            Transform root,
            RoomRuntimeRoot room,
            PlaceholderPlayerController player,
            RoomCombatController combat,
            BossCatalogDefinition bossCatalog,
            DifficultyTierDefinition difficultyTier,
            BossWorldBand band)
        {
            var catalog = bossCatalog != null ? bossCatalog : BossCatalogDefinition.CreateRuntimeDefault();
            var bosses = catalog.BossesForBand(band).ToArray();
            for (var index = 0; index < bosses.Length; index++)
            {
                var x = -7f + index * 5f;
                SpawnBoss(root, room, player, combat, difficultyTier, bosses[index], new Vector3(x, 0.42f, 0.6f), InspectionEntityMode.FrozenRuntime);
            }
        }

        public static EnemyRuntimeController SpawnEnemy(
            Transform root,
            RoomRuntimeRoot room,
            PlaceholderPlayerController player,
            RoomCombatController combat,
            EnemyCatalog enemyCatalog,
            DifficultyTierDefinition difficultyTier,
            string spawnKind,
            Vector3 localPosition,
            InspectionEntityMode mode)
        {
            if (room == null || player == null || combat == null)
            {
                return null;
            }

            var catalog = enemyCatalog != null ? enemyCatalog : EnemyCatalog.CreateRuntimeDefault();
            var definition = EnemyDefinitionResolver.Resolve(catalog, spawnKind, out _);
            var parent = root != null ? root : player.transform.parent != null ? player.transform.parent : room.transform;
            var enemyObject = combat.EnemyPrefab != null
                ? UnityEngine.Object.Instantiate(combat.EnemyPrefab, parent)
                : GameObject.CreatePrimitive(PrimitiveType.Capsule);
            enemyObject.name = $"InspectionEnemy.{definition.SpawnKind}";
            enemyObject.SetActive(true);
            enemyObject.transform.localPosition = RoomLocalCollision.ResolveMoveIgnoringObstacles(room, localPosition, definition.RadiusMeters);
            var enemy = enemyObject.GetComponent<EnemyRuntimeController>() ?? enemyObject.AddComponent<EnemyRuntimeController>();
            enemy.Configure(room, player, definition, difficultyTier ?? DifficultyTierDefinition.CreateRuntimeDeveloperSample());
            enemy.ConfigureSpawnContext(combat.EnemyPrefab, combat.ProjectilePrefab, catalog, difficultyTier, combat.Diagnostics);
            enemy.SetInspectionMode(mode);
            if (mode == InspectionEntityMode.FrozenRuntime)
            {
                enemy.BeginEntryGrace(999999f, Time.time);
            }

            AddLabel(enemyObject.transform, definition.DisplayName, new Vector3(0f, 1.35f, 0f), Color.white, 0.065f);
            return enemy;
        }

        public static EnemyRuntimeController SpawnBoss(
            Transform root,
            RoomRuntimeRoot room,
            PlaceholderPlayerController player,
            RoomCombatController combat,
            DifficultyTierDefinition difficultyTier,
            BossDefinition boss,
            Vector3 localPosition,
            InspectionEntityMode mode)
        {
            if (boss == null || combat == null)
            {
                return null;
            }

            var enemy = SpawnEnemy(root, room, player, combat, combat.EnemyCatalog, difficultyTier, "spawnEnemyBoss", localPosition, mode);
            if (enemy == null)
            {
                return null;
            }

            enemy.ConfigureBoss(boss);
            enemy.SetInspectionMode(mode);
            AddLabel(enemy.transform, $"{boss.DisplayName}\n{boss.MaxHealth} HP", new Vector3(0f, 1.9f, 0f), new Color(1f, 0.86f, 0.62f), 0.07f);
            return enemy;
        }

        public static GameObject SpawnDisplay(string label, Vector3 localPosition, MaterialRole role, PrimitiveType primitive = PrimitiveType.Cube)
        {
            var display = GameObject.CreatePrimitive(primitive);
            display.name = $"DebugSpawn.{label}";
            display.transform.localPosition = localPosition;
            display.transform.localScale = Vector3.one * 0.6f;
            MaterialResolver.ApplyTo(display, role);
            AddLabel(display.transform, label, new Vector3(0f, 0.8f, 0f), Color.white, 0.07f);
            return display;
        }

        private static Transform CreateRoot(RoomRuntimeRoot room)
        {
            var rootObject = new GameObject("DeveloperLabRoomContent");
            rootObject.transform.SetParent(room.transform, false);
            return rootObject.transform;
        }

        private static void AddTitle(Transform root, string title, Rect bounds)
        {
            AddLabel(root, title, new Vector3(0f, 0.28f, bounds.yMax - 0.72f), new Color(0.78f, 1f, 0.76f), 0.12f);
        }

        private static string TitleFor(int index)
        {
            return index >= 1 && index <= DeveloperLabDefinition.RoomTitles.Length
                ? DeveloperLabDefinition.RoomTitles[index - 1]
                : "Developer Lab";
        }

        private static int RoomIndex(BranchRoomId roomId)
        {
            var value = roomId.Value ?? string.Empty;
            if (value == BranchRoomId.Origin.Value)
            {
                return 1;
            }

            return value.StartsWith("lab_room_", StringComparison.Ordinal) &&
                   int.TryParse(value.Substring("lab_room_".Length), out var index)
                ? Mathf.Clamp(index, 1, DeveloperLabDefinition.RoomCount)
                : 1;
        }

        private static GameObject AddDisplay(Transform root, string label, Vector3 localPosition, Vector3 localScale, MaterialRole role, PrimitiveType primitive)
        {
            var display = GameObject.CreatePrimitive(primitive);
            display.name = $"LabDisplay.{label}";
            display.transform.SetParent(root, false);
            display.transform.localPosition = localPosition;
            display.transform.localScale = localScale;
            MaterialResolver.ApplyTo(display, role);
            AddLabel(display.transform, label, new Vector3(0f, Mathf.Max(0.45f, localScale.y) + 0.28f, 0f), Color.white, 0.065f);
            return display;
        }

        private static void AddCoin(Transform root, CoinDenomination denomination, Vector3 localPosition)
        {
            var role = denomination switch
            {
                CoinDenomination.Silver => MaterialRole.CoinSilver,
                CoinDenomination.Gold => MaterialRole.CoinGold,
                _ => MaterialRole.CoinCopper
            };
            var coin = AddDisplay(root, $"{denomination} coin", localPosition, Vector3.one * (denomination == CoinDenomination.Gold ? 0.42f : 0.32f), role, PrimitiveType.Cylinder);
            var pickup = coin.AddComponent<CoinPickupController>();
            pickup.Configure("developer_lab", $"lab_{denomination}", denomination, CoinDenominationResolver.ValueFor(denomination), false);
        }

        private static void AddChest(Transform root, ChestKind kind, Vector3 localPosition)
        {
            var role = kind == ChestKind.Golden ? MaterialRole.ChestGolden : MaterialRole.ChestNormal;
            var chest = AddDisplay(root, $"{kind} chest", localPosition, new Vector3(0.8f, 0.48f, 0.62f), role, PrimitiveType.Cube);
            var controller = chest.AddComponent<RoomChestController>();
            controller.Configure("developer_lab", $"lab_{kind}", kind, ChestState.Unopened);
        }

        private static TextMesh AddLabel(Transform parent, string text, Vector3 localPosition, Color color, float scale)
        {
            var labelObject = new GameObject($"Label.{text}", typeof(TextMesh));
            labelObject.transform.SetParent(parent, false);
            labelObject.transform.localPosition = localPosition;
            labelObject.transform.localRotation = Quaternion.Euler(62f, 0f, 0f);
            labelObject.transform.localScale = Vector3.one * scale;
            var mesh = labelObject.GetComponent<TextMesh>();
            mesh.text = text;
            mesh.anchor = TextAnchor.MiddleCenter;
            mesh.alignment = TextAlignment.Center;
            mesh.fontSize = 46;
            mesh.color = color;
            return mesh;
        }
    }
}
