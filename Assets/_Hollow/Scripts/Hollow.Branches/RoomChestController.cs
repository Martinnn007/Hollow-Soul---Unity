using Hollow.Rewards;
using UnityEngine;

namespace Hollow.Branches
{
    public sealed class RoomChestController : MonoBehaviour
    {
        public string RoomId { get; private set; } = string.Empty;

        public string ChestId { get; private set; } = string.Empty;

        public ChestKind Kind { get; private set; }

        public ChestState State { get; private set; }

        public bool IsOpened => State == ChestState.Opened;

        public void Configure(string roomId, string chestId, ChestKind kind, ChestState state)
        {
            RoomId = roomId ?? string.Empty;
            ChestId = chestId ?? string.Empty;
            Kind = kind;
            State = state;
            ApplyVisualState();
        }

        public bool Open()
        {
            if (State == ChestState.Opened)
            {
                return false;
            }

            State = ChestState.Opened;
            ApplyVisualState();
            return true;
        }

        private void ApplyVisualState()
        {
            var baseScale = Kind == ChestKind.Golden
                ? new Vector3(0.78f, 0.5f, 0.62f)
                : new Vector3(0.72f, 0.46f, 0.58f);
            transform.localScale = State == ChestState.Opened
                ? new Vector3(baseScale.x, baseScale.y * 0.72f, baseScale.z)
                : baseScale;
            gameObject.name = State == ChestState.Opened ? $"OpenedChest_{Kind}_{ChestId}" : $"Chest_{Kind}_{ChestId}";
        }
    }
}
