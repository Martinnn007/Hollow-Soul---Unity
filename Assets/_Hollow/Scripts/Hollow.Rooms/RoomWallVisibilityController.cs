using System;
using System.Collections.Generic;
using Hollow.Data.Definitions;
using Hollow.Presentation;
using UnityEngine;

namespace Hollow.Rooms
{
    public sealed class RoomWallVisibilityController : MonoBehaviour
    {
        public const float TransparentAlpha = 0.32f;

        [SerializeField] private List<WallBinding> wallBindings = new();
        [SerializeField] private List<RoomWallSide> currentTransparentSides = new() { RoomWallSide.North };
        [SerializeField] private Rect roomLocalBounds;
        [SerializeField] private bool hasRoomLocalBounds;
        [SerializeField] private string biomeId = RoomBiomeIds.HollowThreshold;

        public IReadOnlyList<WallBinding> WallBindings => wallBindings;

        public RoomWallSide CurrentTransparentSide { get; private set; } = RoomWallSide.North;

        public IReadOnlyList<RoomWallSide> CurrentTransparentSides => currentTransparentSides;

        public string BiomeId => RoomBiomeIds.Normalize(biomeId);

        public void Configure(IEnumerable<WallBinding> nextBindings)
        {
            Configure(nextBindings, default, useRoomBounds: false, RoomBiomeIds.HollowThreshold);
        }

        public void Configure(IEnumerable<WallBinding> nextBindings, Rect nextRoomLocalBounds)
        {
            Configure(nextBindings, nextRoomLocalBounds, useRoomBounds: true, RoomBiomeIds.HollowThreshold);
        }

        public void Configure(IEnumerable<WallBinding> nextBindings, string nextBiomeId)
        {
            Configure(nextBindings, default, useRoomBounds: false, nextBiomeId);
        }

        public void Configure(IEnumerable<WallBinding> nextBindings, Rect nextRoomLocalBounds, string nextBiomeId)
        {
            Configure(nextBindings, nextRoomLocalBounds, useRoomBounds: true, nextBiomeId);
        }

        private void Configure(IEnumerable<WallBinding> nextBindings, Rect nextRoomLocalBounds, bool useRoomBounds, string nextBiomeId)
        {
            wallBindings.Clear();
            if (nextBindings != null)
            {
                wallBindings.AddRange(nextBindings);
            }

            roomLocalBounds = nextRoomLocalBounds;
            hasRoomLocalBounds = useRoomBounds;
            biomeId = RoomBiomeIds.Normalize(nextBiomeId);
            ApplyVisibility(Camera.main);
        }

        private void LateUpdate()
        {
            ApplyVisibility(Camera.main);
        }

        public RoomWallSide ApplyVisibility(Camera camera)
        {
            var sides = camera != null
                ? DetermineTransparentSides(camera)
                : new[] { RoomWallSide.North };
            ApplyVisibility(sides);
            return CurrentTransparentSide;
        }

        public IReadOnlyList<RoomWallSide> DetermineTransparentSides(Camera camera)
        {
            if (camera == null)
            {
                return new[] { RoomWallSide.North };
            }

            var localViewTowardCamera = transform.InverseTransformDirection(-camera.transform.forward);
            return DetermineTransparentSidesFromViewDirection(localViewTowardCamera);
        }

        public RoomWallSide DetermineClosestSide(Vector3 worldPosition)
        {
            if (hasRoomLocalBounds)
            {
                return DetermineClosestSideFromLocalPosition(transform.InverseTransformPoint(worldPosition));
            }

            return DetermineClosestSideFromRenderers(worldPosition);
        }

        public IReadOnlyList<RoomWallSide> DetermineTransparentSides(Vector3 worldPosition)
        {
            if (!hasRoomLocalBounds)
            {
                return DetermineTransparentSidesFromRendererFootprint(worldPosition);
            }

            return DetermineTransparentSidesFromLocalPosition(transform.InverseTransformPoint(worldPosition));
        }

        private IReadOnlyList<RoomWallSide> DetermineTransparentSidesFromRendererFootprint(Vector3 worldPosition)
        {
            if (!TryGetRendererLocalFootprint(out var footprint))
            {
                return new[] { DetermineClosestSideFromRenderers(worldPosition) };
            }

            var localPosition = transform.InverseTransformPoint(worldPosition);
            return DetermineTransparentSidesFromBounds(localPosition, footprint);
        }

        public IReadOnlyList<RoomWallSide> DetermineTransparentSidesFromLocalPosition(Vector3 localPosition)
        {
            return DetermineTransparentSidesFromBounds(localPosition, roomLocalBounds);
        }

        private static IReadOnlyList<RoomWallSide> DetermineTransparentSidesFromViewDirection(Vector3 localViewTowardCamera)
        {
            var planar = new Vector2(localViewTowardCamera.x, localViewTowardCamera.z);
            if (planar.sqrMagnitude < 0.0001f)
            {
                return new[] { RoomWallSide.North };
            }

            planar.Normalize();
            const float axisThreshold = 0.25f;
            var sides = new List<RoomWallSide>(2);
            if (Mathf.Abs(planar.y) >= axisThreshold)
            {
                sides.Add(planar.y < 0f ? RoomWallSide.North : RoomWallSide.South);
            }

            if (Mathf.Abs(planar.x) >= axisThreshold)
            {
                sides.Add(planar.x < 0f ? RoomWallSide.West : RoomWallSide.East);
            }

            if (sides.Count == 0)
            {
                sides.Add(Mathf.Abs(planar.y) >= Mathf.Abs(planar.x)
                    ? planar.y < 0f ? RoomWallSide.North : RoomWallSide.South
                    : planar.x < 0f ? RoomWallSide.West : RoomWallSide.East);
            }

            if (sides.Count > 2)
            {
                sides.RemoveRange(2, sides.Count - 2);
            }

            return sides;
        }

        private static IReadOnlyList<RoomWallSide> DetermineTransparentSidesFromBounds(Vector3 localPosition, Rect bounds)
        {
            var sides = new List<RoomWallSide>(2);
            var center = bounds.center;
            var xThreshold = Mathf.Max(0.001f, bounds.width * 0.05f);
            var zThreshold = Mathf.Max(0.001f, bounds.height * 0.05f);

            if (localPosition.z < center.y - zThreshold)
            {
                sides.Add(RoomWallSide.North);
            }
            else if (localPosition.z > center.y + zThreshold)
            {
                sides.Add(RoomWallSide.South);
            }

            if (localPosition.x < center.x - xThreshold)
            {
                sides.Add(RoomWallSide.West);
            }
            else if (localPosition.x > center.x + xThreshold)
            {
                sides.Add(RoomWallSide.East);
            }

            if (sides.Count == 0)
            {
                sides.Add(DetermineClosestSideFromBounds(localPosition, bounds));
            }

            return sides;
        }

        public RoomWallSide DetermineClosestSideFromLocalPosition(Vector3 localPosition)
        {
            return DetermineClosestSideFromBounds(localPosition, roomLocalBounds);
        }

        private static RoomWallSide DetermineClosestSideFromBounds(Vector3 localPosition, Rect bounds)
        {
            if (localPosition.z < bounds.yMin ||
                localPosition.z > bounds.yMax ||
                localPosition.x < bounds.xMin ||
                localPosition.x > bounds.xMax)
            {
                return DetermineClosestOutsideSide(localPosition, bounds);
            }

            var side = RoomWallSide.North;
            var distance = Mathf.Abs(localPosition.z - bounds.yMin);

            var southDistance = Mathf.Abs(localPosition.z - bounds.yMax);
            if (southDistance < distance)
            {
                distance = southDistance;
                side = RoomWallSide.South;
            }

            var eastDistance = Mathf.Abs(localPosition.x - bounds.xMax);
            if (eastDistance < distance)
            {
                distance = eastDistance;
                side = RoomWallSide.East;
            }

            var westDistance = Mathf.Abs(localPosition.x - bounds.xMin);
            if (westDistance < distance)
            {
                side = RoomWallSide.West;
            }

            return side;
        }

        private static RoomWallSide DetermineClosestOutsideSide(Vector3 localPosition, Rect bounds)
        {
            var side = RoomWallSide.North;
            var distance = float.PositiveInfinity;
            ConsiderOutsideSide(localPosition.z, bounds.yMin, RoomWallSide.North, ref side, ref distance, outsideWhenLess: true);
            ConsiderOutsideSide(localPosition.z, bounds.yMax, RoomWallSide.South, ref side, ref distance, outsideWhenLess: false);
            ConsiderOutsideSide(localPosition.x, bounds.xMax, RoomWallSide.East, ref side, ref distance, outsideWhenLess: false);
            ConsiderOutsideSide(localPosition.x, bounds.xMin, RoomWallSide.West, ref side, ref distance, outsideWhenLess: true);
            return side;
        }

        private bool TryGetRendererLocalFootprint(out Rect footprint)
        {
            var hasBounds = false;
            var minX = float.PositiveInfinity;
            var maxX = float.NegativeInfinity;
            var minZ = float.PositiveInfinity;
            var maxZ = float.NegativeInfinity;

            foreach (var binding in wallBindings)
            {
                var renderer = binding.Renderer;
                if (renderer == null)
                {
                    continue;
                }

                var wall = renderer.transform;
                var position = wall.localPosition;
                var scale = wall.localScale;
                minX = Mathf.Min(minX, position.x - scale.x * 0.5f);
                maxX = Mathf.Max(maxX, position.x + scale.x * 0.5f);
                minZ = Mathf.Min(minZ, position.z - scale.z * 0.5f);
                maxZ = Mathf.Max(maxZ, position.z + scale.z * 0.5f);
                hasBounds = true;
            }

            if (!hasBounds)
            {
                footprint = default;
                return false;
            }

            footprint = Rect.MinMaxRect(minX, minZ, maxX, maxZ);
            return true;
        }

        private static void ConsiderOutsideSide(
            float value,
            float boundary,
            RoomWallSide candidate,
            ref RoomWallSide side,
            ref float distance,
            bool outsideWhenLess)
        {
            var outside = outsideWhenLess ? value < boundary : value > boundary;
            if (!outside)
            {
                return;
            }

            var candidateDistance = Mathf.Abs(value - boundary);
            if (candidateDistance < distance)
            {
                distance = candidateDistance;
                side = candidate;
            }
        }

        private RoomWallSide DetermineClosestSideFromRenderers(Vector3 worldPosition)
        {
            var closestSide = RoomWallSide.North;
            var closestDistance = float.PositiveInfinity;
            foreach (var binding in wallBindings)
            {
                var renderer = binding.Renderer;
                if (renderer == null)
                {
                    continue;
                }

                var closestPoint = renderer.bounds.ClosestPoint(worldPosition);
                var distance = (closestPoint - worldPosition).sqrMagnitude;
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestSide = binding.Side;
                }
            }

            return closestSide;
        }

        public void ApplyVisibility(RoomWallSide transparentSide)
        {
            ApplyVisibility(new[] { transparentSide });
        }

        public void ApplyVisibility(IEnumerable<RoomWallSide> transparentSides)
        {
            currentTransparentSides.Clear();
            if (transparentSides != null)
            {
                foreach (var side in transparentSides)
                {
                    if (!currentTransparentSides.Contains(side))
                    {
                        currentTransparentSides.Add(side);
                    }
                }
            }

            if (currentTransparentSides.Count == 0)
            {
                currentTransparentSides.Add(RoomWallSide.North);
            }

            CurrentTransparentSide = currentTransparentSides[0];
            var opaque = RoomBiomePresentationResolver.ResolveMaterial(BiomeId, MaterialRole.RoomWall);
            var transparent = RoomBiomePresentationResolver.ResolveMaterial(BiomeId, MaterialRole.RoomWallTransparent);
            foreach (var binding in wallBindings)
            {
                var renderer = binding.Renderer;
                if (renderer == null)
                {
                    continue;
                }

                renderer.sharedMaterial = currentTransparentSides.Contains(binding.Side) ? transparent : opaque;
            }
        }

        [Serializable]
        public sealed class WallBinding
        {
            [SerializeField] private RoomWallSide side;
            [SerializeField] private Renderer renderer;

            public WallBinding(RoomWallSide side, Renderer renderer)
            {
                this.side = side;
                this.renderer = renderer;
            }

            public RoomWallSide Side => side;

            public Renderer Renderer => renderer;
        }
    }

    public enum RoomWallSide
    {
        North,
        South,
        East,
        West
    }
}
