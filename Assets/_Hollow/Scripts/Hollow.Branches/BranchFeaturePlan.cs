using System;
using System.Collections.Generic;
using System.Linq;

namespace Hollow.Branches
{
    public sealed class BranchFeaturePlan
    {
        public BranchFeaturePlan(
            string bossRoomId,
            string secretRoomId,
            string bossKeyRoomId,
            string bossConnectionFromRoomId,
            string bossConnectionFromPortId)
        {
            BossRoomId = bossRoomId ?? string.Empty;
            SecretRoomId = secretRoomId ?? string.Empty;
            BossKeyRoomId = bossKeyRoomId ?? string.Empty;
            BossConnectionFromRoomId = bossConnectionFromRoomId ?? string.Empty;
            BossConnectionFromPortId = bossConnectionFromPortId ?? string.Empty;
        }

        public static BranchFeaturePlan Empty { get; } = new(string.Empty, string.Empty, string.Empty, string.Empty, string.Empty);

        public string BossRoomId { get; }

        public string SecretRoomId { get; }

        public string BossKeyRoomId { get; }

        public string BossConnectionFromRoomId { get; }

        public string BossConnectionFromPortId { get; }

        public bool HasBossKeyRoom => !string.IsNullOrWhiteSpace(BossKeyRoomId);

        public bool IsBossKeyRoom(BranchRoomId roomId)
        {
            return string.Equals(roomId.Value, BossKeyRoomId, StringComparison.Ordinal);
        }

        public bool IsSecretRoom(BranchRoomId roomId)
        {
            return string.Equals(roomId.Value, SecretRoomId, StringComparison.Ordinal);
        }

        public static BranchFeaturePlan Create(BranchFloorGraph graph)
        {
            if (graph == null)
            {
                return Empty;
            }

            return Create(graph, BranchRoomDistanceMap.Create(graph));
        }

        public static BranchFeaturePlan Create(BranchFloorGraph graph, BranchRoomDistanceMap distanceMap)
        {
            if (graph == null)
            {
                return Empty;
            }

            var distances = distanceMap ?? BranchRoomDistanceMap.Create(graph);
            var bossRoom = graph.Rooms.FirstOrDefault(room => room.Role == BranchRoomRole.Boss);
            var secretRoom = graph.Rooms.FirstOrDefault(room => room.Role == BranchRoomRole.Secret);
            var bossConnection = bossRoom != null
                ? graph.Connections.FirstOrDefault(connection => connection.ToRoomId == bossRoom.Id)
                : null;
            var bossKeyRoom = graph.Rooms
                .Where(room => room.Id != BranchRoomId.Origin)
                .Where(room => room.Role == BranchRoomRole.Combat)
                .OrderByDescending(room => distances.TryGetValue(room.Id.Value, out var distance) ? distance : 0)
                .ThenBy(room => room.Id.Value, StringComparer.Ordinal)
                .FirstOrDefault();

            return new BranchFeaturePlan(
                bossRoom?.Id.Value ?? string.Empty,
                secretRoom?.Id.Value ?? string.Empty,
                bossKeyRoom?.Id.Value ?? string.Empty,
                bossConnection?.FromRoomId.Value ?? string.Empty,
                bossConnection?.FromPortId ?? string.Empty);
        }
    }
}
