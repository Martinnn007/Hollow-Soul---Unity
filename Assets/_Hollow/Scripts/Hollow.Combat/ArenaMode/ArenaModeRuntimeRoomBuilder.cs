using System.Collections.Generic;
using System.Linq;
using Hollow.Rooms;
using UnityEngine;

namespace Hollow.Combat
{
    public static class ArenaModeRuntimeRoomBuilder
    {
        public static ImportedRoomRuntimeAsset BuildRoom(ArenaModeRuntimeSettings settings)
        {
            settings ??= new ArenaModeRuntimeSettings();
            settings.EnsurePlayableDefaults();
            var size = SizeFor(settings.RoomSize);
            var halfWidth = size.x * 0.5f;
            var halfDepth = size.y * 0.5f;
            var bounds = Rect.MinMaxRect(-halfWidth, -halfDepth, halfWidth, halfDepth);
            var floorRegions = new[]
            {
                new RoomLayoutFloorRegion("arena_floor", Vector3.zero, new Vector2(halfWidth, halfDepth))
            };
            var obstacles = BuildObstacles(settings.LayoutStyle, settings.ObstaclePreset, bounds);
            var walkableTiles = BuildWalkableTiles(bounds);
            var layout = new RoomLayout(
                Mathf.RoundToInt(size.x),
                Mathf.RoundToInt(size.y),
                bounds,
                walkableTiles,
                System.Array.Empty<Vector2Int>(),
                floorRegions,
                obstacles);

            return new ImportedRoomRuntimeAsset(
                $"arena_{settings.PresetId}",
                settings.DisplayName,
                layout,
                new RoomInstanceFootprint(Vector2Int.zero, new[] { Vector2Int.zero }, new Vector2Int(Mathf.RoundToInt(size.x), Mathf.RoundToInt(size.y))),
                System.Array.Empty<RoomDoorPort>(),
                System.Array.Empty<ImportedSpawnPoint>(),
                System.Array.Empty<ImportedSpawnPoint>(),
                new ImportedSpawnPoint
                {
                    id = "arena_safe_start",
                    kind = "spawn_point_safeStart",
                    position = ToImported(Vector3.zero)
                },
                BuildHazards(settings.ObstaclePreset, bounds),
                System.Array.Empty<ImportedRoomInteractiveObject>(),
                System.Array.Empty<ImportedRoomDecor>(),
                null);
        }

        public static IReadOnlyList<ImportedSpawnPoint> BuildSpawnPoints(
            ArenaModeRuntimeSettings settings,
            IReadOnlyList<ArenaModeEnemyGroupDefinition> groups,
            int waveIndex)
        {
            settings ??= new ArenaModeRuntimeSettings();
            settings.EnsurePlayableDefaults();
            var bounds = BoundsFor(settings.RoomSize);
            var obstacles = BuildObstacles(settings.LayoutStyle, settings.ObstaclePreset, bounds);
            var result = new List<ImportedSpawnPoint>();
            var groupOffset = 0;
            foreach (var group in groups ?? System.Array.Empty<ArenaModeEnemyGroupDefinition>())
            {
                if (group == null)
                {
                    continue;
                }

                for (var index = 0; index < group.Count; index++)
                {
                    var position = PositionFor(group, index, groupOffset, waveIndex, bounds);
                    position = NudgeAwayFromCenter(position);
                    position = ClampInside(position, bounds, 0.65f);
                    position = AvoidObstacles(position, obstacles, bounds);
                    result.Add(new ImportedSpawnPoint
                    {
                        id = $"arena_wave{waveIndex:00}_{result.Count:00}",
                        kind = group.SpawnKind,
                        position = ToImported(position)
                    });
                }

                groupOffset++;
            }

            return result;
        }

        public static IReadOnlyList<ImportedSpawnPoint> BuildCuratedSpawnPoints(
            ImportedRoomRuntimeAsset roomAsset,
            IReadOnlyList<ArenaModeEnemyGroupDefinition> groups,
            int waveIndex)
        {
            var anchors = (roomAsset?.EnemySpawns ?? System.Array.Empty<ImportedSpawnPoint>())
                .Where(spawn => spawn?.position != null)
                .OrderBy(spawn => spawn.id)
                .ToArray();
            if (anchors.Length == 0)
            {
                return System.Array.Empty<ImportedSpawnPoint>();
            }

            var result = new List<ImportedSpawnPoint>();
            var bounds = roomAsset.Layout?.Bounds ?? Rect.MinMaxRect(-6f, -4f, 6f, 4f);
            var obstacles = roomAsset.Layout?.Obstacles ?? System.Array.Empty<RoomLayoutObstacle>();
            foreach (var group in groups ?? System.Array.Empty<ArenaModeEnemyGroupDefinition>())
            {
                if (group == null)
                {
                    continue;
                }

                for (var index = 0; index < group.Count; index++)
                {
                    var anchorIndex = (waveIndex * 5 + result.Count) % anchors.Length;
                    var position = anchors[anchorIndex].position.ToUnityVector3();
                    if (result.Count >= anchors.Length)
                    {
                        var offsetAngle = GoldenAngle(result.Count + waveIndex * 13);
                        var offsetRadius = 0.32f + 0.12f * ((result.Count / anchors.Length) % 3);
                        position += new Vector3(Mathf.Cos(offsetAngle) * offsetRadius, 0f, Mathf.Sin(offsetAngle) * offsetRadius);
                    }

                    position = ClampInside(position, bounds, 0.45f);
                    position = AvoidObstacles(position, obstacles, bounds);
                    result.Add(new ImportedSpawnPoint
                    {
                        id = $"arena_curated_wave{waveIndex:00}_{result.Count:00}",
                        kind = group.SpawnKind,
                        position = ToImported(position)
                    });
                }
            }

            return result;
        }

        public static IReadOnlyList<string> SpawnKindsFor(IReadOnlyList<ImportedSpawnPoint> spawnPoints)
        {
            return (spawnPoints ?? System.Array.Empty<ImportedSpawnPoint>())
                .Select(spawn => string.IsNullOrWhiteSpace(spawn.kind) ? "spawnEnemyNormal" : spawn.kind)
                .ToArray();
        }

        public static Vector2 SizeFor(ArenaRoomSize size)
        {
            return size switch
            {
                ArenaRoomSize.Small => new Vector2(12f, 8f),
                ArenaRoomSize.Large => new Vector2(22f, 14f),
                ArenaRoomSize.Grand => new Vector2(28f, 18f),
                _ => new Vector2(16f, 10f)
            };
        }

        public static Rect BoundsFor(ArenaRoomSize size)
        {
            var meters = SizeFor(size);
            return Rect.MinMaxRect(-meters.x * 0.5f, -meters.y * 0.5f, meters.x * 0.5f, meters.y * 0.5f);
        }

        private static IReadOnlyList<Vector2Int> BuildWalkableTiles(Rect bounds)
        {
            var tiles = new List<Vector2Int>();
            var minX = Mathf.CeilToInt(bounds.xMin + 0.5f);
            var maxX = Mathf.FloorToInt(bounds.xMax - 0.5f);
            var minZ = Mathf.CeilToInt(bounds.yMin + 0.5f);
            var maxZ = Mathf.FloorToInt(bounds.yMax - 0.5f);
            for (var z = minZ; z <= maxZ; z++)
            {
                for (var x = minX; x <= maxX; x++)
                {
                    tiles.Add(new Vector2Int(x, z));
                }
            }

            return tiles;
        }

        private static IReadOnlyList<RoomLayoutObstacle> BuildObstacles(ArenaLayoutStyle layout, ArenaObstaclePreset preset, Rect bounds)
        {
            var obstacles = new List<RoomLayoutObstacle>();
            if (preset == ArenaObstaclePreset.None || layout == ArenaLayoutStyle.Open)
            {
                return obstacles;
            }

            void Add(string id, float x, float z, float sx = 1f, float sz = 1f)
            {
                obstacles.Add(new RoomLayoutObstacle(id, "rock", new Vector3(x, 0.5f, z), new Vector3(sx, 1f, sz), true));
            }

            var x = Mathf.Min(4.5f, bounds.width * 0.28f);
            var z = Mathf.Min(3.2f, bounds.height * 0.28f);
            if (preset is ArenaObstaclePreset.LightCover or ArenaObstaclePreset.RockField)
            {
                Add("arena_rock_nw", -x, z);
                Add("arena_rock_ne", x, z);
                Add("arena_rock_sw", -x, -z);
                Add("arena_rock_se", x, -z);
            }

            if (preset is ArenaObstaclePreset.Pillars or ArenaObstaclePreset.RockField)
            {
                Add("arena_pillar_w", -x * 0.55f, 0f, 1.1f, 1.1f);
                Add("arena_pillar_e", x * 0.55f, 0f, 1.1f, 1.1f);
            }

            if (layout is ArenaLayoutStyle.Lanes or ArenaLayoutStyle.Scramble)
            {
                Add("arena_lane_n", 0f, z * 0.62f, Mathf.Min(3f, bounds.width * 0.18f), 0.85f);
                Add("arena_lane_s", 0f, -z * 0.62f, Mathf.Min(3f, bounds.width * 0.18f), 0.85f);
            }

            return obstacles;
        }

        private static IReadOnlyList<ImportedRoomHazard> BuildHazards(ArenaObstaclePreset preset, Rect bounds)
        {
            if (preset != ArenaObstaclePreset.HazardLanes)
            {
                return System.Array.Empty<ImportedRoomHazard>();
            }

            var hazards = new List<ImportedRoomHazard>();
            var z = Mathf.Min(2.25f, bounds.height * 0.23f);
            for (var index = -2; index <= 2; index++)
            {
                hazards.Add(new ImportedRoomHazard
                {
                    id = $"arena_spike_n_{index + 2}",
                    kind = RoomHazardKind.Spike,
                    center = ToImported(new Vector3(index * 1.25f, 0f, z)),
                    radius = 0.45f
                });
                hazards.Add(new ImportedRoomHazard
                {
                    id = $"arena_spike_s_{index + 2}",
                    kind = RoomHazardKind.Spike,
                    center = ToImported(new Vector3(index * 1.25f, 0f, -z)),
                    radius = 0.45f
                });
            }

            return hazards;
        }

        private static Vector3 PositionFor(ArenaModeEnemyGroupDefinition group, int index, int groupOffset, int waveIndex, Rect bounds)
        {
            var count = Mathf.Max(1, group.Count);
            var angle = GoldenAngle(index + groupOffset * 7 + waveIndex * 11);
            var radius = RadiusFor(group.SpawnPattern, bounds);
            var basePosition = group.SpawnPattern switch
            {
                ArenaSpawnPattern.Corners => CornerPosition(index + groupOffset, bounds),
                ArenaSpawnPattern.EdgeLanes => EdgePosition(index + groupOffset, count, bounds),
                ArenaSpawnPattern.RangedBackline => new Vector3(Mathf.Lerp(bounds.xMin + 1.5f, bounds.xMax - 1.5f, (index + 1f) / (count + 1f)), 0f, bounds.yMax - 1.35f),
                ArenaSpawnPattern.PatrolLine => new Vector3(Mathf.Lerp(bounds.xMin + 2f, bounds.xMax - 2f, (index + 1f) / (count + 1f)), 0f, groupOffset % 2 == 0 ? bounds.yMax - 2f : bounds.yMin + 2f),
                ArenaSpawnPattern.Cluster => RingPosition(angle, Mathf.Max(2.2f, radius * 0.55f)),
                ArenaSpawnPattern.Scattered => RingPosition(angle, Mathf.Lerp(2.4f, radius, ((index % 5) + 1f) / 5f)),
                ArenaSpawnPattern.CenterRing => RingPosition(angle, Mathf.Min(3.25f, radius)),
                _ => RingPosition(angle, radius)
            };

            return basePosition + OffsetForGrouping(group.GroupingMode, index, count);
        }

        private static float RadiusFor(ArenaSpawnPattern pattern, Rect bounds)
        {
            var outer = Mathf.Min(bounds.width, bounds.height) * 0.42f;
            return pattern switch
            {
                ArenaSpawnPattern.CenterRing => Mathf.Min(3.25f, outer),
                ArenaSpawnPattern.Cluster => outer * 0.62f,
                ArenaSpawnPattern.RangedBackline => outer,
                _ => outer
            };
        }

        private static Vector3 RingPosition(float angleRadians, float radius)
        {
            return new Vector3(Mathf.Cos(angleRadians) * radius, 0f, Mathf.Sin(angleRadians) * radius);
        }

        private static Vector3 CornerPosition(int index, Rect bounds)
        {
            var margin = 1.7f;
            return (index % 4) switch
            {
                0 => new Vector3(bounds.xMin + margin, 0f, bounds.yMin + margin),
                1 => new Vector3(bounds.xMax - margin, 0f, bounds.yMin + margin),
                2 => new Vector3(bounds.xMax - margin, 0f, bounds.yMax - margin),
                _ => new Vector3(bounds.xMin + margin, 0f, bounds.yMax - margin)
            };
        }

        private static Vector3 EdgePosition(int index, int count, Rect bounds)
        {
            var t = (index + 1f) / (count + 1f);
            return (index % 4) switch
            {
                0 => new Vector3(Mathf.Lerp(bounds.xMin + 1f, bounds.xMax - 1f, t), 0f, bounds.yMax - 1.25f),
                1 => new Vector3(bounds.xMax - 1.25f, 0f, Mathf.Lerp(bounds.yMin + 1f, bounds.yMax - 1f, t)),
                2 => new Vector3(Mathf.Lerp(bounds.xMax - 1f, bounds.xMin + 1f, t), 0f, bounds.yMin + 1.25f),
                _ => new Vector3(bounds.xMin + 1.25f, 0f, Mathf.Lerp(bounds.yMax - 1f, bounds.yMin + 1f, t))
            };
        }

        private static Vector3 OffsetForGrouping(ArenaGroupingMode grouping, int index, int count)
        {
            if (count <= 1 || grouping == ArenaGroupingMode.Solo)
            {
                return Vector3.zero;
            }

            var spacing = grouping switch
            {
                ArenaGroupingMode.TightPack => 0.42f,
                ArenaGroupingMode.Pairs => 0.55f,
                ArenaGroupingMode.LoosePack => 0.82f,
                _ => 1.05f
            };
            var row = index / 4;
            var column = index % 4;
            return new Vector3((column - 1.5f) * spacing, 0f, row * spacing);
        }

        private static Vector3 AvoidObstacles(Vector3 position, IReadOnlyList<RoomLayoutObstacle> obstacles, Rect bounds)
        {
            foreach (var obstacle in obstacles)
            {
                var halfX = obstacle.Size.x * 0.5f + 0.75f;
                var halfZ = obstacle.Size.z * 0.5f + 0.75f;
                if (Mathf.Abs(position.x - obstacle.Center.x) > halfX ||
                    Mathf.Abs(position.z - obstacle.Center.z) > halfZ)
                {
                    continue;
                }

                var away = position - obstacle.Center;
                away.y = 0f;
                if (away.sqrMagnitude < 0.001f)
                {
                    away = position.sqrMagnitude > 0.001f ? position : Vector3.forward;
                }

                position += away.normalized * 1.25f;
                position = ClampInside(position, bounds, 0.65f);
            }

            return position;
        }

        private static Vector3 NudgeAwayFromCenter(Vector3 position)
        {
            if (position.sqrMagnitude >= 3.2f * 3.2f)
            {
                return position;
            }

            var direction = position.sqrMagnitude > 0.001f ? position.normalized : Vector3.forward;
            return direction * 3.2f;
        }

        private static Vector3 ClampInside(Vector3 position, Rect bounds, float margin)
        {
            position.x = Mathf.Clamp(position.x, bounds.xMin + margin, bounds.xMax - margin);
            position.z = Mathf.Clamp(position.z, bounds.yMin + margin, bounds.yMax - margin);
            return position;
        }

        private static float GoldenAngle(int index)
        {
            return index * 137.507764f * Mathf.Deg2Rad;
        }

        private static ImportedVector3 ToImported(Vector3 value)
        {
            return new ImportedVector3 { x = value.x, y = value.y, z = value.z };
        }
    }
}
