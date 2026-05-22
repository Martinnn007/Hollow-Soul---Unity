using UnityEngine;

namespace Hollow.World
{
    public enum SpaceshipTerminalKind
    {
        Departures = 0,
        MissionChallenge = 1,
        TechnologyUpgrade = 2,
        SterilizationConsole = 3,
        QuarantineDoorButton = 4
    }

    public sealed class SpaceshipTerminal : MonoBehaviour
    {
        [SerializeField] private SpaceshipTerminalKind terminalKind;
        [SerializeField] private string payloadId;
        [SerializeField] private string displayName;

        public SpaceshipTerminalKind TerminalKind => terminalKind;

        public string PayloadId => payloadId ?? string.Empty;

        public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? PayloadId : displayName;

        public void Configure(SpaceshipTerminalKind nextKind, string nextPayloadId, string nextDisplayName)
        {
            terminalKind = nextKind;
            payloadId = nextPayloadId ?? string.Empty;
            displayName = nextDisplayName ?? string.Empty;
        }
    }
}
