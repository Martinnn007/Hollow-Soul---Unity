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
            AddArtPassDisplay(root, "Rock obstacle", new Vector3(-8f, 0.5f, 1.6f), PresentationPrefabRole.RoomObstacleRock);
            AddDisplay(root, "Pit / hole marker", new Vector3(-5.5f, 0.03f, 1.6f), new Vector3(1.2f, 0.06f, 1.2f), MaterialRole.DesignerHole, PrimitiveType.Cube);
            AddArtPassDisplay(root, "Spike hazard", new Vector3(-3f, 0.08f, 1.6f), PresentationPrefabRole.RoomHazardSpike);
            AddArtPassDisplay(root, "Standard barrel", new Vector3(-0.5f, 0.45f, 1.6f), PresentationPrefabRole.StandardBarrel);
            AddArtPassDisplay(root, "Explosive barrel", new Vector3(2f, 0.45f, 1.6f), PresentationPrefabRole.ExplosiveBarrel);
            AddArtPassDisplay(root, "Door active", new Vector3(4.5f, 0.65f, 1.6f), PresentationPrefabRole.DoorActive);
            AddArtPassDisplay(root, "Door locked", new Vector3(7f, 0.65f, 1.6f), PresentationPrefabRole.DoorLocked);
        }

        private static void PopulateEconomy(Transform root)
        {
            AddCoin(root, CoinDenomination.Copper, new Vector3(-8f, 0.24f, 1.4f));
            AddCoin(root, CoinDenomination.Silver, new Vector3(-6.8f, 0.24f, 1.4f));
            AddCoin(root, CoinDenomination.Gold, new Vector3(-5.6f, 0.24f, 1.4f));
            AddArtPassDisplay(root, "HP refill", new Vector3(-3.4f, 0.32f, 1.4f), PresentationPrefabRole.RewardPickup);
            AddChest(root, ChestKind.Normal, new Vector3(-0.8f, 0.34f, 1.4f));
            AddChest(root, ChestKind.Golden, new Vector3(1.8f, 0.34f, 1.4f));
            AddArtPassDisplay(root, "Room reward pickup", new Vector3(4.6f, 0.32f, 1.4f), PresentationPrefabRole.RewardPickup);
        }

        private static void PopulateBuildPickups(Transform root)
        {
            for (var index = 0; index < BuildPickupLabels.Length; index++)
            {
                var x = -10f + (index % 6) * 4f;
                var z = index < 6 ? 1.9f : index < 12 ? 0f : -1.9f;
                AddArtPassDisplay(root, BuildPickupLabels[index], new Vector3(x, 0.32f, z), RoleForBuildPickup(BuildPickupLabels[index]));
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
            AddArtPassDisplay(root, "Player projectile", new Vector3(-8f, 0.28f, 1.4f), PresentationPrefabRole.Projectile);
            AddDisplay(root, "Power red shot", new Vector3(-6.4f, 0.28f, 1.4f), Vector3.one * 0.32f, MaterialRole.ProjectilePower, PrimitiveType.Sphere);
            AddArtPassDisplay(root, "Enemy projectile", new Vector3(-4.8f, 0.28f, 1.4f), PresentationPrefabRole.EnemyProjectile);
            AddDisplay(root, "Shield guard", new Vector3(-2.6f, 0.5f, 1.4f), new Vector3(0.08f, 1f, 1.4f), MaterialRole.ShieldGuard, PrimitiveType.Cube);
            AddDisplay(root, "Shield parry", new Vector3(-0.8f, 0.5f, 1.4f), new Vector3(0.08f, 1f, 1.4f), MaterialRole.ShieldParry, PrimitiveType.Cube);
            AddArtPassDisplay(root, "Explosion cue", new Vector3(1.4f, 0.24f, 1.4f), PresentationPrefabRole.VfxChestOpen);
            AddArtPassDisplay(root, "Pickup VFX", new Vector3(3.8f, 0.24f, 1.4f), PresentationPrefabRole.VfxRewardClaim);
        }

        private static void PopulateHazardLaneNotes(Transform root)
        {
            AddLabel(root, "This room uses the active runtime room asset for live hazards/barrels.\nWalk into spikes or attack barrels to test real M45 behavior.", new Vector3(0f, 0.25f, -2.4f), Color.white, 0.11f);
        }

        private static void PopulateProgressionProps(Transform root)
        {
            AddArtPassDisplay(root, "Boss key", new Vector3(-8f, 0.38f, 1.4f), PresentationPrefabRole.BossKeyPickup);
            AddArtPassDisplay(root, "Shop stand/card", new Vector3(-5.2f, 0.48f, 1.4f), PresentationPrefabRole.HubShop);
            AddArtPassDisplay(root, "Hub portal", new Vector3(-2f, 0.6f, 1.4f), PresentationPrefabRole.HubReturnPortal);
            AddArtPassDisplay(root, "Branch portal", new Vector3(1f, 0.6f, 1.4f), PresentationPrefabRole.NextBranchPortal);
            AddArtPassDisplay(root, "Defeated portal", new Vector3(4f, 0.6f, 1.4f), PresentationPrefabRole.DoorUnavailable);
            AddArtPassDisplay(root, "Final portal", new Vector3(7f, 0.6f, 1.4f), PresentationPrefabRole.SecretDoorDebug);
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

        private static GameObject AddArtPassDisplay(Transform root, string label, Vector3 localPosition, PresentationPrefabRole role)
        {
            var display = new GameObject($"LabDisplay.{label}");
            display.transform.SetParent(root, false);
            display.transform.localPosition = localPosition;
            PresentationPrefabResolver.InstantiateVisual(role, display.transform, Vector3.zero, Vector3.one);
            AddLabel(display.transform, label, new Vector3(0f, 0.92f, 0f), Color.white, 0.065f);
            return display;
        }

        private static void AddCoin(Transform root, CoinDenomination denomination, Vector3 localPosition)
        {
            var role = denomination switch
            {
                CoinDenomination.Silver => PresentationPrefabRole.CoinSilver,
                CoinDenomination.Gold => PresentationPrefabRole.CoinGold,
                _ => PresentationPrefabRole.CoinCopper
            };
            var coin = AddArtPassDisplay(root, $"{denomination} coin", localPosition, role);
            var pickup = coin.AddComponent<CoinPickupController>();
            pickup.Configure("developer_lab", $"lab_{denomination}", denomination, CoinDenominationResolver.ValueFor(denomination), false);
        }

        private static void AddChest(Transform root, ChestKind kind, Vector3 localPosition)
        {
            var role = kind == ChestKind.Golden ? PresentationPrefabRole.ChestGolden : PresentationPrefabRole.ChestNormal;
            var chest = AddArtPassDisplay(root, $"{kind} chest", localPosition, role);
            var controller = chest.AddComponent<RoomChestController>();
            controller.Configure("developer_lab", $"lab_{kind}", kind, ChestState.Unopened);
        }

        private static PresentationPrefabRole RoleForBuildPickup(string pickupId)
        {
            if (pickupId.Contains("blade", StringComparison.OrdinalIgnoreCase) ||
                pickupId.Contains("sword", StringComparison.OrdinalIgnoreCase) ||
                pickupId.Contains("fang", StringComparison.OrdinalIgnoreCase))
            {
                return PresentationPrefabRole.WeaponMelee;
            }

            if (pickupId.Contains("bolt", StringComparison.OrdinalIgnoreCase) ||
                pickupId.Contains("bow", StringComparison.OrdinalIgnoreCase))
            {
                return PresentationPrefabRole.WeaponRanged;
            }

            if (pickupId.Contains("armor", StringComparison.OrdinalIgnoreCase))
            {
                return PresentationPrefabRole.Armor;
            }

            if (pickupId is "mending_charm" or "echo_burst")
            {
                return PresentationPrefabRole.ActiveItemPickup;
            }

            if (pickupId.EndsWith("_card", StringComparison.OrdinalIgnoreCase))
            {
                return PresentationPrefabRole.ConsumableCardPickup;
            }

            return PresentationPrefabRole.RewardPickup;
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
