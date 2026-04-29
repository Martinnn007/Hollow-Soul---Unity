using Hollow.Data.Definitions;
using Hollow.Entities;
using Hollow.Rooms;
using UnityEngine;

namespace Hollow.Combat
{
    public abstract class RoomHazardController : MonoBehaviour
    {
        protected RoomRuntimeRoot Room { get; private set; }
        protected RoomCombatController Combat { get; private set; }
        protected PlaceholderPlayerController Player { get; private set; }
        protected RoomHazardTuningProfileDefinition Tuning { get; private set; }

        public RoomHazardMarker Marker { get; private set; }

        public virtual void Configure(
            RoomHazardMarker marker,
            RoomRuntimeRoot room,
            RoomCombatController combat,
            PlaceholderPlayerController player,
            RoomHazardTuningProfileDefinition tuning)
        {
            Marker = marker;
            Room = room;
            Combat = combat;
            Player = player;
            Tuning = RoomHazardTuningProfileDefinition.Resolve(tuning);
        }
    }
}
