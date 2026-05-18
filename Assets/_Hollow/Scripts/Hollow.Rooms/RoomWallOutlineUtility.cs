using System.Collections.Generic;
using UnityEngine;

namespace Hollow.Rooms
{
    public static class RoomWallOutlineUtility
    {
        private const float EdgeEpsilon = 0.001f;

        public static List<RoomWallEdge> BuildExposedEdges(RoomLayout layout)
        {
            var edges = new List<RoomWallEdge>();
            var regions = BuildRegions(layout);
            for (var index = 0; index < regions.Count; index++)
            {
                var region = regions[index];
                AddExposedEdge(edges, regions, index, RoomWallSide.North, region.XMin, region.XMax, region.ZMin, horizontalOnX: true);
                AddExposedEdge(edges, regions, index, RoomWallSide.South, region.XMin, region.XMax, region.ZMax, horizontalOnX: true);
                AddExposedEdge(edges, regions, index, RoomWallSide.West, region.ZMin, region.ZMax, region.XMin, horizontalOnX: false);
                AddExposedEdge(edges, regions, index, RoomWallSide.East, region.ZMin, region.ZMax, region.XMax, horizontalOnX: false);
            }

            return edges;
        }

        private static List<RoomWallRegion> BuildRegions(RoomLayout layout)
        {
            var regions = new List<RoomWallRegion>();
            if (layout == null)
            {
                return regions;
            }

            if (layout.FloorRegions != null && layout.FloorRegions.Count > 0)
            {
                foreach (var region in layout.FloorRegions)
                {
                    if (region == null)
                    {
                        continue;
                    }

                    regions.Add(new RoomWallRegion(
                        region.Center.x - region.HalfSize.x,
                        region.Center.x + region.HalfSize.x,
                        region.Center.z - region.HalfSize.y,
                        region.Center.z + region.HalfSize.y));
                }

                return regions;
            }

            if (layout.WalkableTiles != null && layout.WalkableTiles.Count > 0)
            {
                foreach (var tile in layout.WalkableTiles)
                {
                    regions.Add(new RoomWallRegion(tile.x - 0.5f, tile.x + 0.5f, tile.y - 0.5f, tile.y + 0.5f));
                }

                return regions;
            }

            var bounds = layout.Bounds;
            regions.Add(new RoomWallRegion(bounds.xMin, bounds.xMax, bounds.yMin, bounds.yMax));
            return regions;
        }

        private static void AddExposedEdge(
            List<RoomWallEdge> edges,
            IReadOnlyList<RoomWallRegion> regions,
            int regionIndex,
            RoomWallSide side,
            float axisMin,
            float axisMax,
            float fixedCoordinate,
            bool horizontalOnX)
        {
            var exposedIntervals = new List<Vector2> { new(axisMin, axisMax) };
            var region = regions[regionIndex];
            for (var index = 0; index < regions.Count; index++)
            {
                if (index == regionIndex)
                {
                    continue;
                }

                var other = regions[index];
                if (!TryGetAdjacentOverlap(region, other, side, out var overlap))
                {
                    continue;
                }

                SubtractInterval(exposedIntervals, overlap);
            }

            foreach (var interval in exposedIntervals)
            {
                if (interval.y - interval.x <= EdgeEpsilon)
                {
                    continue;
                }

                edges.Add(new RoomWallEdge(side, interval.x, interval.y, fixedCoordinate, horizontalOnX));
            }
        }

        private static bool TryGetAdjacentOverlap(RoomWallRegion region, RoomWallRegion other, RoomWallSide side, out Vector2 overlap)
        {
            overlap = default;
            switch (side)
            {
                case RoomWallSide.North when Approximately(other.ZMax, region.ZMin):
                case RoomWallSide.South when Approximately(other.ZMin, region.ZMax):
                    return TryOverlap(region.XMin, region.XMax, other.XMin, other.XMax, out overlap);
                case RoomWallSide.West when Approximately(other.XMax, region.XMin):
                case RoomWallSide.East when Approximately(other.XMin, region.XMax):
                    return TryOverlap(region.ZMin, region.ZMax, other.ZMin, other.ZMax, out overlap);
                default:
                    return false;
            }
        }

        private static bool TryOverlap(float leftMin, float leftMax, float rightMin, float rightMax, out Vector2 overlap)
        {
            var min = Mathf.Max(leftMin, rightMin);
            var max = Mathf.Min(leftMax, rightMax);
            overlap = new Vector2(min, max);
            return max - min > EdgeEpsilon;
        }

        private static void SubtractInterval(List<Vector2> intervals, Vector2 subtraction)
        {
            for (var index = intervals.Count - 1; index >= 0; index--)
            {
                var interval = intervals[index];
                var overlapMin = Mathf.Max(interval.x, subtraction.x);
                var overlapMax = Mathf.Min(interval.y, subtraction.y);
                if (overlapMax - overlapMin <= EdgeEpsilon)
                {
                    continue;
                }

                intervals.RemoveAt(index);
                if (overlapMin - interval.x > EdgeEpsilon)
                {
                    intervals.Add(new Vector2(interval.x, overlapMin));
                }

                if (interval.y - overlapMax > EdgeEpsilon)
                {
                    intervals.Add(new Vector2(overlapMax, interval.y));
                }
            }

            intervals.Sort((left, right) => left.x.CompareTo(right.x));
        }

        private static bool Approximately(float left, float right)
        {
            return Mathf.Abs(left - right) <= EdgeEpsilon;
        }

        private readonly struct RoomWallRegion
        {
            public RoomWallRegion(float xMin, float xMax, float zMin, float zMax)
            {
                XMin = xMin;
                XMax = xMax;
                ZMin = zMin;
                ZMax = zMax;
            }

            public float XMin { get; }

            public float XMax { get; }

            public float ZMin { get; }

            public float ZMax { get; }
        }
    }

    public readonly struct RoomWallEdge
    {
        public RoomWallEdge(RoomWallSide side, float axisMin, float axisMax, float fixedCoordinate, bool horizontalOnX)
        {
            Side = side;
            AxisMin = axisMin;
            AxisMax = axisMax;
            FixedCoordinate = fixedCoordinate;
            HorizontalOnX = horizontalOnX;
        }

        public RoomWallSide Side { get; }

        public float AxisMin { get; }

        public float AxisMax { get; }

        public float FixedCoordinate { get; }

        public bool HorizontalOnX { get; }
    }
}
