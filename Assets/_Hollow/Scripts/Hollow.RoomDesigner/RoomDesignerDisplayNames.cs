using System;

namespace Hollow.RoomDesigner
{
    public static class RoomDesignerDisplayNames
    {
        public static string ForTool(RoomDesignerTool tool)
        {
            return tool switch
            {
                RoomDesignerTool.Ground => "Ground",
                RoomDesignerTool.Hole => "Hole",
                RoomDesignerTool.Rock => "Rock",
                RoomDesignerTool.EnemySpawn => "Enemy",
                RoomDesignerTool.RewardSpawn => "Reward",
                RoomDesignerTool.ActiveDoor => "Door",
                RoomDesignerTool.SecretDoor => "Secret",
                RoomDesignerTool.Erase => "Erase",
                RoomDesignerTool.Eyedropper => "Pick",
                RoomDesignerTool.SafeStart => "Start",
                RoomDesignerTool.EnemyNormal => "Normal",
                RoomDesignerTool.EnemyFlying => "Flying",
                RoomDesignerTool.EnemyFast => "Fast",
                RoomDesignerTool.EnemyHeavy => "Heavy",
                RoomDesignerTool.EnemyCharger => "Charger",
                RoomDesignerTool.EnemyTurret => "Turret",
                RoomDesignerTool.EnemySplitter => "Splitter",
                RoomDesignerTool.EnemySpittingPod => "Spitting Pod",
                RoomDesignerTool.EnemyRat => "Rat",
                RoomDesignerTool.EnemySpider => "Spider",
                RoomDesignerTool.EnemySkeletonSword => "Sword Skel",
                RoomDesignerTool.EnemySkeletonSpear => "Spear Skel",
                RoomDesignerTool.EnemyKnight => "Knight",
                RoomDesignerTool.EnemyGiant => "Giant",
                RoomDesignerTool.InactiveDoor => "Inactive",
                RoomDesignerTool.Spike => "Spike",
                RoomDesignerTool.StandardBarrel => "Barrel",
                RoomDesignerTool.ExplosiveBarrel => "Boom",
                RoomDesignerTool.ChestSpawn => "Chest",
                _ => tool.ToString()
            };
        }

        public static string ForToolIcon(RoomDesignerTool tool)
        {
            return tool switch
            {
                RoomDesignerTool.Ground => "GRD",
                RoomDesignerTool.Hole => "HOL",
                RoomDesignerTool.Rock => "ROK",
                RoomDesignerTool.EnemySpawn => "ENM",
                RoomDesignerTool.RewardSpawn => "RWD",
                RoomDesignerTool.ActiveDoor => "DOR",
                RoomDesignerTool.SecretDoor => "SEC",
                RoomDesignerTool.Erase => "DEL",
                RoomDesignerTool.Eyedropper => "PCK",
                RoomDesignerTool.SafeStart => "STR",
                RoomDesignerTool.EnemyNormal => "NRM",
                RoomDesignerTool.EnemyFlying => "FLY",
                RoomDesignerTool.EnemyFast => "FST",
                RoomDesignerTool.EnemyHeavy => "HVY",
                RoomDesignerTool.EnemyCharger => "CHG",
                RoomDesignerTool.EnemyTurret => "TRT",
                RoomDesignerTool.EnemySplitter => "SPL",
                RoomDesignerTool.EnemySpittingPod => "POD",
                RoomDesignerTool.EnemyRat => "RAT",
                RoomDesignerTool.EnemySpider => "SPD",
                RoomDesignerTool.EnemySkeletonSword => "SWD",
                RoomDesignerTool.EnemySkeletonSpear => "SPR",
                RoomDesignerTool.EnemyKnight => "KNT",
                RoomDesignerTool.EnemyGiant => "GNT",
                RoomDesignerTool.InactiveDoor => "OFF",
                RoomDesignerTool.Spike => "SPK",
                RoomDesignerTool.StandardBarrel => "BRL",
                RoomDesignerTool.ExplosiveBarrel => "XPL",
                RoomDesignerTool.ChestSpawn => "CHS",
                _ => tool.ToString().Substring(0, Math.Min(3, tool.ToString().Length)).ToUpperInvariant()
            };
        }

        public static string ForCellKind(string kind)
        {
            return kind switch
            {
                RoomDesignerCellKinds.Ground => "Ground",
                RoomDesignerCellKinds.Hole => "Hole",
                RoomDesignerCellKinds.Rock => "Rock",
                RoomDesignerCellKinds.Spike => "Spike",
                _ => Shorten(kind)
            };
        }

        public static string ForMarkerKind(string kind)
        {
            return kind switch
            {
                RoomDesignerMarkerKinds.SafeStart => "Start",
                RoomDesignerMarkerKinds.RoomReward => "Reward",
                RoomDesignerMarkerKinds.Enemy => "Enemy",
                RoomDesignerMarkerKinds.EnemyNormal => "Normal",
                RoomDesignerMarkerKinds.EnemyFlying => "Flying",
                RoomDesignerMarkerKinds.EnemyFast => "Fast",
                RoomDesignerMarkerKinds.EnemyHeavy => "Heavy",
                RoomDesignerMarkerKinds.EnemyCharger => "Charger",
                RoomDesignerMarkerKinds.EnemyTurret => "Turret",
                RoomDesignerMarkerKinds.EnemySplitter => "Splitter",
                RoomDesignerMarkerKinds.EnemySpittingPod => "Pod",
                RoomDesignerMarkerKinds.EnemyRat => "Rat",
                RoomDesignerMarkerKinds.EnemySpider => "Spider",
                RoomDesignerMarkerKinds.EnemySkeletonSword => "Sword Skel",
                RoomDesignerMarkerKinds.EnemySkeletonSpear => "Spear Skel",
                RoomDesignerMarkerKinds.EnemyKnight => "Knight",
                RoomDesignerMarkerKinds.EnemyGiant => "Giant",
                RoomDesignerMarkerKinds.StandardBarrel => "Barrel",
                RoomDesignerMarkerKinds.ExplosiveBarrel => "Boom",
                RoomDesignerMarkerKinds.ChestSpawn => "Chest",
                _ => Shorten(kind)
            };
        }

        public static string ForDoor(RoomDesignerDoorPortState door)
        {
            if (door == null)
            {
                return "No door";
            }

            var direction = string.IsNullOrWhiteSpace(door.direction)
                ? "?"
                : door.direction.Substring(0, 1).ToUpperInvariant();
            var state = door.state switch
            {
                RoomDesignerDoorKinds.Door => "Door",
                RoomDesignerDoorKinds.Secret => "Secret",
                RoomDesignerDoorKinds.Inactive => "Off",
                RoomDesignerDoorKinds.Available => "Port",
                _ => Shorten(door.state)
            };
            return $"{direction}{door.laneIndex} {state}";
        }

        private static string Shorten(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return "Unknown";
            }

            var trimmed = value
                .Replace("spawn_point_", string.Empty)
                .Replace("spawnEnemy", string.Empty)
                .Replace("tile", string.Empty)
                .Replace("Tile", string.Empty);
            return string.IsNullOrWhiteSpace(trimmed) ? value : trimmed;
        }
    }
}
