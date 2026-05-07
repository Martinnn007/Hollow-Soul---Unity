using Hollow.Data.Definitions;
using Hollow.Presentation;
using UnityEngine;

namespace Hollow.RoomDesigner
{
    public static class RoomDesignerScenePreviewBuilder
    {
        public static bool BuildVisualForCell(GameObject host, RoomDesignerCell cell)
        {
            if (host == null || cell == null)
            {
                return false;
            }

            return cell.kind switch
            {
                RoomDesignerCellKinds.Ground => AttachBoundVisual(host, PresentationPrefabRole.RoomFloor),
                RoomDesignerCellKinds.Rock => AttachBoundVisual(host, PresentationPrefabRole.RoomObstacleRock),
                RoomDesignerCellKinds.Spike => AttachBoundVisual(host, PresentationPrefabRole.RoomHazardSpike),
                RoomDesignerCellKinds.Hole => true,
                _ => false
            };
        }

        public static bool BuildVisualForDoor(GameObject host, RoomDesignerDoorPortState door)
        {
            if (host == null || door == null)
            {
                return false;
            }

            var role = door.state switch
            {
                RoomDesignerDoorKinds.Door => PresentationPrefabRole.DoorActive,
                RoomDesignerDoorKinds.Secret => PresentationPrefabRole.SecretDoorDebug,
                _ => PresentationPrefabRole.DoorUnavailable
            };
            return AttachBoundVisual(host, role);
        }

        public static bool BuildVisualForMarker(GameObject host, RoomDesignerMarker marker)
        {
            if (host == null || marker == null)
            {
                return false;
            }

            var attached = AttachBoundVisual(host, PrefabRoleForMarker(marker.kind));
            if (attached)
            {
                host.transform.localScale = Vector3.one;
            }

            return attached;
        }

        public static PresentationPrefabRole PrefabRoleForMarker(string markerKind)
        {
            return markerKind switch
            {
                RoomDesignerMarkerKinds.SafeStart => PresentationPrefabRole.Player,
                RoomDesignerMarkerKinds.RoomReward => PresentationPrefabRole.RewardPickup,
                RoomDesignerMarkerKinds.ChestSpawn => PresentationPrefabRole.ChestNormal,
                RoomDesignerMarkerKinds.StandardBarrel => PresentationPrefabRole.StandardBarrel,
                RoomDesignerMarkerKinds.ExplosiveBarrel => PresentationPrefabRole.ExplosiveBarrel,
                RoomDesignerMarkerKinds.EnemyFlying => PresentationPrefabRole.EnemyFlying,
                RoomDesignerMarkerKinds.EnemyFast => PresentationPrefabRole.EnemyFast,
                RoomDesignerMarkerKinds.EnemyHeavy => PresentationPrefabRole.EnemyHeavy,
                RoomDesignerMarkerKinds.EnemyCharger => PresentationPrefabRole.EnemyCharger,
                RoomDesignerMarkerKinds.EnemyTurret => PresentationPrefabRole.EnemyTurret,
                RoomDesignerMarkerKinds.EnemySplitter => PresentationPrefabRole.EnemySplitter,
                RoomDesignerMarkerKinds.EnemySpittingPod => PresentationPrefabRole.EnemySpittingPod,
                RoomDesignerMarkerKinds.EnemyRat => PresentationPrefabRole.EnemyRat,
                RoomDesignerMarkerKinds.EnemySpider => PresentationPrefabRole.EnemySpider,
                RoomDesignerMarkerKinds.EnemyHollowBird => PresentationPrefabRole.EnemyHollowBird,
                RoomDesignerMarkerKinds.EnemyHollowBeast => PresentationPrefabRole.EnemyHollowBeast,
                RoomDesignerMarkerKinds.EnemySkeletonSword => PresentationPrefabRole.EnemySkeletonSword,
                RoomDesignerMarkerKinds.EnemySkeletonSpear => PresentationPrefabRole.EnemySkeletonSpear,
                RoomDesignerMarkerKinds.EnemyKnight => PresentationPrefabRole.EnemyKnight,
                RoomDesignerMarkerKinds.EnemyGiant => PresentationPrefabRole.EnemyGiant,
                RoomDesignerMarkerKinds.EnemyHollowArcher => PresentationPrefabRole.EnemyHollowArcher,
                RoomDesignerMarkerKinds.EnemyPowderGunner => PresentationPrefabRole.EnemyPowderGunner,
                RoomDesignerMarkerKinds.EnemyKnifeThrower => PresentationPrefabRole.EnemyKnifeThrower,
                RoomDesignerMarkerKinds.EnemyRepeaterTurret => PresentationPrefabRole.EnemyRepeaterTurret,
                RoomDesignerMarkerKinds.EnemyClockworkSentry => PresentationPrefabRole.EnemyClockworkSentry,
                RoomDesignerMarkerKinds.EnemyStarforgedOctantSentry => PresentationPrefabRole.EnemyStarforgedOctantSentry,
                RoomDesignerMarkerKinds.EnemyCrimsonRailSpider => PresentationPrefabRole.EnemyCrimsonRailSpider,
                RoomDesignerMarkerKinds.EnemyAzureMinigunTurret => PresentationPrefabRole.EnemyAzureMinigunTurret,
                RoomDesignerMarkerKinds.EnemyHollowAcolyte => PresentationPrefabRole.EnemyHollowAcolyte,
                RoomDesignerMarkerKinds.EnemyWraith => PresentationPrefabRole.EnemyWraith,
                RoomDesignerMarkerKinds.EnemySoulEater => PresentationPrefabRole.EnemySoulEater,
                RoomDesignerMarkerKinds.EnemyCurseBinder => PresentationPrefabRole.EnemyCurseBinder,
                RoomDesignerMarkerKinds.EnemyGraveLantern => PresentationPrefabRole.EnemyGraveLantern,
                _ => PresentationPrefabRole.EnemyNormal
            };
        }

        private static bool AttachBoundVisual(GameObject host, PresentationPrefabRole role)
        {
            if (!HasConfiguredPrefab(role))
            {
                return false;
            }

            var visual = PresentationPrefabResolver.InstantiateVisual(role, host.transform, Vector3.zero, Vector3.one);
            if (visual == null)
            {
                return false;
            }

            var renderer = host.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.enabled = false;
            }

            return true;
        }

        private static bool HasConfiguredPrefab(PresentationPrefabRole role)
        {
            var catalog = PresentationContentProvider.ActiveCatalog;
            if (catalog == null)
            {
                catalog = Resources.Load<PresentationContentCatalog>(PresentationContentProvider.DefaultCatalogResourcePath);
                if (catalog != null)
                {
                    PresentationContentProvider.Configure(catalog);
                }
            }

            return catalog != null && catalog.TryGetPrefab(role, out var prefab) && prefab != null;
        }
    }
}
