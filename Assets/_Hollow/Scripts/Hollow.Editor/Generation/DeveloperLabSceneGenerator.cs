using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Hollow.Branches;
using Hollow.Combat;
using Hollow.Data.Definitions;
using Hollow.Presentation;
using Hollow.RoomDesigner;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Hollow.Editor.Generation
{
    public static class DeveloperLabSceneGenerator
    {
        public const string SceneDirectory = "Assets/_Hollow/Scenes/DeveloperLab";
        public const string ContentDataDirectory = "Assets/_Hollow/Data/DeveloperLab";
        public const string ContentDefinitionPath = ContentDataDirectory + "/DeveloperLabContentDefinition.asset";

        public static IReadOnlyList<string> ScenePaths => Enumerable
            .Range(1, DeveloperLabDefinition.RoomCount)
            .Select(index => $"{SceneDirectory}/DeveloperLab_{index:00}_{Slug(DeveloperLabDefinition.RoomTitles[index - 1])}.unity")
            .ToArray();

        [MenuItem("Hollow/Developer Lab/Generate Developer Lab Scenes")]
        public static void GenerateScenes()
        {
            Directory.CreateDirectory(SceneDirectory);
            Directory.CreateDirectory(ContentDataDirectory);

            for (var index = 1; index <= DeveloperLabDefinition.RoomCount; index++)
            {
                GenerateScene(index, ScenePaths[index - 1]);
            }

            AssetDatabase.Refresh();
            Debug.Log("Generated editable Developer Lab scenes.");
        }

        private static void GenerateScene(int index, string scenePath)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var roomId = DeveloperLabDefinition.RoomAssetIds[index - 1];
            var title = DeveloperLabDefinition.RoomTitles[index - 1];
            var metadataRoot = new GameObject("DeveloperLabSceneMetadata");
            var metadata = metadataRoot.AddComponent<DeveloperLabSceneRoomMetadata>();
            metadata.Configure(
                roomId,
                title,
                index,
                RoomDesignerFootprintPreset.Wide2x1.ToString(),
                $"{Milestone55AssetGenerator.LabRoomDirectory}/{roomId}.hollowruntime.json",
                ContentDefinitionPath);

            var previewRoot = new GameObject("RoomPreviewRoot");
            var markerRoot = new GameObject("AuthoringMarkers");
            CreatePreviewRoom(previewRoot.transform);
            AddShellMarkers(markerRoot.transform, index);
            AddGalleryMarkers(markerRoot.transform, index);
            CreateCameraAndLight();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, scenePath);
        }

        private static void CreatePreviewRoom(Transform root)
        {
            var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name = "Preview.Wide2x1Floor";
            floor.transform.SetParent(root, false);
            floor.transform.localPosition = new Vector3(0f, -0.03f, 0f);
            floor.transform.localScale = new Vector3(26f, 0.06f, 7f);
            MaterialResolver.ApplyTo(floor, MaterialRole.RoomFloor);

            AddPreviewLabel(root, "Move AuthoringMarkers only. Child visuals are preview-only.", new Vector3(0f, 0.2f, -4.5f), 0.12f);
        }

        private static void AddShellMarkers(Transform root, int index)
        {
            AddMarker(root, "shell_safe_start", DeveloperLabContentCategory.RoomMarker, "Safe Start", new Vector3(-10f, 0f, 0f), markerKind: RoomDesignerMarkerKinds.SafeStart, exportToRuntime: true, includeInGallery: false);
            AddMarker(root, "shell_enemy_anchor", DeveloperLabContentCategory.RoomMarker, "Enemy Anchor", new Vector3(-8f, 0f, -1f), markerKind: RoomDesignerMarkerKinds.EnemyNormal, exportToRuntime: true, includeInGallery: false);
            AddMarker(root, "shell_reward", DeveloperLabContentCategory.RoomMarker, "Reward Anchor", new Vector3(9.5f, 0f, 0f), markerKind: RoomDesignerMarkerKinds.RoomReward, exportToRuntime: true, includeInGallery: false);

            foreach (var door in RoomDesignerFootprintUtility.CreateAvailableDoorPorts(RoomDesignerFootprintPreset.Wide2x1))
            {
                var active = door.id is "west_0" or "east_0";
                AddMarker(
                    root,
                    $"shell_door_{door.id}",
                    DeveloperLabContentCategory.DoorPort,
                    active ? $"{door.id} Door" : $"{door.id} Off",
                    new Vector3(door.x, 0f, door.z),
                    exportToRuntime: true,
                    includeInGallery: false,
                    doorDirection: door.direction,
                    doorLaneIndex: door.laneIndex,
                    hostCellX: door.hostCellX,
                    hostCellZ: door.hostCellZ,
                    doorState: active ? RoomDesignerDoorKinds.Door : RoomDesignerDoorKinds.Inactive);
            }

            switch (index)
            {
                case 1:
                    AddMarker(root, "shell_rock_0", DeveloperLabContentCategory.RoomCell, "Rock", new Vector3(-8f, 0f, 1f), cellKind: RoomDesignerCellKinds.Rock, exportToRuntime: true, includeInGallery: false);
                    AddMarker(root, "shell_hole_0", DeveloperLabContentCategory.RoomCell, "Hole", new Vector3(-5f, 0f, 1f), cellKind: RoomDesignerCellKinds.Hole, exportToRuntime: true, includeInGallery: false);
                    AddMarker(root, "shell_spike_0", DeveloperLabContentCategory.RoomCell, "Spike", new Vector3(-2f, 0f, 1f), cellKind: RoomDesignerCellKinds.Spike, exportToRuntime: true, includeInGallery: false);
                    AddMarker(root, "shell_barrel_standard_0", DeveloperLabContentCategory.RoomMarker, "Standard Barrel", new Vector3(1f, 0f, 1f), markerKind: RoomDesignerMarkerKinds.StandardBarrel, exportToRuntime: true, includeInGallery: false);
                    AddMarker(root, "shell_barrel_explosive_0", DeveloperLabContentCategory.RoomMarker, "Explosive Barrel", new Vector3(4f, 0f, 1f), markerKind: RoomDesignerMarkerKinds.ExplosiveBarrel, exportToRuntime: true, includeInGallery: false);
                    break;
                case 4:
                    for (var enemyIndex = 0; enemyIndex < EnemySpawnKinds.Length; enemyIndex++)
                    {
                        AddMarker(root, $"shell_enemy_{enemyIndex:00}", DeveloperLabContentCategory.RoomMarker, EnemySpawnKinds[enemyIndex], new Vector3(-9f + enemyIndex * 3f, 0f, 0.8f), markerKind: EnemySpawnKinds[enemyIndex], exportToRuntime: true, includeInGallery: false);
                    }

                    break;
                case 6:
                    AddMarker(root, "shell_spike_lane", DeveloperLabContentCategory.RoomCell, "Spike", new Vector3(-7f, 0f, 0f), cellKind: RoomDesignerCellKinds.Spike, exportToRuntime: true, includeInGallery: false);
                    AddMarker(root, "shell_hole_lane", DeveloperLabContentCategory.RoomCell, "Hole", new Vector3(-3f, 0f, 0f), cellKind: RoomDesignerCellKinds.Hole, exportToRuntime: true, includeInGallery: false);
                    AddMarker(root, "shell_barrel_standard_lane", DeveloperLabContentCategory.RoomMarker, "Standard Barrel", new Vector3(1f, 0f, 0f), markerKind: RoomDesignerMarkerKinds.StandardBarrel, exportToRuntime: true, includeInGallery: false);
                    AddMarker(root, "shell_barrel_explosive_lane_0", DeveloperLabContentCategory.RoomMarker, "Explosive Barrel", new Vector3(4f, 0f, 0f), markerKind: RoomDesignerMarkerKinds.ExplosiveBarrel, exportToRuntime: true, includeInGallery: false);
                    AddMarker(root, "shell_barrel_explosive_lane_1", DeveloperLabContentCategory.RoomMarker, "Explosive Barrel", new Vector3(5.1f, 0f, 0f), markerKind: RoomDesignerMarkerKinds.ExplosiveBarrel, exportToRuntime: true, includeInGallery: false);
                    break;
                case 8:
                case 9:
                case 10:
                    AddMarker(root, "shell_boss_anchor", DeveloperLabContentCategory.RoomMarker, "Boss Anchor", new Vector3(0f, 0f, 0f), markerKind: RoomDesignerMarkerKinds.EnemyHeavy, exportToRuntime: true, includeInGallery: false);
                    break;
            }
        }

        private static void AddGalleryMarkers(Transform root, int index)
        {
            switch (index)
            {
                case 1:
                    AddArt(root, "gallery_rock", "Rock obstacle", new Vector3(-8f, 0.5f, 1.6f), PresentationPrefabRole.RoomObstacleRock);
                    AddPrimitive(root, "gallery_pit", "Pit / hole marker", new Vector3(-5.5f, 0.03f, 1.6f), new Vector3(1.2f, 0.06f, 1.2f), MaterialRole.DesignerHole);
                    AddArt(root, "gallery_spike", "Spike hazard", new Vector3(-3f, 0.08f, 1.6f), PresentationPrefabRole.RoomHazardSpike);
                    AddArt(root, "gallery_barrel", "Standard barrel", new Vector3(-0.5f, 0.45f, 1.6f), PresentationPrefabRole.StandardBarrel);
                    AddArt(root, "gallery_explosive_barrel", "Explosive barrel", new Vector3(2f, 0.45f, 1.6f), PresentationPrefabRole.ExplosiveBarrel);
                    AddArt(root, "gallery_door_active", "Door active", new Vector3(4.5f, 0.65f, 1.6f), PresentationPrefabRole.DoorActive);
                    AddArt(root, "gallery_door_locked", "Door locked", new Vector3(7f, 0.65f, 1.6f), PresentationPrefabRole.DoorLocked);
                    break;
                case 2:
                    AddCoin(root, "gallery_coin_copper", "Copper coin", new Vector3(-8f, 0.24f, 1.4f), "Copper");
                    AddCoin(root, "gallery_coin_silver", "Silver coin", new Vector3(-6.8f, 0.24f, 1.4f), "Silver");
                    AddCoin(root, "gallery_coin_gold", "Gold coin", new Vector3(-5.6f, 0.24f, 1.4f), "Gold");
                    AddArt(root, "gallery_hp_refill", "HP refill", new Vector3(-3.4f, 0.32f, 1.4f), PresentationPrefabRole.RewardPickup);
                    AddChest(root, "gallery_chest_normal", "Normal chest", new Vector3(-0.8f, 0f, 1.4f), "Normal");
                    AddChest(root, "gallery_chest_golden", "Golden chest", new Vector3(1.8f, 0f, 1.4f), "Golden");
                    AddArt(root, "gallery_reward_pickup", "Room reward pickup", new Vector3(4.6f, 0.32f, 1.4f), PresentationPrefabRole.RewardPickup);
                    break;
                case 3:
                    AddBuildPickupMarkers(root);
                    break;
                case 4:
                    for (var enemyIndex = 0; enemyIndex < EnemySpawnKinds.Length; enemyIndex++)
                    {
                        AddEnemy(root, $"gallery_enemy_{enemyIndex:00}", EnemySpawnKinds[enemyIndex], new Vector3(-9f + enemyIndex * 3f, 0.35f, 0.8f));
                    }

                    break;
                case 5:
                    AddArt(root, "gallery_projectile_player", "Player projectile", new Vector3(-8f, 0.28f, 1.4f), PresentationPrefabRole.Projectile);
                    AddPrimitive(root, "gallery_projectile_power", "Power red shot", new Vector3(-6.4f, 0.28f, 1.4f), Vector3.one * 0.32f, MaterialRole.ProjectilePower, PrimitiveType.Sphere);
                    AddArt(root, "gallery_projectile_enemy", "Enemy projectile", new Vector3(-4.8f, 0.28f, 1.4f), PresentationPrefabRole.EnemyProjectile);
                    AddPrimitive(root, "gallery_shield_guard", "Shield guard", new Vector3(-2.6f, 0.5f, 1.4f), new Vector3(0.08f, 1f, 1.4f), MaterialRole.ShieldGuard);
                    AddPrimitive(root, "gallery_shield_parry", "Shield parry", new Vector3(-0.8f, 0.5f, 1.4f), new Vector3(0.08f, 1f, 1.4f), MaterialRole.ShieldParry);
                    AddArt(root, "gallery_vfx_explosion", "Explosion cue", new Vector3(1.4f, 0.24f, 1.4f), PresentationPrefabRole.VfxChestOpen);
                    AddArt(root, "gallery_vfx_pickup", "Pickup VFX", new Vector3(3.8f, 0.24f, 1.4f), PresentationPrefabRole.VfxRewardClaim);
                    break;
                case 6:
                    AddLabelMarker(root, "gallery_hazard_note", "Live hazard/physics lane\nSpikes, pits, barrels, explosive chains, and coin drops are authored in the room shell.", new Vector3(0f, 0.25f, -2.4f), 0.11f);
                    break;
                case 7:
                    AddArt(root, "gallery_boss_key", "Boss key", new Vector3(-8f, 0.38f, 1.4f), PresentationPrefabRole.BossKeyPickup);
                    AddArt(root, "gallery_shop", "Shop stand/card", new Vector3(-5.2f, 0.48f, 1.4f), PresentationPrefabRole.HubShop);
                    AddArt(root, "gallery_hub_portal", "Hub portal", new Vector3(-2f, 0.6f, 1.4f), PresentationPrefabRole.HubReturnPortal);
                    AddArt(root, "gallery_branch_portal", "Branch portal", new Vector3(1f, 0.6f, 1.4f), PresentationPrefabRole.NextBranchPortal);
                    AddArt(root, "gallery_defeated_portal", "Defeated portal", new Vector3(4f, 0.6f, 1.4f), PresentationPrefabRole.DoorUnavailable);
                    AddArt(root, "gallery_final_portal", "Final portal", new Vector3(7f, 0.6f, 1.4f), PresentationPrefabRole.SecretDoorDebug);
                    break;
                case 8:
                    AddBossMarkers(root, BossWorldBand.World1);
                    break;
                case 9:
                    AddBossMarkers(root, BossWorldBand.World2);
                    break;
                case 10:
                    AddBossMarkers(root, BossWorldBand.World3);
                    break;
            }
        }

        private static void AddBuildPickupMarkers(Transform root)
        {
            for (var index = 0; index < BuildPickupLabels.Length; index++)
            {
                var pickupId = BuildPickupLabels[index];
                var x = -10f + (index % 6) * 4f;
                var z = index < 6 ? 1.9f : index < 12 ? 0f : -1.9f;
                AddArt(root, $"gallery_pickup_{pickupId}", pickupId, new Vector3(x, 0.32f, z), RoleForBuildPickup(pickupId), pickupId: pickupId);
            }
        }

        private static void AddBossMarkers(Transform root, BossWorldBand band)
        {
            var bosses = BossCatalogDefinition.CreateRuntimeRoster()
                .Where(boss => boss.WorldBand == band)
                .OrderBy(boss => boss.BossId)
                .ToArray();
            for (var index = 0; index < bosses.Length; index++)
            {
                AddMarker(
                    root,
                    $"gallery_boss_{bosses[index].BossId}",
                    DeveloperLabContentCategory.Boss,
                    bosses[index].DisplayName,
                    new Vector3(-7f + index * 5f, 0.42f, 0.6f),
                    presentationRole: PresentationPrefabRole.EnemyBoss,
                    bossId: bosses[index].BossId,
                    spawnMode: InspectionEntityMode.FrozenRuntime,
                    labelOffset: new Vector3(0f, 1.9f, 0f),
                    labelColor: new Color(1f, 0.86f, 0.62f));
            }
        }

        private static void AddArt(Transform root, string id, string label, Vector3 position, PresentationPrefabRole role, string pickupId = "")
        {
            AddMarker(root, id, DeveloperLabContentCategory.ArtPassDisplay, label, position, presentationRole: role, pickupId: pickupId);
        }

        private static void AddPrimitive(Transform root, string id, string label, Vector3 position, Vector3 scale, MaterialRole materialRole, PrimitiveType primitiveType = PrimitiveType.Cube)
        {
            AddMarker(root, id, DeveloperLabContentCategory.PrimitiveDisplay, label, position, visualScale: scale, materialRole: materialRole, primitiveType: primitiveType);
        }

        private static void AddCoin(Transform root, string id, string label, Vector3 position, string denomination)
        {
            AddMarker(root, id, DeveloperLabContentCategory.Coin, label, position, coinDenomination: denomination, presentationRole: denomination == "Gold" ? PresentationPrefabRole.CoinGold : denomination == "Silver" ? PresentationPrefabRole.CoinSilver : PresentationPrefabRole.CoinCopper);
        }

        private static void AddChest(Transform root, string id, string label, Vector3 position, string kind)
        {
            AddMarker(root, id, DeveloperLabContentCategory.Chest, label, position, chestKind: kind, presentationRole: kind == "Golden" ? PresentationPrefabRole.ChestGolden : PresentationPrefabRole.ChestNormal);
        }

        private static void AddEnemy(Transform root, string id, string spawnKind, Vector3 position)
        {
            AddMarker(root, id, DeveloperLabContentCategory.Enemy, spawnKind, position, presentationRole: RoleForEnemy(spawnKind), enemyKind: spawnKind, spawnMode: InspectionEntityMode.FrozenRuntime, labelOffset: new Vector3(0f, 1.35f, 0f));
        }

        private static void AddLabelMarker(Transform root, string id, string label, Vector3 position, float labelScale)
        {
            AddMarker(root, id, DeveloperLabContentCategory.Label, label, position, includeLabel: false, labelScale: labelScale);
        }

        private static DeveloperLabSceneMarker AddMarker(
            Transform root,
            string id,
            DeveloperLabContentCategory category,
            string label,
            Vector3 position,
            Vector3? visualScale = null,
            PresentationPrefabRole presentationRole = PresentationPrefabRole.RewardPickup,
            MaterialRole materialRole = MaterialRole.RewardPickup,
            PrimitiveType primitiveType = PrimitiveType.Cube,
            string cellKind = "",
            string markerKind = "",
            string pickupId = "",
            string enemyKind = "",
            string bossId = "",
            string chestKind = "",
            string coinDenomination = "",
            InspectionEntityMode spawnMode = InspectionEntityMode.FrozenRuntime,
            bool exportToRuntime = false,
            bool includeInGallery = true,
            bool includeLabel = true,
            Vector3? labelOffset = null,
            float labelScale = 0.065f,
            Color? labelColor = null,
            string doorDirection = "",
            int doorLaneIndex = 0,
            int hostCellX = 0,
            int hostCellZ = 0,
            string doorState = "door")
        {
            var markerObject = new GameObject($"Marker.{id}");
            markerObject.transform.SetParent(root, false);
            markerObject.transform.localPosition = position;
            var marker = markerObject.AddComponent<DeveloperLabSceneMarker>();
            marker.Configure(
                id,
                category,
                label,
                visualScale ?? Vector3.one,
                presentationRole,
                materialRole,
                primitiveType,
                cellKind,
                markerKind,
                pickupId,
                enemyKind,
                bossId,
                chestKind,
                coinDenomination,
                spawnMode,
                exportToRuntime,
                includeInGallery,
                includeLabel,
                labelOffset ?? new Vector3(0f, 0.92f, 0f),
                labelScale,
                labelColor ?? Color.white,
                doorDirection,
                doorLaneIndex,
                hostCellX,
                hostCellZ,
                doorState);
            AddPreviewForMarker(marker);
            return marker;
        }

        private static void AddPreviewForMarker(DeveloperLabSceneMarker marker)
        {
            if (marker == null)
            {
                return;
            }

            switch (marker.Category)
            {
                case DeveloperLabContentCategory.ArtPassDisplay:
                case DeveloperLabContentCategory.Coin:
                case DeveloperLabContentCategory.Chest:
                    var visual = PresentationPrefabResolver.InstantiateVisual(marker.PresentationRole, marker.transform, Vector3.zero, marker.VisualScale);
                    if (visual != null)
                    {
                        visual.hideFlags = HideFlags.None;
                        if (marker.Category == DeveloperLabContentCategory.Chest)
                        {
                            var targetSize = marker.ChestKind == "Golden"
                                ? new Vector3(0.88f, 0.58f, 0.7f)
                                : new Vector3(0.78f, 0.52f, 0.64f);
                            PresentationVisualBoundsFitter.FitToTargetBounds(visual.transform, targetSize, -marker.transform.localPosition.y);
                        }
                    }

                    break;
                case DeveloperLabContentCategory.PrimitiveDisplay:
                case DeveloperLabContentCategory.RoomCell:
                case DeveloperLabContentCategory.RoomMarker:
                case DeveloperLabContentCategory.DoorPort:
                    var primitive = GameObject.CreatePrimitive(marker.PrimitiveType);
                    primitive.name = "PreviewPrimitive";
                    primitive.transform.SetParent(marker.transform, false);
                    primitive.transform.localScale = marker.VisualScale == Vector3.one && marker.Category == DeveloperLabContentCategory.DoorPort
                        ? new Vector3(0.8f, 1f, 0.12f)
                        : marker.VisualScale == Vector3.one && marker.Category != DeveloperLabContentCategory.PrimitiveDisplay
                            ? Vector3.one * 0.25f
                            : marker.VisualScale;
                    MaterialResolver.ApplyTo(primitive, marker.MaterialRole);
                    break;
            }

            if (marker.IncludeLabel || marker.Category == DeveloperLabContentCategory.Label)
            {
                AddPreviewLabel(marker.transform, marker.Label, marker.Category == DeveloperLabContentCategory.Label ? Vector3.zero : marker.LabelOffset, marker.LabelScale, marker.LabelColor);
            }
        }

        private static TextMesh AddPreviewLabel(Transform parent, string text, Vector3 localPosition, float scale, Color? color = null)
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
            mesh.color = color ?? Color.white;
            return mesh;
        }

        private static void CreateCameraAndLight()
        {
            var cameraObject = new GameObject("AuthoringCamera", typeof(Camera));
            cameraObject.transform.position = new Vector3(0f, 10f, -9f);
            cameraObject.transform.rotation = Quaternion.Euler(58f, 0f, 0f);
            cameraObject.GetComponent<Camera>().orthographic = false;

            var lightObject = new GameObject("AuthoringLight", typeof(Light));
            lightObject.transform.position = new Vector3(0f, 7f, -3f);
            lightObject.transform.rotation = Quaternion.Euler(55f, -30f, 0f);
            lightObject.GetComponent<Light>().type = LightType.Directional;
        }

        public static PresentationPrefabRole RoleForBuildPickup(string pickupId)
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

        private static PresentationPrefabRole RoleForEnemy(string spawnKind)
        {
            return spawnKind switch
            {
                "spawnEnemyFlying" => PresentationPrefabRole.EnemyFlying,
                "spawnEnemyFast" => PresentationPrefabRole.EnemyFast,
                "spawnEnemyHeavy" => PresentationPrefabRole.EnemyHeavy,
                "spawnEnemyCharger" => PresentationPrefabRole.EnemyCharger,
                "spawnEnemyTurret" => PresentationPrefabRole.EnemyTurret,
                "spawnEnemySplitter" => PresentationPrefabRole.EnemySplitter,
                "spawnEnemySpittingPod" => PresentationPrefabRole.EnemySpittingPod,
                "spawnEnemyRat" => PresentationPrefabRole.EnemyRat,
                "spawnEnemySpider" => PresentationPrefabRole.EnemySpider,
                _ => PresentationPrefabRole.EnemyNormal
            };
        }

        private static string Slug(string value)
        {
            return new string((value ?? string.Empty)
                .Where(char.IsLetterOrDigit)
                .ToArray());
        }

        private static readonly string[] EnemySpawnKinds =
        {
            RoomDesignerMarkerKinds.EnemyNormal,
            RoomDesignerMarkerKinds.EnemyFlying,
            RoomDesignerMarkerKinds.EnemyFast,
            RoomDesignerMarkerKinds.EnemyHeavy,
            RoomDesignerMarkerKinds.EnemyCharger,
            RoomDesignerMarkerKinds.EnemyTurret,
            RoomDesignerMarkerKinds.EnemySplitter,
            RoomDesignerMarkerKinds.EnemySpittingPod,
            RoomDesignerMarkerKinds.EnemyRat,
            RoomDesignerMarkerKinds.EnemySpider
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
    }
}
