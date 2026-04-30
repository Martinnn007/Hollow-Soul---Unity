using System;
using System.Collections.Generic;
using System.Linq;
using Hollow.Combat;
using Hollow.Data.Definitions;
using Hollow.Presentation;
using Hollow.Rewards;
using UnityEngine;

namespace Hollow.Branches
{
    public sealed class DebugSpawnMenuController : MonoBehaviour
    {
        private const int WindowId = 55055;

        private static readonly DebugSpawnGroup[] Groups =
        {
            new("Enemies", new[] { "spawnEnemyNormal", "spawnEnemyFlying", "spawnEnemyFast", "spawnEnemyHeavy", "spawnEnemyCharger", "spawnEnemyTurret", "spawnEnemySplitter" }),
            new("Bosses", new[] { "stone_warden", "splinter_saint", "gravel_maw", "cartouche_widow", "iron_reliquary", "mirror_husk", "ash_comet", "choir_of_teeth", "rust_bishop", "hollow_star_larva" }),
            new("Weapons", new[] { "starter_blade", "starter_bolt", "skeletal_sword", "bone_bow", "dragon_fang", "dragon_bow" }),
            new("Armor", new[] { "skeletal_armor", "dragon_scale_armor" }),
            new("Items", new[] { "vital_locket", "iron_stitch", "fleet_pin", "stamina_thread", "cursed_skull", "bone_totem", "dragon_tooth", "dragon_heart", "double_barrel", "triple_shot", "quad_shot", "power_up", "fire_rate_up" }),
            new("Actives", new[] { "mending_charm", "echo_burst" }),
            new("Cards", new[] { "ember_card", "swift_card", "mend_card", "blade_lesson", "bolt_lesson" }),
            new("Coins", new[] { "copper_coin", "silver_coin", "gold_coin" }),
            new("Chests", new[] { "normal_chest", "golden_chest" }),
            new("Hazards/Props", new[] { "rock", "spike", "standard_barrel", "explosive_barrel", "pit_marker" }),
            new("Projectiles/VFX", new[] { "player_projectile", "power_projectile", "enemy_projectile", "shield_guard", "explosion" }),
            new("Portals/Doors", new[] { "door_active", "door_locked", "door_cleared", "hub_portal", "branch_portal", "defeated_portal", "final_portal" })
        };

        private BranchSessionController session;
        private bool visible;
        private bool spawnFrozen;
        private int groupIndex;
        private int entityIndex;
        private Rect windowRect = new(24f, 100f, 420f, 260f);

        public void Bind(BranchSessionController controller)
        {
            session = controller;
        }

        private void Update()
        {
            if (!IsAvailable())
            {
                visible = false;
                return;
            }

            if (UnityEngine.Input.GetKeyDown(KeyCode.F4))
            {
                visible = !visible;
            }

            if (!visible)
            {
                return;
            }

            if (UnityEngine.Input.GetKeyDown(KeyCode.F6))
            {
                CycleGroup(1);
            }

            if (UnityEngine.Input.GetKeyDown(KeyCode.F7))
            {
                CycleEntity(1);
            }

            if (UnityEngine.Input.GetKeyDown(KeyCode.F8))
            {
                spawnFrozen = !spawnFrozen;
            }

            if (UnityEngine.Input.GetKeyDown(KeyCode.F5))
            {
                SpawnSelected();
            }
        }

        private void OnGUI()
        {
            if (!visible || !IsAvailable())
            {
                return;
            }

            windowRect = GUILayout.Window(WindowId, windowRect, DrawWindow, "Developer Spawn Menu (F4)");
        }

        private void DrawWindow(int id)
        {
            var group = CurrentGroup();
            GUILayout.Label("Editor/development only. Spawns are non-authoritative and never count for room clear.");
            GUILayout.Space(6f);
            GUILayout.Label($"Group: {group.Name}");
            GUILayout.Label($"Entity: {CurrentEntity()}");
            GUILayout.Label($"Mode: {(spawnFrozen ? "Frozen Runtime" : "Live Runtime")}");
            GUILayout.Space(8f);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Prev Group"))
            {
                CycleGroup(-1);
            }

            if (GUILayout.Button("Next Group (F6)"))
            {
                CycleGroup(1);
            }
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Prev Entity"))
            {
                CycleEntity(-1);
            }

            if (GUILayout.Button("Next Entity (F7)"))
            {
                CycleEntity(1);
            }
            GUILayout.EndHorizontal();
            spawnFrozen = GUILayout.Toggle(spawnFrozen, "Spawn frozen (F8)");
            if (GUILayout.Button("Spawn In Front Of Player (F5)"))
            {
                SpawnSelected();
            }

            GUI.DragWindow();
        }

        private void SpawnSelected()
        {
            if (session == null ||
                session.RuntimeRoomRoot == null ||
                session.PlayerController == null ||
                session.RoomCombatController == null)
            {
                return;
            }

            var group = CurrentGroup();
            var entity = CurrentEntity();
            var position = SpawnPosition();
            switch (group.Name)
            {
                case "Enemies":
                    DeveloperLabRoomPopulator.SpawnEnemy(
                        session.RuntimeRoomRoot.transform,
                        session.RuntimeRoomRoot,
                        session.PlayerController,
                        session.RoomCombatController,
                        session.RoomCombatController.EnemyCatalog,
                        session.RoomCombatController.DifficultyTier,
                        entity,
                        position,
                        spawnFrozen ? InspectionEntityMode.FrozenRuntime : InspectionEntityMode.LiveRuntime);
                    break;
                case "Bosses":
                    var bossCatalog = session.BossCatalog != null ? session.BossCatalog : BossCatalogDefinition.CreateRuntimeDefault();
                    var boss = bossCatalog.Bosses.FirstOrDefault(candidate => candidate != null && candidate.BossId == entity) ?? bossCatalog.FallbackBoss;
                    DeveloperLabRoomPopulator.SpawnBoss(
                        session.RuntimeRoomRoot.transform,
                        session.RuntimeRoomRoot,
                        session.PlayerController,
                        session.RoomCombatController,
                        session.RoomCombatController.DifficultyTier,
                        boss,
                        position,
                        spawnFrozen ? InspectionEntityMode.FrozenRuntime : InspectionEntityMode.LiveRuntime);
                    break;
                case "Coins":
                    SpawnCoin(entity, position);
                    break;
                case "Chests":
                    SpawnChest(entity, position);
                    break;
                case "Hazards/Props":
                    SpawnProp(entity, position);
                    break;
                case "Projectiles/VFX":
                    SpawnProjectileOrVfx(entity, position);
                    break;
                case "Portals/Doors":
                    SpawnPortalOrDoor(entity, position);
                    break;
                default:
                    SpawnPickupStand(entity, position);
                    break;
            }
        }

        private void SpawnCoin(string entity, Vector3 position)
        {
            var denomination = entity.Contains("gold", StringComparison.OrdinalIgnoreCase)
                ? CoinDenomination.Gold
                : entity.Contains("silver", StringComparison.OrdinalIgnoreCase)
                    ? CoinDenomination.Silver
                    : CoinDenomination.Copper;
            var role = denomination switch
            {
                CoinDenomination.Gold => MaterialRole.CoinGold,
                CoinDenomination.Silver => MaterialRole.CoinSilver,
                _ => MaterialRole.CoinCopper
            };
            var coin = CreatePrimitive($"DebugCoin.{denomination}", PrimitiveType.Cylinder, position, Vector3.one * 0.34f, role);
            coin.AddComponent<CoinPickupController>().Configure("debug_spawn", Guid.NewGuid().ToString("N"), denomination, CoinDenominationResolver.ValueFor(denomination), false);
            AddLabel(coin.transform, $"{denomination} coin");
        }

        private void SpawnChest(string entity, Vector3 position)
        {
            var kind = entity.Contains("golden", StringComparison.OrdinalIgnoreCase) ? ChestKind.Golden : ChestKind.Normal;
            var role = kind == ChestKind.Golden ? MaterialRole.ChestGolden : MaterialRole.ChestNormal;
            var chest = CreatePrimitive($"DebugChest.{kind}", PrimitiveType.Cube, position + Vector3.up * 0.18f, new Vector3(0.8f, 0.48f, 0.62f), role);
            chest.AddComponent<RoomChestController>().Configure("debug_spawn", Guid.NewGuid().ToString("N"), kind, ChestState.Unopened);
            AddLabel(chest.transform, $"{kind} chest");
        }

        private void SpawnProp(string entity, Vector3 position)
        {
            var role = entity switch
            {
                "spike" => MaterialRole.RoomHazardSpike,
                "standard_barrel" => MaterialRole.RoomBarrel,
                "explosive_barrel" => MaterialRole.RoomExplosiveBarrel,
                "pit_marker" => MaterialRole.DesignerHole,
                _ => MaterialRole.RoomObstacleRock
            };
            var primitive = entity.Contains("barrel", StringComparison.OrdinalIgnoreCase) || entity == "spike"
                ? PrimitiveType.Cylinder
                : PrimitiveType.Cube;
            var scale = entity == "spike" || entity == "pit_marker"
                ? new Vector3(0.8f, 0.08f, 0.8f)
                : Vector3.one * 0.8f;
            var prop = CreatePrimitive($"DebugProp.{entity}", primitive, position + Vector3.up * scale.y * 0.5f, scale, role);
            AddLabel(prop.transform, entity);
        }

        private void SpawnProjectileOrVfx(string entity, Vector3 position)
        {
            var role = entity switch
            {
                "power_projectile" => MaterialRole.ProjectilePower,
                "enemy_projectile" => MaterialRole.EnemyProjectile,
                "shield_guard" => MaterialRole.ShieldGuard,
                "explosion" => MaterialRole.RoomExplosiveBarrel,
                _ => MaterialRole.Projectile
            };
            var scale = entity == "shield_guard" ? new Vector3(0.08f, 1f, 1.4f) : Vector3.one * 0.36f;
            var visual = CreatePrimitive($"DebugVfx.{entity}", PrimitiveType.Sphere, position + Vector3.up * 0.3f, scale, role);
            AddLabel(visual.transform, entity);
        }

        private void SpawnPortalOrDoor(string entity, Vector3 position)
        {
            var role = entity switch
            {
                "door_locked" => MaterialRole.DoorLocked,
                "door_cleared" => MaterialRole.DoorCleared,
                "hub_portal" => MaterialRole.HubReturnPortal,
                "branch_portal" => MaterialRole.NextBranchPortal,
                "defeated_portal" => MaterialRole.DoorUnavailable,
                "final_portal" => MaterialRole.SecretDoorDebug,
                _ => MaterialRole.DoorActive
            };
            var primitive = entity.Contains("portal", StringComparison.OrdinalIgnoreCase) ? PrimitiveType.Cylinder : PrimitiveType.Cube;
            var visual = CreatePrimitive($"DebugPortalDoor.{entity}", primitive, position + Vector3.up * 0.55f, Vector3.one * 0.9f, role);
            AddLabel(visual.transform, entity);
        }

        private void SpawnPickupStand(string entity, Vector3 position)
        {
            var pickup = CreatePrimitive($"DebugPickup.{entity}", PrimitiveType.Cube, position + Vector3.up * 0.28f, Vector3.one * 0.55f, MaterialRole.RewardPickup);
            AddLabel(pickup.transform, entity);
        }

        private GameObject CreatePrimitive(string name, PrimitiveType primitive, Vector3 localPosition, Vector3 localScale, MaterialRole role)
        {
            var target = GameObject.CreatePrimitive(primitive);
            target.name = name;
            target.transform.SetParent(session.PlayerController.transform.parent != null ? session.PlayerController.transform.parent : session.RuntimeRoomRoot.transform, false);
            target.transform.localPosition = localPosition;
            target.transform.localScale = localScale;
            MaterialResolver.ApplyTo(target, role);
            var collider = target.GetComponent<Collider>();
            if (collider != null)
            {
                collider.enabled = false;
            }

            return target;
        }

        private Vector3 SpawnPosition()
        {
            var player = session.PlayerController;
            var direction = player.transform.forward;
            direction.y = 0f;
            if (direction.sqrMagnitude < 0.001f)
            {
                direction = Vector3.forward;
            }

            var desired = player.transform.localPosition + direction.normalized * 1.8f;
            return RoomLocalCollision.ResolveMoveIgnoringObstacles(session.RuntimeRoomRoot, desired, 0.35f);
        }

        private bool IsAvailable()
        {
            return (Application.isEditor || Debug.isDebugBuild) && session != null;
        }

        private DebugSpawnGroup CurrentGroup()
        {
            groupIndex = Mathf.Clamp(groupIndex, 0, Groups.Length - 1);
            return Groups[groupIndex];
        }

        private string CurrentEntity()
        {
            var group = CurrentGroup();
            entityIndex = group.Entities.Length == 0 ? 0 : Mathf.Clamp(entityIndex, 0, group.Entities.Length - 1);
            return group.Entities.Length == 0 ? string.Empty : group.Entities[entityIndex];
        }

        private void CycleGroup(int delta)
        {
            groupIndex = Mod(groupIndex + delta, Groups.Length);
            entityIndex = 0;
        }

        private void CycleEntity(int delta)
        {
            var count = CurrentGroup().Entities.Length;
            entityIndex = count <= 0 ? 0 : Mod(entityIndex + delta, count);
        }

        private static int Mod(int value, int modulus)
        {
            return modulus <= 0 ? 0 : (value % modulus + modulus) % modulus;
        }

        private static void AddLabel(Transform parent, string text)
        {
            var label = new GameObject($"Label.{text}", typeof(TextMesh));
            label.transform.SetParent(parent, false);
            label.transform.localPosition = new Vector3(0f, 0.85f, 0f);
            label.transform.localRotation = Quaternion.Euler(62f, 0f, 0f);
            label.transform.localScale = Vector3.one * 0.07f;
            var mesh = label.GetComponent<TextMesh>();
            mesh.text = text;
            mesh.anchor = TextAnchor.MiddleCenter;
            mesh.alignment = TextAlignment.Center;
            mesh.fontSize = 42;
            mesh.color = Color.white;
        }

        private readonly struct DebugSpawnGroup
        {
            public DebugSpawnGroup(string name, IReadOnlyList<string> entities)
            {
                Name = name;
                Entities = entities?.ToArray() ?? Array.Empty<string>();
            }

            public string Name { get; }

            public string[] Entities { get; }
        }
    }
}
