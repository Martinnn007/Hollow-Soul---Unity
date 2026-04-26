using System;
using System.Collections.Generic;
using UnityEngine;

namespace Hollow.Rooms
{
    [Serializable]
    public sealed class ImportedHollowRoomManifest
    {
        public ImportedHollowRuntime hollowRuntime;
    }

    [Serializable]
    public sealed class ImportedHollowRuntime
    {
        public int schemaVersion;
        public string sourceProjectId;
        public string canonicalRoomId;
        public string displayName;
        public string roomType;
        public string rewardType;
        public string prototypeStatus;
        public float tileSizeMeters = 1f;
        public ImportedRoomDimensions dimensions;
        public ImportedRoomFootprint footprint;
        public List<ImportedGridPosition> walkableTiles = new();
        public List<ImportedGridPosition> holeTiles = new();
        public List<ImportedRoomFloorRegion> floorRegions = new();
        public List<ImportedRoomDoorPort> doorPorts = new();
        public ImportedVector3 playerSafeStart;
        public List<ImportedSpawnPoint> enemySpawns = new();
        public List<ImportedSpawnPoint> itemSpawns = new();
        public List<ImportedRoomObstacle> obstacles = new();
        public List<ImportedRoomDecor> decor = new();
    }

    [Serializable]
    public sealed class ImportedRoomDimensions
    {
        public int widthTiles;
        public int heightTiles;
        public ImportedRoomBounds bounds;
    }

    [Serializable]
    public sealed class ImportedRoomBounds
    {
        public float minX;
        public float maxX;
        public float minZ;
        public float maxZ;
    }

    [Serializable]
    public sealed class ImportedRoomFootprint
    {
        public ImportedGridPosition primaryCell;
        public List<ImportedGridPosition> occupiedBranchCells = new();
        public ImportedChunkBasis chunkBasisTiles;
    }

    [Serializable]
    public sealed class ImportedChunkBasis
    {
        public int width;
        public int height;
    }

    [Serializable]
    public sealed class ImportedGridPosition
    {
        public int x;
        public int z;
    }

    [Serializable]
    public sealed class ImportedVector3
    {
        public float x;
        public float y;
        public float z;

        public Vector3 ToUnityVector3()
        {
            return new Vector3(x, y, z);
        }
    }

    [Serializable]
    public sealed class ImportedRoomFloorRegion
    {
        public string id;
        public ImportedVector3 center;
        public ImportedHalfSize halfSize;
    }

    [Serializable]
    public sealed class ImportedHalfSize
    {
        public float x;
        public float z;
    }

    [Serializable]
    public sealed class ImportedRoomDoorPort
    {
        public string id;
        public string direction;
        public int laneIndex;
        public ImportedGridPosition hostCell;
        public ImportedEdgeCenter gridEdgeCenter;
        public ImportedVector3 positionMeters;
        public string kind;
    }

    [Serializable]
    public sealed class ImportedEdgeCenter
    {
        public float x;
        public float z;
    }

    [Serializable]
    public sealed class ImportedRoomObstacle
    {
        public string id;
        public string kind;
        public ImportedVector3 center;
        public ImportedVector3 size;
        public bool blocksProjectiles;
    }

    [Serializable]
    public sealed class ImportedSpawnPoint
    {
        public string id;
        public string kind;
        public ImportedVector3 position;
    }

    [Serializable]
    public sealed class ImportedRoomDecor
    {
        public string id;
        public string kind;
        public ImportedVector3 center;
        public ImportedVector3 size;
        public bool blocking;
        public bool blocksProjectiles;
    }

    public sealed class ImportedRoomRuntimeAsset
    {
        public ImportedRoomRuntimeAsset(
            string id,
            string displayName,
            RoomLayout layout,
            IReadOnlyList<RoomDoorPort> doorPorts,
            IReadOnlyList<ImportedSpawnPoint> enemySpawns,
            IReadOnlyList<ImportedSpawnPoint> itemSpawns,
            ImportedSpawnPoint safeStart,
            IReadOnlyList<ImportedRoomDecor> decor,
            ImportedHollowRoomManifest sourceManifest)
        {
            Id = id;
            DisplayName = displayName;
            Layout = layout;
            DoorPorts = doorPorts;
            EnemySpawns = enemySpawns;
            ItemSpawns = itemSpawns;
            SafeStart = safeStart;
            Decor = decor;
            SourceManifest = sourceManifest;
        }

        public string Id { get; }

        public string DisplayName { get; }

        public RoomLayout Layout { get; }

        public IReadOnlyList<RoomDoorPort> DoorPorts { get; }

        public IReadOnlyList<ImportedSpawnPoint> EnemySpawns { get; }

        public IReadOnlyList<ImportedSpawnPoint> ItemSpawns { get; }

        public ImportedSpawnPoint SafeStart { get; }

        public IReadOnlyList<ImportedRoomDecor> Decor { get; }

        public ImportedHollowRoomManifest SourceManifest { get; }
    }
}
