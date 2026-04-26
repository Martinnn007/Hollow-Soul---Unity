using System.Collections.Generic;
using Hollow.Rooms;
using UnityEngine;

namespace Hollow.Branches
{
    public sealed class BranchCellOccupancyMap
    {
        private readonly Dictionary<Vector2Int, BranchRoomInstanceId> ownerByCell = new();

        public IReadOnlyDictionary<Vector2Int, BranchRoomInstanceId> OwnerByCell => ownerByCell;

        public bool TryRegister(BranchRoomInstanceId roomInstanceId, RoomInstanceFootprint footprint)
        {
            if (footprint == null)
            {
                return false;
            }

            foreach (var cell in footprint.OccupiedCells)
            {
                if (ownerByCell.ContainsKey(cell))
                {
                    return false;
                }
            }

            foreach (var cell in footprint.OccupiedCells)
            {
                ownerByCell[cell] = roomInstanceId;
            }

            return true;
        }

        public bool TryGetOwner(Vector2Int cell, out BranchRoomInstanceId roomInstanceId)
        {
            return ownerByCell.TryGetValue(cell, out roomInstanceId);
        }
    }
}
