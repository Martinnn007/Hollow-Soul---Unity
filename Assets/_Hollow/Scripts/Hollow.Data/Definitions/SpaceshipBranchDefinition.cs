using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Hollow.Data.Definitions
{
    [CreateAssetMenu(menuName = "Hollow/Spaceship/Branch Definition", fileName = "SpaceshipBranchDefinition")]
    public sealed class SpaceshipBranchDefinition : ScriptableObject
    {
        public const string BranchId = "spaceship_meta_branch";
        public const string ArrivalsRoomId = "ship_arrivals_quarantine";
        public const string MainHallRoomId = "ship_main_hall";
        public const string DeparturesRoomId = "ship_departures";
        public const string MissionCenterRoomId = "ship_mission_center";
        public const string TechnologyLabRoomId = "ship_technology_lab";

        [SerializeField] private List<TextAsset> roomTemplates = new();

        public IReadOnlyList<TextAsset> RoomTemplates => roomTemplates != null
            ? roomTemplates
            : Array.Empty<TextAsset>();

        public void Configure(IEnumerable<TextAsset> nextRoomTemplates)
        {
            roomTemplates = (nextRoomTemplates ?? Enumerable.Empty<TextAsset>())
                .Where(template => template != null)
                .Distinct()
                .ToList();
        }

        public static IReadOnlyList<string> RequiredRoomIds { get; } = new[]
        {
            ArrivalsRoomId,
            MainHallRoomId,
            DeparturesRoomId,
            MissionCenterRoomId,
            TechnologyLabRoomId
        };

        public static string LabelForRoom(string roomId)
        {
            return roomId switch
            {
                ArrivalsRoomId => "ARR",
                MainHallRoomId => "HALL",
                DeparturesRoomId => "DEP",
                MissionCenterRoomId => "MIS",
                TechnologyLabRoomId => "LAB",
                _ => string.Empty
            };
        }
    }
}
